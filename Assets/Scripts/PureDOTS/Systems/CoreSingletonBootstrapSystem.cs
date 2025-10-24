using PureDOTS.Runtime.Components;
using Unity.Entities;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Ensures the core deterministic singletons exist even without authoring data.
    /// Runs once at startup so downstream systems can safely require these components.
    /// </summary>
    [UpdateInGroup(typeof(TimeSystemGroup), OrderFirst = true)]
    public partial struct CoreSingletonBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            EnsureSingletons(state.EntityManager);
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            // No-op; this system only seeds singleton entities on create.
        }

        public static void EnsureSingletons(EntityManager entityManager)
        {
            using (var timeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeState>()))
            {
                if (timeQuery.IsEmptyIgnoreFilter)
                {
                    var entity = entityManager.CreateEntity(typeof(TimeState));
                    entityManager.SetComponentData(entity, new TimeState
                    {
                        FixedDeltaTime = TimeSettingsDefaults.FixedDeltaTime,
                        CurrentSpeedMultiplier = TimeSettingsDefaults.DefaultSpeedMultiplier,
                        Tick = 0,
                        IsPaused = TimeSettingsDefaults.PauseOnStart
                    });
                }
            }

            using (var historyQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<HistorySettings>()))
            {
                if (historyQuery.IsEmptyIgnoreFilter)
                {
                    var entity = entityManager.CreateEntity(typeof(HistorySettings));
                    entityManager.SetComponentData(entity, HistorySettingsDefaults.CreateDefault());
                }
            }

            Entity rewindEntity;
            using (var rewindQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RewindState>()))
            {
                if (rewindQuery.IsEmptyIgnoreFilter)
                {
                    rewindEntity = entityManager.CreateEntity(typeof(RewindState));
                    entityManager.SetComponentData(rewindEntity, new RewindState
                    {
                        Mode = RewindMode.Record,
                        StartTick = 0,
                        TargetTick = 0,
                        PlaybackTick = 0,
                        PlaybackTicksPerSecond = HistorySettingsDefaults.DefaultTicksPerSecond,
                        ScrubDirection = 0,
                        ScrubSpeedMultiplier = 1f
                    });
                }
                else
                {
                    rewindEntity = rewindQuery.GetSingletonEntity();
                }
            }

            if (!entityManager.HasBuffer<TimeControlCommand>(rewindEntity))
            {
                entityManager.AddBuffer<TimeControlCommand>(rewindEntity);
            }

            // For compatibility with previous behaviour, ensure the system would be disabled after seeding.
        }
    }
}
