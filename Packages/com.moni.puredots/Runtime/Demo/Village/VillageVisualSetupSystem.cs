using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace PureDOTS.Demo.Village
{
    /// <summary>
    /// System that adds render components to village demo entities (homes, workplaces, villagers).
    /// Requires VillageWorldTag to be present in the world to run.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(VillageDemoBootstrapSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct VillageVisualSetupSystem : ISystem
    {
        bool _initialized;

        public void OnCreate(ref SystemState state)
        {
            UnityEngine.Debug.Log($"[VillageVisualSetupSystem] OnCreate in world: {state.WorldUnmanaged.Name}");
            // Only run in worlds that opt into the village demo via VillageWorldTag
            state.RequireForUpdate<VillageWorldTag>();
            state.RequireForUpdate<PureDOTS.Demo.Rendering.RenderMeshArraySingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_initialized)
                return;

            _initialized = true;

            var em = state.EntityManager;
            var renderQuery = em.CreateEntityQuery(ComponentType.ReadOnly<PureDOTS.Demo.Rendering.RenderMeshArraySingleton>());
            if (renderQuery.IsEmptyIgnoreFilter)
            {
                UnityEngine.Debug.LogWarning($"[VillageVisualSetupSystem] RenderMeshArraySingleton missing in world: {state.WorldUnmanaged.Name}; skipping visuals.");
                return;
            }

            var renderMeshArray = em.GetSharedComponentManaged<PureDOTS.Demo.Rendering.RenderMeshArraySingleton>(renderQuery.GetSingletonEntity()).Value;

            if (renderMeshArray == null)
            {
                UnityEngine.Debug.LogWarning($"[VillageVisualSetupSystem] RenderMeshArray is null in world: {state.WorldUnmanaged.Name}; skipping visuals.");
                return;
            }

            var desc = new RenderMeshDescription();
            int entityCount = 0;

            // Homes
            using (var homes = em.CreateEntityQuery(
                       ComponentType.ReadOnly<HomeLot>(),
                       ComponentType.ReadOnly<LocalTransform>())
                       .ToEntityArray(Allocator.Temp))
            {
                foreach (var e in homes)
                {
                    RenderMeshUtility.AddComponents(
                        e,
                        em,
                        desc,
                        renderMeshArray,
                        MaterialMeshInfo.FromRenderMeshArrayIndices(
                            PureDOTS.Demo.Rendering.DemoMeshIndices.VillageHomeMeshIndex, 
                            PureDOTS.Demo.Rendering.DemoMeshIndices.DemoMaterialIndex));
                    entityCount++;
                }
            }

            // Works
            using (var works = em.CreateEntityQuery(
                       ComponentType.ReadOnly<WorkLot>(),
                       ComponentType.ReadOnly<LocalTransform>())
                       .ToEntityArray(Allocator.Temp))
            {
                foreach (var e in works)
                {
                    RenderMeshUtility.AddComponents(
                        e,
                        em,
                        desc,
                        renderMeshArray,
                        MaterialMeshInfo.FromRenderMeshArrayIndices(
                            PureDOTS.Demo.Rendering.DemoMeshIndices.VillageWorkMeshIndex, 
                            PureDOTS.Demo.Rendering.DemoMeshIndices.DemoMaterialIndex));
                    entityCount++;
                }
            }

            // Villagers
            using (var villagers = em.CreateEntityQuery(
                       ComponentType.ReadOnly<VillagerTag>(),
                       ComponentType.ReadOnly<LocalTransform>())
                       .ToEntityArray(Allocator.Temp))
            {
                foreach (var e in villagers)
                {
                    RenderMeshUtility.AddComponents(
                        e,
                        em,
                        desc,
                        renderMeshArray,
                        MaterialMeshInfo.FromRenderMeshArrayIndices(
                            PureDOTS.Demo.Rendering.DemoMeshIndices.VillageVillagerMeshIndex, 
                            PureDOTS.Demo.Rendering.DemoMeshIndices.DemoMaterialIndex));
                    entityCount++;
                }
            }

            UnityEngine.Debug.Log($"[VillageVisualSetupSystem] Render components added for {entityCount} village entities in world: {state.WorldUnmanaged.Name}");
        }
    }
}

