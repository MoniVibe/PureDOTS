using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Components.Orbital;
using PureDOTS.Runtime.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace PureDOTS.Systems.Orbital
{
    /// <summary>
    /// Updates hierarchical frame transforms, caching world positions/orientations.
    /// Only recomputes when parent frame's quaternion delta exceeds threshold (default 0.001 rad).
    /// Avoids cascading quaternion recompositions every frame.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(LinearVelocityIntegrationSystem))]
    public partial struct FrameHierarchySystem : ISystem
    {
        private ComponentLookup<OrbitalFrame> _frameLookup;
        private ComponentLookup<FrameWorldTransform> _worldTransformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<OrbitalFrame>();
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            _frameLookup = state.GetComponentLookup<OrbitalFrame>(false);
            _worldTransformLookup = state.GetComponentLookup<FrameWorldTransform>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var tickTimeState = SystemAPI.GetSingleton<TickTimeState>();
            uint currentTick = tickTimeState.Tick;

            _frameLookup.Update(ref state);
            _worldTransformLookup.Update(ref state);

            // First pass: detect quaternion deltas and mark dirty frames
            var markDirtyJob = new MarkDirtyFramesJob
            {
                FrameLookup = _frameLookup,
                CurrentTick = currentTick
            };
            state.Dependency = markDirtyJob.ScheduleParallel(state.Dependency);

            // Second pass: update world transforms for all frames (cached computation)
            var updateTransformsJob = new UpdateWorldTransformsJob
            {
                FrameLookup = _frameLookup,
                WorldTransformLookup = _worldTransformLookup,
                CurrentTick = currentTick
            };
            state.Dependency = updateTransformsJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct MarkDirtyFramesJob : IJobEntity
        {
            public ComponentLookup<OrbitalFrame> FrameLookup;
            public uint CurrentTick;

            public void Execute(Entity entity, ref OrbitalFrame frame, in FrameParent parent)
            {
                if (parent.ParentFrameEntity == Entity.Null)
                {
                    // Root frame - always update
                    frame.PreviousOrientation = frame.Orientation;
                    return;
                }

                if (!FrameLookup.HasComponent(parent.ParentFrameEntity))
                {
                    return;
                }

                var parentFrame = FrameLookup[parent.ParentFrameEntity];
                quaternion parentDelta = math.mul(
                    math.inverse(parentFrame.PreviousOrientation),
                    parentFrame.Orientation
                );

                // Compute angle of rotation
                float angle = math.acos(math.clamp(math.dot(
                    parentFrame.PreviousOrientation.value,
                    parentFrame.Orientation.value
                ) * 2f - 1f, -1f, 1f));

                float threshold = frame.DeltaThreshold > 0f ? frame.DeltaThreshold : 0.001f;

                if (angle > threshold)
                {
                    // Mark this frame as dirty
                    frame.PreviousOrientation = frame.Orientation;
                }
            }
        }

        [BurstCompile]
        private partial struct UpdateWorldTransformsJob : IJobEntity
        {
            public ComponentLookup<OrbitalFrame> FrameLookup;
            public ComponentLookup<FrameWorldTransform> WorldTransformLookup;
            public uint CurrentTick;

            public void Execute(Entity entity, ref OrbitalFrame frame, in FrameParent parent, ref FrameWorldTransform worldTransform)
            {
                if (parent.ParentFrameEntity == Entity.Null)
                {
                    // Root frame: world = local
                    worldTransform.WorldPosition = frame.Origin;
                    worldTransform.WorldOrientation = frame.Orientation;
                    worldTransform.LastUpdateTick = CurrentTick;
                    return;
                }

                if (!FrameLookup.HasComponent(parent.ParentFrameEntity))
                {
                    return;
                }

                var parentFrame = FrameLookup[parent.ParentFrameEntity];

                // Get parent world transform
                float3 parentWorldPos = parentFrame.Origin;
                quaternion parentWorldRot = parentFrame.Orientation;

                if (WorldTransformLookup.HasComponent(parent.ParentFrameEntity))
                {
                    var parentWorld = WorldTransformLookup[parent.ParentFrameEntity];
                    parentWorldPos = parentWorld.WorldPosition;
                    parentWorldRot = parentWorld.WorldOrientation;
                }

                // Compute world transform: worldPos = parentRot * localPos + parentPos
                float3 worldPos = math.mul(parentWorldRot, frame.Origin) + parentWorldPos;
                quaternion worldRot = math.mul(parentWorldRot, frame.Orientation);

                worldTransform.WorldPosition = worldPos;
                worldTransform.WorldOrientation = worldRot;
                worldTransform.LastUpdateTick = CurrentTick;
            }
        }
    }
}

