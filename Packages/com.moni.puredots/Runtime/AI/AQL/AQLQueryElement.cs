using Unity.Entities;
using Unity.Collections;

namespace PureDOTS.Runtime.AI.AQL
{
    /// <summary>
    /// Buffer element to hold queued AQL queries for execution.
    /// </summary>
    public struct AQLQueryElement : IBufferElementData
    {
        public AQLQuery Query;
    }
}
