using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PureDOTS.Runtime.AI;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Telemetry;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace PureDOTS.Runtime.Scenarios
{
    /// <summary>
    /// Executes scenarios headlessly by spinning a DOTS world with the headless profile
    /// and driving ticks deterministically. Keeps allocations minimal to stay friendly to CI.
    /// </summary>
    public static class ScenarioRunnerExecutor
    {
        private const float FixedDeltaTime = 1f / 60f;
        private static readonly ProfilerMarker RunMarker = new("ScenarioRunner.Execute");
        private static readonly WorldSystemFilterFlags HeadlessFilterFlags = WorldSystemFilterFlags.Default
                                                                               | WorldSystemFilterFlags.Editor
                                                                               | WorldSystemFilterFlags.Streaming
                                                                               | WorldSystemFilterFlags.ProcessAfterLoad
                                                                               | WorldSystemFilterFlags.EntitySceneOptimizations;

        private static readonly string[] RootGroupTypeNames =
        {
            "PureDOTS.Systems.TimeSystemGroup, PureDOTS.Systems",
            "PureDOTS.Systems.EnvironmentSystemGroup, PureDOTS.Systems",
            "PureDOTS.Systems.SpatialSystemGroup, PureDOTS.Systems",
            "PureDOTS.Systems.GameplaySystemGroup, PureDOTS.Systems",
            "PureDOTS.Systems.HistorySystemGroup, PureDOTS.Systems"
        };

        public static ScenarioRunResult RunFromFile(string scenarioPath, string reportPath = null)
        {
            if (string.IsNullOrWhiteSpace(scenarioPath) || !File.Exists(scenarioPath))
            {
                throw new FileNotFoundException($"Scenario file not found: {scenarioPath}", scenarioPath);
            }

            var json = File.ReadAllText(scenarioPath);
            if (!ScenarioRunner.TryParse(json, out var data, out var parseError))
            {
                throw new InvalidOperationException($"Scenario parse failed: {parseError}");
            }

            return Run(data, scenarioPath, reportPath);
        }

        public static ScenarioRunResult Run(ScenarioDefinitionData data, string sourceLabel = "inline", string reportPath = null)
        {
            if (!ScenarioRunner.TryBuild(data, Allocator.Temp, out var scenario, out var buildError))
            {
                throw new InvalidOperationException($"Scenario build failed: {buildError}");
            }

            using (scenario)
            {
                var result = ExecuteScenario(in scenario);
                WriteReport(reportPath, result);
                Debug.Log($"ScenarioRunner: completed {scenario.ScenarioId} ({sourceLabel}) ticks={scenario.RunTicks} commands={scenario.InputCommands.Length} snapshots={result.SnapshotLogCount} frameBudgetExceeded={result.FrameTimingBudgetExceeded}");
                return result;
            }
        }

        private static ScenarioRunResult ExecuteScenario(in ResolvedScenario scenario)
        {
            using var world = CreateWorld("ScenarioWorld");
            var result = new ScenarioRunResult
            {
                ScenarioId = scenario.ScenarioId.ToString(),
                RunTicks = scenario.RunTicks,
                Seed = scenario.Seed,
                EntityCountEntries = scenario.EntityCounts.Length,
                Metrics = new List<ScenarioMetric>(8)
            };

            DefaultWorldInitializationInitializationHook(world);
            InjectScenarioMetadata(world.EntityManager, in scenario);

            var initGroup = world.GetOrCreateSystemManaged<InitializationSystemGroup>();
            var timeGroup = TryGetGroup(world, RootGroupTypeNames[0]);
            var simulationGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
            var fixedStepGroup = world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();

            // Warm-up initialization once to seed singletons (CoreSingletonBootstrapSystem runs here).
            world.Unmanaged.Time = new TimeData(FixedDeltaTime, 0);
            initGroup.Update();

            using (var commandQueue = BuildCommandLookup(in scenario))
            {
                var rewindEntity = ResolveRewindEntity(world.EntityManager);

                using (RunMarker.Auto())
                {
                    for (int i = 0; i < scenario.RunTicks; i++)
                    {
                        var elapsed = (i + 1) * FixedDeltaTime;
                        world.Unmanaged.Time = new TimeData(FixedDeltaTime, elapsed);

                        FlushCommandsForTick(world.EntityManager, rewindEntity, commandQueue, i);

                        timeGroup?.Update();
                        fixedStepGroup?.Update();
                        simulationGroup?.Update();
                    }
                }

                PopulateTelemetry(world.EntityManager, rewindEntity, ref result);
            }

            return result;
        }

        private static World CreateWorld(string name)
        {
            var world = new World(name, WorldFlags.Game);
            World.DefaultGameObjectInjectionWorld = world;

            var systems = ResolveSystems();
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);

            // Ensure ECBs exist and groups sorted similarly to the bootstrap.
            world.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            world.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

            if (world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>() is { } fixedStepGroup)
            {
                fixedStepGroup.Timestep = FixedDeltaTime;
                fixedStepGroup.SortSystems();
            }

            world.GetOrCreateSystemManaged<SimulationSystemGroup>()?.SortSystems();
            world.GetOrCreateSystemManaged<InitializationSystemGroup>()?.SortSystems();

            return world;
        }

        private static void DefaultWorldInitializationInitializationHook(World world)
        {
            // Mirror bootstrap: ensure root groups are materialized for downstream lookups.
            foreach (var typeName in RootGroupTypeNames)
            {
                var type = Type.GetType(typeName);
                if (type != null && typeof(ComponentSystemGroup).IsAssignableFrom(type))
                {
                    world.GetOrCreateSystemManaged(type);
                }
            }
        }

        private static NativeParallelMultiHashMap<int, ScenarioInputCommand> BuildCommandLookup(in ResolvedScenario scenario)
        {
            var map = new NativeParallelMultiHashMap<int, ScenarioInputCommand>(scenario.InputCommands.Length, Allocator.Temp);
            for (int i = 0; i < scenario.InputCommands.Length; i++)
            {
                var command = scenario.InputCommands[i];
                map.Add(command.Tick, command);
            }
            return map;
        }

        private static Entity ResolveRewindEntity(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>());
            if (query.IsEmpty)
            {
                throw new InvalidOperationException("Scenario runner expected RewindState singleton to exist after initialization.");
            }
            return query.GetSingletonEntity();
        }

        private static void FlushCommandsForTick(EntityManager entityManager, Entity rewindEntity, NativeParallelMultiHashMap<int, ScenarioInputCommand> commands, int currentTick)
        {
            if (!commands.TryGetFirstValue(currentTick, out var command, out var iterator))
            {
                return;
            }

            if (!entityManager.HasBuffer<TimeControlCommand>(rewindEntity))
            {
                entityManager.AddBuffer<TimeControlCommand>(rewindEntity);
            }

            var buffer = entityManager.GetBuffer<TimeControlCommand>(rewindEntity);
            do
            {
                if (TryTranslateCommand(command, out var translated))
                {
                    buffer.Add(translated);
                }
            } while (commands.TryGetNextValue(out command, ref iterator));
        }

        private static bool TryTranslateCommand(in ScenarioInputCommand command, out TimeControlCommand timeCommand)
        {
            timeCommand = default;
            var id = command.CommandId.ToString().ToLowerInvariant();

            switch (id)
            {
                case "time.pause":
                    timeCommand = new TimeControlCommand { Type = TimeControlCommandType.Pause };
                    return true;
                case "time.play":
                case "time.resume":
                    timeCommand = new TimeControlCommand { Type = TimeControlCommandType.Resume };
                    return true;
                case "time.step":
                    timeCommand = new TimeControlCommand { Type = TimeControlCommandType.StepTicks, UintParam = ParseUInt(command.Payload, 1) };
                    return true;
                case "time.setspeed":
                    timeCommand = new TimeControlCommand { Type = TimeControlCommandType.SetSpeed, FloatParam = ParseFloat(command.Payload, 1f) };
                    return true;
                case "time.rewind":
                    timeCommand = new TimeControlCommand { Type = TimeControlCommandType.StartRewind, UintParam = ParseUInt(command.Payload, 0) };
                    return true;
                case "time.stoprewind":
                    timeCommand = new TimeControlCommand { Type = TimeControlCommandType.StopRewind };
                    return true;
                case "time.scrub":
                    timeCommand = new TimeControlCommand { Type = TimeControlCommandType.ScrubTo, UintParam = ParseUInt(command.Payload, 0) };
                    return true;
                default:
                    Debug.LogWarning($"ScenarioRunner: unknown command id {id}");
                    return false;
            }
        }

        private static uint ParseUInt(in FixedString64Bytes payload, uint fallback)
        {
            if (uint.TryParse(payload.ToString(), out var value))
            {
                return value;
            }
            return fallback;
        }

        private static float ParseFloat(in FixedString64Bytes payload, float fallback)
        {
            if (float.TryParse(payload.ToString(), out var value))
            {
                return value;
            }
            return fallback;
        }

        private static void PopulateTelemetry(EntityManager entityManager, Entity rewindEntity, ref ScenarioRunResult result)
        {
            if (entityManager.HasComponent<TickTimeState>(rewindEntity))
            {
                result.FinalTick = entityManager.GetComponentData<TickTimeState>(rewindEntity).Tick;
            }

            if (entityManager.HasComponent<TelemetryStream>(rewindEntity))
            {
                result.TelemetryVersion = entityManager.GetComponentData<TelemetryStream>(rewindEntity).Version;
            }

            if (entityManager.HasComponent<DebugDisplayData>(rewindEntity))
            {
                var debug = entityManager.GetComponentData<DebugDisplayData>(rewindEntity);
                result.CommandLogCount = debug.CommandLogCount;
                result.SnapshotLogCount = debug.SnapshotLogCount;
                result.FrameTimingBudgetExceeded = debug.FrameTimingBudgetExceeded;
                result.FrameTimingWorstMs = debug.FrameTimingWorstDurationMs;
                result.FrameTimingWorstGroup = debug.FrameTimingWorstGroup.ToString();
                result.RegistryContinuityFailures = debug.RegistryContinuityFailureCount;
                result.RegistryContinuityWarnings = debug.RegistryContinuityWarningCount;
            }

            if (entityManager.HasComponent<InputCommandLogState>(rewindEntity) && entityManager.HasComponent<TickSnapshotLogState>(rewindEntity))
            {
                var commandState = entityManager.GetComponentData<InputCommandLogState>(rewindEntity);
                var snapshotState = entityManager.GetComponentData<TickSnapshotLogState>(rewindEntity);
                result.CommandCapacity = commandState.Capacity;
                result.SnapshotCapacity = snapshotState.Capacity;
                result.CommandBytes = commandState.Capacity * UnsafeUtility.SizeOf<InputCommandLogEntry>();
                result.SnapshotBytes = snapshotState.Capacity * UnsafeUtility.SizeOf<TickSnapshotLogEntry>();
                result.TotalLogBytes = result.CommandBytes + result.SnapshotBytes;
            }

            TryCollectSpace4XMiningTelemetry(entityManager, ref result);
        }

        private static readonly string Space4XTelemetryTypeName = "Space4X.Registry.Space4XMiningTelemetry";

        private static Type s_space4xTelemetryType;
        private static FieldInfo s_space4xOreInHoldField;
        private static FieldInfo s_space4xOreDepositedField;
        private static FieldInfo s_space4xStorehouseInventoryField;
        private static MethodInfo s_getComponentDataMethod;

        private static void TryCollectSpace4XMiningTelemetry(EntityManager entityManager, ref ScenarioRunResult result)
        {
            EnsureSpace4XReflectionHandles();
            if (s_space4xTelemetryType == null || s_getComponentDataMethod == null)
            {
                Debug.LogWarning("[ScenarioRunner] Space4XMiningTelemetry type or accessor missing; skipping mining metrics.");
                return;
            }

            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly(s_space4xTelemetryType));
            if (query.IsEmptyIgnoreFilter)
            {
                Debug.LogWarning("[ScenarioRunner] Space4XMiningTelemetry query returned no entities.");
                return;
            }

            var telemetryEntity = query.GetSingletonEntity();
            var telemetry = s_getComponentDataMethod
                .MakeGenericMethod(s_space4xTelemetryType)
                .Invoke(entityManager, new object[] { telemetryEntity });

            if (telemetry == null)
            {
                Debug.LogWarning("[ScenarioRunner] Space4XMiningTelemetry reflection returned null.");
                return;
            }

            var appended = false;
            if (s_space4xOreInHoldField != null)
            {
                var value = Convert.ToDouble(s_space4xOreInHoldField.GetValue(telemetry));
                result.AddMetric("space4x.mining.oreInHold", value);
                appended = true;
            }

            if (s_space4xOreDepositedField != null)
            {
                var value = Convert.ToDouble(s_space4xOreDepositedField.GetValue(telemetry));
                result.AddMetric("space4x.mining.oreDeposited", value);
                appended = true;
            }

            if (s_space4xStorehouseInventoryField != null)
            {
                var value = Convert.ToDouble(s_space4xStorehouseInventoryField.GetValue(telemetry));
                result.AddMetric("space4x.mining.storehouseInventory", value);
                appended = true;
            }

            if (!appended)
            {
                Debug.LogWarning("[ScenarioRunner] Space4XMiningTelemetry fields were missing; no mining metrics recorded.");
            }
            else
            {
                Debug.Log("[ScenarioRunner] Space4X mining metrics appended to scenario report.");
            }
        }

        private static void EnsureSpace4XReflectionHandles()
        {
            if (s_getComponentDataMethod == null)
            {
                s_getComponentDataMethod = typeof(EntityManager)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == nameof(EntityManager.GetComponentData)
                                         && m.IsGenericMethodDefinition
                                         && m.GetGenericArguments().Length == 1
                                         && m.GetParameters().Length == 1
                                         && m.GetParameters()[0].ParameterType == typeof(Entity));
            }

            if (s_space4xTelemetryType != null)
            {
                return;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var resolved = assembly.GetType(Space4XTelemetryTypeName);
                if (resolved != null)
                {
                    s_space4xTelemetryType = resolved;
                    s_space4xOreInHoldField = resolved.GetField("OreInHold", BindingFlags.Public | BindingFlags.Instance);
                    s_space4xOreDepositedField = resolved.GetField("OreDeposited", BindingFlags.Public | BindingFlags.Instance);
                    s_space4xStorehouseInventoryField = resolved.GetField("StorehouseInventory", BindingFlags.Public | BindingFlags.Instance);
                    break;
                }
            }
        }

        private static void InjectScenarioMetadata(EntityManager entityManager, in ResolvedScenario scenario)
        {
            var entity = entityManager.CreateEntity(typeof(ScenarioInfo), typeof(ScenarioEntityCountElement));
            entityManager.SetComponentData(entity, new ScenarioInfo
            {
                ScenarioId = scenario.ScenarioId,
                Seed = scenario.Seed,
                RunTicks = scenario.RunTicks
            });

            var buffer = entityManager.GetBuffer<ScenarioEntityCountElement>(entity);
            for (int i = 0; i < scenario.EntityCounts.Length; i++)
            {
                buffer.Add(new ScenarioEntityCountElement
                {
                    RegistryId = scenario.EntityCounts[i].RegistryId,
                    Count = scenario.EntityCounts[i].Count
                });
            }

            if (scenario.HasBehaviorOverride)
            {
                var overrideEntity = entityManager.CreateEntity(typeof(BehaviorScenarioOverrideComponent));
                entityManager.SetComponentData(overrideEntity, new BehaviorScenarioOverrideComponent
                {
                    Value = scenario.BehaviorOverride
                });
            }
        }

        private static void WriteReport(string reportPath, in ScenarioRunResult result)
        {
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                return;
            }

            var extension = Path.GetExtension(reportPath).ToLowerInvariant();
            if (extension == ".csv")
            {
                ScenarioRunResultCsv.Write(reportPath, result);
            }
            else
            {
                var serialized = ScenarioRunResultJson.Serialize(result);
                File.WriteAllText(reportPath, serialized);
            }
        }

        private static IReadOnlyList<Type> ResolveSystems()
        {
            if (TryResolveSystemsFromRegistry(out var systemsFromRegistry))
            {
                return systemsFromRegistry;
            }

            return DefaultWorldInitialization
                .GetAllSystems(HeadlessFilterFlags)
                .Where(t => t != typeof(Unity.Entities.PresentationSystemGroup))
                .ToArray();
        }

        private static bool TryResolveSystemsFromRegistry(out IReadOnlyList<Type> systems)
        {
            systems = null;

            var registryType = Type.GetType("PureDOTS.Systems.SystemRegistry, PureDOTS.Systems");
            if (registryType == null)
            {
                return false;
            }

            var builtinProfiles = registryType.GetNestedType("BuiltinProfiles", BindingFlags.Public | BindingFlags.Static);
            var headlessProperty = builtinProfiles?.GetProperty("Headless", BindingFlags.Public | BindingFlags.Static);
            var getSystems = registryType.GetMethod("GetSystems", BindingFlags.Public | BindingFlags.Static);

            var profile = headlessProperty?.GetValue(null);
            if (profile == null || getSystems == null)
            {
                return false;
            }

            if (getSystems.Invoke(null, new[] { profile }) is IReadOnlyList<Type> resolved)
            {
                systems = resolved;
                return true;
            }

            return false;
        }

        private static ComponentSystemGroup TryGetGroup(World world, string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null || !typeof(ComponentSystemGroup).IsAssignableFrom(type))
            {
                return null;
            }

            return world.GetOrCreateSystemManaged(type) as ComponentSystemGroup;
        }
    }

    public struct ScenarioRunResult
    {
        public string ScenarioId;
        public uint Seed;
        public int RunTicks;
        public uint FinalTick;
        public uint TelemetryVersion;
        public int CommandLogCount;
        public int SnapshotLogCount;
        public bool FrameTimingBudgetExceeded;
        public float FrameTimingWorstMs;
        public string FrameTimingWorstGroup;
        public int RegistryContinuityWarnings;
        public int RegistryContinuityFailures;
        public int EntityCountEntries;
        public int CommandCapacity;
        public int SnapshotCapacity;
        public int CommandBytes;
        public int SnapshotBytes;
        public int TotalLogBytes;
        public List<ScenarioMetric> Metrics;

        public void AddMetric(string key, double value)
        {
            if (Metrics == null)
            {
                Metrics = new List<ScenarioMetric>();
            }

            Metrics.Add(new ScenarioMetric
            {
                Key = string.IsNullOrEmpty(key) ? string.Empty : key,
                Value = value
            });
        }

        public override string ToString()
        {
            return $"scenarioId={ScenarioId}, seed={Seed}, runTicks={RunTicks}, finalTick={FinalTick}, commands={CommandLogCount}, snapshots={SnapshotLogCount}, frameBudgetExceeded={FrameTimingBudgetExceeded}";
        }
    }

    public struct ScenarioMetric
    {
        public string Key;
        public double Value;
    }
}
