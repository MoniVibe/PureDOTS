using Unity.Entities;
using Unity.Collections;

namespace PureDOTS.Runtime.AI.AQL
{
    /// <summary>
    /// Tag component to mark entities for AQL queries by name.
    /// </summary>
    public struct AQLTag : IComponentData
    {
        public FixedString64Bytes Name;
    }
}
