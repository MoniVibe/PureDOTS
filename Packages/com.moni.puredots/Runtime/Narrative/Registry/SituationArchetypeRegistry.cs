using Unity.Collections;

namespace PureDOTS.Runtime.Narrative
{
    /// <summary>
    /// Registry blob containing all situation archetype definitions.
    /// </summary>
    public struct SituationArchetypeRegistry
    {
        public BlobArray<SituationArchetype> Archetypes;
    }
}

