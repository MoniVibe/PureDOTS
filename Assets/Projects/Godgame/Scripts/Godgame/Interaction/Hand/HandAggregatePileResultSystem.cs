using Godgame.Resources;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Godgame.Interaction.HandSystems
{
    /// <summary>
    /// Applies aggregate pile command results back to the hand state.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Godgame.Systems.AggregatePileSystem))]
    public partial struct HandAggregatePileResultSystem : ISystem
    {
        private EntityQuery _handQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AggregatePileCommandState>();
            _handQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<Hand>()
                }
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_handQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var handEntity = _handQuery.GetSingletonEntity();
            var handRW = SystemAPI.GetComponentRW<Hand>(handEntity);
            ref var hand = ref handRW.ValueRW;

            var hasCarryEvents = SystemAPI.HasBuffer<HandCarryingChangedEvent>(handEntity);
            DynamicBuffer<HandCarryingChangedEvent> carryEvents = default;
            if (hasCarryEvents)
            {
                carryEvents = SystemAPI.GetBuffer<HandCarryingChangedEvent>(handEntity);
            }

            BlobAssetReference<ResourceTypeIndexBlob> catalog = default;
            if (SystemAPI.TryGetSingleton(out ResourceTypeIndex resourceIndex) && resourceIndex.Catalog.IsCreated)
            {
                catalog = resourceIndex.Catalog;
            }

            var commandEntity = SystemAPI.GetSingletonEntity<AggregatePileCommandState>();
            var resultBuffer = state.EntityManager.GetBuffer<AggregatePileCommandResult>(commandEntity);

            if (resultBuffer.Length == 0)
            {
                return;
            }

            var changed = false;

            for (var i = resultBuffer.Length - 1; i >= 0; i--)
            {
                var result = resultBuffer[i];
                if (result.Requester != handEntity)
                {
                    continue;
                }

                switch (result.Type)
                {
                    case AggregatePileCommandResultType.TakeCompleted:
                    case AggregatePileCommandResultType.TakePartial:
                    {
                        var added = (int)math.round(result.Amount);
                        if (added > 0)
                        {
                            if (!hand.HasHeldType &&
                                ResourceTypeCatalogUtility.TryResolveResourceType(result.ResourceTypeIndex, catalog, out var newType))
                            {
                                hand.HasHeldType = true;
                                hand.HeldType = newType;
                            }

                            hand.HeldAmount = math.min(hand.HeldCapacity, hand.HeldAmount + added);
                            changed = true;
                        }
                        break;
                    }

                    case AggregatePileCommandResultType.AddAccepted:
                    case AggregatePileCommandResultType.AddRejected:
                    {
                        var removed = (int)math.round(result.Amount);
                        if (removed > 0)
                        {
                            hand.HeldAmount = math.max(0, hand.HeldAmount - removed);
                            if (hand.HeldAmount == 0)
                            {
                                hand.HasHeldType = false;
                                hand.HeldType = ResourceType.None;
                            }
                            changed = true;
                        }
                        break;
                    }
                }

                resultBuffer.RemoveAtSwapBack(i);
            }

            if (changed && hasCarryEvents)
            {
                carryEvents.Add(new HandCarryingChangedEvent
                {
                    HasResource = hand.HasHeldType && hand.HeldAmount > 0,
                    Type = hand.HeldType,
                    Amount = hand.HeldAmount,
                    Capacity = hand.HeldCapacity
                });
            }
        }
    }
}
