using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Player input command for deterministic lockstep simulation.
    /// Commands are stored and processed by tick, not player state.
    /// Later, these will serialize directly across sockets.
    /// </summary>
    public struct InputCommand : IComponentData
    {
        public int Tick;
        public ulong PlayerId;
        public FixedBytes16 Payload;
    }

    /// <summary>
    /// Buffer element for queuing input commands.
    /// Commands are processed by tick in InputCommandProcessorSystem.
    /// </summary>
    [InternalBufferCapacity(64)]
    public struct InputCommandBuffer : IBufferElementData
    {
        public int Tick;
        public ulong PlayerId;
        public FixedBytes16 Payload;
    }

    /// <summary>
    /// Tag component marking the singleton entity that owns the input command queue.
    /// </summary>
    public struct InputCommandQueueTag : IComponentData { }

    /// <summary>
    /// State tracking for input command processing.
    /// </summary>
    public struct InputCommandState : IComponentData
    {
        public int LastProcessedTick;
        public int CommandCount;
        public int DroppedCommandCount;
    }

    /// <summary>
    /// Configuration for input delay and quantization.
    /// InputDelayTicks: Number of ticks to delay input processing (default 2 for lockstep).
    /// </summary>
    public struct InputDelayConfig : IComponentData
    {
        public int InputDelayTicks;
        
        public static InputDelayConfig Default => new InputDelayConfig
        {
            InputDelayTicks = 2
        };
    }
}

