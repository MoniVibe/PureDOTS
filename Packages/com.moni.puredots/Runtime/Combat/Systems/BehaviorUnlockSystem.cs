using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Skills;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Checks SkillSet thresholds + implant tags, unlocks behaviors, adds to BehaviorSet buffer.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(BehaviorGatingSystem))]
    public partial struct BehaviorUnlockSystem : ISystem
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

            var job = new UnlockBehaviorsJob
            {
                CurrentTick = timeState.Tick
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        partial struct UnlockBehaviorsJob : IJobEntity
        {
            public uint CurrentTick;

            void Execute(
                in SkillSet skillSet,
                in ImplantTag implantTag,
                DynamicBuffer<BehaviorSet> behaviorSet,
                DynamicBuffer<BehaviorUnlockEvent> unlockEvents)
            {
                // Check for new behavior unlocks based on skill thresholds
                // Simplified: check if casting skill > 0.7 and has DualSynapse implant
                byte castingSkill = skillSet.GetLevel(SkillId.None); // Would use actual casting skill ID
                
                if (castingSkill > 70 && (implantTag.Flags & ImplantFlags.DualSynapse) != 0)
                {
                    ushort dualCastBehaviorId = (ushort)ActionId.DualCast;
                    
                    // Check if already unlocked
                    bool alreadyUnlocked = false;
                    for (int i = 0; i < behaviorSet.Length; i++)
                    {
                        if (behaviorSet[i].BehaviorId == dualCastBehaviorId)
                        {
                            alreadyUnlocked = true;
                            break;
                        }
                    }

                    if (!alreadyUnlocked)
                    {
                        behaviorSet.Add(new BehaviorSet
                        {
                            BehaviorId = dualCastBehaviorId,
                            UnlockTick = CurrentTick
                        });

                        unlockEvents.Add(new BehaviorUnlockEvent
                        {
                            BehaviorId = dualCastBehaviorId,
                            UnlockTick = CurrentTick
                        });
                    }
                }

                // Additional unlock checks would go here
                // Example: strafe-shoot at skill >= 0.5
                if (castingSkill >= 50)
                {
                    ushort strafeShootId = (ushort)ActionId.StrafeShoot;
                    bool exists = false;
                    for (int i = 0; i < behaviorSet.Length; i++)
                    {
                        if (behaviorSet[i].BehaviorId == strafeShootId)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        behaviorSet.Add(new BehaviorSet
                        {
                            BehaviorId = strafeShootId,
                            UnlockTick = CurrentTick
                        });
                    }
                }
            }
        }
    }
}

