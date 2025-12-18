using Unity.Entities;

namespace Space4X.Presentation
{
    /// <summary>
    /// Marker component indicating an entity should be treated as a craft in presentation queries.
    /// </summary>
    public struct CraftPresentationTag : IComponentData
    {
    }

    /// <summary>
    /// Marker component indicating an entity represents a carrier for Space4X visuals.
    /// </summary>
    public struct CarrierPresentationTag : IComponentData
    {
    }

    /// <summary>
    /// Marker component added to asteroid entities so presentation systems can filter them.
    /// </summary>
    public struct AsteroidPresentationTag : IComponentData
    {
    }
}
