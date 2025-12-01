using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Demo.Village
{
    /// <summary>
    /// Bootstrap system that creates demo village entities: homes, workplaces, and villagers.
    /// Spawns 10 homes, 10 workplaces, and 10 villagers in a strip layout.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(PureDOTS.Demo.Rendering.SharedRenderBootstrap))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct VillageDemoBootstrapSystem : ISystem
    {
        bool _initialized;

        public void OnCreate(ref SystemState state)
        {
            UnityEngine.Debug.Log($"[VillageDemoBootstrapSystem] OnCreate in world: {state.WorldUnmanaged.Name}");
            state.RequireForUpdate<PureDOTS.Demo.Rendering.RenderMeshArraySingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_initialized)
                return;

            _initialized = true;

            var em  = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            const int   villagerCount = 10;
            const float spacing       = 2f;

            int villageCount = 0;
            int homeCount = 0;
            int workCount = 0;

            for (int i = 0; i < villagerCount; i++)
            {
                float x = (i - villagerCount / 2) * spacing;

                float3 homePos = new float3(x, 0f, 0f);
                float3 workPos = new float3(x, 0f, 10f);

                // Home lot
                {
                    Entity home = ecb.CreateEntity();
                    ecb.AddComponent(home, new HomeLot    { Position = homePos });
                    ecb.AddComponent(home, LocalTransform.FromPosition(homePos));
                    ecb.AddComponent(home, new VillageTag());
                    homeCount++;
                    villageCount++;
                }

                // Work lot
                {
                    Entity work = ecb.CreateEntity();
                    ecb.AddComponent(work, new WorkLot    { Position = workPos });
                    ecb.AddComponent(work, LocalTransform.FromPosition(workPos));
                    ecb.AddComponent(work, new VillageTag());
                    workCount++;
                    villageCount++;
                }

                // Villager
                {
                    Entity villager = ecb.CreateEntity();
                    ecb.AddComponent(villager, new VillagerTag());
                    ecb.AddComponent(villager, new VillagerHome  { Position = homePos });
                    ecb.AddComponent(villager, new VillagerWork  { Position = workPos });
                    ecb.AddComponent(villager, new VillagerState { Phase    = 0 });
                    ecb.AddComponent(villager, LocalTransform.FromPosition(homePos));
                }
            }

            ecb.Playback(em);
            ecb.Dispose();

            UnityEngine.Debug.Log($"[VillageDemoBootstrapSystem] World '{state.WorldUnmanaged.Name}': spawned {villagerCount} Villagers, {homeCount} Homes, {workCount} Works.");
        }
    }
}

