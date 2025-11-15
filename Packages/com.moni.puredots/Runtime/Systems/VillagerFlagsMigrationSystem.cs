using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

#pragma warning disable 0618 // Migration system touches obsolete legacy tags by design

namespace PureDOTS.Systems
{
    /// <summary>
    /// Migration system that syncs legacy tag components with VillagerFlags for backward compatibility.
    /// Ensures VillagerFlags exists on all villagers and mirrors legacy tag state.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(VillagerSystemGroup), OrderFirst = true)]
    public partial struct VillagerFlagsMigrationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VillagerId>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Sync legacy tags to VillagerFlags for entities that don't have VillagerFlags yet
            foreach (var (id, entity) in SystemAPI.Query<RefRO<VillagerId>>()
                         .WithNone<VillagerFlags>()
                         .WithEntityAccess())
            {
                var flags = new VillagerFlags();

                // Sync from legacy tags if they exist
                if (state.EntityManager.HasComponent<VillagerDeadTag>(entity))
                {
                    flags.IsDead = true;
                }
                if (state.EntityManager.HasComponent<VillagerSelectedTag>(entity))
                {
                    flags.IsSelected = true;
                }
                if (state.EntityManager.HasComponent<VillagerHighlightedTag>(entity))
                {
                    flags.IsHighlighted = true;
                }
                if (state.EntityManager.HasComponent<VillagerInCombatTag>(entity))
                {
                    flags.IsInCombat = true;
                }
                if (state.EntityManager.HasComponent<VillagerCarryingTag>(entity))
                {
                    flags.IsCarrying = true;
                }

                ecb.AddComponent(entity, flags);
            }

            // Sync state from VillagerFlags back to legacy tags (for systems still using them)
            foreach (var (flags, entity) in SystemAPI.Query<RefRO<VillagerFlags>>().WithEntityAccess())
            {
                var flagsValue = flags.ValueRO;

                // Update legacy tags to match flags (for backward compatibility)
                if (flagsValue.IsDead)
                {
                    if (!state.EntityManager.HasComponent<VillagerDeadTag>(entity))
                    {
                        ecb.AddComponent<VillagerDeadTag>(entity);
                    }
                }
                else
                {
                    if (state.EntityManager.HasComponent<VillagerDeadTag>(entity))
                    {
                        ecb.RemoveComponent<VillagerDeadTag>(entity);
                    }
                }

                // Similar for other tags if needed
                // Note: Selected/Highlighted tags are typically managed by presentation systems
                // so we don't auto-sync those here
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
#pragma warning restore 0618

