using PureDOTS.Runtime.Anatomy;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Anatomy
{
    /// <summary>
    /// Seeds body part state buffers based on AnatomyId and the anatomy catalog.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(AnatomyCatalogBootstrapSystem))]
    public partial struct AnatomyBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<AnatomyCatalogRef>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            var catalogRef = SystemAPI.GetSingleton<AnatomyCatalogRef>();
            if (!catalogRef.Catalog.IsCreated)
            {
                return;
            }

            using var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (anatomyId, entity) in SystemAPI.Query<RefRO<AnatomyId>>()
                         .WithNone<BodyPartState>()
                         .WithEntityAccess())
            {
                if (anatomyId.ValueRO.Value.Length == 0)
                {
                    continue;
                }

                if (!AnatomyCatalogUtility.TryGetDefinitionIndex(catalogRef.Catalog, anatomyId.ValueRO.Value, out var index))
                {
                    continue;
                }

                ref var definition = ref catalogRef.Catalog.Value.Definitions[index];
                var buffer = ecb.AddBuffer<BodyPartState>(entity);
                for (int i = 0; i < definition.Parts.Length; i++)
                {
                    var part = definition.Parts[i];
                    buffer.Add(new BodyPartState
                    {
                        PartId = part.PartId,
                        ParentId = part.ParentId,
                        Kind = part.Kind,
                        Flags = part.Flags,
                        MaxHealth = math.max(1f, part.MaxHealth),
                        CurrentHealth = math.max(1f, part.MaxHealth),
                        RegenRate = math.max(0f, part.RegenRate),
                        DamageMultiplier = part.DamageMultiplier <= 0f ? 1f : part.DamageMultiplier
                    });
                }
            }

            ecb.Playback(state.EntityManager);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
