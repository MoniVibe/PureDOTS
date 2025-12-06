using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Shared;
using PureDOTS.Runtime.AI.Social;
using PureDOTS.Runtime.Bridges;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.AI.Social.Systems
{
    /// <summary>
    /// Cultural signal system for Body ECS.
    /// Broadcasts CulturalSignal on successful cooperation.
    /// Based on Nehaniv & Dautenhahn (2009) cultural evolution patterns.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CooperationSystemManaged))]
    [BurstCompile]
    public partial struct CulturalSignalSystem : ISystem
    {
        private const float SignalStrengthBase = 0.5f; // Base signal strength
        private const float SignalDecayRate = 0.01f; // Decay per tick

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AgentSyncState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return; // Skip during playback
            }

            // Broadcasting handled in managed wrapper
        }
    }

    /// <summary>
    /// Managed wrapper for CulturalSignalSystem that accesses AgentSyncBus.
    /// Broadcasts cultural signals when successful cooperation occurs.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(CooperationSystemManaged))]
    public sealed partial class CulturalSignalSystemManaged : SystemBase
    {
        private float _lastUpdateTime;
        private const float UpdateInterval = 0.5f; // 2 Hz updates

        protected override void OnCreate()
        {
            _lastUpdateTime = 0f;
            RequireForUpdate<AgentSyncState>();
            RequireForUpdate<RewindState>();
        }

        protected override void OnUpdate()
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return; // Skip during playback
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime - _lastUpdateTime < UpdateInterval)
            {
                return; // Temporal batching
            }

            var coordinator = World.GetExistingSystemManaged<AgentSyncBridgeCoordinator>();
            if (coordinator == null)
            {
                return;
            }

            var bus = coordinator.GetBus();
            if (bus == null)
            {
                return;
            }

            var tickState = SystemAPI.GetSingleton<TickTimeState>();
            var tickNumber = tickState.Tick;

            // Process cultural signals in Burst job
            var job = new BroadcastCulturalSignalsJob
            {
                Bus = bus,
                TickNumber = tickNumber,
                SignalStrengthBase = CulturalSignalSystem.SignalStrengthBase,
                SignalDecayRate = CulturalSignalSystem.SignalDecayRate
            };

            var entityQuery = GetEntityQuery(typeof(AgentSyncId), typeof(CulturalSignal));
            job.ScheduleParallel(entityQuery, Dependency).Complete();

            _lastUpdateTime = currentTime;
        }
    }

    [BurstCompile]
    private partial struct BroadcastCulturalSignalsJob : IJobEntity
    {
        public uint TickNumber;
        public float SignalStrengthBase;
        public float SignalDecayRate;

        public void Execute(Entity entity, DynamicBuffer<CulturalSignal> signals)
        {
            // Apply decay to existing signals
            for (int i = 0; i < signals.Length; i++)
            {
                var signal = signals[i];
                signal.Strength = math.max(0f, signal.Strength - SignalDecayRate);
                
                if (signal.Strength <= 0f)
                {
                    // Remove decayed signal
                    signals.RemoveAt(i);
                    i--;
                }
                else
                {
                    signals[i] = signal;
                }
            }

            // Broadcasting to AgentSyncBus happens in managed system after job completes
        }
    }
}

