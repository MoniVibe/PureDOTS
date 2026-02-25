using PureDOTS.Runtime.UI;
using Unity.Entities;

namespace PureDOTS.Systems.UI
{
    /// <summary>
    /// Ensures the shared UI kernel root and buffers exist.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial struct UiKernelBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.EntityManager;
            if (!SystemAPI.TryGetSingletonEntity<UiKernelRootTag>(out var root))
            {
                root = entityManager.CreateEntity(typeof(UiKernelRootTag), typeof(UiKernelState));
                entityManager.SetComponentData(root, new UiKernelState
                {
                    ActivePrimaryPanel = UiPanelKind.None,
                    InventoryOpen = 0,
                    InventoryTab = 0,
                    TooltipDepth = 0,
                    TooltipPinnedCount = 0,
                    Revision = 1
                });

                entityManager.AddBuffer<UiIntent>(root);
                entityManager.AddBuffer<UiOpenPanel>(root);
                entityManager.AddBuffer<UiTooltipEntry>(root);
                return;
            }

            if (!entityManager.HasComponent<UiKernelState>(root))
            {
                entityManager.AddComponentData(root, new UiKernelState
                {
                    ActivePrimaryPanel = UiPanelKind.None,
                    InventoryOpen = 0,
                    InventoryTab = 0,
                    TooltipDepth = 0,
                    TooltipPinnedCount = 0,
                    Revision = 1
                });
            }

            if (!entityManager.HasBuffer<UiIntent>(root))
            {
                entityManager.AddBuffer<UiIntent>(root);
            }

            if (!entityManager.HasBuffer<UiOpenPanel>(root))
            {
                entityManager.AddBuffer<UiOpenPanel>(root);
            }

            if (!entityManager.HasBuffer<UiTooltipEntry>(root))
            {
                entityManager.AddBuffer<UiTooltipEntry>(root);
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
