using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Placeholder registry definitions for future faction/empire support.
    /// </summary>
    public struct FactionRegistry : IComponentData { } // TODO: define faction summary fields

    public struct FactionRegistryEntry : IBufferElementData
    {
        // TODO: add faction identity, territory, economy metrics
    }

    /// <summary>
    /// Placeholder registry for global climate / hazard events.
    /// </summary>
    public struct ClimateHazardRegistry : IComponentData { } // TODO: add aggregate hazard data

    public struct ClimateHazardRegistryEntry : IBufferElementData
    {
        // TODO: add hazard type, intensity, affected region, duration, spatial info
    }

    /// <summary>
    /// Placeholder registry for area-based effects (buffs, slow fields, time manipulation).
    /// </summary>
    public struct AreaEffectRegistry : IComponentData { } // TODO: add aggregate effect counters/timestamps

    public struct AreaEffectRegistryEntry : IBufferElementData
    {
        // TODO: add effect id, owner, radius, modifiers, spatial metadata
    }

    /// <summary>
    /// Placeholder registry for culture/alignment/outlook state.
    /// </summary>
    public struct CultureAlignmentRegistry : IComponentData { } // TODO: add global culture metrics

    public struct CultureAlignmentRegistryEntry : IBufferElementData
    {
        // TODO: add culture id, alignment scores, affinity modifiers
    }
}
