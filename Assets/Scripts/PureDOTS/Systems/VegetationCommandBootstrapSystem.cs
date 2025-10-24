using PureDOTS.Runtime.Components;
using Unity.Entities;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Ensures the vegetation harvest command queue exists for simulation systems.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class VegetationCommandBootstrapSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            var entityManager = EntityManager;

            if (!SystemAPI.HasSingleton<VegetationHarvestCommandQueue>())
            {
                var queueEntity = entityManager.CreateEntity(typeof(VegetationHarvestCommandQueue));
                entityManager.AddBuffer<VegetationHarvestCommand>(queueEntity);
                entityManager.AddBuffer<VegetationHarvestReceipt>(queueEntity);
            }

            Enabled = false;
        }

        protected override void OnUpdate()
        {
            // No-op. Bootstrap only.
        }
    }
}
