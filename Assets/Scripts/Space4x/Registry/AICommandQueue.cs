using Unity.Entities;

namespace Space4X.Registry
{
    /// <summary>
    /// Minimal AI command queue element so buffer lookups compile.
    /// </summary>
    public struct AICommandQueue : IBufferElementData
    {
        public Entity Target;
        public byte CommandType;
    }
}
