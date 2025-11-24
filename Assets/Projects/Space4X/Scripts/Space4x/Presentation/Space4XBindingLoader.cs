using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Space4X.Presentation
{
    /// <summary>
    /// Loads and manages Minimal/Fancy binding sets for hot-swapping.
    /// </summary>
    public static class Space4XBindingLoader
    {
        private const string BindingsDirectory = "projects/space4x/bindings";
        private static Space4XBindingGenerator.BindingSet _currentBindings;
        private static bool _isMinimal = true;

        public static void LoadBindings(bool minimal)
        {
            _isMinimal = minimal;
            var filename = minimal ? "Minimal.json" : "Fancy.json";
            var filepath = Path.Combine(Application.dataPath, "..", BindingsDirectory, filename);

            if (!File.Exists(filepath))
            {
                Debug.LogWarning($"[Space4XBindingLoader] Binding file not found: {filepath}");
                return;
            }

            try
            {
                var json = File.ReadAllText(filepath);
                _currentBindings = JsonUtility.FromJson<Space4XBindingGenerator.BindingSet>(json);
                Debug.Log($"[Space4XBindingLoader] Loaded {(minimal ? "Minimal" : "Fancy")} bindings");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Space4XBindingLoader] Failed to load bindings: {ex.Message}");
            }
        }

        public static void SwapBindings()
        {
            LoadBindings(!_isMinimal);
        }

        public static bool IsMinimal => _isMinimal;

        public static Space4XBindingGenerator.BindingSet CurrentBindings => _currentBindings;

        public static Space4XBindingGenerator.WeaponBindingEntry GetWeaponBinding(string weaponId)
        {
            if (_currentBindings?.weapons == null) return null;
            
            foreach (var binding in _currentBindings.weapons)
            {
                if (binding.id.Equals(weaponId, StringComparison.OrdinalIgnoreCase))
                {
                    return binding;
                }
            }
            
            return null;
        }

        public static Space4XBindingGenerator.ProjectileBindingEntry GetProjectileBinding(string projectileId)
        {
            if (_currentBindings?.projectiles == null) return null;
            
            foreach (var binding in _currentBindings.projectiles)
            {
                if (binding.id.Equals(projectileId, StringComparison.OrdinalIgnoreCase))
                {
                    return binding;
                }
            }
            
            return null;
        }
    }
}

