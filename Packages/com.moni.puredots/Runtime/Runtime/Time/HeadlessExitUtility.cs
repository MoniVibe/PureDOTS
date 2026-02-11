using System;
using Unity.Entities;
using UnityEngine;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;

namespace PureDOTS.Runtime.Time
{
    public static class HeadlessExitUtility
    {
        private const string ExitOnResultEnv = "PUREDOTS_HEADLESS_EXIT_ON_RESULT";

        public static void Request(EntityManager entityManager, uint tick, int exitCode)
        {
            if (RuntimeMode.IsHeadless && Application.isBatchMode)
            {
                HeadlessExitState.SignalExit("HeadlessExitUtility");
            }

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

        public static bool ShouldExitOnResult(string legacyEnvVar = null)
        {
            if (IsTruthyEnv(ExitOnResultEnv))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(legacyEnvVar) && IsTruthyEnv(legacyEnvVar);
        }

        private static bool IsTruthyEnv(string name)
        {
            var value = global::System.Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim();
            return value == "1"
                   || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("on", StringComparison.OrdinalIgnoreCase);
        }
    }
}
