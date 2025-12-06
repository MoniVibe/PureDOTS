using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Bridges simulation state to presentation via event messages.
    /// Hot simulation writes to message buffers; cold presentation reads them.
    /// Never blocks simulation waiting for presentation updates.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(VillagerSystemGroup))]
    public partial struct PresentationBridgeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            var currentTick = tickTimeState.Tick;

            // Get or create message stream entity
            Entity streamEntity;
            if (!SystemAPI.TryGetSingletonEntity<SimToPresentationMessageStream>(out streamEntity))
            {
                streamEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<SimToPresentationMessageStream>(streamEntity);
                state.EntityManager.AddBuffer<SimToPresentationMessage>(streamEntity);
            }
            var messageBuffer = SystemAPI.GetBuffer<SimToPresentationMessage>(streamEntity);

            // Clear old messages (keep only last N ticks)
            const uint MessageRetentionTicks = 10;
            var minTick = currentTick > MessageRetentionTicks ? currentTick - MessageRetentionTicks : 0;
            
            for (int i = messageBuffer.Length - 1; i >= 0; i--)
            {
                if (messageBuffer[i].Tick < minTick)
                {
                    messageBuffer.RemoveAt(i);
                }
            }

            // Write villager state updates to message buffer
            foreach (var (transform, needs, aiState, entity) in SystemAPI.Query<
                         RefRO<LocalTransform>,
                         RefRO<VillagerNeeds>,
                         RefRO<VillagerAIState>>()
                         .WithEntityAccess())
            {
                var needsValue = needs.ValueRO;
                var aiValue = aiState.ValueRO;

                messageBuffer.Add(new SimToPresentationMessage
                {
                    Type = SimToPresentationMessage.MessageType.StateUpdate,
                    SourceEntity = entity,
                    Position = transform.ValueRO.Position,
                    State = (byte)aiValue.CurrentState,
                    HealthPercent = math.clamp(needsValue.Health / math.max(1f, needsValue.MaxHealth) * 100f, 0f, 100f),
                    HungerPercent = needsValue.HungerFloat,
                    EnergyPercent = needsValue.EnergyFloat,
                    Tick = currentTick
                });
            }
        }
    }

    /// <summary>
    /// Singleton entity holding the sim-to-presentation message stream.
    /// </summary>
    public struct SimToPresentationMessageStream : IComponentData
    {
        public uint LastProcessedTick;
    }
}

