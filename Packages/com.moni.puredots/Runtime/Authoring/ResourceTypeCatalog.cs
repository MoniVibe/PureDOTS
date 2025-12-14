using System;
using System.Collections.Generic;
using UnityEngine;

namespace PureDOTS.Authoring
{
    [CreateAssetMenu(fileName = "PureDotsResourceTypes", menuName = "PureDOTS/Resource Type Catalog", order = 1)]
    public sealed class ResourceTypeCatalog : ScriptableObject
    {
        public const int LatestSchemaVersion = 1;

        [SerializeField, HideInInspector]
        private int _schemaVersion = LatestSchemaVersion;

        public List<ResourceTypeDefinition> entries = new();

        public int SchemaVersion => _schemaVersion;

#if UNITY_EDITOR
        public void SetSchemaVersion(int value)
        {
            _schemaVersion = value;
        }

        private void OnValidate()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(entries[i].id))
                {
                    entries.RemoveAt(i);
                }
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entries.Count; i++)
            {
                var trimmed = entries[i].id.Trim();
                if (seen.Contains(trimmed))
                {
                    Debug.LogWarning($"Duplicate resource type id '{trimmed}' removed from catalog.", this);
                    entries.RemoveAt(i);
                    i--;
                    continue;
                }

                entries[i] = new ResourceTypeDefinition
                {
                    id = trimmed,
                    displayColor = entries[i].displayColor
                };
                seen.Add(trimmed);
            }
        }
#endif
    }
}
