using PureDOTS.Runtime.Components.Events;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Systems.Events
{
    /// <summary>
    /// Base pattern for event-driven systems.
    /// Example usage:
    /// [BurstCompile]
    /// public partial struct MyEventSystem : ISystem
    /// {
    ///     public void OnUpdate(ref SystemState state)
    ///     {
    ///         foreach (var (trigger, entity) in SystemAPI.Query<RefRO<EventTrigger>>()
    ///             .WithChangeFilter<EventTrigger>()
    ///             .WithEntityAccess())
    ///         {
    ///             HandleEvent(trigger.ValueRO, entity);
    ///         }
    ///     }
    /// }
    /// </summary>
    public static class EventDrivenSystemPattern
    {
        // This is a documentation/pattern class - actual systems implement the pattern directly
    }
}

