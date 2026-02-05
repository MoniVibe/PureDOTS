using System;
using System.Collections.Generic;
using System.Threading;

namespace PureDOTS.Runtime.Core
{
    /// <summary>
    /// Lightweight runtime gate for bug-hunt runs (system isolation + shutdown audit).
    /// </summary>
    public static class BugHuntGate
    {
        private static int _initialized;
        private static bool _enabled;
        private static bool _shutdownAuditEnabled;
        private static readonly HashSet<string> DisabledFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static string _disabledRaw = string.Empty;

        public static bool IsEnabled
        {
            get
            {
                EnsureInitialized();
                return _enabled;
            }
        }

        public static bool ShutdownAuditEnabled
        {
            get
            {
                EnsureInitialized();
                return _shutdownAuditEnabled;
            }
        }

        public static string DisabledRaw
        {
            get
            {
                EnsureInitialized();
                return _disabledRaw;
            }
        }

        public static bool IsDisabled(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            EnsureInitialized();
            if (!_enabled)
            {
                return false;
            }

            return DisabledFlags.Contains("all") || DisabledFlags.Contains(token.Trim());
        }

        private static void EnsureInitialized()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
            {
                return;
            }

            var bugHuntEnv = Environment.GetEnvironmentVariable("PUREDOTS_BUGHUNT");
            var shutdownEnv = Environment.GetEnvironmentVariable("PUREDOTS_SHUTDOWN_AUDIT");
            var disabled = Environment.GetEnvironmentVariable("PUREDOTS_BUGHUNT_DISABLE") ?? string.Empty;

            _disabledRaw = disabled;
            _shutdownAuditEnabled = IsTruthy(bugHuntEnv) || IsTruthy(shutdownEnv) || !string.IsNullOrWhiteSpace(disabled);
            _enabled = IsTruthy(bugHuntEnv) || !string.IsNullOrWhiteSpace(disabled) || _shutdownAuditEnabled;

            if (string.IsNullOrWhiteSpace(disabled))
            {
                return;
            }

            var parts = disabled.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var token = parts[i].Trim();
                if (token.Length == 0)
                {
                    continue;
                }

                DisabledFlags.Add(token);
            }
        }

        private static bool IsTruthy(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value = value.Trim();
            return value.Equals("1", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                   || value.Equals("on", StringComparison.OrdinalIgnoreCase);
        }
    }
}
