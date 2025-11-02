using Unity.Entities;
using Unity.Mathematics;

namespace Space4X.CameraComponents
{
    /// <summary>
    /// Tracks accumulated mouse/scroll input so simulation catch-up ticks can amortize camera movement deterministically.
    /// </summary>
    public struct CameraInputBudget : IComponentData
    {
        public float2 RotateBudget;
        public float ZoomBudget;
        public int TicksToSpend;
        public uint BridgeFrameId;
    }
}




