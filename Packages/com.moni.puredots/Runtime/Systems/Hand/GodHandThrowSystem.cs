using PureDOTS.Input;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace PureDOTS.Systems.Hand
{
    /// <summary>
    /// Component storing throw mode state for god-hand.
    /// </summary>
    public struct GodHandThrowState : IComponentData
    {
        public bool ThrowModeEnabled;
        public Entity HeldEntity;
    }

    /// <summary>
    /// Buffer element for queued throw launches.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct GodHandLaunchQueueElement : IBufferElementData
    {
        public Entity HeldEntity;
        public float3 FrozenPosition;
        public float3 LaunchDirection;
        public float LaunchStrength;
    }

    /// <summary>
    /// Processes god-hand throw mode commands (toggle, queue, launch).
    /// Manages throw mode state and launch queue.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GodHandThrowSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RtsInputSingletonTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity rtsInputEntity = SystemAPI.GetSingletonEntity<RtsInputSingletonTag>();

            if (!state.EntityManager.HasBuffer<GodHandCommandEvent>(rtsInputEntity))
            {
                return;
            }

            // Ensure god-hand throw state singleton exists
            Entity godHandEntity;
            if (!SystemAPI.TryGetSingletonEntity<GodHandThrowState>(out godHandEntity))
            {
                godHandEntity = state.EntityManager.CreateEntity(typeof(GodHandThrowState));
                state.EntityManager.SetComponentData(godHandEntity, new GodHandThrowState
                {
                    ThrowModeEnabled = false,
                    HeldEntity = Entity.Null
                });
            }

            var throwState = state.EntityManager.GetComponentData<GodHandThrowState>(godHandEntity);
            var commandBuffer = state.EntityManager.GetBuffer<GodHandCommandEvent>(rtsInputEntity);

            // Ensure launch queue buffer exists
            if (!state.EntityManager.HasBuffer<GodHandLaunchQueueElement>(godHandEntity))
            {
                state.EntityManager.AddBuffer<GodHandLaunchQueueElement>(godHandEntity);
            }

            var launchQueue = state.EntityManager.GetBuffer<GodHandLaunchQueueElement>(godHandEntity);

            for (int i = 0; i < commandBuffer.Length; i++)
            {
                var command = commandBuffer[i];
                ProcessGodHandCommand(ref state, ref throwState, launchQueue, command);
            }

            state.EntityManager.SetComponentData(godHandEntity, throwState);
            commandBuffer.Clear();
        }

        private void ProcessGodHandCommand(ref SystemState state, ref GodHandThrowState throwState, DynamicBuffer<GodHandLaunchQueueElement> launchQueue, GodHandCommandEvent command)
        {
            switch (command.Kind)
            {
                case GodHandCommandKind.ToggleThrowMode:
                    throwState.ThrowModeEnabled = !throwState.ThrowModeEnabled;
                    break;

                case GodHandCommandKind.QueueOrLaunchHeld:
                    // This is handled by RMB release logic in throw mode
                    // For now, if we have a held entity, queue it
                    if (throwState.HeldEntity != Entity.Null && state.EntityManager.Exists(throwState.HeldEntity))
                    {
                        // Get entity position
                        float3 position = float3.zero;
                        if (state.EntityManager.HasComponent<LocalTransform>(throwState.HeldEntity))
                        {
                            position = state.EntityManager.GetComponentData<LocalTransform>(throwState.HeldEntity).Position;
                        }

                        launchQueue.Add(new GodHandLaunchQueueElement
                        {
                            HeldEntity = throwState.HeldEntity,
                            FrozenPosition = position,
                            LaunchDirection = new float3(0f, 1f, 0f), // Default upward
                            LaunchStrength = 10f
                        });

                        // Freeze entity (disable physics/AI)
                        // TODO: Add FrozenTag or disable physics components
                        throwState.HeldEntity = Entity.Null;
                    }
                    break;

                case GodHandCommandKind.LaunchNextQueued:
                    if (launchQueue.Length > 0)
                    {
                        var queued = launchQueue[0];
                        LaunchEntity(ref state, queued);
                        launchQueue.RemoveAt(0);
                    }
                    break;

                case GodHandCommandKind.LaunchAllQueued:
                    for (int i = 0; i < launchQueue.Length; i++)
                    {
                        LaunchEntity(ref state, launchQueue[i]);
                    }
                    launchQueue.Clear();
                    break;
            }
        }

        private void LaunchEntity(ref SystemState state, GodHandLaunchQueueElement queued)
        {
            if (!state.EntityManager.Exists(queued.HeldEntity))
            {
                return;
            }

            // Apply velocity/impulse to entity
            // TODO: Use Unity Physics velocity component or custom movement component
            // For now, this is a placeholder - games should implement actual physics launch
            if (state.EntityManager.HasComponent<PhysicsVelocity>(queued.HeldEntity))
            {
                var velocity = state.EntityManager.GetComponentData<PhysicsVelocity>(queued.HeldEntity);
                velocity.Linear = queued.LaunchDirection * queued.LaunchStrength;
                state.EntityManager.SetComponentData(queued.HeldEntity, velocity);
            }

            // Re-enable physics/AI
            // TODO: Remove FrozenTag or re-enable physics components
        }
    }
}



