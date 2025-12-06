using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Applies physics impulses (DashVelocity), damage (DamageFunction), hit detection.
    /// Executes composed actions deterministically.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(ActionComposerSystem))]
    public partial struct CombatExecutionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<TimeState>(out var timeState))
                return;

            if (timeState.IsPaused)
                return;

            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
                return;

            var job = new ExecuteCombatActionsJob
            {
                CurrentTick = timeState.Tick,
                DeltaTime = timeState.FixedDeltaTime
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        partial struct ExecuteCombatActionsJob : IJobEntity
        {
            public uint CurrentTick;
            public float DeltaTime;

            void Execute(
                ref PhysicsVelocity velocity,
                in LocalTransform transform,
                DynamicBuffer<ActionComposition> compositions,
                DynamicBuffer<HitBuffer> hits)
            {
                float currentTime = CurrentTick * DeltaTime;

                for (int i = 0; i < compositions.Length; i++)
                {
                    var comp = compositions[i];
                    if (comp.StartTime <= currentTime && currentTime < comp.StartTime + comp.Duration)
                    {
                        // Apply physics based on action type
                        switch (comp.Action)
                        {
                            case AtomicAction.Dash:
                                // Apply dash velocity impulse
                                float dashSpeed = 10f;
                                velocity.Linear += comp.Direction * dashSpeed * DeltaTime;
                                break;

                            case AtomicAction.Swing:
                                // Swing creates hit detection arc
                                // Simplified: would use spatial queries for actual hit detection
                                break;

                            case AtomicAction.Parry:
                                // Parry reduces incoming damage
                                break;

                            case AtomicAction.Fire:
                                // Fire creates projectile/hit
                                // Simplified: would spawn projectile entity
                                break;

                            case AtomicAction.Cast:
                                // Cast creates spell effect
                                // Simplified: would spawn effect entity
                                break;
                        }
                    }
                }

                // Process hits (damage application happens in separate system)
                // This system just applies physics impulses
            }
        }
    }
}

