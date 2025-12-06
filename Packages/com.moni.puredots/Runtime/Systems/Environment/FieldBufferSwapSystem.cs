using PureDOTS.Environment;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Environment
{
    /// <summary>
    /// Swaps double-buffered field buffers at tick end to ensure deterministic reads.
    /// Runs last in EnvironmentSystemGroup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(EnvironmentSystemGroup), OrderLast = true)]
    public partial struct FieldBufferSwapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
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

            // Swap all double-buffered fields
            foreach (var (field, entity) in SystemAPI.Query<RefRW<DoubleBufferedField>>().WithEntityAccess())
            {
                var currentIndex = field.ValueRO.ReadBufferIndex;
                var nextIndex = (byte)(1 - currentIndex); // Toggle 0<->1
                field.ValueRW.ReadBufferIndex = nextIndex;
            }
        }
    }
}

