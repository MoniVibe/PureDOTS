using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using PureDOTS.Systems;
using PureDOTS.Tests;
using PureDOTS.Tests.Playmode;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.Integration
{
    /// <summary>
    /// Base fixture for deterministic rewind tests that records inputs, runs simulation, rewinds, and replays.
    /// Provides utilities for capturing state snapshots and validating deterministic replay.
    /// </summary>
    public abstract class DeterministicRewindTestFixture : EcsTestFixture
    {
        protected uint RecordTickCount { get; set; } = 100;
        protected uint RewindTargetTick { get; set; } = 0;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // Bootstrap core singletons
            CoreSingletonBootstrapSystem.EnsureSingletons(EntityManager);

            // Ensure rewind state is initialized
            if (!EntityManager.HasSingleton<RewindState>())
            {
                var rewindEntity = EntityManager.CreateEntity(typeof(RewindState));
                EntityManager.SetComponentData(rewindEntity, new RewindState
                {
                    Mode = RewindMode.Record,
                    TargetTick = 0,
                    TickDuration = 1f / 60f,
                    MaxHistoryTicks = 600,
                    PendingStepTicks = 0
                });
                EntityManager.AddComponentData(rewindEntity, new RewindLegacyState
                {
                    PlaybackSpeed = 1f,
                    CurrentTick = 0,
                    StartTick = 0,
                    PlaybackTick = 0,
                    PlaybackTicksPerSecond = 60f,
                    ScrubDirection = 0,
                    ScrubSpeedMultiplier = 1f,
                    RewindWindowTicks = 0,
                    ActiveTrack = default
                });
            }

            // Ensure time state is initialized
            if (!EntityManager.HasSingleton<TimeState>())
            {
                var timeEntity = EntityManager.CreateEntity(typeof(TimeState));
                EntityManager.SetComponentData(timeEntity, new TimeState
                {
                    Tick = 0,
                    FixedDeltaTime = 1f / 60f,
                    IsPaused = false,
                    CurrentSpeedMultiplier = 1f
                });
            }
        }

        /// <summary>
        /// Records simulation state for deterministic replay validation.
        /// </summary>
        protected StateSnapshot CaptureStateSnapshot()
        {
            var snapshot = new StateSnapshot
            {
                Tick = EntityManager.HasSingleton<TimeState>() ? EntityManager.GetSingleton<TimeState>().Tick : 0,
                EntityCount = EntityManager.GetAllEntities().Length
            };

            // Capture component data for key entities
            // Subclasses can extend this to capture domain-specific state
            return snapshot;
        }

        /// <summary>
        /// Compares two state snapshots and returns true if they match.
        /// </summary>
        protected bool CompareSnapshots(StateSnapshot a, StateSnapshot b, out string difference)
        {
            difference = string.Empty;

            if (a.Tick != b.Tick)
            {
                difference = $"Tick mismatch: {a.Tick} != {b.Tick}";
                return false;
            }

            if (a.EntityCount != b.EntityCount)
            {
                difference = $"Entity count mismatch: {a.EntityCount} != {b.EntityCount}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Runs simulation for a specified number of ticks in Record mode.
        /// </summary>
        protected void RunRecordPhase(uint tickCount)
        {
            var rewindState = EntityManager.GetSingleton<RewindState>();
            rewindState.Mode = RewindMode.Record;
            EntityManager.SetSingleton(rewindState);

            var timeState = EntityManager.GetSingleton<TimeState>();
            uint startTick = timeState.Tick;

            for (uint i = 0; i < tickCount; i++)
            {
                timeState.Tick = startTick + i;
                EntityManager.SetSingleton(timeState);

                // Update world systems
                World.Update();

                // Allow subclasses to inject inputs or verify state
                OnRecordTick(timeState.Tick);
            }
        }

        /// <summary>
        /// Rewinds simulation to a target tick using Playback mode.
        /// </summary>
        protected void RunRewindPhase(uint targetTick)
        {
            var timeState = EntityManager.GetSingleton<TimeState>();
            var rewindState = EntityManager.GetSingleton<RewindState>();
            var legacy = EntityManager.GetSingleton<RewindLegacyState>();

            rewindState.Mode = RewindMode.Playback;
            rewindState.TargetTick = (int)targetTick;
            EntityManager.SetSingleton(rewindState);
            legacy.StartTick = timeState.Tick;
            legacy.PlaybackTick = timeState.Tick;
            EntityManager.SetSingleton(legacy);

            // Simulate playback (in real implementation, this would replay from history)
            // For now, we'll just set the tick directly
            timeState.Tick = targetTick;
            EntityManager.SetSingleton(timeState);

            // Update world systems (presentation systems will run, simulation systems should be disabled)
            World.Update();

            // Transition to catch-up if needed
            if (targetTick < legacy.StartTick)
            {
                rewindState.Mode = RewindMode.CatchUp;
                EntityManager.SetSingleton(rewindState);
                World.Update();
            }
        }

        /// <summary>
        /// Runs simulation from a starting tick to match recorded state.
        /// Used to validate deterministic replay.
        /// </summary>
        protected void RunReplayPhase(uint startTick, uint endTick)
        {
            var rewindState = EntityManager.GetSingleton<RewindState>();
            rewindState.Mode = RewindMode.Record;
            EntityManager.SetSingleton(rewindState);

            var timeState = EntityManager.GetSingleton<TimeState>();
            timeState.Tick = startTick;
            EntityManager.SetSingleton(timeState);

            for (uint tick = startTick; tick <= endTick; tick++)
            {
                timeState.Tick = tick;
                EntityManager.SetSingleton(timeState);

                World.Update();

                // Allow subclasses to verify state matches recorded phase
                OnReplayTick(tick);
            }
        }

        /// <summary>
        /// Called during each tick of the record phase. Override to inject inputs or verify state.
        /// </summary>
        protected virtual void OnRecordTick(uint tick)
        {
        }

        /// <summary>
        /// Called during each tick of the replay phase. Override to verify deterministic state.
        /// </summary>
        protected virtual void OnReplayTick(uint tick)
        {
        }

        /// <summary>
        /// Validates that the current state matches a recorded snapshot.
        /// </summary>
        protected void AssertStateMatches(StateSnapshot recorded, string message = "")
        {
            var current = CaptureStateSnapshot();
            if (!CompareSnapshots(recorded, current, out var difference))
            {
                Assert.Fail($"{message} State mismatch: {difference}");
            }
        }

        /// <summary>
        /// Snapshot of simulation state at a specific tick.
        /// </summary>
        protected struct StateSnapshot
        {
            public uint Tick;
            public int EntityCount;
            // Extend with domain-specific fields as needed
        }
    }
}
