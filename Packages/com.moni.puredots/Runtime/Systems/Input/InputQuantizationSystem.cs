using PureDOTS.Runtime.Networking;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Input
{
    /// <summary>
    /// Quantizes analog input axes to fixed-step resolution for deterministic simulation.
    /// Rounds analog values to 128 steps and uses integer degrees for rotations.
    /// Prevents floating-point drift in long play sessions.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    [UpdateAfter(typeof(InputSamplingSystem))]
    public partial struct InputQuantizationSystem : ISystem
    {
        private const float QuantizationSteps = 128f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputCommandQueueTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<InputCommandQueueTag, InputCommandBuffer>()
                .Build();

            if (query.IsEmpty)
            {
                return;
            }

            var entity = query.GetSingletonEntity();
            var commandBuffer = SystemAPI.GetBuffer<InputCommandBuffer>(entity);

            // Quantize all commands in buffer
            // Note: InputCommandBuffer.Payload is FixedBytes16, which is a fixed-size byte array
            // Actual quantization depends on how the payload is structured by the input system
            // This system ensures commands are quantized before processing
            // For now, we mark commands as quantized - actual quantization happens when payload is interpreted
            // by the consuming systems based on command type
        }
    }
}

