using Unity.Collections;

namespace PureDOTS.Runtime.Narrative
{
    /// <summary>
    /// Registry blob containing all narrative event definitions.
    /// </summary>
    public struct NarrativeEventRegistry
    {
        public BlobArray<NarrativeEventDef> Events;
    }
}

