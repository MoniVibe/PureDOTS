using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Selects weapons from a pool and queues install requests (data-only).
    /// </summary>
    [UpdateInGroup(typeof(CombatSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(WeaponInstallSystem))]
    public partial struct WeaponPoolSelectionSystem : ISystem
    {
        private ComponentLookup<WeaponMount> _weaponMountLookup;
        private ComponentLookup<WeaponSpawner> _weaponSpawnerLookup;
        private ComponentLookup<PersistentId> _persistentLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<WeaponPoolConfig>();

            _weaponMountLookup = state.GetComponentLookup<WeaponMount>(true);
            _weaponSpawnerLookup = state.GetComponentLookup<WeaponSpawner>(true);
            _persistentLookup = state.GetComponentLookup<PersistentId>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            if (timeState.IsPaused)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<RewindState>(out var rewindState) || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _weaponMountLookup.Update(ref state);
            _weaponSpawnerLookup.Update(ref state);
            _persistentLookup.Update(ref state);

            var entityManager = state.EntityManager;
            var currentTime = timeState.ElapsedTime;
            var currentTick = timeState.Tick;

            foreach (var (config, pool, entity) in SystemAPI.Query<RefRW<WeaponPoolConfig>, DynamicBuffer<WeaponPoolEntry>>().WithEntityAccess())
            {
                if (config.ValueRO.AutoInstall == 0)
                {
                    continue;
                }

                if (pool.Length == 0)
                {
                    continue;
                }

                if (config.ValueRO.MaxSelections > 0 && config.ValueRO.SelectionsMade >= config.ValueRO.MaxSelections)
                {
                    continue;
                }

                if (config.ValueRO.MinIntervalSec > 0f &&
                    currentTime - config.ValueRO.LastSelectTime < config.ValueRO.MinIntervalSec)
                {
                    continue;
                }

                if (config.ValueRO.RequireNoWeapon != 0 &&
                    (_weaponMountLookup.HasComponent(entity) || _weaponSpawnerLookup.HasComponent(entity)))
                {
                    continue;
                }

                var seed = ComputeSeed(entity, currentTick, config.ValueRO.SelectionsMade);
                if (!WeaponPoolSelectionHelpers.TrySelectEntry(ref config.ValueRW, pool, seed, out var index))
                {
                    continue;
                }

                var entry = pool[index];
                WeaponSeedingHelpers.EnsureInstallBuffer(entityManager, entity);
                var buffer = entityManager.GetBuffer<WeaponInstallRequest>(entity);
                buffer.Add(BuildRequest(entry, currentTick, config.ValueRO));

                config.ValueRW.SelectionsMade++;
                config.ValueRW.LastSelectTime = currentTime;
            }
        }

        private static WeaponInstallRequest BuildRequest(in WeaponPoolEntry entry, uint currentTick, in WeaponPoolConfig config)
        {
            return new WeaponInstallRequest
            {
                WeaponId = entry.WeaponId,
                AmmoId = entry.AmmoId,
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                FireDirection = float3.zero,
                Mode = entry.InstallMode,
                AimMode = entry.AimMode,
                TriggerTick = currentTick,
                RequireEnergy = entry.RequireEnergy,
                RequireMaterials = entry.RequireMaterials,
                RequireCrew = entry.RequireCrew,
                ConsumeBudget = entry.ConsumeBudget != 0 ? entry.ConsumeBudget : config.ConsumeBudget,
                ReplaceExisting = entry.ReplaceExisting != 0 ? entry.ReplaceExisting : config.ReplaceExisting,
                InitialEnergy = entry.InitialEnergy,
                InitialHeat = entry.InitialHeat,
                MagazineCapacity = entry.MagazineCapacity,
                MagazineCurrent = entry.MagazineCurrent,
                AmmoPerShot = entry.AmmoPerShot,
                ReloadSec = entry.ReloadSec,
                StockpileCapacity = entry.StockpileCapacity,
                StockpileCurrent = entry.StockpileCurrent
            };
        }

        private uint ComputeSeed(Entity entity, uint tick, int selectionIndex)
        {
            var persistent = _persistentLookup.HasComponent(entity)
                ? _persistentLookup[entity].Value
                : (uint)entity.Index;

            var seed = 0x9E3779B9u;
            seed ^= persistent + 0x85ebca6bu;
            seed ^= tick * 0xC2B2AE35u;
            seed ^= (uint)selectionIndex * 0x27d4eb2du;
            seed ^= (uint)entity.Version * 0x165667b1u;
            return seed == 0u ? 1u : seed;
        }
    }
}
