using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Scenarios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Installs weapon mounts or spawners when build conditions are met.
    /// </summary>
    [UpdateInGroup(typeof(CombatSystemGroup), OrderFirst = true)]
    public partial struct WeaponInstallSystem : ISystem
    {
        private ComponentLookup<WeaponBuildBudget> _budgetLookup;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<WeaponInstallRequest>();
            state.RequireForUpdate<WeaponCatalog>();

            _budgetLookup = state.GetComponentLookup<WeaponBuildBudget>(false);
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

            if (!SystemAPI.TryGetSingleton<WeaponCatalog>(out var weaponCatalog) || !weaponCatalog.Catalog.IsCreated)
            {
                return;
            }

            _budgetLookup.Update(ref state);

            var entityManager = state.EntityManager;
            var currentTick = timeState.Tick;
            var currentTime = timeState.ElapsedTime;

            foreach (var (requests, entity) in SystemAPI.Query<DynamicBuffer<WeaponInstallRequest>>().WithEntityAccess())
            {
                if (requests.Length == 0)
                {
                    continue;
                }

                for (int i = requests.Length - 1; i >= 0; i--)
                {
                    var request = requests[i];

                    if (request.TriggerTick != 0 && currentTick < request.TriggerTick)
                    {
                        continue;
                    }

                    if (!HasWeaponSpec(weaponCatalog.Catalog, request.WeaponId))
                    {
                        continue;
                    }

                    if (!MeetsBudget(entity, request, currentTime))
                    {
                        continue;
                    }

                    bool installed;
                    if (request.Mode == WeaponInstallMode.Mount)
                    {
                        installed = InstallMount(entityManager, entity, request);
                        if (installed)
                        {
                            ScenarioMetricsUtility.AddMetric(entityManager, "weapon.install.mount_total", 1.0);
                        }
                    }
                    else
                    {
                        installed = InstallSpawner(entityManager, entity, request);
                        if (installed)
                        {
                            ScenarioMetricsUtility.AddMetric(entityManager, "weapon.install.spawner_total", 1.0);
                        }
                    }

                    if (installed)
                    {
                        ScenarioMetricsUtility.AddMetric(entityManager, "weapon.install.completed_total", 1.0);
                    }
                    else
                    {
                        ScenarioMetricsUtility.AddMetric(entityManager, "weapon.install.skipped_total", 1.0);
                    }

                    if (request.ConsumeBudget != 0 && _budgetLookup.HasComponent(entity))
                    {
                        var budget = _budgetLookup[entity];
                        budget.Energy = math.max(0f, budget.Energy - request.RequireEnergy);
                        budget.Materials = math.max(0f, budget.Materials - request.RequireMaterials);
                        budget.Crew = math.max(0f, budget.Crew - request.RequireCrew);
                        budget.LastSpendTime = currentTime;
                        _budgetLookup[entity] = budget;
                    }

                    requests.RemoveAtSwapBack(i);
                }
            }
        }

        private bool MeetsBudget(Entity entity, in WeaponInstallRequest request, float currentTime)
        {
            if (request.RequireEnergy <= 0f && request.RequireMaterials <= 0f && request.RequireCrew <= 0f)
            {
                return true;
            }

            if (!_budgetLookup.HasComponent(entity))
            {
                return false;
            }

            var budget = _budgetLookup[entity];
            if (budget.Energy < request.RequireEnergy)
            {
                return false;
            }
            if (budget.Materials < request.RequireMaterials)
            {
                return false;
            }
            if (budget.Crew < request.RequireCrew)
            {
                return false;
            }

            return true;
        }

        private bool InstallMount(EntityManager entityManager, Entity entity, in WeaponInstallRequest request)
        {
            var hasMount = entityManager.HasComponent<WeaponMount>(entity);
            if (hasMount && request.ReplaceExisting == 0)
            {
                return false;
            }

            var mount = new WeaponMount
            {
                WeaponId = request.WeaponId,
                TurretId = default,
                TargetEntity = request.TargetEntity,
                TargetPosition = request.TargetPosition,
                LastFireTime = -999f,
                HeatLevel = request.InitialHeat,
                EnergyReserve = request.InitialEnergy > 0f ? request.InitialEnergy : 1000f,
                IsFiring = true,
                ShotSequence = 0
            };

            if (hasMount)
            {
                entityManager.SetComponentData(entity, mount);
            }
            else
            {
                entityManager.AddComponentData(entity, mount);
            }

            ApplyAmmoComponents(entityManager, entity, request);
            return true;
        }

        private bool InstallSpawner(EntityManager entityManager, Entity entity, in WeaponInstallRequest request)
        {
            var hasSpawner = entityManager.HasComponent<WeaponSpawner>(entity);
            if (hasSpawner && request.ReplaceExisting == 0)
            {
                return false;
            }

            var spawner = new WeaponSpawner
            {
                WeaponId = request.WeaponId,
                AmmoId = request.AmmoId,
                TargetEntity = request.TargetEntity,
                TargetPosition = request.TargetPosition,
                FireDirection = request.FireDirection,
                AimMode = request.AimMode,
                LastFireTime = -999f,
                EnergyReserve = request.InitialEnergy > 0f ? request.InitialEnergy : 1000f,
                HeatLevel = request.InitialHeat,
                ShotSequence = 0,
                IsActive = 1
            };

            if (hasSpawner)
            {
                entityManager.SetComponentData(entity, spawner);
            }
            else
            {
                entityManager.AddComponentData(entity, spawner);
            }

            ApplyAmmoComponents(entityManager, entity, request);
            return true;
        }

        private static void ApplyAmmoComponents(EntityManager entityManager, Entity entity, in WeaponInstallRequest request)
        {
            var ammoId = request.AmmoId.Length > 0
                ? request.AmmoId
                : new FixedString32Bytes("ammo.standard");

            if (request.MagazineCapacity > 0)
            {
                var magazine = new WeaponMagazine
                {
                    AmmoType = ammoId,
                    Capacity = request.MagazineCapacity,
                    Current = request.MagazineCurrent > 0 ? request.MagazineCurrent : request.MagazineCapacity,
                    AmmoPerShot = request.AmmoPerShot > 0 ? request.AmmoPerShot : 1,
                    ReloadSec = request.ReloadSec > 0f ? request.ReloadSec : 1f,
                    LastReloadTime = -999f
                };

                if (entityManager.HasComponent<WeaponMagazine>(entity))
                {
                    entityManager.SetComponentData(entity, magazine);
                }
                else
                {
                    entityManager.AddComponentData(entity, magazine);
                }
            }

            if (request.StockpileCapacity > 0)
            {
                var stockpile = new AmmoStockpile
                {
                    AmmoType = ammoId,
                    Capacity = request.StockpileCapacity,
                    Current = request.StockpileCurrent > 0 ? request.StockpileCurrent : request.StockpileCapacity
                };

                if (entityManager.HasComponent<AmmoStockpile>(entity))
                {
                    entityManager.SetComponentData(entity, stockpile);
                }
                else
                {
                    entityManager.AddComponentData(entity, stockpile);
                }
            }
        }

        private static bool HasWeaponSpec(BlobAssetReference<WeaponCatalogBlob> catalog, FixedString64Bytes weaponId)
        {
            if (!catalog.IsCreated)
            {
                return false;
            }

            ref var weapons = ref catalog.Value.Weapons;
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i].Id.Equals(weaponId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
