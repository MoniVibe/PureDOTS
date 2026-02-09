using Unity.Entities;
using UnityEngine;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Time
{
    public static class HeadlessExitUtility
    {
        public static void Request(EntityManager entityManager, uint tick, int exitCode)
        {
            if (exitCode != 0)
            {
                Debug.LogError(
                    $"[HeadlessExitUtility] Request exit_code={exitCode} tick={tick}\n" +
                    System.Environment.StackTrace);
            }

            using var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<HeadlessExitRequest>());
            if (query.IsEmptyIgnoreFilter)
            {
                var created = entityManager.CreateEntity(typeof(HeadlessExitRequest));
                entityManager.SetComponentData(created, new HeadlessExitRequest
                {
                    ExitCode = exitCode,
                    RequestedTick = tick
                });
                return;
            }

            var entity = query.GetSingletonEntity();
            var existing = entityManager.GetComponentData<HeadlessExitRequest>(entity);
            var newExitCode = existing.ExitCode;
            var newTick = existing.RequestedTick;
            var update = false;

            // Only upgrade exit code (success -> failure); never downgrade or overwrite.
            if (existing.ExitCode == 0 && exitCode != 0)
            {
                newExitCode = exitCode;
                update = true;
            }

            // Preserve the first observed exit tick when possible.
            if (newTick == 0 && tick != 0)
            {
                newTick = tick;
                update = true;
            }

            if (update)
            {
                entityManager.SetComponentData(entity, new HeadlessExitRequest
                {
                    ExitCode = newExitCode,
                    RequestedTick = newTick
                });
            }
        }
    }
}
