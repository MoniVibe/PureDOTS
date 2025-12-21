using System;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Runtime.Telemetry;
using PureDOTS.Runtime.Time;
using PureDOTS.Systems.Telemetry;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using SystemEnv = System.Environment;
using UnityDebug = UnityEngine.Debug;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Headless proof for global time controls (pause/step/resume/speed) and local time bubbles.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(TelemetryExportSystem))]
    public partial struct HeadlessTimeControlProofSystem : ISystem
    {
        private const string EnabledEnv = "PUREDOTS_HEADLESS_TIME_PROOF";
        private const string ExitOnResultEnv = "PUREDOTS_HEADLESS_TIME_PROOF_EXIT";
        private const string TimeoutSecondsEnv = "PUREDOTS_HEADLESS_TIME_PROOF_TIMEOUT_S";
        private const string StartTickEnv = "PUREDOTS_HEADLESS_TIME_PROOF_START_TICK";

        private const float DefaultTimeoutSeconds = 10f;
        private const uint DefaultStartTick = 240;
        private const int PauseHoldFrames = 4;
        private const int LocalHoldFrames = 4;
        private const int StepTicks = 2;
        private const float SpeedFast = 2f;
        private const float SpeedNormal = 1f;
        private const float LocalScale = 0.5f;
        private const float ProbeRadius = 6f;
        private const byte RewindRequiredMask = (byte)HeadlessRewindProofStage.RecordReturn;

        private static readonly FixedString32Bytes ExpectedPaused = new FixedString32Bytes("paused");
        private static readonly FixedString32Bytes ExpectedSteps = new FixedString32Bytes("+2");
        private static readonly FixedString32Bytes ExpectedPlaying = new FixedString32Bytes("play");
        private static readonly FixedString32Bytes ExpectedSpeed = new FixedString32Bytes("2");
        private static readonly FixedString32Bytes ExpectedLocalPause = new FixedString32Bytes("0");
        private static readonly FixedString32Bytes ExpectedLocalScale = new FixedString32Bytes("0.5");
        private static readonly FixedString32Bytes ExpectedLocalRewind = new FixedString32Bytes("<0");
        private static readonly FixedString32Bytes ExpectedRewindSubject = new FixedString32Bytes("time_control");
        private static readonly FixedString64Bytes RewindProofId = new FixedString64Bytes("time.control");

        private enum Phase : byte
        {
            Init = 0,
            GlobalPause,
            GlobalPauseHold,
            GlobalStep,
            GlobalResume,
            GlobalSpeedUp,
            GlobalSpeedReset,
            LocalPause,
            LocalScale,
            LocalRewind,
            Complete,
            Failed
        }

        private byte _enabled;
        private Phase _phase;
        private double _phaseStartTime;
        private int _holdFrames;
        private uint _stepTargetTick;
        private Entity _probeEntity;
        private byte _commandIssued;
        private byte _bubbleRequested;
        private byte _bubbleRemovalRequested;
        private uint _pendingBubbleId;
        private float _baselineAccumulated;
        private int _baselineUpdates;
        private float _timeoutSeconds;
        private uint _startTick;
        private byte _loggedWaitingForStart;
        private byte _loggedWaitingForStableMode;
        private byte _rewindSubjectRegistered;
        private byte _rewindPending;
        private byte _rewindPass;
        private float _rewindObserved;

        public void OnCreate(ref SystemState state)
        {
            if (!ResolveEnabled())
            {
                state.Enabled = false;
                return;
            }

            _enabled = 1;
            _phase = Phase.Init;
            _timeoutSeconds = ReadEnvFloat(TimeoutSecondsEnv, ReadEnvFloat("TIMEOUT_S", DefaultTimeoutSeconds));
            _startTick = ReadEnvUInt(StartTickEnv, ReadEnvUInt("START_TICK", DefaultStartTick));
            _loggedWaitingForStart = 0;
            _loggedWaitingForStableMode = 0;

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_enabled == 0)
            {
                return;
            }

            EnsureRewindSubject(ref state);
            TryFlushRewindProof(ref state);

            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.Tick < _startTick)
            {
                if (_loggedWaitingForStart == 0)
                {
                    _loggedWaitingForStart = 1;
                    UnityDebug.Log($"[HeadlessTimeControlProof] waiting startTick={_startTick} currentTick={timeState.Tick}");
                }
                return;
            }

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode == RewindMode.Rewind || rewindState.Mode == RewindMode.Step)
            {
                if (_loggedWaitingForStableMode == 0)
                {
                    _loggedWaitingForStableMode = 1;
                    UnityDebug.Log($"[HeadlessTimeControlProof] waiting rewindMode=Play/Paused currentMode={rewindState.Mode}");
                }
                return;
            }

            EnsureProbe(ref state);
            UpdateProbe(ref state);

            if (_phase == Phase.Complete || _phase == Phase.Failed)
            {
                return;
            }

            var elapsed = SystemAPI.Time.ElapsedTime;
            if (_phaseStartTime <= 0)
            {
                _phaseStartTime = elapsed;
            }

            switch (_phase)
            {
                case Phase.Init:
                    AdvancePhase(Phase.GlobalPause, elapsed);
                    break;

                case Phase.GlobalPause:
                    if (_commandIssued == 0)
                    {
                        EnqueueCommand(ref state, TimeControlCommandType.Pause);
                        _commandIssued = 1;
                        _phaseStartTime = elapsed;
                    }

                    if (timeState.IsPaused)
                    {
                        _holdFrames = 0;
                        AdvancePhase(Phase.GlobalPauseHold, elapsed);
                        break;
                    }

                    if (IsTimedOut(elapsed))
                    {
                        Fail(ref state, timeState.Tick, "global.pause");
                    }
                    break;

                case Phase.GlobalPauseHold:
                    if (!timeState.IsPaused)
                    {
                        Fail(ref state, timeState.Tick, "global.pause_hold");
                        break;
                    }

                    if (TimeHelpers.GetGlobalDelta(SystemAPI.GetSingleton<TickTimeState>(), timeState) <= 0f)
                    {
                        _holdFrames++;
                    }

                    if (_holdFrames >= PauseHoldFrames)
                    {
                        EmitStep(ref state, timeState.Tick, "global.pause", true, 1f, ExpectedPaused);
                        _commandIssued = 0;
                        AdvancePhase(Phase.GlobalStep, elapsed);
                        break;
                    }

                    if (IsTimedOut(elapsed))
                    {
                        Fail(ref state, timeState.Tick, "global.pause_hold");
                    }
                    break;

                case Phase.GlobalStep:
                    if (_commandIssued == 0)
                    {
                        _stepTargetTick = timeState.Tick + StepTicks;
                        EnqueueCommand(ref state, TimeControlCommandType.StepTicks, StepTicks);
                        _commandIssued = 1;
                        _phaseStartTime = elapsed;
                    }

                    if (timeState.Tick >= _stepTargetTick && timeState.IsPaused)
                    {
                        EmitStep(ref state, timeState.Tick, "global.step", true, StepTicks, ExpectedSteps);
                        _commandIssued = 0;
                        AdvancePhase(Phase.GlobalResume, elapsed);
                        break;
                    }

                    if (IsTimedOut(elapsed))
                    {
                        Fail(ref state, timeState.Tick, "global.step");
                    }
                    break;

                case Phase.GlobalResume:
                    if (_commandIssued == 0)
                    {
                        EnqueueCommand(ref state, TimeControlCommandType.Resume);
                        _commandIssued = 1;
                        _phaseStartTime = elapsed;
                    }

                    if (!timeState.IsPaused && timeState.Tick > _stepTargetTick)
                    {
                        EmitStep(ref state, timeState.Tick, "global.resume", true, timeState.Tick - _stepTargetTick, ExpectedPlaying);
                        _commandIssued = 0;
                        AdvancePhase(Phase.GlobalSpeedUp, elapsed);
                        break;
                    }

                    if (IsTimedOut(elapsed))
                    {
                        Fail(ref state, timeState.Tick, "global.resume");
                    }
                    break;

                case Phase.GlobalSpeedUp:
                    if (_commandIssued == 0)
                    {
                        EnqueueCommand(ref state, TimeControlCommandType.SetSpeed, SpeedFast);
                        _commandIssued = 1;
                        _phaseStartTime = elapsed;
                    }

                    if (math.abs(timeState.CurrentSpeedMultiplier - SpeedFast) < 0.01f)
                    {
                        EmitStep(ref state, timeState.Tick, "global.speed_up", true, timeState.CurrentSpeedMultiplier, ExpectedSpeed);
                        _commandIssued = 0;
                        AdvancePhase(Phase.GlobalSpeedReset, elapsed);
                        break;
                    }

                    if (IsTimedOut(elapsed))
                    {
                        Fail(ref state, timeState.Tick, "global.speed_up");
                    }
                    break;

                case Phase.GlobalSpeedReset:
                    if (_commandIssued == 0)
                    {
                        EnqueueCommand(ref state, TimeControlCommandType.SetSpeed, SpeedNormal);
                        _commandIssued = 1;
                        _phaseStartTime = elapsed;
                    }

                    if (math.abs(timeState.CurrentSpeedMultiplier - SpeedNormal) < 0.01f)
                    {
                        EmitStep(ref state, timeState.Tick, "global.speed_reset", true, timeState.CurrentSpeedMultiplier, new FixedString32Bytes("1"));
                        _commandIssued = 0;
                        AdvancePhase(Phase.LocalPause, elapsed);
                        break;
                    }

                    if (IsTimedOut(elapsed))
                    {
                        Fail(ref state, timeState.Tick, "global.speed_reset");
                    }
                    break;

                case Phase.LocalPause:
                    if (!EnsureBubbleCleared(ref state))
                    {
                        break;
                    }

                    if (!IssueBubbleOnce(ref state, TimeBubbleMode.Pause, 0f))
                    {
                        break;
                    }

                    if (!TryGetProbeMembership(ref state, out var membership) || membership.LocalMode != TimeBubbleMode.Pause)
                    {
                        if (IsTimedOut(elapsed))
                        {
                            Fail(ref state, timeState.Tick, "local.pause");
                        }
                        break;
                    }

                    if (_holdFrames == 0)
                    {
                        CaptureProbeBaseline(ref state);
                    }

                    if (ProbeStable(ref state))
                    {
                        _holdFrames++;
                    }

                    if (_holdFrames >= LocalHoldFrames)
                    {
                        EmitStep(ref state, timeState.Tick, "local.pause", true, 0f, ExpectedLocalPause);
                        _commandIssued = 0;
                        _bubbleRequested = 0;
                        _holdFrames = 0;
                        AdvancePhase(Phase.LocalScale, elapsed);
                        break;
                    }

                    if (IsTimedOut(elapsed))
                    {
                        Fail(ref state, timeState.Tick, "local.pause_hold");
                    }
                    break;

                case Phase.LocalScale:
                    if (!EnsureBubbleCleared(ref state))
                    {
                        break;
                    }

                    if (!IssueBubbleOnce(ref state, TimeBubbleMode.Scale, LocalScale))
                    {
                        break;
                    }

                    if (!TryGetProbeMembership(ref state, out var scaleMembership) || scaleMembership.LocalMode != TimeBubbleMode.Scale)
                    {
                        if (IsTimedOut(elapsed))
                        {
                            Fail(ref state, timeState.Tick, "local.scale");
                        }
                        break;
                    }

                    if (ProbeDeltaMatches(ref state, LocalScale))
                    {
                        EmitStep(ref state, timeState.Tick, "local.scale", true, GetProbeDelta(ref state), ExpectedLocalScale);
                        _commandIssued = 0;
                        _bubbleRequested = 0;
                        AdvancePhase(Phase.LocalRewind, elapsed);
                        break;
                    }

                    if (IsTimedOut(elapsed))
                    {
                        Fail(ref state, timeState.Tick, "local.scale_verify");
                    }
                    break;

                case Phase.LocalRewind:
                    if (!EnsureBubbleCleared(ref state))
                    {
                        break;
                    }

                    if (!IssueBubbleOnce(ref state, TimeBubbleMode.Rewind, -1f))
                    {
                        break;
                    }

                    if (!TryGetProbeMembership(ref state, out var rewindMembership) || rewindMembership.LocalMode != TimeBubbleMode.Rewind)
                    {
                        if (IsTimedOut(elapsed))
                        {
                            Fail(ref state, timeState.Tick, "local.rewind");
                        }
                        break;
                    }

                    if (GetProbeDelta(ref state) < 0f)
                    {
                        EmitStep(ref state, timeState.Tick, "local.rewind", true, GetProbeDelta(ref state), ExpectedLocalRewind);
                        _commandIssued = 0;
                        _bubbleRequested = 0;
                        AdvancePhase(Phase.Complete, elapsed);
                        Complete(ref state, timeState.Tick);
                        break;
                    }

                    if (IsTimedOut(elapsed))
                    {
                        Fail(ref state, timeState.Tick, "local.rewind_verify");
                    }
                    break;
            }
        }

        private void EnsureProbe(ref SystemState state)
        {
            if (_probeEntity != Entity.Null && state.EntityManager.Exists(_probeEntity))
            {
                return;
            }

            _probeEntity = state.EntityManager.CreateEntity(
                typeof(LocalTransform),
                typeof(TimeBubbleAffectableTag),
                typeof(HeadlessTimeProofProbe));

            state.EntityManager.SetComponentData(_probeEntity,
                LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            state.EntityManager.SetComponentData(_probeEntity, new HeadlessTimeProofProbe());
        }

        private void UpdateProbe(ref SystemState state)
        {
            if (_probeEntity == Entity.Null || !state.EntityManager.Exists(_probeEntity))
            {
                return;
            }

            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            var membership = state.EntityManager.HasComponent<TimeBubbleMembership>(_probeEntity)
                ? state.EntityManager.GetComponentData<TimeBubbleMembership>(_probeEntity)
                : default;

            var probe = state.EntityManager.GetComponentData<HeadlessTimeProofProbe>(_probeEntity);
            var delta = TimeHelpers.GetEffectiveDelta(tickTimeState, timeState, membership);
            probe.LastDelta = delta;

            if (TimeHelpers.ShouldUpdate(timeState, rewindState, membership))
            {
                probe.UpdateCount++;
                probe.Accumulated += delta;
            }

            state.EntityManager.SetComponentData(_probeEntity, probe);
        }

        private bool TryGetProbeMembership(ref SystemState state, out TimeBubbleMembership membership)
        {
            if (_probeEntity != Entity.Null && state.EntityManager.HasComponent<TimeBubbleMembership>(_probeEntity))
            {
                membership = state.EntityManager.GetComponentData<TimeBubbleMembership>(_probeEntity);
                return true;
            }

            membership = default;
            return false;
        }

        private bool IssueBubbleOnce(ref SystemState state, TimeBubbleMode mode, float scale)
        {
            if (_bubbleRequested != 0)
            {
                return true;
            }

            var requestEntity = state.EntityManager.CreateEntity(typeof(TimeBubbleCreateRequest));
            state.EntityManager.SetComponentData(requestEntity, new TimeBubbleCreateRequest
            {
                Center = float3.zero,
                Radius = ProbeRadius,
                Mode = mode,
                Scale = scale,
                Priority = 200,
                DurationTicks = 0,
                SourceEntity = _probeEntity,
                IsPending = true
            });

            _bubbleRequested = 1;
            _phaseStartTime = SystemAPI.Time.ElapsedTime;
            return true;
        }

        private bool EnsureBubbleCleared(ref SystemState state)
        {
            if (_probeEntity == Entity.Null || !state.EntityManager.Exists(_probeEntity))
            {
                return false;
            }

            foreach (var (bubbleParams, bubbleId, entity) in SystemAPI.Query<RefRO<TimeBubbleParams>, RefRO<TimeBubbleId>>()
                .WithEntityAccess())
            {
                if (bubbleParams.ValueRO.SourceEntity != _probeEntity)
                {
                    continue;
                }

                if (_bubbleRemovalRequested == 0 || _pendingBubbleId != bubbleId.ValueRO.Id)
                {
                    RequestBubbleRemoval(ref state, bubbleId.ValueRO.Id);
                    _pendingBubbleId = bubbleId.ValueRO.Id;
                    _bubbleRemovalRequested = 1;
                }

                return false;
            }

            if (state.EntityManager.HasComponent<TimeBubbleMembership>(_probeEntity))
            {
                return false;
            }

            _bubbleRemovalRequested = 0;
            _pendingBubbleId = 0;
            return true;
        }

        private void RequestBubbleRemoval(ref SystemState state, uint bubbleId)
        {
            var requestEntity = state.EntityManager.CreateEntity(typeof(TimeBubbleRemoveRequest));
            state.EntityManager.SetComponentData(requestEntity, new TimeBubbleRemoveRequest
            {
                BubbleId = bubbleId,
                IsPending = true
            });
        }

        private void CaptureProbeBaseline(ref SystemState state)
        {
            var probe = state.EntityManager.GetComponentData<HeadlessTimeProofProbe>(_probeEntity);
            _baselineAccumulated = probe.Accumulated;
            _baselineUpdates = probe.UpdateCount;
        }

        private bool ProbeStable(ref SystemState state)
        {
            var probe = state.EntityManager.GetComponentData<HeadlessTimeProofProbe>(_probeEntity);
            return probe.UpdateCount == _baselineUpdates &&
                   math.abs(probe.Accumulated - _baselineAccumulated) < 0.0001f;
        }

        private bool ProbeDeltaMatches(ref SystemState state, float scale)
        {
            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var expected = tickTimeState.FixedDeltaTime * scale;
            var probe = state.EntityManager.GetComponentData<HeadlessTimeProofProbe>(_probeEntity);
            return math.abs(probe.LastDelta - expected) < 0.0001f && probe.UpdateCount > 0;
        }

        private float GetProbeDelta(ref SystemState state)
        {
            var probe = state.EntityManager.GetComponentData<HeadlessTimeProofProbe>(_probeEntity);
            return probe.LastDelta;
        }

        private void AdvancePhase(Phase nextPhase, double now)
        {
            _phase = nextPhase;
            _phaseStartTime = now;
            _commandIssued = 0;
            _holdFrames = 0;
        }

        private bool IsTimedOut(double now)
        {
            return _timeoutSeconds > 0f && (now - _phaseStartTime) > _timeoutSeconds;
        }

        private void Complete(ref SystemState state, uint tick)
        {
            _rewindPending = 1;
            _rewindPass = 1;
            _rewindObserved = 1f;
            TryFlushRewindProof(ref state);
            UnityDebug.Log($"[HeadlessTimeControlProof] PASS tick={tick}");
            ExitIfRequested(ref state, tick, 0);
        }

        private void Fail(ref SystemState state, uint tick, string step)
        {
            _phase = Phase.Failed;
            _rewindPending = 1;
            _rewindPass = 0;
            _rewindObserved = 0f;
            TryFlushRewindProof(ref state);
            UnityDebug.LogError($"[HeadlessTimeControlProof] FAIL tick={tick} step={step}");
            EmitStep(ref state, tick, step, false, 0f, default);
            ExitIfRequested(ref state, tick, 2);
        }

        private void EmitStep(ref SystemState state, uint tick, string step, bool pass, float observed, in FixedString32Bytes expected)
        {
            var timeoutTicks = ResolveTimeoutTicks();
            TelemetryLoopProofUtility.Emit(state.EntityManager, tick, TelemetryLoopIds.Time, pass, observed, expected, timeoutTicks, step: new FixedString32Bytes(step));
        }

        private uint ResolveTimeoutTicks()
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.FixedDeltaTime <= 0f)
            {
                return 0;
            }

            var ticks = (uint)math.ceil(_timeoutSeconds / timeState.FixedDeltaTime);
            return ticks == 0 ? 1u : ticks;
        }

        private void EnqueueCommand(ref SystemState state, TimeControlCommandType type, int ticks = 0)
        {
            var rewindEntity = SystemAPI.GetSingletonEntity<RewindState>();
            if (!state.EntityManager.HasBuffer<TimeControlCommand>(rewindEntity))
            {
                state.EntityManager.AddBuffer<TimeControlCommand>(rewindEntity);
            }

            var buffer = state.EntityManager.GetBuffer<TimeControlCommand>(rewindEntity);
            buffer.Add(new TimeControlCommand
            {
                Type = type,
                UintParam = ticks > 0 ? (uint)ticks : 0
            });
        }

        private void EnqueueCommand(ref SystemState state, TimeControlCommandType type, float speed)
        {
            var rewindEntity = SystemAPI.GetSingletonEntity<RewindState>();
            if (!state.EntityManager.HasBuffer<TimeControlCommand>(rewindEntity))
            {
                state.EntityManager.AddBuffer<TimeControlCommand>(rewindEntity);
            }

            var buffer = state.EntityManager.GetBuffer<TimeControlCommand>(rewindEntity);
            buffer.Add(new TimeControlCommand
            {
                Type = type,
                FloatParam = speed
            });
        }

        private void EnsureRewindSubject(ref SystemState state)
        {
            if (_rewindSubjectRegistered != 0)
            {
                return;
            }

            if (HeadlessRewindProofUtility.TryEnsureSubject(state.EntityManager, RewindProofId, RewindRequiredMask))
            {
                _rewindSubjectRegistered = 1;
            }
        }

        private void TryFlushRewindProof(ref SystemState state)
        {
            if (_rewindPending == 0)
            {
                return;
            }

            if (!HeadlessRewindProofUtility.TryGetState(state.EntityManager, out var rewindProof) || rewindProof.SawRecord == 0)
            {
                return;
            }

            HeadlessRewindProofUtility.TryMarkResult(state.EntityManager, RewindProofId, _rewindPass != 0, _rewindObserved, ExpectedRewindSubject, RewindRequiredMask);
            _rewindPending = 0;
        }

        private static bool ResolveEnabled()
        {
            if (TryReadEnvFlag(EnabledEnv, out var enabled))
            {
                return enabled;
            }

            return RuntimeMode.IsHeadless;
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

        private static float ReadEnvFloat(string key, float fallback)
        {
            var env = SystemEnv.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(env))
            {
                return fallback;
            }

            return float.TryParse(env, out var parsed) ? parsed : fallback;
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
