using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Combat
{
    /// <summary>
    /// Helper utilities for selecting a weapon from a weapon pool.
    /// </summary>
    public static class WeaponPoolSelectionHelpers
    {
        public static bool TrySelectEntry(ref WeaponPoolConfig config, DynamicBuffer<WeaponPoolEntry> pool, uint seed, out int index)
        {
            index = -1;
            if (pool.Length == 0)
            {
                return false;
            }

            switch (config.SelectionMode)
            {
                case WeaponPoolSelectionMode.RoundRobin:
                    index = math.clamp(config.RoundRobinIndex, 0, pool.Length - 1);
                    config.RoundRobinIndex = (index + 1) % pool.Length;
                    return true;

                case WeaponPoolSelectionMode.FirstValid:
                    for (int i = 0; i < pool.Length; i++)
                    {
                        if (pool[i].Weight > 0f)
                        {
                            index = i;
                            return true;
                        }
                    }
                    return false;
            }

            float totalWeight = 0f;
            for (int i = 0; i < pool.Length; i++)
            {
                var entry = pool[i];
                if (entry.Weight <= 0f)
                {
                    continue;
                }

                totalWeight += entry.Weight * GetRoleBias(config, entry.Role);
            }

            if (totalWeight <= 0f)
            {
                index = 0;
                return true;
            }

            var rng = new Random(seed == 0u ? 1u : seed);
            var roll = rng.NextFloat(0f, totalWeight);
            var accum = 0f;

            for (int i = 0; i < pool.Length; i++)
            {
                var entry = pool[i];
                if (entry.Weight <= 0f)
                {
                    continue;
                }

                accum += entry.Weight * GetRoleBias(config, entry.Role);
                if (roll <= accum)
                {
                    index = i;
                    return true;
                }
            }

            index = pool.Length - 1;
            return true;
        }

        private static float GetRoleBias(in WeaponPoolConfig config, WeaponPoolRole role)
        {
            float bias = role switch
            {
                WeaponPoolRole.Primary => config.PrimaryBias,
                WeaponPoolRole.Secondary => config.SecondaryBias,
                WeaponPoolRole.PointDefense => config.PointDefenseBias,
                WeaponPoolRole.Support => config.SupportBias,
                WeaponPoolRole.Experimental => config.ExperimentalBias,
                _ => 1f
            };

            return bias > 0f ? bias : 1f;
        }
    }
}
