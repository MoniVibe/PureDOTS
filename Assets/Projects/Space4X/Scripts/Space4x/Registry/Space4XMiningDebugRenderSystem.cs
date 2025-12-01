using System.Collections.Generic;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using PresentationSystemGroup = PureDOTS.Systems.PresentationSystemGroup;

namespace Space4X.Registry
{
    /// <summary>
    /// Renders simple primitives for carriers, mining vessels, and asteroids so the mining loop is visible in hybrid scenes.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct Space4XMiningDebugRenderSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Space4XMiningVisualConfig>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<RewindState>(out var rewind) && rewind.Mode == RewindMode.Playback)
            {
                return;
            }

            var config = SystemAPI.GetSingleton<Space4XMiningVisualConfig>();

            var carrierMesh = MiningDebugRenderResources.GetMesh(config.CarrierPrimitive);
            var carrierMaterial = MiningDebugRenderResources.GetMaterial(config.CarrierColor);
            var vesselMesh = MiningDebugRenderResources.GetMesh(config.MiningVesselPrimitive);
            var vesselMaterial = MiningDebugRenderResources.GetMaterial(config.MiningVesselColor);
            var asteroidMesh = MiningDebugRenderResources.GetMesh(config.AsteroidPrimitive);
            var asteroidMaterial = MiningDebugRenderResources.GetMaterial(config.AsteroidColor);

            foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Carrier>())
            {
                MiningDebugRenderResources.DrawMesh(carrierMesh, carrierMaterial, transform.ValueRO, config.CarrierScale);
            }

            foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<MiningVessel>())
            {
                MiningDebugRenderResources.DrawMesh(vesselMesh, vesselMaterial, transform.ValueRO, config.MiningVesselScale);
            }

            foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Asteroid>())
            {
                MiningDebugRenderResources.DrawMesh(asteroidMesh, asteroidMaterial, transform.ValueRO, config.AsteroidScale);
            }
        }
    }

    internal static class MiningDebugRenderResources
    {
        private static readonly Dictionary<Space4XMiningPrimitive, Mesh> MeshCache = new();
        private static readonly Dictionary<int, Material> MaterialCache = new();

        public static Mesh GetMesh(Space4XMiningPrimitive primitive)
        {
            if (MeshCache.TryGetValue(primitive, out var mesh) && mesh != null)
            {
                return mesh;
            }

            var unityPrimitive = primitive switch
            {
                Space4XMiningPrimitive.Sphere => PrimitiveType.Sphere,
                Space4XMiningPrimitive.Capsule => PrimitiveType.Capsule,
                Space4XMiningPrimitive.Cylinder => PrimitiveType.Cylinder,
                _ => PrimitiveType.Cube
            };

            var temp = GameObject.CreatePrimitive(unityPrimitive);
            var filter = temp.GetComponent<MeshFilter>();
            mesh = filter != null ? filter.sharedMesh : null;

            if (Application.isPlaying)
            {
                Object.Destroy(temp);
            }
            else
            {
                Object.DestroyImmediate(temp);
            }

            if (mesh != null)
            {
                MeshCache[primitive] = mesh;
            }

            return mesh;
        }

        public static Material GetMaterial(float4 rgba)
        {
            var color = new Color(math.saturate(rgba.x), math.saturate(rgba.y), math.saturate(rgba.z), math.saturate(rgba.w));
            var key = color.GetHashCode();

            if (MaterialCache.TryGetValue(key, out var material) && material != null)
            {
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
            material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            material.hideFlags = HideFlags.HideAndDontSave;
            material.enableInstancing = true;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            MaterialCache[key] = material;
            return material;
        }

        public static void DrawMesh(Mesh mesh, Material material, in LocalTransform transform, float uniformScale)
        {
            if (mesh == null || material == null)
            {
                return;
            }

            var scale = math.max(0.0001f, transform.Scale) * math.max(0.0001f, uniformScale);
            var matrix = Matrix4x4.TRS(
                (Vector3)transform.Position,
                new Quaternion(transform.Rotation.value.x, transform.Rotation.value.y, transform.Rotation.value.z, transform.Rotation.value.w),
                new Vector3(scale, scale, scale));

            Graphics.DrawMesh(mesh, matrix, material, 0);
        }
    }
}



