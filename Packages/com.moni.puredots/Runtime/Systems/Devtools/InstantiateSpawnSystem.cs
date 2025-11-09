#if DEVTOOLS_ENABLED
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Devtools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Devtools
{
    /// <summary>
    /// Instantiates prefabs from validated spawn candidates.
    /// Uses EndFixedStepSimulationEntityCommandBufferSystem for structural changes.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(ValidateSpawnCandidatesSystem))]
    public partial struct InstantiateSpawnSystem : ISystem
    {
        private EndFixedStepSimulationEntityCommandBufferSystem.Singleton _ecbSingleton;

        public void OnCreate(ref SystemState state)
        {
            _ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SpawnRequest>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<PrototypeRegistryBlob>())
            {
                return;
            }

            var registry = SystemAPI.GetSingleton<PrototypeRegistryBlob>();
            var ecb = _ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (request, candidates, validationResults, statOverrides, entity) in SystemAPI.Query<RefRO<SpawnRequest>, DynamicBuffer<SpawnCandidate>, DynamicBuffer<SpawnValidationResult>, DynamicBuffer<StatOverride>>().WithEntityAccess())
            {
                var req = request.ValueRO;

                // If ValidateOnly flag is set, skip instantiation
                if ((req.Flags & SpawnFlags.ValidateOnly) != 0)
                {
                    continue;
                }

                // Get prefab from registry
                if (!PrototypeLookup.TryGetPrefab(registry.Entries, req.PrototypeId, out var prefab))
                {
                    continue;
                }

                // Instantiate valid candidates
                for (int i = 0; i < candidates.Length; i++)
                {
                    var candidate = candidates[i];
                    var validation = validationResults[i];

                    if (candidate.IsValid == 0 || validation.FailureReason != ValidationFailureReason.None)
                    {
                        continue;
                    }

                    // Instantiate prefab
                    var instance = ecb.Instantiate(prefab);
                    ecb.SetComponent(instance, LocalTransform.FromPositionRotation(candidate.Position, candidate.Rotation));

                    // Apply stat overrides if any
                    ApplyStatOverrides(ecb, instance, statOverrides, registry.Entries, req.PrototypeId);

                    // Apply default alignment/outlook
                    if (PrototypeLookup.TryGetAlignmentDefault(registry.Entries, req.PrototypeId, out var alignment))
                    {
                        ecb.AddComponent(instance, alignment);
                    }
                    if (PrototypeLookup.TryGetOutlookDefault(registry.Entries, req.PrototypeId, out var outlook))
                    {
                        ecb.AddComponent(instance, outlook);
                    }
                }

                // Cleanup request entity
                ecb.DestroyEntity(entity);
            }
        }

        private void ApplyStatOverrides(EntityCommandBuffer ecb, Entity instance, DynamicBuffer<StatOverride> overrides, BlobAssetReference<BlobArray<PrototypeRegistry.PrototypeEntry>> registry, int prototypeId)
        {
            if (overrides.Length == 0)
            {
                return;
            }

            // Get default stats
            if (PrototypeLookup.TryGetStatsDefault(registry, prototypeId, out var defaultStats))
            {
                var stats = defaultStats;
                // Apply overrides by name matching (Burst-compatible FixedString comparison)
                for (int i = 0; i < overrides.Length; i++)
                {
                    var ovr = overrides[i];
                    var name = ovr.Name;
                    
                    // Match stat name (case-sensitive comparison - matches common stat names)
                    if (name == new FixedString64Bytes("Health") || name == new FixedString64Bytes("health"))
                    {
                        stats.Health = ovr.Value;
                    }
                    else if (name == new FixedString64Bytes("Speed") || name == new FixedString64Bytes("speed"))
                    {
                        stats.Speed = ovr.Value;
                    }
                    else if (name == new FixedString64Bytes("Mass") || name == new FixedString64Bytes("mass"))
                    {
                        stats.Mass = ovr.Value;
                    }
                    else if (name == new FixedString64Bytes("Damage") || name == new FixedString64Bytes("damage"))
                    {
                        stats.Damage = ovr.Value;
                    }
                    else if (name == new FixedString64Bytes("Range") || name == new FixedString64Bytes("range"))
                    {
                        stats.Range = ovr.Value;
                    }
                }
                ecb.AddComponent(instance, stats);
            }
        }
    }
}
#endif

