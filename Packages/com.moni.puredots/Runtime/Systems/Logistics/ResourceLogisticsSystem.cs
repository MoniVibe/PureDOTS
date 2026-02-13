using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Logistics;
using PureDOTS.Runtime.Logistics.Components;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resources;
using PureDOTS.Runtime.Scenarios;
using PureDOTS.Runtime.Time;
using PureDOTS.Systems;

namespace PureDOTS.Systems.Logistics
{
    /// <summary>
    /// System that manages resource logistics orders and shipments.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct ResourceLogisticsSystem : ISystem
    {
        private static readonly FixedString64Bytes DeliveredPerTickMetricKey = new FixedString64Bytes("deliveredPerTick");

        private ComponentLookup<Shipment> _shipmentLookup;
        private double _cumulativeDeliveredAmount;
        private uint _deliverySamples;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _shipmentLookup = state.GetComponentLookup<Shipment>(true);
            _cumulativeDeliveredAmount = 0.0;
            _deliverySamples = 0;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
                return;

            var timeState = SystemAPI.GetSingleton<TimeState>();
            _shipmentLookup.Update(ref state);

            foreach (var (order, entity) in SystemAPI.Query<RefRW<LogisticsOrder>>().WithEntityAccess())
            {
                var currentOrder = order.ValueRO;
                if (currentOrder.ShipmentEntity == Entity.Null ||
                    !_shipmentLookup.HasComponent(currentOrder.ShipmentEntity))
                {
                    continue;
                }

                var shipment = _shipmentLookup[currentOrder.ShipmentEntity];
                switch (shipment.Status)
                {
                    case ShipmentStatus.Created:
                    case ShipmentStatus.Loading:
                        SetStatusIfDifferent(ref order.ValueRW, LogisticsOrderStatus.Dispatched);
                        break;
                    case ShipmentStatus.InTransit:
                    case ShipmentStatus.Unloading:
                    case ShipmentStatus.Rerouting:
                        SetStatusIfDifferent(ref order.ValueRW, LogisticsOrderStatus.InTransit);
                        break;
                    case ShipmentStatus.Delivered:
                        SetStatusIfDifferent(ref order.ValueRW, LogisticsOrderStatus.Delivered);
                        break;
                    case ShipmentStatus.Failed:
                        SetStatusIfDifferent(ref order.ValueRW, LogisticsOrderStatus.Failed);
                        break;
                }
            }

            var deliveredThisTick = 0.0;
            foreach (var receipts in SystemAPI.Query<DynamicBuffer<DeliveryReceipt>>())
            {
                for (int i = 0; i < receipts.Length; i++)
                {
                    if (receipts[i].DeliveryTick == timeState.Tick)
                    {
                        deliveredThisTick += receipts[i].DeliveredAmount;
                    }
                }
            }

            _deliverySamples++;
            _cumulativeDeliveredAmount += deliveredThisTick;

            var deliveredPerTick = _deliverySamples > 0
                ? _cumulativeDeliveredAmount / _deliverySamples
                : 0.0;
            ScenarioMetricsUtility.SetMetric(state.EntityManager, DeliveredPerTickMetricKey, deliveredPerTick);
        }

        [BurstCompile]
        private static void SetStatusIfDifferent(ref LogisticsOrder order, LogisticsOrderStatus desiredStatus)
        {
            if (order.Status != desiredStatus)
            {
                order.Status = desiredStatus;
            }
        }
    }
}
