using Unity.Entities;

namespace PureDOTS.Runtime.AI.AQL
{
    /// <summary>
    /// Buffer element to hold results of AQL queries.
    /// </summary>
    public struct AQLResultElement : IBufferElementData
    {
        public Entity Entity;
    }
}
