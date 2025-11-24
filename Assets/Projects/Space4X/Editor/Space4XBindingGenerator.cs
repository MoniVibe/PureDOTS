using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PureDOTS.Authoring.Combat;
using PureDOTS.Authoring.Resource;
using PureDOTS.Runtime.Combat;
using UnityEditor;
using UnityEngine;

namespace Space4X.Editor
{
    /// <summary>
    /// Generates Minimal and Fancy binding JSON files for Space4X assets.
    /// Maps WeaponId→muzzle FX, ProjectileId→tracer/beam/impact, HullId→socket map, etc.
    /// </summary>
    public static class Space4XBindingGenerator
    {
        private const string BindingsDirectory = "projects/space4x/bindings";
        private const string MinimalBindingsFile = "Minimal.json";
        private const string FancyBindingsFile = "Fancy.json";

        [System.Serializable]
        public class BindingSet
        {
            public string name;
            public List<WeaponBindingEntry> weapons = new();
            public List<ProjectileBindingEntry> projectiles = new();
            public List<HullBindingEntry> hulls = new();
            public List<StationBindingEntry> stations = new();
            public List<ResourceBindingEntry> resources = new();
            public List<ProductBindingEntry> products = new();
        }

        [System.Serializable]
        public class WeaponBindingEntry
        {
            public string id;
            public string muzzleFx;
            public string muzzleSfx;
            public Vector3 muzzleOffset;
        }

        [System.Serializable]
        public class ProjectileBindingEntry
        {
            public string id;
            public string tracerFx;
            public string beamFx;
            public string impactFx;
            public string impactSfx;
        }

        [System.Serializable]
        public class SocketEntry
        {
            public string name;
            public Vector3 position;
        }

        [System.Serializable]
        public class HullBindingEntry
        {
            public string id;
            public List<SocketEntry> sockets = new();
            public string meshStyle;
        }

        [System.Serializable]
        public class StationBindingEntry
        {
            public string id;
            public List<string> facilityTags = new();
            public List<string> zones = new();
            public string meshStyle;
        }

        [System.Serializable]
        public class ResourceBindingEntry
        {
            public string id;
            public string hudToken;
            public string iconStyle;
        }

        [System.Serializable]
        public class ProductBindingEntry
        {
            public string id;
            public string hudToken;
            public string iconStyle;
        }

        public static void GenerateMinimalBindings()
        {
            var bindings = new BindingSet { name = "Minimal" };
            CollectBindings(bindings, isMinimal: true);
            WriteBindings(bindings, MinimalBindingsFile);
            Debug.Log($"[Space4XBindingGenerator] Generated minimal bindings to {BindingsDirectory}/{MinimalBindingsFile}");
        }

        public static void GenerateFancyBindings()
        {
            var bindings = new BindingSet { name = "Fancy" };
            CollectBindings(bindings, isMinimal: false);
            WriteBindings(bindings, FancyBindingsFile);
            Debug.Log($"[Space4XBindingGenerator] Generated fancy bindings to {BindingsDirectory}/{FancyBindingsFile}");
        }

        public static void CollectBindings(BindingSet bindings, bool isMinimal)
        {
            // Collect weapon bindings
            var weaponCatalogs = AssetDatabase.FindAssets("t:WeaponCatalogAsset");
            foreach (var guid in weaponCatalogs)
            {
                var asset = AssetDatabase.LoadAssetAtPath<WeaponCatalogAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || asset.Entries == null) continue;

                foreach (var entry in asset.Entries)
                {
                    var weaponId = string.IsNullOrWhiteSpace(entry.WeaponId) ? $"weapon.{entry.Class}" : entry.WeaponId.ToLowerInvariant();
                    bindings.weapons.Add(new WeaponBindingEntry
                    {
                        id = weaponId,
                        muzzleFx = isMinimal ? "fx.muzzle.minimal" : "fx.muzzle.fancy",
                        muzzleSfx = isMinimal ? "sfx.muzzle.minimal" : "sfx.muzzle.fancy",
                        muzzleOffset = Vector3.zero
                    });
                }
            }

            // Collect projectile bindings
            var projectileCatalogs = AssetDatabase.FindAssets("t:ProjectileCatalogAsset");
            foreach (var guid in projectileCatalogs)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ProjectileCatalogAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || asset.Entries == null) continue;

                foreach (var entry in asset.Entries)
                {
                    var projectileId = string.IsNullOrWhiteSpace(entry.ProjectileId) ? $"projectile.{entry.Kind}" : entry.ProjectileId.ToLowerInvariant();
                    bindings.projectiles.Add(new ProjectileBindingEntry
                    {
                        id = projectileId,
                        tracerFx = isMinimal ? "fx.tracer.minimal" : "fx.tracer.fancy",
                        beamFx = entry.Kind == ProjectileKind.Beam ? (isMinimal ? "fx.beam.minimal" : "fx.beam.fancy") : null,
                        impactFx = isMinimal ? "fx.impact.minimal" : "fx.impact.fancy",
                        impactSfx = isMinimal ? "sfx.impact.minimal" : "sfx.impact.fancy"
                    });
                }
            }

            // Collect deposit/resource bindings
            var seenResources = new HashSet<string>();
            var depositCatalogs = AssetDatabase.FindAssets("t:DepositCatalogAsset");
            foreach (var guid in depositCatalogs)
            {
                var asset = AssetDatabase.LoadAssetAtPath<DepositCatalogAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || asset.Entries == null) continue;

                foreach (var entry in asset.Entries)
                {
                    var resourceId = string.IsNullOrWhiteSpace(entry.ResourceId) ? "resource.unknown" : entry.ResourceId.ToLowerInvariant();
                    if (seenResources.Add(resourceId))
                    {
                        bindings.resources.Add(new ResourceBindingEntry
                        {
                            id = resourceId,
                            hudToken = isMinimal ? $"token.{resourceId}.minimal" : $"token.{resourceId}.fancy",
                            iconStyle = isMinimal ? "icon.resource.minimal" : "icon.resource.fancy"
                        });
                    }
                }
            }

            // Placeholder hulls, stations, products (would be populated from actual catalogs)
            bindings.hulls.Add(new HullBindingEntry
            {
                id = "hull.test",
                sockets = new List<SocketEntry> { new SocketEntry { name = "muzzle", position = Vector3.zero } },
                meshStyle = isMinimal ? "mesh.hull.minimal" : "mesh.hull.fancy"
            });

            bindings.stations.Add(new StationBindingEntry
            {
                id = "station.basic",
                facilityTags = new List<string> { "dock", "refit", "repair" },
                zones = new List<string> { "safe" },
                meshStyle = isMinimal ? "mesh.station.minimal" : "mesh.station.fancy"
            });
        }

        private static void WriteBindings(BindingSet bindings, string filename)
        {
            var directory = Path.Combine(Application.dataPath, "..", BindingsDirectory);
            Directory.CreateDirectory(directory);

            var json = JsonUtility.ToJson(bindings, prettyPrint: true);
            var filepath = Path.Combine(directory, filename);
            File.WriteAllText(filepath, json);
        }

        public static string ComputeHash(BindingSet bindings)
        {
            var json = JsonUtility.ToJson(bindings, prettyPrint: false);
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}

