using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Skills;
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Systems.Combat
{
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
                byte castingSkill = skillSet.GetLevel(SkillId.None);

                if (castingSkill > 70 && (implantTag.Flags & ImplantFlags.DualSynapse) != 0)
                {
                    ushort dualCastBehaviorId = (ushort)ActionId.DualCast;
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
