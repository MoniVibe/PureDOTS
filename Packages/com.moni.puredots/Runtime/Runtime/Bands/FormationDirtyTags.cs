using Unity.Entities;

namespace PureDOTS.Runtime.Bands
{
    /// <summary>
    /// Tag component indicating formation command has changed and needs recalculation.
    /// </summary>
    public struct FormationCommandDirtyTag : IComponentData
    {
    }

    /// <summary>
    /// Tag component indicating morale needs batch update.
    /// </summary>
    public struct MoraleDirtyTag : IComponentData
    {
    }
}

