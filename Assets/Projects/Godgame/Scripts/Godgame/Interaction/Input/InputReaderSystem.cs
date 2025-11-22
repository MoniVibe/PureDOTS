using Godgame.Interaction;
using Unity.Entities;
using Unity.Mathematics;

namespace Godgame.Interaction.Input
{
    /// <summary>
    /// Mirrors high-priority Mono snapshots into the DOTS InputState singleton so DOTS systems remain informed.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct InputReaderSystem : ISystem
    {
        private bool _previousPrimaryHeld;
        private bool _previousEffectTriggered;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var inputEntity = SystemAPI.GetSingletonEntity<InputState>();
            var inputStateRW = SystemAPI.GetComponentRW<InputState>(inputEntity);
            ref var inputState = ref inputStateRW.ValueRW;

            if (Godgame.Camera.GodgameCameraInputBridge.TryGetSnapshot(out var snapshot))
            {
                inputState.PointerPos = new float2(snapshot.PointerPosition.x, snapshot.PointerPosition.y);
                inputState.PointerDelta = new float2(snapshot.PointerDelta.x, snapshot.PointerDelta.y);
                inputState.PointerWorld = snapshot.PointerWorld;
                inputState.PointerWorldValid = snapshot.HasPointerWorld;
                inputState.Scroll = snapshot.Scroll;
                inputState.PrimaryHeld = snapshot.PrimaryHeld;
                inputState.PrimaryClicked = snapshot.PrimaryHeld && !_previousPrimaryHeld;
                inputState.SecondaryHeld = snapshot.SecondaryHeld;
                inputState.MiddleHeld = snapshot.MiddleHeld;
                inputState.ThrowModifier = snapshot.ThrowModifier;
                inputState.EffectTriggered = snapshot.EffectPressed && !_previousEffectTriggered;
                inputState.Move = new float2(snapshot.Move.x, snapshot.Move.y);
                inputState.Vertical = snapshot.Vertical;
                inputState.CameraToggleMode = snapshot.ToggleMode;

                _previousPrimaryHeld = snapshot.PrimaryHeld;
                _previousEffectTriggered = snapshot.EffectPressed;
            }
            else
            {
                inputState = default;
                _previousPrimaryHeld = false;
                _previousEffectTriggered = false;
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}

