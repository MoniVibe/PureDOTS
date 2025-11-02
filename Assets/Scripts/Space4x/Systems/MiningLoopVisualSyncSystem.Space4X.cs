using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Visuals;
using PureDOTS.Systems;
using PureDOTS.Systems.Visuals;
using Space4X.Runtime.Transport;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.Systems.Visuals
{
    [BurstCompile]
    [UpdateInGroup(typeof(RecordSimulationSystemGroup))]
    [UpdateAfter(typeof(MiningLoopVisualSyncSystem))]
    public partial struct Space4XMiningLoopVisualExtensionSystem : ISystem
    {
        private EntityQuery _minerRegistryQuery;
        private uint _lastTick;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MinerVesselRegistry>();
            state.RequireForUpdate<MiningVisualManifest>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _minerRegistryQuery = state.GetEntityQuery(ComponentType.ReadOnly<MinerVesselRegistryEntry>());
            _lastTick = 0;
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var deltaSeconds = 0f;
            if (_lastTick != 0 && timeState.Tick > _lastTick)
            {
                deltaSeconds = (timeState.Tick - _lastTick) * timeState.FixedDeltaTime;
            }

            _lastTick = timeState.Tick;

            if (_minerRegistryQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var manifestEntity = SystemAPI.GetSingletonEntity<MiningVisualManifest>();
            var manifestRW = SystemAPI.GetComponentRW<MiningVisualManifest>(manifestEntity);
            var manifestSnapshot = manifestRW.ValueRO;
            var requests = state.EntityManager.GetBuffer<MiningVisualRequest>(manifestEntity);

            var registryEntity = SystemAPI.GetSingletonEntity<MinerVesselRegistry>();
            var entries = state.EntityManager.GetBuffer<MinerVesselRegistryEntry>(registryEntity);

            var totalLoad = 0f;
            var spawnCount = math.min(entries.Length, 72);

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                totalLoad += entry.Load;

                if (i >= spawnCount)
                {
                    continue;
                }

                var position = entry.Position;
                position.z -= (i % 8) * 1.4f;
                position.x -= (i / 8) * 1.75f;

                requests.Add(new MiningVisualRequest
                {
                    VisualType = MiningVisualType.Vessel,
                    SourceEntity = entry.VesselEntity,
                    Position = position,
                    BaseScale = math.saturate(entry.Load / math.max(1f, entry.Capacity)) + 0.4f
                });
            }

            var throughput = 0f;
            if (deltaSeconds > 0f)
            {
                var loadDelta = totalLoad - manifestSnapshot.VesselLoadCumulative;
                throughput = math.abs(loadDelta) / deltaSeconds * 60f;
            }

            manifestSnapshot.VesselCount = spawnCount;
            manifestSnapshot.VesselThroughput = throughput;
            manifestSnapshot.VesselLoadCumulative = totalLoad;
            manifestRW.ValueRW = manifestSnapshot;
        }
    }
}

