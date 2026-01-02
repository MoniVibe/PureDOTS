using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using SystemEnvironment = System.Environment;

namespace PureDOTS.Runtime.Scenarios
{
    public static class HeadlessInvariantBundleWriter
    {
        private const string BundlePathEnvVar = "PUREDOTS_INVARIANT_BUNDLE_PATH";
        private static string s_cachedPath;
        private static bool s_pathResolved;

        public static bool TryWriteBundle(
            EntityManager entityManager,
            string code,
            string message,
            uint tick,
            float worldSeconds,
            Entity entity,
            bool hasEntity,
            float3 position,
            bool hasPosition,
            float3 velocity,
            bool hasVelocity,
            quaternion rotation,
            bool hasRotation)
        {
            if (!TryResolvePath(out var path))
            {
                return false;
            }

            try
            {
                var sb = new StringBuilder(512);
                sb.Append('{');
                sb.Append("\"code\":\"");
                AppendEscaped(sb, code);
                sb.Append("\",\"message\":\"");
                AppendEscaped(sb, message);
                sb.Append("\",\"tick\":");
                sb.Append(tick);
                sb.Append(",\"worldSeconds\":");
                sb.Append(worldSeconds.ToString("0.###", CultureInfo.InvariantCulture));

                if (TryGetScenarioInfo(entityManager, out var info))
                {
                    sb.Append(",\"scenarioId\":\"");
                    AppendEscaped(sb, info.ScenarioId.ToString());
                    sb.Append("\",\"seed\":");
                    sb.Append(info.Seed);
                    sb.Append(",\"runTicks\":");
                    sb.Append(info.RunTicks);
                }

                if (hasEntity)
                {
                    sb.Append(",\"entity\":{");
                    sb.Append("\"index\":");
                    sb.Append(entity.Index);
                    sb.Append(",\"version\":");
                    sb.Append(entity.Version);
                    sb.Append('}');
                }

                if (hasPosition)
                {
                    AppendVector3(sb, "position", position);
                }

                if (hasVelocity)
                {
                    AppendVector3(sb, "velocity", velocity);
                }

                if (hasRotation)
                {
                    AppendQuaternion(sb, "rotation", rotation);
                }

                sb.Append('}');

                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HeadlessInvariantBundleWriter] Failed to write invariant bundle: {ex.Message}");
                return false;
            }
        }

        private static bool TryResolvePath(out string path)
        {
            if (!s_pathResolved)
            {
                s_cachedPath = SystemEnvironment.GetEnvironmentVariable(BundlePathEnvVar);
                s_pathResolved = true;
            }

            path = s_cachedPath;
            return !string.IsNullOrWhiteSpace(path);
        }

        private static bool TryGetScenarioInfo(EntityManager entityManager, out ScenarioInfo info)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ScenarioInfo>());
            if (query.IsEmptyIgnoreFilter)
            {
                info = default;
                return false;
            }

            info = query.GetSingleton<ScenarioInfo>();
            return true;
        }

        private static void AppendVector3(StringBuilder sb, string name, float3 value)
        {
            sb.Append(",\"").Append(name).Append("\":{");
            sb.Append("\"x\":").Append(value.x.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(",\"y\":").Append(value.y.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(",\"z\":").Append(value.z.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append('}');
        }

        private static void AppendQuaternion(StringBuilder sb, string name, quaternion value)
        {
            sb.Append(",\"").Append(name).Append("\":{");
            sb.Append("\"x\":").Append(value.value.x.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(",\"y\":").Append(value.value.y.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(",\"z\":").Append(value.value.z.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(",\"w\":").Append(value.value.w.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append('}');
        }

        private static void AppendEscaped(StringBuilder sb, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (ch < ' ')
                        {
                            sb.Append("\\u");
                            sb.Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                        break;
                }
            }
        }
    }
}
