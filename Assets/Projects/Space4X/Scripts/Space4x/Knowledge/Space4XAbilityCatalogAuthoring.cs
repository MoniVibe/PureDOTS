#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Space4X.Knowledge
{
    /// <summary>
    /// Authoring ScriptableObject for Space4X tech abilities.
    /// </summary>
    [CreateAssetMenu(fileName = "TechAbilityCatalog", menuName = "Space4X/Knowledge/Tech Ability Catalog")]
    public class TechAbilityCatalogAuthoring : ScriptableObject
    {
        [Serializable]
        public class TechAbilityDef
        {
            public string abilityId;
            public string displayName;
            public string requiredModuleId;
            [Range(0, 10)] public int requiredTechLevel = 1;
            [Min(0)] public float powerCost = 10f;
            [Min(0)] public float cooldown = 5f;
            [Min(0)] public float activationTime = 0f;
            public TechAbilityEffectType effectType = TechAbilityEffectType.Damage;
            public float effectMagnitude = 100f;
            public float effectDuration = 0f;
            [Min(0)] public float range = 50f;
        }

        public List<TechAbilityDef> abilities = new();
    }

    public sealed class TechAbilityCatalogBaker : Baker<TechAbilityCatalogAuthoring>
    {
        public override void Bake(TechAbilityCatalogAuthoring authoring)
        {
            using var bb = new BlobBuilder(Allocator.Temp);
            ref var root = ref bb.ConstructRoot<TechAbilityCatalogBlob>();

            var arr = bb.Allocate(ref root.Abilities, authoring.abilities.Count);
            for (int i = 0; i < authoring.abilities.Count; i++)
            {
                var src = authoring.abilities[i];
                arr[i] = new TechAbilitySpec
                {
                    AbilityId = new FixedString64Bytes(src.abilityId),
                    DisplayName = new FixedString64Bytes(src.displayName),
                    RequiredModuleId = new FixedString64Bytes(src.requiredModuleId ?? ""),
                    RequiredTechLevel = (byte)src.requiredTechLevel,
                    PowerCost = src.powerCost,
                    Cooldown = src.cooldown,
                    ActivationTime = src.activationTime,
                    EffectType = src.effectType,
                    EffectMagnitude = src.effectMagnitude,
                    EffectDuration = src.effectDuration,
                    Range = src.range
                };
            }

            var blob = bb.CreateBlobAssetReference<TechAbilityCatalogBlob>(Allocator.Persistent);
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new TechAbilityCatalogRef { Blob = blob });
        }
    }

    /// <summary>
    /// Authoring ScriptableObject for Space4X psionic abilities.
    /// </summary>
    [CreateAssetMenu(fileName = "PsionicAbilityCatalog", menuName = "Space4X/Knowledge/Psionic Ability Catalog")]
    public class PsionicAbilityCatalogAuthoring : ScriptableObject
    {
        [Serializable]
        public class PsionicAbilityDef
        {
            public string abilityId;
            public string displayName;
            public PsionicDiscipline discipline = PsionicDiscipline.Telepathy;
            [Range(0, 100)] public int minPsionicPotential = 20;
            [Range(0, 10)] public int minEnlightenmentLevel = 0;
            [Min(0)] public float willpowerCost = 15f;
            [Min(0)] public float strainCost = 5f;
            [Min(0)] public float cooldown = 10f;
            public PsionicEffectType effectType = PsionicEffectType.MindRead;
            public float effectMagnitude = 1f;
            public float effectDuration = 5f;
            public float range = 20f;
        }

        public List<PsionicAbilityDef> abilities = new();
    }

    public sealed class PsionicAbilityCatalogBaker : Baker<PsionicAbilityCatalogAuthoring>
    {
        public override void Bake(PsionicAbilityCatalogAuthoring authoring)
        {
            using var bb = new BlobBuilder(Allocator.Temp);
            ref var root = ref bb.ConstructRoot<PsionicAbilityCatalogBlob>();

            var arr = bb.Allocate(ref root.Abilities, authoring.abilities.Count);
            for (int i = 0; i < authoring.abilities.Count; i++)
            {
                var src = authoring.abilities[i];
                arr[i] = new PsionicAbilitySpec
                {
                    AbilityId = new FixedString64Bytes(src.abilityId),
                    DisplayName = new FixedString64Bytes(src.displayName),
                    Discipline = src.discipline,
                    MinPsionicPotential = (byte)src.minPsionicPotential,
                    MinEnlightenmentLevel = (byte)src.minEnlightenmentLevel,
                    WillpowerCost = src.willpowerCost,
                    StrainCost = src.strainCost,
                    Cooldown = src.cooldown,
                    EffectType = src.effectType,
                    EffectMagnitude = src.effectMagnitude,
                    EffectDuration = src.effectDuration,
                    Range = src.range
                };
            }

            var blob = bb.CreateBlobAssetReference<PsionicAbilityCatalogBlob>(Allocator.Persistent);
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PsionicAbilityCatalogRef { Blob = blob });
        }
    }

    /// <summary>
    /// Authoring ScriptableObject for Space4X tactical maneuvers.
    /// </summary>
    [CreateAssetMenu(fileName = "TacticalManeuverCatalog", menuName = "Space4X/Knowledge/Tactical Maneuver Catalog")]
    public class TacticalManeuverCatalogAuthoring : ScriptableObject
    {
        [Serializable]
        public class TacticalManeuverDef
        {
            public string maneuverId;
            public string displayName;
            public ManeuverCategory category = ManeuverCategory.Offensive;
            [Range(0, 100)] public int requiredExperience = 10;
            [Range(0, 100)] public int requiredCommandSkill = 0;
            [Min(0)] public float staminaCost = 20f;
            [Min(0)] public float cooldown = 15f;
            [Min(0)] public float executionTime = 2f;
            public TacticalEffectType effectType = TacticalEffectType.DamageBoost;
            public float effectMagnitude = 0.2f;
            public float effectDuration = 5f;
            public bool isShipManeuver = true;
        }

        public List<TacticalManeuverDef> maneuvers = new();
    }

    public sealed class TacticalManeuverCatalogBaker : Baker<TacticalManeuverCatalogAuthoring>
    {
        public override void Bake(TacticalManeuverCatalogAuthoring authoring)
        {
            using var bb = new BlobBuilder(Allocator.Temp);
            ref var root = ref bb.ConstructRoot<TacticalManeuverCatalogBlob>();

            var arr = bb.Allocate(ref root.Maneuvers, authoring.maneuvers.Count);
            for (int i = 0; i < authoring.maneuvers.Count; i++)
            {
                var src = authoring.maneuvers[i];
                arr[i] = new TacticalManeuverSpec
                {
                    ManeuverId = new FixedString64Bytes(src.maneuverId),
                    DisplayName = new FixedString64Bytes(src.displayName),
                    Category = src.category,
                    RequiredExperience = (byte)src.requiredExperience,
                    RequiredCommandSkill = (byte)src.requiredCommandSkill,
                    StaminaCost = src.staminaCost,
                    Cooldown = src.cooldown,
                    ExecutionTime = src.executionTime,
                    EffectType = src.effectType,
                    EffectMagnitude = src.effectMagnitude,
                    EffectDuration = src.effectDuration,
                    IsShipManeuver = src.isShipManeuver
                };
            }

            var blob = bb.CreateBlobAssetReference<TacticalManeuverCatalogBlob>(Allocator.Persistent);
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new TacticalManeuverCatalogRef { Blob = blob });
        }
    }
}
#endif

