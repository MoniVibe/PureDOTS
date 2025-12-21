using System;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Runtime.Time;
using PureDOTS.Systems.Telemetry;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using SystemEnv = System.Environment;
using UnityDebug = UnityEngine.Debug;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Headless proof that global rewind enters playback and returns to record, with optional guard checks.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(TelemetryExportSystem))]
    public partial struct HeadlessRewindProofSystem : ISystem
    {
        private const string EnabledEnv = "PUREDOTS_HEADLESS_REWIND_PROOF";
        private const string ExitOnResultEnv = "PUREDOTS_HEADLESS_REWIND_PROOF_EXIT";
        private const string TriggerTickEnv = "PUREDOTS_HEADLESS_REWIND_PROOF_TRIGGER_TICK";
        private const string TicksBackEnv = "PUREDOTS_HEADLESS_REWIND_PROOF_BACK_TICKS";
        private const string TimeoutTicksEnv = "PUREDOTS_HEADLESS_REWIND_PROOF_TIMEOUT_TICKS";
        private const string RequireGuardEnv = "PUREDOTS_HEADLESS_REWIND_PROOF_REQUIRE_GUARD";

        private const uint DefaultTriggerTick = 120;
        private const uint DefaultTicksBack = 60;
        private const uint DefaultTimeoutTicks = 1800;
        private const uint NoSubjectGraceTicks = 10;

        private static readonly FixedString32Bytes ExpectedDepth = new FixedString32Bytes(">0");
        private static readonly FixedString32Bytes StepCore = new FixedString32Bytes("core");

        private byte _enabled;
        private Entity _proofEntity;
        private HeadlessRewindProofConfig _config;

        public void OnCreate(ref SystemState state)
        {
            if (!ResolveEnabled())
            {
                state.Enabled = false;
                return;
            }

            _enabled = 1;
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            _config = BuildConfig();
            _proofEntity = EnsureProofEntity(ref state, _config);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_enabled == 0)
            {
                return;
            }

            if (_proofEntity == Entity.Null || !state.EntityManager.Exists(_proofEntity))
            {
                _proofEntity = EnsureProofEntity(ref state, _config);
                if (_proofEntity == Entity.Null)
                {
                    return;
                }
            }

            var config = state.EntityManager.GetComponentData<HeadlessRewindProofConfig>(_proofEntity);
            if (config.Enabled == 0)
            {
                return;
            }

            var proof = state.EntityManager.GetComponentData<HeadlessRewindProofState>(_proofEntity);
            if (proof.Phase == HeadlessRewindProofPhase.Completed || proof.Result != 0)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            var rewindEntity = SystemAPI.GetSingletonEntity<RewindState>();
            var tick = timeState.Tick;

            if (proof.Phase == HeadlessRewindProofPhase.Idle)
            {
                if (tick < config.TriggerTick || rewindState.Mode != RewindMode.Play)
                {
                    return;
                }

                var targetTick = tick > config.TicksBack ? tick - config.TicksBack : 0u;
                if (!TryQueueRewindCommand(ref state, rewindEntity, targetTick))
                {
                    return;
                }

                proof.Phase = HeadlessRewindProofPhase.Requested;
                proof.StartTick = tick;
                proof.TargetTick = targetTick;
                proof.DeadlineTick = config.TimeoutTicks > 0 ? tick + config.TimeoutTicks : 0u;
                state.EntityManager.SetComponentData(_proofEntity, proof);
                return;
            }

            var dirty = false;
            if (proof.Phase == HeadlessRewindProofPhase.Requested && rewindState.Mode == RewindMode.Rewind)
            {
                proof.Phase = HeadlessRewindProofPhase.Playback;
                proof.SawPlayback = 1;
                proof.PlaybackEnterTick = tick;
                dirty = true;
            }
            else if (proof.Phase == HeadlessRewindProofPhase.Playback)
            {
                if (rewindState.Mode == RewindMode.Step)
                {
                    proof.Phase = HeadlessRewindProofPhase.CatchUp;
                    proof.SawCatchUp = 1;
                    proof.CatchUpEnterTick = tick;
                    dirty = true;
                }
                else if (rewindState.Mode == RewindMode.Play)
                {
                    proof.Phase = HeadlessRewindProofPhase.Record;
                    proof.SawRecord = 1;
                    proof.RecordReturnTick = tick;
                    dirty = true;
                }
            }
            else if (proof.Phase == HeadlessRewindProofPhase.CatchUp && rewindState.Mode == RewindMode.Play)
            {
                proof.Phase = HeadlessRewindProofPhase.Record;
                proof.SawRecord = 1;
                proof.RecordReturnTick = tick;
                dirty = true;
            }

            if (proof.Phase == HeadlessRewindProofPhase.Record)
            {
                if (tick <= proof.RecordReturnTick)
                {
                    if (dirty)
                    {
                        state.EntityManager.SetComponentData(_proofEntity, proof);
                    }
                    return;
                }

                var subjectsSatisfied = AreSubjectsSatisfied(ref state, _proofEntity, out var hasSubjects);
                if (hasSubjects && subjectsSatisfied)
                {
                    var guardViolations = ResolveGuardViolations(ref state, config);
                    FinalizeProof(ref state, _proofEntity, config, ref proof, tick, guardViolations);
                    return;
                }

                if (!hasSubjects && tick > proof.RecordReturnTick + NoSubjectGraceTicks)
                {
                    var guardViolations = ResolveGuardViolations(ref state, config);
                    FinalizeProof(ref state, _proofEntity, config, ref proof, tick, guardViolations);
                    return;
                }

                if (proof.DeadlineTick > 0 && tick >= proof.DeadlineTick)
                {
                    var guardViolations = ResolveGuardViolations(ref state, config);
                    FinalizeProof(ref state, _proofEntity, config, ref proof, tick, guardViolations, timedOut: true);
                    return;
                }

                if (dirty)
                {
                    state.EntityManager.SetComponentData(_proofEntity, proof);
                }
                return;
            }

            if (proof.DeadlineTick > 0 && tick >= proof.DeadlineTick)
            {
                var guardViolations = ResolveGuardViolations(ref state, config);
                FinalizeProof(ref state, _proofEntity, config, ref proof, tick, guardViolations, timedOut: true);
                return;
            }

            if (dirty)
            {
                state.EntityManager.SetComponentData(_proofEntity, proof);
            }
        }

        private static void FinalizeProof(ref SystemState state, Entity proofEntity, in HeadlessRewindProofConfig config, ref HeadlessRewindProofState proof, uint tick, int guardViolations, bool timedOut = false)
        {
            proof.GuardViolationCount = guardViolations;
            var depth = proof.StartTick > proof.TargetTick ? proof.StartTick - proof.TargetTick : 0u;
            var pass = !timedOut &&
                       proof.SawPlayback != 0 &&
                       proof.SawRecord != 0 &&
                       depth > 0 &&
                       (config.RequireGuardViolationsClear == 0 || guardViolations == 0);

            if (TryEvaluateSubjects(ref state, proofEntity, config, tick, ref pass))
            {
                // pass may be updated by subject failures.
            }

            var subjectCount = 0;
            if (state.EntityManager.HasBuffer<HeadlessRewindProofSubject>(proofEntity))
            {
                var buffer = state.EntityManager.GetBuffer<HeadlessRewindProofSubject>(proofEntity);
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (!buffer[i].ProofId.IsEmpty)
                    {
                        subjectCount++;
                    }
                }
            }

            if (subjectCount == 0)
            {
                pass = false;
            }

            if (pass)
            {
                UnityDebug.Log($"[HeadlessRewindProof] PASS tick={tick} start={proof.StartTick} target={proof.TargetTick} depth={depth} subjects={subjectCount} guardViolations={guardViolations}");
            }
            else
            {
                UnityDebug.LogError($"[HeadlessRewindProof] FAIL tick={tick} start={proof.StartTick} target={proof.TargetTick} depth={depth} playback={proof.SawPlayback} record={proof.SawRecord} subjects={subjectCount} guardViolations={guardViolations} timeout={timedOut}");
            }

            TelemetryLoopProofUtility.Emit(state.EntityManager, tick, TelemetryLoopIds.Rewind, pass, depth, ExpectedDepth, config.TimeoutTicks, step: StepCore);

            proof.Result = (byte)(pass ? 1 : 2);
            proof.Phase = HeadlessRewindProofPhase.Completed;
            state.EntityManager.SetComponentData(proofEntity, proof);

            ExitIfRequested(ref state, tick, pass ? 0 : 2);
        }

        private static int ResolveGuardViolations(ref SystemState state, in HeadlessRewindProofConfig config)
        {
            if (config.RequireGuardViolationsClear == 0)
            {
                return 0;
            }

            using var query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>());
            if (query.IsEmptyIgnoreFilter)
            {
                return 0;
            }

            return query.GetSingleton<DebugDisplayData>().RewindGuardViolationCount;
        }

        private static bool AreSubjectsSatisfied(ref SystemState state, Entity proofEntity, out bool hasSubjects)
        {
            var entityManager = state.EntityManager;
            if (!entityManager.HasBuffer<HeadlessRewindProofSubject>(proofEntity))
            {
                hasSubjects = false;
                return true;
            }

            var buffer = entityManager.GetBuffer<HeadlessRewindProofSubject>(proofEntity);
            var foundSubject = false;
            for (int i = 0; i < buffer.Length; i++)
            {
                var entry = buffer[i];
                if (entry.ProofId.IsEmpty)
                {
                    continue;
                }

                foundSubject = true;
                if (entry.Result == 1)
                {
                    continue;
                }

                if (entry.Result == 2)
                {
                    hasSubjects = true;
                    return false;
                }

                var requiredSatisfied = entry.RequiredMask == 0 ||
                                        (entry.ObservedMask & entry.RequiredMask) == entry.RequiredMask;
                if (!requiredSatisfied)
                {
                    hasSubjects = true;
                    return false;
                }
            }

            hasSubjects = foundSubject;
            return true;
        }

        private static bool TryEvaluateSubjects(ref SystemState state, Entity proofEntity, in HeadlessRewindProofConfig config, uint tick, ref bool pass)
        {
            var entityManager = state.EntityManager;
            if (!entityManager.HasBuffer<HeadlessRewindProofSubject>(proofEntity))
            {
                return false;
            }

            var buffer = entityManager.GetBuffer<HeadlessRewindProofSubject>(proofEntity);
            for (int i = 0; i < buffer.Length; i++)
            {
                var entry = buffer[i];
                if (entry.ProofId.IsEmpty)
                {
                    continue;
                }

                var requiredSatisfied = entry.RequiredMask == 0 || (entry.ObservedMask & entry.RequiredMask) == entry.RequiredMask;
                var success = entry.Result != 2 && requiredSatisfied;
                if (entry.Result == 1)
                {
                    success = true;
                }
                else if (!requiredSatisfied)
                {
                    entry.Result = 2;
                }

                var observed = entry.Observed;
                if (observed <= 0f && entry.ObservedMask != 0)
                {
                    observed = entry.ObservedMask;
                }

                var expected = entry.Expected.Length > 0
                    ? entry.Expected
                    : (requiredSatisfied ? new FixedString32Bytes("ok") : new FixedString32Bytes("mask"));

                var step = TruncateStep(entry.ProofId);
                TelemetryLoopProofUtility.Emit(entityManager, tick, TelemetryLoopIds.Rewind, success, observed, expected, config.TimeoutTicks, step: step);

                if (!success)
                {
                    pass = false;
                }

                buffer[i] = entry;
            }

            return true;
        }

        private static FixedString32Bytes TruncateStep(in FixedString64Bytes value)
        {
            var step = new FixedString32Bytes();
            for (int i = 0; i < value.Length && step.Length < step.Capacity; i++)
            {
                step.Append((char)value[i]);
            }
            return step;
        }

        private static bool TryQueueRewindCommand(ref SystemState state, Entity rewindEntity, uint targetTick)
        {
            if (!state.EntityManager.HasBuffer<TimeControlCommand>(rewindEntity))
            {
                state.EntityManager.AddBuffer<TimeControlCommand>(rewindEntity);
            }

            var buffer = state.EntityManager.GetBuffer<TimeControlCommand>(rewindEntity);
            buffer.Add(new TimeControlCommand
            {
                Type = TimeControlCommandType.StartRewind,
                UintParam = targetTick
            });

            return true;
        }

        private static Entity EnsureProofEntity(ref SystemState state, in HeadlessRewindProofConfig config)
        {
            var entityManager = state.EntityManager;
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<HeadlessRewindProofState>());
            if (!query.IsEmptyIgnoreFilter)
            {
                var existing = query.GetSingletonEntity();
                if (!entityManager.HasComponent<HeadlessRewindProofConfig>(existing))
                {
                    entityManager.AddComponentData(existing, config);
                }
                else
                {
                    entityManager.SetComponentData(existing, config);
                }

                if (!entityManager.HasBuffer<HeadlessRewindProofSubject>(existing))
                {
                    entityManager.AddBuffer<HeadlessRewindProofSubject>(existing);
                }

                return existing;
            }

            var entity = entityManager.CreateEntity(typeof(HeadlessRewindProofState), typeof(HeadlessRewindProofConfig));
            entityManager.SetComponentData(entity, config);
            entityManager.SetComponentData(entity, new HeadlessRewindProofState
            {
                Phase = HeadlessRewindProofPhase.Idle,
                Result = 0
            });
            entityManager.AddBuffer<HeadlessRewindProofSubject>(entity);
            return entity;
        }

        private static HeadlessRewindProofConfig BuildConfig()
        {
            var config = new HeadlessRewindProofConfig
            {
                Enabled = 1,
                TriggerTick = ReadEnvUInt(TriggerTickEnv, DefaultTriggerTick),
                TicksBack = math.max(1u, ReadEnvUInt(TicksBackEnv, DefaultTicksBack)),
                TimeoutTicks = ReadEnvUInt(TimeoutTicksEnv, DefaultTimeoutTicks),
                RequireGuardViolationsClear = ReadEnvFlag(RequireGuardEnv, defaultValue: true) ? (byte)1 : (byte)0
            };
            return config;
        }

        private static bool ResolveEnabled()
        {
            if (TryReadEnvFlag(EnabledEnv, out var enabled))
            {
                return enabled;
            }

            return RuntimeMode.IsHeadless;
        }

        private static bool ReadEnvFlag(string key, bool defaultValue)
        {
            return TryReadEnvFlag(key, out var value) ? value : defaultValue;
        }

        private static bool TryReadEnvFlag(string key, out bool value)
        {
            var env = SystemEnv.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(env))
            {
                value = false;
                return false;
            }

            value = string.Equals(env, "1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(env, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(env, "yes", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        private static uint ReadEnvUInt(string key, uint fallback)
        {
            var env = SystemEnv.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(env))
            {
                return fallback;
            }

            return uint.TryParse(env, out var parsed) ? parsed : fallback;
        }

        private static void ExitIfRequested(ref SystemState state, uint tick, int exitCode)
        {
            if (!string.Equals(SystemEnv.GetEnvironmentVariable(ExitOnResultEnv), "1", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            HeadlessExitUtility.Request(state.EntityManager, tick, exitCode);
        }
    }
}
