using Godgame.Camera;
using Godgame.Presentation;
using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Presentation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Interaction.Input
{
    /// <summary>
    /// Tracks the currently selected band and allocates deterministic band ids for spawned bands.
    /// </summary>
    public struct BandSelectionState : IComponentData
    {
        public Entity SelectedBand;
        public int NextBandId;
    }

    /// <summary>
    /// Consumes click/effect input and spawns band aggregates that flow through the band registry.
    /// Also enqueues presentation effect requests when Q is pressed.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InputReaderSystem))]
    [UpdateBefore(typeof(Godgame.Registry.GodgameBandSyncSystem))]
    public partial struct BandSpawnInputSystem : ISystem
    {
        private const float DefaultSpawnDistance = 8f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputState>();
            state.RequireForUpdate<BandRegistry>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            EnsureSelectionSingleton(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            var input = SystemAPI.GetSingleton<InputState>();
            bool spawnRequested = input.PrimaryClicked;
            bool effectRequested = input.EffectTriggered;

            if (!spawnRequested && !effectRequested)
            {
                return;
            }

            if (!SystemAPI.TryGetSingletonRW<BandSelectionState>(out var selection))
            {
                return;
            }

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            float3 spawnPosition;
            if (input.PointerWorldValid)
            {
                spawnPosition = input.PointerWorld;
            }
            else if (SystemAPI.TryGetSingleton<CameraTransform>(out var camera))
            {
                var forward = math.mul(camera.Rotation, new float3(0f, 0f, 1f));
                spawnPosition = camera.Position + forward * DefaultSpawnDistance;
            }
            else
            {
                spawnPosition = float3.zero;
            }

            if (spawnRequested)
            {
                var bandEntity = ecb.CreateEntity();

                var bandId = selection.ValueRO.NextBandId > 0 ? selection.ValueRO.NextBandId : 1;

                ecb.AddComponent(bandEntity, new BandId
                {
                    Value = bandId,
                    FactionId = 0,
                    Leader = Entity.Null
                });

                ecb.AddComponent(bandEntity, new BandStats
                {
                    MemberCount = 8,
                    AverageDiscipline = 55f,
                    Morale = 70f,
                    Cohesion = 60f,
                    Fatigue = 0f,
                    Flags = BandStatusFlags.Idle,
                    LastUpdateTick = 0
                });

                ecb.AddComponent(bandEntity, new BandFormation
                {
                    Formation = BandFormationType.Line,
                    Spacing = 1.6f,
                    Width = 4f,
                    Depth = 2f,
                    Facing = math.forward(quaternion.identity),
                    Anchor = spawnPosition,
                    Stability = 1f,
                    LastSolveTick = 0
                });

                ecb.AddComponent(bandEntity, LocalTransform.FromPositionRotationScale(
                    spawnPosition,
                    quaternion.identity,
                    1f));

                selection.ValueRW = new BandSelectionState
                {
                    SelectedBand = bandEntity,
                    NextBandId = bandId + 1
                };
            }

            if (effectRequested)
            {
                var selected = selection.ValueRO.SelectedBand;
                if (selected != Entity.Null && SystemAPI.Exists(selected))
                {
                    float3 targetPosition = input.PointerWorldValid
                        ? input.PointerWorld
                        : ResolveTargetPosition(ref state, selected);

                    if (SystemAPI.TryGetSingletonEntity<PresentationCommandQueue>(out var queueEntity))
                    {
                        var effectBuffer = state.EntityManager.GetBuffer<PlayEffectRequest>(queueEntity);
                        effectBuffer.Add(new PlayEffectRequest
                        {
                            EffectId = GodgamePresentationIds.MiraclePingEffectId,
                            Target = selected,
                            Position = targetPosition,
                            Rotation = quaternion.identity,
                            DurationSeconds = 1.5f,
                            StyleOverride = PresentationStyleOverride.FromStyle(GodgamePresentationIds.MiraclePingStyle),
                            LifetimePolicy = PresentationLifetimePolicy.Timed,
                            AttachRule = PresentationAttachRule.World
                        });
                    }
                }
            }
        }

        private static float3 ResolveTargetPosition(ref SystemState state, Entity selected)
        {
            if (state.EntityManager.HasComponent<LocalTransform>(selected))
            {
                return state.EntityManager.GetComponentData<LocalTransform>(selected).Position;
            }

            return float3.zero;
        }

        private static void EnsureSelectionSingleton(ref SystemState state)
        {
            var query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<BandSelectionState>());
            if (query.IsEmptyIgnoreFilter)
            {
                var entity = state.EntityManager.CreateEntity(typeof(BandSelectionState));
                state.EntityManager.SetComponentData(entity, new BandSelectionState
                {
                    SelectedBand = Entity.Null,
                    NextBandId = 1
                });
            }

            query.Dispose();
        }
    }
}
