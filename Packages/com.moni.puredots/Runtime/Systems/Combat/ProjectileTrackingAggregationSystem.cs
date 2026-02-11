using PureDOTS.Runtime.Combat;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Aggregates projectile tracking events into counters and manages buffer retention.
    /// </summary>
    [UpdateInGroup(typeof(CombatSystemGroup), OrderLast = true)]
    public partial struct ProjectileTrackingAggregationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectileTrackingHub>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var hubEntity = SystemAPI.GetSingletonEntity<ProjectileTrackingHub>();
            var config = SystemAPI.GetComponentRW<ProjectileTrackingConfig>(hubEntity);
            var counters = SystemAPI.GetComponentRW<ProjectileTrackingCounters>(hubEntity);
            var events = SystemAPI.GetBuffer<ProjectileTrackingEvent>(hubEntity);
            var ammoCounters = SystemAPI.GetBuffer<ProjectileTrackingAmmoCounter>(hubEntity);

            int startIndex = config.ValueRO.ClearEachFrame != 0
                ? 0
                : math.clamp(counters.ValueRO.LastProcessedIndex, 0, events.Length);

            for (int i = startIndex; i < events.Length; i++)
            {
                var evt = events[i];
                UpdateAmmoCounters(ref ammoCounters, evt);
                switch (evt.Kind)
                {
                    case ProjectileTrackingEventKind.Spawn:
                        counters.ValueRW.Spawned++;
                        break;
                    case ProjectileTrackingEventKind.Hit:
                        counters.ValueRW.Hits++;
                        break;
                    case ProjectileTrackingEventKind.Deflect:
                        counters.ValueRW.Deflections++;
                        break;
                    case ProjectileTrackingEventKind.Redirect:
                        counters.ValueRW.Redirects++;
                        break;
                    case ProjectileTrackingEventKind.Control:
                        counters.ValueRW.Controls++;
                        break;
                    case ProjectileTrackingEventKind.Retire:
                        counters.ValueRW.Retired++;
                        break;
                    case ProjectileTrackingEventKind.Expire:
                        counters.ValueRW.Expired++;
                        break;
                    case ProjectileTrackingEventKind.Recycle:
                        counters.ValueRW.Recycled++;
                        break;
                }
            }

            if (config.ValueRO.MaxEvents > 0 && events.Length > config.ValueRO.MaxEvents)
            {
                var excess = events.Length - config.ValueRO.MaxEvents;
                if (excess > 0)
                {
                    events.RemoveRange(0, excess);
                    if (config.ValueRO.ClearEachFrame == 0)
                    {
                        counters.ValueRW.LastProcessedIndex = math.max(0, counters.ValueRW.LastProcessedIndex - excess);
                    }
                }
            }

            if (config.ValueRO.ClearEachFrame != 0)
            {
                events.Clear();
                counters.ValueRW.LastProcessedIndex = 0;
            }
            else
            {
                counters.ValueRW.LastProcessedIndex = events.Length;
            }
        }

        private static void UpdateAmmoCounters(ref DynamicBuffer<ProjectileTrackingAmmoCounter> ammoCounters, in ProjectileTrackingEvent evt)
        {
            var ammoId = evt.AmmoId;
            if (ammoId.Length == 0)
            {
                ammoId = new FixedString32Bytes("ammo.unknown");
            }

            var index = FindOrAddAmmoCounter(ref ammoCounters, ammoId);
            var counter = ammoCounters[index];

            switch (evt.Kind)
            {
                case ProjectileTrackingEventKind.Spawn:
                    counter.Spawned++;
                    break;
                case ProjectileTrackingEventKind.Hit:
                    counter.Hits++;
                    break;
                case ProjectileTrackingEventKind.Deflect:
                    counter.Deflections++;
                    break;
                case ProjectileTrackingEventKind.Redirect:
                    counter.Redirects++;
                    break;
                case ProjectileTrackingEventKind.Control:
                    counter.Controls++;
                    break;
                case ProjectileTrackingEventKind.Retire:
                    counter.Retired++;
                    break;
                case ProjectileTrackingEventKind.Expire:
                    counter.Expired++;
                    break;
                case ProjectileTrackingEventKind.Recycle:
                    counter.Recycled++;
                    break;
            }

            ammoCounters[index] = counter;
        }

        private static int FindOrAddAmmoCounter(ref DynamicBuffer<ProjectileTrackingAmmoCounter> ammoCounters, FixedString32Bytes ammoId)
        {
            for (int i = 0; i < ammoCounters.Length; i++)
            {
                if (ammoCounters[i].AmmoId.Equals(ammoId))
                {
                    return i;
                }
            }

            var entry = new ProjectileTrackingAmmoCounter
            {
                AmmoId = ammoId
            };
            ammoCounters.Add(entry);
            return ammoCounters.Length - 1;
        }
    }
}
