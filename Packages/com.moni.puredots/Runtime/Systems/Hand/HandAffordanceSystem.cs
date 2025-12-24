using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Hand;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Hand
{
    /// <summary>
    /// Reads hover target components and outputs HandAffordances singleton.
    /// Runs after HandRaycastSystem to detect what actions are available.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(HandRaycastSystem))]
    public partial struct HandAffordanceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HandHover>();
            
            // Ensure HandAffordances singleton exists
            if (!SystemAPI.TryGetSingletonEntity<HandAffordances>(out _))
            {
                var entity = state.EntityManager.CreateEntity(typeof(HandAffordances));
                state.EntityManager.SetComponentData(entity, new HandAffordances());
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hover = SystemAPI.GetSingleton<HandHover>();
            var affordances = new HandAffordances
            {
                Flags = HandAffordanceFlags.None,
                TargetEntity = hover.TargetEntity,
                ResourceTypeIndex = 0
            };

            if (hover.TargetEntity == Entity.Null || !state.EntityManager.Exists(hover.TargetEntity))
            {
                SystemAPI.SetSingleton(affordances);
                return;
            }

            var entityManager = state.EntityManager;
            var targetEntity = hover.TargetEntity;

            // Check for Pickable
            if (entityManager.HasComponent<PickableTag>(targetEntity) || 
                entityManager.HasComponent<Pickable>(targetEntity))
            {
                affordances.Flags |= HandAffordanceFlags.CanPickUp;
            }

            // Check for SiphonSource
            if (entityManager.HasComponent<SiphonSource>(targetEntity))
            {
                var siphonSource = entityManager.GetComponentData<SiphonSource>(targetEntity);
                affordances.Flags |= HandAffordanceFlags.CanSiphon;
                affordances.ResourceTypeIndex = siphonSource.ResourceTypeIndex;
            }

            // Check for dump targets
            if (entityManager.HasComponent<DumpTargetStorehouse>(targetEntity))
            {
                affordances.Flags |= HandAffordanceFlags.CanDumpStorehouse;
            }

            if (entityManager.HasComponent<DumpTargetConstruction>(targetEntity))
            {
                affordances.Flags |= HandAffordanceFlags.CanDumpConstruction;
            }

            if (entityManager.HasComponent<DumpTargetGround>(targetEntity))
            {
                affordances.Flags |= HandAffordanceFlags.CanDumpGround;
            }

            // Check for MiracleSurface
            if (entityManager.HasComponent<MiracleSurface>(targetEntity))
            {
                affordances.Flags |= HandAffordanceFlags.CanCastMiracle;
            }

            SystemAPI.SetSingleton(affordances);
        }
    }
}

