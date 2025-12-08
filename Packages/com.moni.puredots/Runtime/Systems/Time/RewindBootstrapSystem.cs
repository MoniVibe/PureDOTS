using Unity.Burst;
using Unity.Entities;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Systems.Time
{
    /// <summary>
    /// Ensures a default RewindState singleton exists so systems relying on it can run without exceptions.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct RewindBootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // If a RewindState already exists, do nothing.
            if (SystemAPI.TryGetSingleton<RewindState>(out _))
            {
                state.Enabled = false;
                return;
            }

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new RewindState
            {
                Mode = RewindMode.Idle,
                CurrentTick = 0,
                TargetTick = 0,
                PlaybackSpeed = 1f,
                StartTick = 0,
                PlaybackTick = 0,
                PlaybackTicksPerSecond = 60f,
                ScrubDirection = ScrubDirection.None,
                ScrubSpeedMultiplier = 1f,
                RewindWindowTicks = 0,
                ActiveTrack = default
            });

            // Disable after seeding; no per-frame work required.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            // No runtime work; creation is handled in OnCreate.
        }
    }
}
