using System;
using PureDOTS.Runtime.Bands;
using PureDOTS.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Godgame.Registry
{
    /// <summary>
    /// Minimal villager summary data used by the Godgame registry bridge.
    /// </summary>
    public struct GodgameVillager : IComponentData
    {
        public FixedString64Bytes DisplayName;
        public int VillagerId;
        public int FactionId;
        public byte IsAvailable;
        public byte IsReserved;
        public float HealthPercent;
        public float MoralePercent;
        public float EnergyPercent;
        public VillagerJob.JobType JobType;
        public VillagerJob.JobPhase JobPhase;
        public VillagerDisciplineType Discipline;
        public byte DisciplineLevel;
        public byte IsCombatReady;
        public VillagerAIState.State AIState;
        public VillagerAIState.Goal AIGoal;
        public Entity CurrentTarget;
        public uint ActiveTicketId;
        public ushort CurrentResourceTypeIndex;
        public float Productivity;
    }

    /// <summary>
    /// Minimal storehouse summary data mirrored into the shared registry.
    /// </summary>
    public struct GodgameStorehouse : IComponentData
    {
        public FixedString64Bytes Label;
        public int StorehouseId;
        public float TotalCapacity;
        public float TotalStored;
        public float TotalReserved;
        public ushort PrimaryResourceTypeIndex;
        public uint LastMutationTick;
        public FixedList32Bytes<GodgameStorehouseResourceSummary> ResourceSummaries;
    }

    /// <summary>
    /// Per-resource capacity summary for a Godgame storehouse.
    /// </summary>
    public struct GodgameStorehouseResourceSummary
    {
        public ushort ResourceTypeIndex;
        public float Capacity;
        public float Stored;
        public float Reserved;
    }

    /// <summary>
    /// Mirror component caching resource node summary data prior to registry export.
    /// </summary>
    public struct GodgameResourceNodeMirror : IComponentData
    {
        public ushort ResourceTypeIndex;
        public float RemainingAmount;
        public float MaxAmount;
        public float RegenerationRate;
        public byte IsDepleted;
        public uint LastMutationTick;
    }

    /// <summary>
    /// Mirror component describing the state of a villager spawner before registry export.
    /// </summary>
    public struct GodgameSpawnerMirror : IComponentData
    {
        public FixedString64Bytes SpawnerTypeId;
        public int TotalCapacity;
        public int SpawnedCount;
        public int PendingSpawnCount;
        public float SpawnRadius;
        public VillagerJob.JobType DefaultJobType;
        public VillagerAIState.Goal DefaultAIGoal;
        public byte IsActive;
        public uint LastMutationTick;
    }

    /// <summary>
    /// Mirror component describing the tactical summary of a band for registry publishing and presentation.
    /// </summary>
    public struct GodgameBand : IComponentData
    {
        public FixedString64Bytes DisplayName;
        public int BandId;
        public int FactionId;
        public Entity Leader;
        public int MemberCount;
        public float Morale;
        public float Cohesion;
        public float AverageDiscipline;
        public float Fatigue;
        public BandStatusFlags StatusFlags;
        public BandFormationType Formation;
        public float FormationSpacing;
        public float FormationWidth;
        public float FormationDepth;
        public float3 Anchor;
        public float3 Facing;
    }

    /// <summary>
    /// Snapshot cached by the registry bridge so presentation systems can publish telemetry.
    /// </summary>
    public struct GodgameRegistrySnapshot : IComponentData
    {
        public int VillagerCount;
        public int AvailableVillagers;
        public int IdleVillagers;
        public int ReservedVillagers;
        public int CombatReadyVillagers;
        public float AverageVillagerHealth;
        public float AverageVillagerMorale;
        public float AverageVillagerEnergy;
        public int StorehouseCount;
        public float TotalStorehouseCapacity;
        public float TotalStorehouseStored;
        public float TotalStorehouseReserved;
        public int ResourceNodeCount;
        public int ActiveResourceNodes;
        public float TotalResourceUnitsRemaining;
        public int SpawnerCount;
        public int ActiveSpawnerCount;
        public int PendingSpawnerCount;
        public int BandCount;
        public int BandMemberCount;
        public float AverageBandMorale;
        public float AverageBandCohesion;
        public float AverageBandDiscipline;
        public int MiracleCount;
        public int ActiveMiracles;
        public int SustainedMiracles;
        public int CoolingMiracles;
        public float TotalMiracleEnergyCost;
        public float TotalMiracleCooldownSeconds;
        public uint LastRegistryTick;
    }

    /// <summary>
    /// Canonical archetype ids reserved for Godgame specific registry metadata.
    /// Values chosen to avoid collisions with other projects.
    /// </summary>
    public static class GodgameRegistryIds
    {
        public const ushort VillagerArchetype = 0x4701;
        public const ushort StorehouseArchetype = 0x4702;
        public const ushort ResourceNodeArchetype = 0x4703;
        public const ushort MiracleArchetype = 0x4704;
        public const ushort SpawnerArchetype = 0x4705;
        public const ushort BandArchetype = 0x4706;
    }
}
