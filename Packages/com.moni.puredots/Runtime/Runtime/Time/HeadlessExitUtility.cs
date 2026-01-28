using System;
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
                    Environment.StackTrace);
            }

            using var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<HeadlessExitRequest>());
            var entity = query.IsEmptyIgnoreFilter
                ? entityManager.CreateEntity(typeof(HeadlessExitRequest))
                : query.GetSingletonEntity();

            entityManager.SetComponentData(entity, new HeadlessExitRequest
            {
                ExitCode = exitCode,
                RequestedTick = tick
            });
        }
    }
}
