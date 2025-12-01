#if PUREDOTS_LEGACY_CAMERA
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Hand;
using PureDOTS.Input;
using PureDOTS.Runtime.Camera;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Input
{
    /// <summary>
    /// Maps device-level input (edges + continuous state) to gameplay intent.
    /// Produces GodIntent component deterministically, making rebinding/device variety trivial.
    /// Single-writer: only this system writes GodIntent.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CameraInputSystemGroup))]
    [UpdateAfter(typeof(CopyInputToEcsSystem))]
    public partial struct IntentMappingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            uint currentTick = timeState.Tick;

            // Process hand intent
            foreach (var (handInput, handEdges, intentRef, handEntity) in SystemAPI
                .Query<RefRO<DivineHandInput>, DynamicBuffer<HandInputEdge>, RefRW<GodIntent>>()
                .WithEntityAccess())
            {
                var input = handInput.ValueRO;
                ref var intent = ref intentRef.ValueRW;

                // Reset intent flags
                intent = new GodIntent
                {
                    LastUpdateTick = currentTick,
                    PlayerId = input.PlayerId,
                    SelectPosition = input.CursorWorldPosition
                };

                // Process edge events
                for (int i = 0; i < handEdges.Length; i++)
                {
                    var edge = handEdges[i];
                    if (edge.Tick != currentTick)
                    {
                        continue; // Only process edges from current tick
                    }

                    if (edge.Button == InputButton.Primary)
                    {
                        if (edge.Kind == InputEdgeKind.Down)
                        {
                            intent.StartSelect = 1;
                        }
                        else if (edge.Kind == InputEdgeKind.Up)
                        {
                            // Up event means confirm or cancel based on charge
                            if (input.ThrowCharge > 0.3f) // Min charge threshold
                            {
                                intent.ConfirmPlace = 1;
                            }
                            else
                            {
                                intent.CancelAction = 1;
                            }
                        }
                    }
                    else if (edge.Button == InputButton.Secondary)
                    {
                        if (edge.Kind == InputEdgeKind.Down)
                        {
                            intent.StartSelect = 1; // Secondary can also start selection / throw prime
                        }
                        else if (edge.Kind == InputEdgeKind.Up)
                        {
                            if (input.ThrowCharge > 0.1f)
                            {
                                intent.ConfirmPlace = 1;
                            }
                            else
                            {
                                intent.CancelAction = 1;
                            }
                        }
                    }
                }

                // Update continuous state from input
                intent.SelectPosition = input.CursorWorldPosition;

                // Gate by UI focus
                if (input.PointerOverUI == 1)
                {
                    // Clear intents when over UI
                    intent.StartSelect = 0;
                    intent.ConfirmPlace = 0;
                }
            }

            // Process camera intent
            foreach (var (cameraInput, cameraEdges, intentRef, cameraEntity) in SystemAPI
                .Query<RefRO<CameraInputState>, DynamicBuffer<CameraInputEdge>, RefRW<GodIntent>>()
                .WithEntityAccess())
            {
                var input = cameraInput.ValueRO;
                ref var intent = ref intentRef.ValueRW;

                // Reset camera intents (preserve PlayerId)
                intent.PlayerId = input.PlayerId;
                intent.PanIntent = float2.zero;
                intent.ZoomIntent = 0f;
                intent.OrbitIntent = float2.zero;
                intent.StartPan = 0;
                intent.StopPan = 0;
                intent.StartOrbit = 0;
                intent.StopOrbit = 0;
                intent.FreeMoveIntent = float2.zero;
                intent.VerticalMoveIntent = 0f;
                intent.CameraYAxisUnlocked = input.YAxisUnlocked;

                // Process edge events
                for (int i = 0; i < cameraEdges.Length; i++)
                {
                    var edge = cameraEdges[i];
                    if (edge.Tick != currentTick)
                    {
                        continue;
                    }

                    if (edge.Button == InputButton.Middle)
                    {
                        if (edge.Kind == InputEdgeKind.Down)
                        {
                            intent.StartOrbit = 1;
                        }
                        else if (edge.Kind == InputEdgeKind.Up)
                        {
                            intent.StopOrbit = 1;
                        }
                    }
                    else if (edge.Button == InputButton.Primary)
                    {
                        if (edge.Kind == InputEdgeKind.Down)
                        {
                            intent.StartPan = 1;
                        }
                        else if (edge.Kind == InputEdgeKind.Up)
                        {
                            intent.StopPan = 1;
                        }
                    }
                }

                // Update continuous camera state
                intent.OrbitIntent = input.OrbitDelta;
                intent.PanIntent = input.PanDelta;
                intent.ZoomIntent = input.ZoomDelta;
                intent.FreeMoveIntent = input.MoveInput;
                intent.VerticalMoveIntent = input.VerticalMove;

                // Gate by UI focus
                if (input.PointerOverUI == 1)
                {
                    // Clear camera intents when over UI (unless explicitly allowed)
                    intent.PanIntent = float2.zero;
                    intent.OrbitIntent = float2.zero;
                    intent.FreeMoveIntent = float2.zero;
                    intent.VerticalMoveIntent = 0f;
                }
            }
        }
    }
}
#endif
