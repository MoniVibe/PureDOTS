using Unity.Entities;

namespace PureDOTS.Runtime.Modifiers
{
    /// <summary>
    /// Event buffer element for batched modifier application requests.
    /// Processed by ModifierEventApplicationSystem in EventSystemGroup.
    /// 
    /// USAGE:
    /// var coordinatorEntity = SystemAPI.GetSingletonEntity&lt;ModifierEventCoordinator&gt;();
    /// var events = SystemAPI.GetBuffer&lt;ApplyModifierEvent&gt;(coordinatorEntity);
    /// events.Add(new ApplyModifierEvent { Target = entity, ModifierId = id, ... });
    /// 
    /// See: Docs/Guides/ModifierSystemGuide.md for detailed examples.
    /// </summary>
    [InternalBufferCapacity(256)]
    public struct ApplyModifierEvent : IBufferElementData
    {
        /// <summary>
        /// Target entity to apply modifier to.
        /// </summary>
        public Entity Target;

        /// <summary>
        /// Modifier ID from catalog (ushort index).
        /// </summary>
        public ushort ModifierId;

        /// <summary>
        /// Modifier value override (0 = use BaseValue from catalog).
        /// </summary>
        public float Value;

        /// <summary>
        /// Duration in ticks (-1 = permanent).
        /// </summary>
        public short Duration;
    }

    /// <summary>
    /// Coordinator entity that holds the singleton ApplyModifierEvent buffer.
    /// Created by bootstrap system.
    /// </summary>
    public struct ModifierEventCoordinator : IComponentData
    {
    }
}

