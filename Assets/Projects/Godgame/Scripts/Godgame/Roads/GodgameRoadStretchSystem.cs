using Godgame.Roads;
using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Godgame.Systems
{
    /// <summary>
    /// Keeps road transforms in sync with their start/end anchors and applies stretch updates from divine-hand handles.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GodgameVillageRoadBootstrapSystem))]
    public partial struct GodgameRoadStretchSystem : ISystem
    {
        private ComponentLookup<GodgameRoadSegment> _roadLookup;
        private ComponentLookup<LocalTransform> _roadTransformLookup;
        private ComponentLookup<GodgameRoadHandle> _handleLookup;
        private ComponentLookup<LocalTransform> _handleTransformLookup;
        private ComponentLookup<HandHeldTag> _handHeldLookup;
        private ComponentLookup<GodgamePresentationBinding> _presentationLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _roadLookup = state.GetComponentLookup<GodgameRoadSegment>();
            _roadTransformLookup = state.GetComponentLookup<LocalTransform>();
            _handleLookup = state.GetComponentLookup<GodgameRoadHandle>();
            _handleTransformLookup = state.GetComponentLookup<LocalTransform>();
            _handHeldLookup = state.GetComponentLookup<HandHeldTag>(true);
            _presentationLookup = state.GetComponentLookup<GodgamePresentationBinding>();

            state.RequireForUpdate<GodgameRoadConfig>();
        }

        private void UpdateRoadBindingScale(Entity road, in GodgameRoadSegment segment, in GodgameRoadConfig config)
        {
            if (!_presentationLookup.HasComponent(road))
            {
                return;
            }

            var binding = _presentationLookup[road];
            var newScale = GodgameVillageRoadBootstrapSystem.ComputeScaleMultiplier(segment, config);
            if (math.abs(binding.ScaleMultiplier - newScale) > 0.01f)
            {
                binding.ScaleMultiplier = newScale;
                _presentationLookup[road] = binding;
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _roadLookup.Update(ref state);
            _roadTransformLookup.Update(ref state);
            _handleLookup.Update(ref state);
            _handleTransformLookup.Update(ref state);
            _handHeldLookup.Update(ref state);
            _presentationLookup.Update(ref state);

            var config = SystemAPI.GetSingleton<GodgameRoadConfig>();

            foreach (var (segment, transformRef, entity) in SystemAPI
                         .Query<RefRW<GodgameRoadSegment>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                // Ensure the stored transform matches the segment data (handles might mutate start/end directly).
                transformRef.ValueRW = GodgameVillageRoadBootstrapSystem.LocalTransformIdentityFromSegment(segment.ValueRO);
            }

            foreach (var (handle, transform, entity) in SystemAPI
                         .Query<RefRO<GodgameRoadHandle>, RefRW<LocalTransform>>()
                         .WithEntityAccess())
            {
                if (!_roadLookup.HasComponent(handle.ValueRO.Road))
                {
                    continue;
                }

                var segment = _roadLookup[handle.ValueRO.Road];
                var desiredPosition = handle.ValueRO.Endpoint == 0 ? segment.Start : segment.End;
                desiredPosition.y = segment.Start.y;

                if (_handHeldLookup.HasComponent(entity))
                {
                    // Clamp dragged position to ground plane and update road.
                    var newPos = transform.ValueRO.Position;
                    newPos.y = desiredPosition.y;

                    if (handle.ValueRO.Endpoint == 0)
                    {
                        segment.Start = newPos;
                    }
                    else
                    {
                        segment.End = newPos;
                    }

                    // Prevent degenerate length
                    if (math.lengthsq(segment.End - segment.Start) < 0.25f)
                    {
                        // Snap back if too short
                        if (handle.ValueRO.Endpoint == 0)
                        {
                            segment.Start = desiredPosition;
                        }
                        else
                        {
                            segment.End = desiredPosition;
                        }
                    }
                    else
                    {
                        _roadLookup[handle.ValueRO.Road] = segment;
                        _roadTransformLookup[handle.ValueRO.Road] =
                            GodgameVillageRoadBootstrapSystem.LocalTransformIdentityFromSegment(segment);
                        UpdateRoadBindingScale(handle.ValueRO.Road, segment, config);
                    }
                }
                else
                {
                    // Keep handle locked to the current endpoint when not held.
                    var handleTransform = transform.ValueRO;
                    handleTransform.Position = desiredPosition;
                    handleTransform.Scale = 1f;
                    transform.ValueRW = handleTransform;
                }
            }
        }
    }
}
