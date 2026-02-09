using System;
using Unity.Entities;
using UnityEngine;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Time
{
    public static class HeadlessExitUtility
    {
        private const string ExitOnResultEnv = "PUREDOTS_HEADLESS_EXIT_ON_RESULT";

        public static void Request(EntityManager entityManager, uint tick, int exitCode)
        {
            if (exitCode != 0)
            {
                Debug.LogError(
                    $"[HeadlessExitUtility] Request exit_code={exitCode} tick={tick}\n" +
                    System.Environment.StackTrace);
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
            var value = Environment.GetEnvironmentVariable(name);
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
