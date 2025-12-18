#if PUREDOTS_SCENARIO
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace PureDOTS.Demo.Rendering
{
    /// <summary>
    /// Visual intent marker used by gameplay/demo systems to request a specific debug visual.
    /// A separate system translates this intent into renderable components.
    /// </summary>
    public struct VisualProfile : IComponentData
    {
        public VisualProfileId Id;
    }

    /// <summary>
    /// Enumerates debug visual profiles. Extend as needed for gameplay visuals.
    /// </summary>
    public enum VisualProfileId : ushort
    {
        None = 0,

        // Debug/demo
        DebugVillager,
        DebugHome,
        DebugWork,
        DebugAsteroid,
        DebugCarrier,
        DebugMiner,
    }

    /// <summary>
    /// Blob catalog mapping VisualProfileId to mesh/material indices and base scale.
    /// </summary>
    public struct VisualProfileCatalogBlob
    {
        public BlobArray<VisualProfileEntry> Entries;
    }

    public struct VisualProfileEntry
    {
        public ushort MeshIndex;
        public ushort MaterialIndex;
        public float  BaseScale;
    }

    /// <summary>
    /// Singleton pointing at the visual profile catalog blob.
    /// </summary>
    public struct VisualProfileCatalog : IComponentData
    {
        public BlobAssetReference<VisualProfileCatalogBlob> Blob;
    }

    /// <summary>
    /// DEMO-ONLY: Bootstrap that builds a debug visual profile catalog for demo entities.
    ///
    /// IMPORTANT: This is an example implementation for PureDOTS demo/testing.
    /// Real games should use game-specific render catalogs and keys.
    ///
    /// Only runs when PureDOTS demo profile is active.
    /// </summary>
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct VisualProfileBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // Only run in demo worlds
            var activeProfile = PureDOTS.Systems.SystemRegistry.ResolveActiveProfile();
            if (activeProfile.Id != PureDOTS.Systems.SystemRegistry.BuiltinProfiles.Demo.Id)
            {
                state.Enabled = false;
                return;
            }

            if (SystemAPI.HasSingleton<VisualProfileCatalog>())
                return;

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<VisualProfileCatalogBlob>();

            var entries = builder.Allocate(ref root.Entries, (int)VisualProfileId.DebugMiner + 1);

            // Inline assignments - no lambda, no captured ref locals
            entries[(int)VisualProfileId.DebugVillager] = new VisualProfileEntry
            {
                MeshIndex = DemoMeshIndices.VillageVillagerMeshIndex,
                MaterialIndex = DemoMeshIndices.DemoMaterialIndex,
                BaseScale = 1f
            };

            entries[(int)VisualProfileId.DebugHome] = new VisualProfileEntry
            {
                MeshIndex = DemoMeshIndices.VillageHomeMeshIndex,
                MaterialIndex = DemoMeshIndices.DemoMaterialIndex,
                BaseScale = 1f
            };

            entries[(int)VisualProfileId.DebugWork] = new VisualProfileEntry
            {
                MeshIndex = DemoMeshIndices.VillageWorkMeshIndex,
                MaterialIndex = DemoMeshIndices.DemoMaterialIndex,
                BaseScale = 1f
            };

            entries[(int)VisualProfileId.DebugAsteroid] = new VisualProfileEntry
            {
                MeshIndex = DemoMeshIndices.VillageVillagerMeshIndex,
                MaterialIndex = DemoMeshIndices.DemoMaterialIndex,
                BaseScale = 1f
            };

            entries[(int)VisualProfileId.DebugCarrier] = new VisualProfileEntry
            {
                MeshIndex = DemoMeshIndices.VillageVillagerMeshIndex,
                MaterialIndex = DemoMeshIndices.DemoMaterialIndex,
                BaseScale = 1f
            };

            entries[(int)VisualProfileId.DebugMiner] = new VisualProfileEntry
            {
                MeshIndex = DemoMeshIndices.VillageVillagerMeshIndex,
                MaterialIndex = DemoMeshIndices.DemoMaterialIndex,
                BaseScale = 1f
            };

            var blob = builder.CreateBlobAssetReference<VisualProfileCatalogBlob>(Allocator.Persistent);
            builder.Dispose();

            var e = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(e, new VisualProfileCatalog { Blob = blob });
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
        }
    }

    /// <summary>
    /// DEMO-ONLY: Assigns render components to entities with VisualProfile components.
    ///
    /// IMPORTANT: This is an example implementation for PureDOTS demo/testing.
    /// Real games should implement proper render assignment systems using RenderKeys
    /// and game-specific render catalogs.
    ///
    /// Only runs when PureDOTS demo profile is active.
    /// </summary>
    [BurstCompile]
    [DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SharedRenderBootstrap))]
    public partial struct AssignVisualsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // Only run in demo worlds
            var activeProfile = PureDOTS.Systems.SystemRegistry.ResolveActiveProfile();
            if (activeProfile.Id != PureDOTS.Systems.SystemRegistry.BuiltinProfiles.Demo.Id)
            {
                state.Enabled = false;
                return;
            }

            state.RequireForUpdate<RenderMeshArraySingleton>();
            state.RequireForUpdate<VisualProfileCatalog>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;

            var renderQuery = em.CreateEntityQuery(ComponentType.ReadOnly<RenderMeshArraySingleton>());
            if (renderQuery.IsEmptyIgnoreFilter)
                return;

            var renderMeshArray = em.GetSharedComponentManaged<RenderMeshArraySingleton>(renderQuery.GetSingletonEntity()).Value;
            if (renderMeshArray == null)
                return;

            ref var catalog = ref SystemAPI.GetSingleton<VisualProfileCatalog>().Blob.Value;
            int meshCount = renderMeshArray.MeshReferences?.Length ?? 0;
            int materialCount = renderMeshArray.MaterialReferences?.Length ?? 0;

            // Guard: invalid array means MaterialMeshInfo would encode -1 indices -> Entities Graphics Key:-1 error.
            if (meshCount <= 0 || materialCount <= 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[AssignVisualsSystem] RenderMeshArraySingleton has no meshes/materials; skipping MaterialMeshInfo assignment.");
#endif
                return;
            }

            var assignQuery = SystemAPI.QueryBuilder()
                .WithAll<VisualProfile, LocalTransform>()
                .WithNone<MaterialMeshInfo>()
                .Build();

            using var entities = assignQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
                return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var profile = em.GetComponentData<VisualProfile>(entity);

                var id = profile.Id;
                if ((int)id >= catalog.Entries.Length)
                    continue;

                ref readonly var entry = ref catalog.Entries[(int)id];

                // Only add valid indices; skip bad catalog rows to avoid -1 encodings.
                if (entry.MeshIndex >= meshCount || entry.MaterialIndex >= materialCount)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[AssignVisualsSystem] Skipping VisualProfile {id} due to out-of-range mesh/material index ({entry.MeshIndex}/{entry.MaterialIndex}) for counts {meshCount}/{materialCount}.");
#endif
                    continue;
                }

                var mmi = MaterialMeshInfo.FromRenderMeshArrayIndices(entry.MeshIndex, entry.MaterialIndex);
                ecb.AddComponent(entity, mmi);

                // No WorldRenderBounds here - Entities Graphics can handle bounds later

                if (entry.BaseScale > 0f && em.HasComponent<LocalTransform>(entity))
                {
                    var xf = em.GetComponentData<LocalTransform>(entity);
                    if (math.abs(xf.Scale - entry.BaseScale) > 0.0001f)
                    {
                        xf.Scale = entry.BaseScale;
                        ecb.SetComponent(entity, xf);
                    }
                }
            }

            ecb.Playback(em);
            ecb.Dispose();
        }
    }
}

#endif
