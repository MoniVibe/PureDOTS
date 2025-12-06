using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Tactical
{
    /// <summary>
    /// Processes formation commands and computes desired positions for formation members.
    /// Runs at tactical frequency (1-5 Hz) via PeriodicTickComponent throttling.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(TacticalSystemGroup), OrderFirst = true)]
    public partial struct FormationCommandSystem : ISystem
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

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Process formations with commands (only if dirty or command changed)
            foreach (var (formationEntity, formationCommand, formation, bandStats, transform) in SystemAPI
                         .Query<Entity, RefRO<FormationCommand>, RefRW<BandFormation>, RefRO<BandStats>, RefRO<LocalTransform>>()
                         .WithAny<FormationCommandDirtyTag>()
                         .WithEntityAccess())
            {
                var command = formationCommand.ValueRO;
                var formationValue = formation.ValueRO;
                var stats = bandStats.ValueRO;

                // Compute formation center and facing
                var center = transform.ValueRO.Position;
                var forward = math.normalizesafe(command.Facing, formationValue.Facing);
                var right = math.cross(forward, new float3(0, 1, 0));
                right = math.normalizesafe(right);

                // Compute member positions based on formation type
                var memberCount = math.max(1, stats.MemberCount);
                var spacing = formationValue.Spacing > 0f ? formationValue.Spacing : 1.5f;

                // Get members from BandMember buffer
                if (SystemAPI.HasBuffer<BandMember>(formationEntity))
                {
                    var members = SystemAPI.GetBuffer<BandMember>(formationEntity);
                    var memberIndex = 0;

                    for (int i = 0; i < members.Length && memberIndex < memberCount; i++)
                    {
                        var memberEntity = members[i].Villager;
                        if (memberEntity == Entity.Null || !SystemAPI.Exists(memberEntity))
                        {
                            continue;
                        }

                        // Compute offset based on formation type and member index
                        var offset = ComputeFormationOffset(
                            formationValue.Formation,
                            memberIndex,
                            memberCount,
                            spacing,
                            forward,
                            right);

                        // Update or add FormationMember component
                        if (SystemAPI.HasComponent<FormationMember>(memberEntity))
                        {
                            var member = SystemAPI.GetComponentRW<FormationMember>(memberEntity);
                            member.ValueRW = new FormationMember
                            {
                                FormationEntity = formationEntity,
                                Offset = offset,
                                Alignment = 1.0f // Will be updated by deviation system
                            };
                        }
                        else
                        {
                            ecb.AddComponent(memberEntity, new FormationMember
                            {
                                FormationEntity = formationEntity,
                                Offset = offset,
                                Alignment = 1.0f
                            });
                        }

                        memberIndex++;
                    }
                }

                // Update formation cohesion (average alignment will be computed separately)
                formation.ValueRW = new BandFormation
                {
                    Formation = formationValue.Formation,
                    Spacing = formationValue.Spacing,
                    Width = formationValue.Width,
                    Depth = formationValue.Depth,
                    Facing = forward,
                    Anchor = center,
                    Stability = formationValue.Stability,
                    LastSolveTick = currentTick,
                    Cohesion = formationValue.Cohesion,
                    Morale = formationValue.Morale,
                    FormationId = formationValue.FormationId
                };

                // Remove dirty tag after processing
                ecb.RemoveComponent<FormationCommandDirtyTag>(formationEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private static float3 ComputeFormationOffset(
            BandFormationType formationType,
            int memberIndex,
            int memberCount,
            float spacing,
            float3 forward,
            float3 right)
        {
            switch (formationType)
            {
                case BandFormationType.Line:
                    // Single row, members spread horizontally
                    var lineWidth = (memberCount - 1) * spacing;
                    var lineOffset = (memberIndex - (memberCount - 1) * 0.5f) * spacing;
                    return right * lineOffset;

                case BandFormationType.Column:
                    // Single column, members in a line forward
                    var columnOffset = memberIndex * spacing;
                    return forward * columnOffset;

                case BandFormationType.Wedge:
                    // V-formation: wider at front, narrower at back
                    var rows = (int)math.ceil(math.sqrt(memberCount));
                    var row = memberIndex / rows;
                    var col = memberIndex % rows;
                    var rowWidth = (rows - 1) * spacing * (1f - row * 0.2f); // Narrower at back
                    var colOffset = (col - (rows - 1) * 0.5f) * spacing * (1f - row * 0.2f);
                    return forward * (row * spacing) + right * colOffset;

                case BandFormationType.Circle:
                    // Circular formation around center
                    var angle = (memberIndex / (float)memberCount) * 2f * math.PI;
                    var radius = spacing * math.sqrt(memberCount / math.PI);
                    return new float3(
                        math.cos(angle) * radius,
                        0f,
                        math.sin(angle) * radius);

                default:
                    return float3.zero;
            }
        }
    }
}

