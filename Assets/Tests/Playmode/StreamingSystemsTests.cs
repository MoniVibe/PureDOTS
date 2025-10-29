using System;
using System.Collections.Generic;
using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Streaming;
using PureDOTS.Systems.Streaming;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests
{
    public sealed class StreamingSystemsTests
    {
        private readonly struct CommandCounter
        {
            public CommandCounter(int loads, int unloads)
            {
                Loads = loads;
                Unloads = unloads;
            }

            public int Loads { get; }
            public int Unloads { get; }

            public CommandCounter WithLoad() => new CommandCounter(Loads + 1, Unloads);
            public CommandCounter WithUnload() => new CommandCounter(Loads, Unloads + 1);
        }

        private sealed class StreamingTestHarness : IDisposable
        {
            private readonly World _world;
            private readonly SystemHandle _scanner;
            private readonly SystemHandle _guard;
            private readonly SystemHandle _loader;
            private readonly SystemHandle _stateSync;
            private readonly SystemHandle _statsSystem;
            private readonly Dictionary<Entity, CommandCounter> _commandCounts = new();
            private readonly Dictionary<Entity, float3> _previousFocusPositions = new();
            private readonly List<StreamingSectionCommand> _lastCommands = new();

            private uint _currentTick;

            public StreamingTestHarness()
            {
                _world = new World("StreamingTestHarness");
                EntityManager = _world.EntityManager;

                TimeEntity = EntityManager.CreateEntity(typeof(TimeState));
                EntityManager.SetComponentData(TimeEntity, new TimeState
                {
                    FixedDeltaTime = 1f / 60f,
                    CurrentSpeedMultiplier = 1f,
                    Tick = 0,
                    IsPaused = false
                });

                Coordinator = EntityManager.CreateEntity(typeof(StreamingCoordinator));
                EntityManager.AddBuffer<StreamingSectionCommand>(Coordinator);
                EntityManager.AddComponentData(Coordinator, new StreamingStatistics
                {
                    FirstLoadTick = StreamingStatistics.TickUnset,
                    FirstUnloadTick = StreamingStatistics.TickUnset
                });
                EntityManager.AddComponentData(Coordinator, new StreamingDebugControl());

                var coordinator = new StreamingCoordinator
                {
                    MaxConcurrentLoads = 2,
                    MaxLoadsPerTick = 2,
                    MaxUnloadsPerTick = 2,
                    CooldownTicks = 120,
                    WorldSequenceNumber = (uint)_world.Unmanaged.SequenceNumber
                };
                EntityManager.SetComponentData(Coordinator, coordinator);
                EntityManager.AddComponentData(Coordinator, new StreamingTestDriver { InstantCompletion = true });

                _scanner = _world.GetOrCreateSystem<StreamingScannerSystem>();
                _guard = _world.GetOrCreateSystem<StreamingGuardrailSystem>();
                _loader = _world.GetOrCreateSystem<StreamingLoaderSystem>();
                _stateSync = _world.GetOrCreateSystem<StreamingStateSyncSystem>();
                _statsSystem = _world.GetOrCreateSystem<StreamingStatisticsSystem>();
            }

            public EntityManager EntityManager { get; }

            public Entity Coordinator { get; }

            public Entity TimeEntity { get; }

            public uint CurrentTick => _currentTick;

            public IReadOnlyList<StreamingSectionCommand> LastCommands => _lastCommands;

            public void Dispose()
            {
                if (_world.IsCreated)
                {
                    _world.Dispose();
                }
            }

            public Entity CreateSection(string name, float3 center, float enterRadius, float exitRadius, int priority = 0)
            {
                var entity = EntityManager.CreateEntity();
                FixedString64Bytes identifier = name;
                EntityManager.AddComponentData(entity, new StreamingSectionDescriptor
                {
                    Identifier = identifier,
                    SceneGuid = default,
                    Center = center,
                    EnterRadius = enterRadius,
                    ExitRadius = exitRadius,
                    Flags = StreamingSectionFlags.None,
                    Priority = priority,
                    EstimatedCost = 0f
                });

                EntityManager.AddComponentData(entity, new StreamingSectionState
                {
                    Status = StreamingSectionStatus.Unloaded,
                    LastSeenTick = 0,
                    CooldownUntilTick = 0,
                    PinCount = 0
                });

                EntityManager.AddComponentData(entity, new StreamingSectionRuntime
                {
                    SceneEntity = Entity.Null
                });

                _commandCounts[entity] = new CommandCounter(0, 0);
                return entity;
            }

            public Entity CreateFocus(float3 initialPosition)
            {
                var entity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(entity, new StreamingFocus
                {
                    Position = initialPosition,
                    Velocity = float3.zero,
                    RadiusScale = 1f,
                    LoadRadiusOffset = 0f,
                    UnloadRadiusOffset = 0f
                });
                _previousFocusPositions[entity] = initialPosition;
                return entity;
            }

            public void UpdateFocus(Entity focus, float3 position)
            {
                if (!_previousFocusPositions.TryGetValue(focus, out var previous))
                {
                    previous = position;
                }

                var focusData = EntityManager.GetComponentData<StreamingFocus>(focus);
                focusData.Velocity = position - previous;
                focusData.Position = position;
                EntityManager.SetComponentData(focus, focusData);
                _previousFocusPositions[focus] = position;
            }

            public void AdvanceTick()
            {
                var time = EntityManager.GetComponentData<TimeState>(TimeEntity);
                time.Tick += 1;
                EntityManager.SetComponentData(TimeEntity, time);
                _currentTick = time.Tick;

                _world.RunSystem(_scanner);
                _world.RunSystem(_guard);

                _lastCommands.Clear();
                var commands = EntityManager.GetBuffer<StreamingSectionCommand>(Coordinator);
                for (int i = 0; i < commands.Length; i++)
                {
                    var command = commands[i];
                    _lastCommands.Add(command);

                    if (!_commandCounts.TryGetValue(command.SectionEntity, out var counter))
                    {
                        counter = new CommandCounter(0, 0);
                    }

                    counter = command.Action == StreamingSectionAction.Load
                        ? counter.WithLoad()
                        : counter.WithUnload();
                    _commandCounts[command.SectionEntity] = counter;
                }

                _world.RunSystem(_loader);
                _world.RunSystem(_stateSync);
                _world.RunSystem(_statsSystem);
            }

            public CommandCounter GetCommandCounts(Entity section)
            {
                return _commandCounts.TryGetValue(section, out var counter) ? counter : new CommandCounter(0, 0);
            }

            public StreamingStatistics GetStatistics()
            {
                return EntityManager.GetComponentData<StreamingStatistics>(Coordinator);
            }

            public StreamingSectionState GetSectionState(Entity section)
            {
                return EntityManager.GetComponentData<StreamingSectionState>(section);
            }
        }

        [Test]
        public void StreamingSmokePath_ProcessesSingleLoadPerSection()
        {
            using var harness = new StreamingTestHarness();

            var sectionA = harness.CreateSection("SectionA", new float3(0f, 0f, 0f), 10f, 8f);
            var sectionB = harness.CreateSection("SectionB", new float3(20f, 0f, 0f), 10f, 8f);
            var sectionC = harness.CreateSection("SectionC", new float3(40f, 0f, 0f), 10f, 8f);
            var focus = harness.CreateFocus(new float3(-30f, 0f, 0f));

            var path = new[]
            {
                new float3(-30f, 0f, 0f),
                new float3(-5f, 0f, 0f),
                new float3(5f, 0f, 0f),
                new float3(25f, 0f, 0f),
                new float3(45f, 0f, 0f),
                new float3(60f, 0f, 0f)
            };

            foreach (var position in path)
            {
                harness.UpdateFocus(focus, position);
                harness.AdvanceTick();
            }

            foreach (var section in new[] { sectionA, sectionB, sectionC })
            {
                var counts = harness.GetCommandCounts(section);
                Assert.LessOrEqual(counts.Loads, 1, "Expected at most one load command per section.");
                Assert.LessOrEqual(counts.Unloads, 1, "Expected at most one unload command per section.");
            }

            var stats = harness.GetStatistics();
            Assert.AreEqual(stats.DesiredCount, stats.LoadedCount, "Desired and Loaded counts diverged at the end of the path.");
            Assert.AreEqual(0, stats.PendingCommands, "Command queue should be empty after processing.");
            Assert.AreNotEqual(StreamingStatistics.TickUnset, stats.FirstLoadTick, "First load tick telemetry was not recorded.");
            Assert.AreNotEqual(StreamingStatistics.TickUnset, stats.FirstUnloadTick, "First unload tick telemetry was not recorded.");
        }

        [Test]
        public void StreamingZigZag_HysteresisPreventsFlipFlop()
        {
            using var harness = new StreamingTestHarness();

            var section = harness.CreateSection("Boundary", float3.zero, 10f, 7f);
            var focus = harness.CreateFocus(float3.zero);

            var positions = new[]
            {
                new float3(0f, 0f, 0f),
                new float3(6f, 0f, 0f),
                new float3(12f, 0f, 0f),
                new float3(6f, 0f, 0f),
                new float3(12f, 0f, 0f),
                new float3(6f, 0f, 0f)
            };

            foreach (var position in positions)
            {
                harness.UpdateFocus(focus, position);
                harness.AdvanceTick();

                int commandsForSection = 0;
                foreach (var command in harness.LastCommands)
                {
                    if (command.SectionEntity != section)
                    {
                        continue;
                    }

                    commandsForSection++;
                    if (math.distance(position, float3.zero) < 7f)
                    {
                        Assert.AreNotEqual(StreamingSectionAction.Unload, command.Action,
                            "Unload should not trigger while within the exit radius.");
                    }
                }

                Assert.LessOrEqual(commandsForSection, 1, "Expected at most one command per tick for the section.");
            }

            var counts = harness.GetCommandCounts(section);
            Assert.AreEqual(3, counts.Loads, "Loads should only occur on entry.");
            Assert.AreEqual(2, counts.Unloads, "Unloads should only occur when crossing the exit radius.");

            var stats = harness.GetStatistics();
            Assert.GreaterOrEqual(stats.PeakPendingCommands, 1, "Peak queue depth should register issued commands.");
        }

        [Test]
        public void StreamingMultiFocusUnion_UnloadsOnlyWhenUnionIsEmpty()
        {
            using var harness = new StreamingTestHarness();

            var left = harness.CreateSection("Left", new float3(-10f, 0f, 0f), 8f, 6f);
            var center = harness.CreateSection("Center", float3.zero, 8f, 6f);
            var right = harness.CreateSection("Right", new float3(10f, 0f, 0f), 8f, 6f);

            var focusA = harness.CreateFocus(float3.zero);
            var focusB = harness.CreateFocus(float3.zero);

            var focusAPositions = new[]
            {
                new float3(0f, 0f, 0f),
                new float3(-10f, 0f, 0f),
                new float3(-10f, 0f, 0f),
                new float3(-10f, 0f, 0f),
                new float3(-10f, 0f, 0f),
                new float3(0f, 0f, 0f)
            };

            var focusBPositions = new[]
            {
                new float3(0f, 0f, 0f),
                new float3(0f, 0f, 0f),
                new float3(10f, 0f, 0f),
                new float3(10f, 0f, 0f),
                new float3(0f, 0f, 0f),
                new float3(0f, 0f, 0f)
            };

            var centerLoadTicks = new List<uint>();
            var centerUnloadTicks = new List<uint>();

            for (int i = 0; i < focusAPositions.Length; i++)
            {
                harness.UpdateFocus(focusA, focusAPositions[i]);
                harness.UpdateFocus(focusB, focusBPositions[i]);
                harness.AdvanceTick();

                foreach (var command in harness.LastCommands)
                {
                    if (command.SectionEntity == center)
                    {
                        if (command.Action == StreamingSectionAction.Load)
                        {
                            centerLoadTicks.Add(harness.CurrentTick);
                        }
                        else if (command.Action == StreamingSectionAction.Unload)
                        {
                            centerUnloadTicks.Add(harness.CurrentTick);
                        }
                    }
                }
            }

            Assert.AreEqual(1, centerUnloadTicks.Count, "Center section should unload exactly once when both focuses leave.");
            Assert.AreEqual(2, centerLoadTicks.Count, "Center section should load on start and when focuses reconverge.");
            Assert.Greater(centerUnloadTicks[0], centerLoadTicks[0], "Unload must occur after the initial load.");

            var centerState = harness.GetSectionState(center);
            Assert.AreEqual(StreamingSectionStatus.Loaded, centerState.Status, "Center section should end loaded once focuses reconverge.");

            var stats = harness.GetStatistics();
            Assert.AreEqual(stats.DesiredCount, stats.LoadedCount, "Union semantics should keep Desired and Loaded in sync.");
        }

        [Test]
        public void StreamingGuard_BlocksLoadDuringCooldown()
        {
            using var harness = new StreamingTestHarness();

            var section = harness.CreateSection("Cooldown", float3.zero, 10f, 8f);
            var focus = harness.CreateFocus(float3.zero);

            // Initial tick to enqueue and process a load.
            harness.AdvanceTick();
            var state = harness.GetSectionState(section);
            Assert.AreEqual(StreamingSectionStatus.Loaded, state.Status);

            // Force a cooldown and error status, then request another tick.
            state.Status = StreamingSectionStatus.Error;
            state.CooldownUntilTick = harness.CurrentTick + 5;
            harness.EntityManager.SetComponentData(section, state);

            harness.UpdateFocus(focus, float3.zero);
            harness.AdvanceTick();

            Assert.AreEqual(0, harness.LastCommands.Count, "Guard should drop load commands during cooldown.");

            state = harness.GetSectionState(section);
            Assert.AreEqual(StreamingSectionStatus.Unloaded, state.Status, "Section should remain unloaded while cooldown is active.");

            var stats = harness.GetStatistics();
            Assert.Greater(stats.ActiveCooldowns, 0, "Active cooldowns count should track dropped loads.");
        }

        [Test]
        public void StreamingGuard_ClearsCooldownsOnDebugRequest()
        {
            using var harness = new StreamingTestHarness();

            var section = harness.CreateSection("CooldownDebug", float3.zero, 10f, 8f);
            var focus = harness.CreateFocus(float3.zero);

            // Prime with an initial load.
            harness.AdvanceTick();

            var state = harness.GetSectionState(section);
            state.Status = StreamingSectionStatus.Error;
            state.CooldownUntilTick = harness.CurrentTick + 10;
            harness.EntityManager.SetComponentData(section, state);

            harness.UpdateFocus(focus, float3.zero);
            harness.AdvanceTick();
            Assert.AreEqual(0, harness.LastCommands.Count, "Cooldown should still suppress load command.");

            // Issue clear request via debug control.
            var control = harness.EntityManager.GetComponentData<StreamingDebugControl>(harness.Coordinator);
            control.ClearCooldowns = true;
            harness.EntityManager.SetComponentData(harness.Coordinator, control);

            harness.AdvanceTick();

            Assert.IsTrue(harness.LastCommands.Count > 0, "Clearing cooldowns should allow load commands to proceed.");
            Assert.AreEqual(StreamingSectionAction.Load, harness.LastCommands[0].Action, "Load command should be reissued after clearing cooldowns.");

            state = harness.GetSectionState(section);
            Assert.AreEqual(StreamingSectionStatus.Loaded, state.Status, "Section should return to loaded state after cooldown clear.");

            var stats = harness.GetStatistics();
            Assert.AreEqual(0, stats.ActiveCooldowns, "Cooldown count should reset after clear request.");
        }
    }
}
