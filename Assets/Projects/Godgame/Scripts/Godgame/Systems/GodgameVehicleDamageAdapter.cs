using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Ships;
using PureDOTS.Systems;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Godgame.Systems
{
    /// <summary>
    /// Bridges HitEvent from projectile systems to directional damage system.
    /// 2D facing (Fore/Aft/Port/Starboard only) for Godgame vehicles.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ProjectileCollisionSystem))]
    [UpdateBefore(typeof(ResolveDirectionalDamageSystem))]
    public partial struct GodgameVehicleDamageAdapter : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var job = new GodgameVehicleDamageAdapterJob();
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct GodgameVehicleDamageAdapterJob : IJobEntity
        {
            void Execute(
                Entity entity,
                DynamicBuffer<ProjectileHitResult> hitResults,
                DynamicBuffer<HitEvent> hitEvents,
                in ProjectileEntity projectile)
            {
                // Convert ProjectileHitResult to HitEvent
                // For 2D vehicles, simplify to 4-way facing
                for (int i = 0; i < hitResults.Length; i++)
                {
                    var hitResult = hitResults[i];

                    // Determine damage kind from projectile spec
                    DamageKind damageKind = DamageKind.Kinetic; // Default

                    // Create hit event
                    hitEvents.Add(new HitEvent
                    {
                        TargetShip = hitResult.HitEntity,
                        WorldPos = hitResult.HitPosition,
                        WorldNormal = hitResult.HitNormal,
                        Kind = damageKind,
                        Damage = 10f, // TODO: Get from projectile spec
                        Seed = projectile.Seed ^ (uint)i
                    });
                }
            }
        }
    }
}

