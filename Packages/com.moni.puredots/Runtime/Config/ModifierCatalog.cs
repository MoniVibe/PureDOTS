using System;
using System.Collections.Generic;
using UnityEngine;
using PureDOTS.Runtime.Modifiers;

namespace PureDOTS.Config
{
    /// <summary>
    /// ScriptableObject defining modifier catalog with data-driven modifier definitions.
    /// Converts to blob asset for fast runtime lookup.
    /// </summary>
    [CreateAssetMenu(fileName = "ModifierCatalog", menuName = "PureDOTS/Modifier Catalog")]
    public class ModifierCatalog : ScriptableObject
    {
        [Header("Modifier Definitions")]
        public List<ModifierDefinition> modifiers = new List<ModifierDefinition>();

        /// <summary>
        /// Gets the modifier definition by modifier ID index.
        /// </summary>
        public ModifierDefinition GetModifier(ushort modifierId)
        {
            if (modifierId < modifiers.Count)
            {
                return modifiers[modifierId];
            }
            return null;
        }
    }

    /// <summary>
    /// Definition for a single modifier.
    /// </summary>
    [Serializable]
    public class ModifierDefinition
    {
        [Header("Identity")]
        public string name = "New Modifier";
        public string description = "";

        [Header("Modifier Settings")]
        public ModifierOperation operation = ModifierOperation.Add;
        public float baseValue = 1f;
        public ModifierCategory category = ModifierCategory.Economy;
        public float durationScale = 1f;

        [Header("Dependencies")]
        public List<ushort> dependencies = new List<ushort>(); // Modifier IDs this depends on
    }
}

