using PureDOTS.Runtime.Groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems.Groups
{
    /// <summary>
    /// Ensures group entities have morale contract profile/state and transition buffer.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct GroupMoraleContractEnsureSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GroupMetrics>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<GroupMetrics>>()
                         .WithNone<GroupMoraleContractProfile>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, GroupMoraleContractProfile.Default);
            }

            foreach (var (_, entity) in SystemAPI.Query<RefRO<GroupMetrics>>()
                         .WithNone<GroupMoraleContractState>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new GroupMoraleContractState
                {
                    Morale01 = 0f,
                    Cohesion01 = 0f,
                    Pressure01 = 0f,
                    AnchorSecurity01 = 0.5f,
                    GoalCommitment01 = 0f,
                    Phase = GroupMoralePhase.Steady,
                    Intent = GroupMoraleIntent.Hold,
                    Influences = GroupMoraleInfluence.None,
                    LastUpdatedTick = 0,
                    PhaseChangedTick = 0
                });
            }

            foreach (var (_, entity) in SystemAPI.Query<RefRO<GroupMetrics>>()
                         .WithNone<GroupMoraleTransitionEvent>()
                         .WithEntityAccess())
            {
                ecb.AddBuffer<GroupMoraleTransitionEvent>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
