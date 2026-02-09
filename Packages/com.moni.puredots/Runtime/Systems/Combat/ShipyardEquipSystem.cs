using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Scenarios;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Processes shipyard equip requests and queues weapon installs on target entities.
    /// </summary>
    [UpdateInGroup(typeof(CombatSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(WeaponInstallSystem))]
    public partial struct ShipyardEquipSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<WeaponPoolConfig> _poolConfigLookup;
        private BufferLookup<WeaponPoolEntry> _poolLookup;
        private ComponentLookup<PersistentId> _persistentLookup;
        private ComponentLookup<ShipyardBuildBudget> _shipyardBudgetLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<Shipyard>();
            state.RequireForUpdate<ShipyardEquipRequest>();

            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _poolConfigLookup = state.GetComponentLookup<WeaponPoolConfig>(false);
            _poolLookup = state.GetBufferLookup<WeaponPoolEntry>(true);
            _persistentLookup = state.GetComponentLookup<PersistentId>(true);
            _shipyardBudgetLookup = state.GetComponentLookup<ShipyardBuildBudget>(false);
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

            _transformLookup.Update(ref state);
            _poolConfigLookup.Update(ref state);
            _poolLookup.Update(ref state);
            _persistentLookup.Update(ref state);
            _shipyardBudgetLookup.Update(ref state);

            var entityManager = state.EntityManager;
            var currentTime = timeState.ElapsedTime;
            var currentTick = timeState.Tick;

            foreach (var (shipyard, requests, shipyardEntity) in SystemAPI.Query<RefRW<Shipyard>, DynamicBuffer<ShipyardEquipRequest>>().WithEntityAccess())
            {
                if (shipyard.ValueRO.IsActive == 0 || requests.Length == 0)
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

                    if (request.InstallEntity == Entity.Null)
                    {
                        requests.RemoveAtSwapBack(i);
                        ScenarioMetricsUtility.AddMetric(entityManager, "shipyard.request.invalid_total", 1.0);
                        continue;
                    }

                    if (shipyard.ValueRO.InstallCooldownSec > 0f &&
                        currentTime - shipyard.ValueRO.LastInstallTime < shipyard.ValueRO.InstallCooldownSec)
                    {
                        continue;
                    }

                    if (shipyard.ValueRO.Range > 0f &&
                        !IsWithinRange(shipyardEntity, request.InstallEntity, shipyard.ValueRO.Range))
                    {
                        continue;
                    }

                    if (!MeetsShipyardBudget(shipyardEntity, request))
                    {
                        continue;
                    }

                    if (!TryResolveEntry(shipyardEntity, request, currentTick, currentTime, out var entry))
                    {
                        continue;
                    }

                    var installRequest = BuildInstallRequest(entry, request, currentTick);
                    WeaponSeedingHelpers.QueueInstall(entityManager, request.InstallEntity, installRequest);
                    ScenarioMetricsUtility.AddMetric(entityManager, "shipyard.install.queued_total", 1.0);

                    shipyard.ValueRW.LastInstallTime = currentTime;
                    ConsumeShipyardBudget(shipyardEntity, request, currentTime);
                    requests.RemoveAtSwapBack(i);
                }
            }
        }

        private bool IsWithinRange(Entity shipyardEntity, Entity targetEntity, float range)
        {
            if (!_transformLookup.HasComponent(shipyardEntity) || !_transformLookup.HasComponent(targetEntity))
            {
                return true;
            }

            var shipyardPos = _transformLookup[shipyardEntity].Position;
            var targetPos = _transformLookup[targetEntity].Position;
            return math.distance(shipyardPos, targetPos) <= range;
        }

        private bool TryResolveEntry(
            Entity shipyardEntity,
            in ShipyardEquipRequest request,
            uint currentTick,
            float currentTime,
            out WeaponPoolEntry entry)
        {
            entry = default;
            if (request.WeaponId.Length > 0)
            {
                entry = new WeaponPoolEntry
                {
                    WeaponId = request.WeaponId,
                    AmmoId = request.AmmoId,
                    Role = WeaponPoolRole.Primary,
                    InstallMode = request.InstallMode,
                    AimMode = request.AimMode,
                    Weight = 1f,
                    RequireEnergy = request.RequireEnergy,
                    RequireMaterials = request.RequireMaterials,
                    RequireCrew = request.RequireCrew,
                    ConsumeBudget = request.ConsumeBudget,
                    ReplaceExisting = request.ReplaceExisting,
                    InitialEnergy = request.InitialEnergy,
                    InitialHeat = request.InitialHeat,
                    MagazineCapacity = request.MagazineCapacity,
                    MagazineCurrent = request.MagazineCurrent,
                    AmmoPerShot = request.AmmoPerShot,
                    ReloadSec = request.ReloadSec,
                    StockpileCapacity = request.StockpileCapacity,
                    StockpileCurrent = request.StockpileCurrent
                };
                return true;
            }

            var poolEntity = request.UseTargetPool != 0 ? request.InstallEntity : shipyardEntity;
            if (!_poolLookup.HasBuffer(poolEntity))
            {
                return false;
            }

            var pool = _poolLookup[poolEntity];
            if (pool.Length == 0)
            {
                return false;
            }

            var poolConfig = _poolConfigLookup.HasComponent(poolEntity)
                ? _poolConfigLookup[poolEntity]
                : default;

            if (poolConfig.MaxSelections > 0 && poolConfig.SelectionsMade >= poolConfig.MaxSelections)
            {
                return false;
            }

            if (poolConfig.MinIntervalSec > 0f &&
                currentTime - poolConfig.LastSelectTime < poolConfig.MinIntervalSec)
            {
                return false;
            }

            var seed = ComputeSeed(poolEntity, currentTick, poolConfig.SelectionsMade);
            if (!WeaponPoolSelectionHelpers.TrySelectEntry(ref poolConfig, pool, seed, out var index))
            {
                return false;
            }

            entry = pool[index];
            if (_poolConfigLookup.HasComponent(poolEntity))
            {
                poolConfig.SelectionsMade++;
                poolConfig.LastSelectTime = currentTime;
                _poolConfigLookup[poolEntity] = poolConfig;
            }

            return true;
        }

        private bool MeetsShipyardBudget(Entity shipyardEntity, in ShipyardEquipRequest request)
        {
            if (request.ShipyardRequireEnergy <= 0f && request.ShipyardRequireMaterials <= 0f && request.ShipyardRequireCrew <= 0f)
            {
                return true;
            }

            if (!_shipyardBudgetLookup.HasComponent(shipyardEntity))
            {
                return false;
            }

            var budget = _shipyardBudgetLookup[shipyardEntity];
            if (budget.Energy < request.ShipyardRequireEnergy)
            {
                return false;
            }
            if (budget.Materials < request.ShipyardRequireMaterials)
            {
                return false;
            }
            if (budget.Crew < request.ShipyardRequireCrew)
            {
                return false;
            }

            return true;
        }

        private void ConsumeShipyardBudget(Entity shipyardEntity, in ShipyardEquipRequest request, float currentTime)
        {
            if (request.ConsumeShipyardBudget == 0 || !_shipyardBudgetLookup.HasComponent(shipyardEntity))
            {
                return;
            }

            var budget = _shipyardBudgetLookup[shipyardEntity];
            budget.Energy = math.max(0f, budget.Energy - request.ShipyardRequireEnergy);
            budget.Materials = math.max(0f, budget.Materials - request.ShipyardRequireMaterials);
            budget.Crew = math.max(0f, budget.Crew - request.ShipyardRequireCrew);
            budget.LastSpendTime = currentTime;
            _shipyardBudgetLookup[shipyardEntity] = budget;
        }

        private static WeaponInstallRequest BuildInstallRequest(in WeaponPoolEntry entry, in ShipyardEquipRequest request, uint currentTick)
        {
            return new WeaponInstallRequest
            {
                WeaponId = entry.WeaponId,
                AmmoId = entry.AmmoId.Length > 0 ? entry.AmmoId : request.AmmoId,
                TargetEntity = Entity.Null,
                TargetPosition = float3.zero,
                FireDirection = float3.zero,
                Mode = entry.InstallMode,
                AimMode = entry.AimMode,
                TriggerTick = currentTick,
                RequireEnergy = request.RequireEnergy > 0f ? request.RequireEnergy : entry.RequireEnergy,
                RequireMaterials = request.RequireMaterials > 0f ? request.RequireMaterials : entry.RequireMaterials,
                RequireCrew = request.RequireCrew > 0f ? request.RequireCrew : entry.RequireCrew,
                ConsumeBudget = request.ConsumeBudget != 0 ? request.ConsumeBudget : entry.ConsumeBudget,
                ReplaceExisting = request.ReplaceExisting != 0 ? request.ReplaceExisting : entry.ReplaceExisting,
                InitialEnergy = request.InitialEnergy > 0f ? request.InitialEnergy : entry.InitialEnergy,
                InitialHeat = request.InitialHeat > 0f ? request.InitialHeat : entry.InitialHeat,
                MagazineCapacity = request.MagazineCapacity > 0 ? request.MagazineCapacity : entry.MagazineCapacity,
                MagazineCurrent = request.MagazineCurrent > 0 ? request.MagazineCurrent : entry.MagazineCurrent,
                AmmoPerShot = request.AmmoPerShot > 0 ? request.AmmoPerShot : entry.AmmoPerShot,
                ReloadSec = request.ReloadSec > 0f ? request.ReloadSec : entry.ReloadSec,
                StockpileCapacity = request.StockpileCapacity > 0 ? request.StockpileCapacity : entry.StockpileCapacity,
                StockpileCurrent = request.StockpileCurrent > 0 ? request.StockpileCurrent : entry.StockpileCurrent
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
