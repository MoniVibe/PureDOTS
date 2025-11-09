using Godgame.Resources;
using Godgame.Systems;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Godgame.Interaction.HandSystems
{
    /// <summary>
    /// Converts resolved hand interactions into aggregate pile commands (siphon/drip).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DivineHandStateSystem))]
    [UpdateBefore(typeof(AggregatePileSystem))]
    public partial struct HandAggregatePileInteractionSystem : ISystem
    {
        private EntityQuery _handQuery;
        private ComponentLookup<AggregatePile> _pileLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputState>();
            state.RequireForUpdate<AggregatePileCommandState>();

            _handQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<Hand>(),
                    ComponentType.ReadOnly<RightClickResolved>()
                }
            });
            state.RequireForUpdate(_handQuery);

            _pileLookup = state.GetComponentLookup<AggregatePile>(isReadOnly: true);
        }

        public void OnUpdate(ref SystemState state)
        {
            _pileLookup.Update(ref state);

            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            var elapsedSeconds = (float)state.WorldUnmanaged.Time.ElapsedTime;
            var input = SystemAPI.GetSingleton<InputState>();
            var commandEntity = SystemAPI.GetSingletonEntity<AggregatePileCommandState>();
            var addBuffer = state.EntityManager.GetBuffer<AggregatePileAddCommand>(commandEntity);
            var takeBuffer = state.EntityManager.GetBuffer<AggregatePileTakeCommand>(commandEntity);

            BlobAssetReference<ResourceTypeIndexBlob> catalog = default;
            if (SystemAPI.TryGetSingleton(out ResourceTypeIndex resourceIndex) &&
                resourceIndex.Catalog.IsCreated)
            {
                catalog = resourceIndex.Catalog;
            }

            var handEntity = _handQuery.GetSingletonEntity();
            var handRW = SystemAPI.GetComponentRW<Hand>(handEntity);
            ref var hand = ref handRW.ValueRW;
            var resolved = SystemAPI.GetComponentRO<RightClickResolved>(handEntity).ValueRO;

            var secondaryHeld = input.SecondaryHeld;
            var cooldownReady = elapsedSeconds >= hand.CooldownUntilSeconds;
            if (!secondaryHeld || !cooldownReady || !resolved.HasHandler)
            {
                return;
            }

            switch (resolved.Handler)
            {
                case HandRightClickHandler.PileSiphon:
                    HandlePileSiphon(ref state, resolved, ref hand, handEntity, deltaTime, takeBuffer, catalog);
                    break;
                case HandRightClickHandler.GroundDrip:
                    HandleGroundDrip(resolved, ref hand, handEntity, deltaTime, addBuffer, catalog);
                    break;
            }
        }

        private void HandlePileSiphon(ref SystemState state,
            RightClickResolved resolved,
            ref Hand hand,
            Entity handEntity,
            float deltaTime,
            DynamicBuffer<AggregatePileTakeCommand> takeBuffer,
            BlobAssetReference<ResourceTypeIndexBlob> catalog)
        {
            if (resolved.Target == Entity.Null || !_pileLookup.HasComponent(resolved.Target))
            {
                return;
            }

            if (hand.HeldAmount >= hand.HeldCapacity)
            {
                return;
            }

            var pile = _pileLookup[resolved.Target];
            if (pile.Amount <= 0f)
            {
                return;
            }

            if (hand.HasHeldType)
            {
                if (!ResourceTypeCatalogUtility.TryGetResourceTypeIndex(hand.HeldType, catalog, out var handIndex) ||
                    handIndex != pile.ResourceTypeIndex)
                {
                    return;
                }
            }

            var missing = hand.HeldCapacity - hand.HeldAmount;
            if (missing <= 0)
            {
                return;
            }

            var siphonRate = math.max(1f, hand.DumpRatePerSecond) * deltaTime;
            var requested = math.min(missing, siphonRate);
            if (requested <= 0.01f)
            {
                return;
            }

            takeBuffer.Add(new AggregatePileTakeCommand
            {
                Requester = handEntity,
                Pile = resolved.Target,
                Amount = requested
            });
        }

        private void HandleGroundDrip(RightClickResolved resolved,
            ref Hand hand,
            Entity handEntity,
            float deltaTime,
            DynamicBuffer<AggregatePileAddCommand> addBuffer,
            BlobAssetReference<ResourceTypeIndexBlob> catalog)
        {
            if (!hand.HasHeldType || hand.HeldAmount <= 0)
            {
                return;
            }

            if (!ResourceTypeCatalogUtility.TryGetResourceTypeIndex(hand.HeldType, catalog, out var resourceTypeIndex))
            {
                return;
            }

            var dumpRate = math.max(1f, hand.DumpRatePerSecond) * deltaTime;
            var amount = math.min(hand.HeldAmount, dumpRate);
            if (amount <= 0.01f)
            {
                return;
            }

            addBuffer.Add(new AggregatePileAddCommand
            {
                Requester = handEntity,
                Position = resolved.HitPosition,
                ResourceTypeIndex = resourceTypeIndex,
                Amount = amount,
                PreferredPile = resolved.Target,
                MergeRadiusOverride = hand.SiphonRange,
                Flags = AggregatePileAddFlags.None
            });
        }
    }
}
