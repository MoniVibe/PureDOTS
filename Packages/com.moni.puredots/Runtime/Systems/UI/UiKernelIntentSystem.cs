using PureDOTS.Runtime.UI;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.UI
{
    /// <summary>
    /// Applies queued UI intents onto shared panel/tooltip state.
    /// UI renderers (UI Toolkit, debug overlays) should read UiKernelState and buffers.
    /// </summary>
    [UpdateInGroup(typeof(Unity.Entities.PresentationSystemGroup))]
    public partial struct UiKernelIntentSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UiKernelRootTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.EntityManager;
            var root = SystemAPI.GetSingletonEntity<UiKernelRootTag>();
            if (!entityManager.HasComponent<UiKernelState>(root) ||
                !entityManager.HasBuffer<UiIntent>(root) ||
                !entityManager.HasBuffer<UiOpenPanel>(root) ||
                !entityManager.HasBuffer<UiTooltipEntry>(root))
            {
                return;
            }

            var intents = entityManager.GetBuffer<UiIntent>(root);
            var openPanels = entityManager.GetBuffer<UiOpenPanel>(root);
            var tooltips = entityManager.GetBuffer<UiTooltipEntry>(root);
            var uiState = entityManager.GetComponentData<UiKernelState>(root);

            var changed = false;
            for (var i = 0; i < intents.Length; i++)
            {
                var intent = intents[i];
                switch (intent.Kind)
                {
                    case UiIntentKind.TogglePanel:
                        changed |= TogglePanel(ref uiState, openPanels, tooltips, intent.Panel, intent.Param0, intent.Flags);
                        break;

                    case UiIntentKind.OpenPanel:
                        changed |= OpenPanel(ref uiState, openPanels, tooltips, intent.Panel, intent.Param0, intent.Flags);
                        break;

                    case UiIntentKind.ClosePanel:
                        changed |= ClosePanel(ref uiState, openPanels, tooltips, intent.Panel, intent.Flags);
                        break;

                    case UiIntentKind.CloseTopLayer:
                        changed |= CloseTopLayer(ref uiState, openPanels, tooltips);
                        break;

                    case UiIntentKind.SetInventoryTab:
                    {
                        var tab = (byte)math.clamp((int)intent.Data0, 0, UiKernelConstants.InventoryTabCount - 1);
                        if (uiState.InventoryTab != tab)
                        {
                            uiState.InventoryTab = tab;
                            changed = true;
                        }
                        break;
                    }

                    case UiIntentKind.PushTooltip:
                        changed |= PushTooltip(tooltips, intent);
                        break;

                    case UiIntentKind.PinTopTooltip:
                        changed |= PinTopTooltip(tooltips);
                        break;

                    case UiIntentKind.PopTooltip:
                        changed |= PopTooltip(tooltips);
                        break;

                    case UiIntentKind.PopAllTooltips:
                        changed |= PopAllTooltips(tooltips);
                        break;
                }
            }

            // Keep convenience state in sync with buffer-based truth.
            uiState.ActivePrimaryPanel = ResolveActivePrimaryPanel(openPanels);
            uiState.InventoryOpen = (byte)(HasOpenPanel(openPanels, UiPanelKind.Inventory) ? 1 : 0);
            uiState.TooltipDepth = (byte)math.min(tooltips.Length, byte.MaxValue);
            uiState.TooltipPinnedCount = CountPinned(tooltips);

            if (changed)
            {
                uiState.Revision++;
            }

            intents.Clear();
            entityManager.SetComponentData(root, uiState);
        }

        private static bool TogglePanel(
            ref UiKernelState uiState,
            DynamicBuffer<UiOpenPanel> openPanels,
            DynamicBuffer<UiTooltipEntry> tooltips,
            UiPanelKind panel,
            byte layer,
            byte flags)
        {
            if (panel == UiPanelKind.None)
            {
                return false;
            }

            if (TryFindPanel(openPanels, panel, out _))
            {
                return ClosePanel(ref uiState, openPanels, tooltips, panel, flags);
            }

            return OpenPanel(ref uiState, openPanels, tooltips, panel, layer, flags);
        }

        private static bool OpenPanel(
            ref UiKernelState uiState,
            DynamicBuffer<UiOpenPanel> openPanels,
            DynamicBuffer<UiTooltipEntry> tooltips,
            UiPanelKind panel,
            byte layer,
            byte flags)
        {
            if (panel == UiPanelKind.None || TryFindPanel(openPanels, panel, out _))
            {
                return false;
            }

            openPanels.Add(new UiOpenPanel
            {
                Panel = panel,
                Layer = layer,
                IsModal = (byte)((flags & UiIntentFlags.Modal) != 0 ? 1 : 0),
                Flags = flags
            });

            if ((flags & UiIntentFlags.KeepTooltips) == 0)
            {
                ClearUnpinnedTooltips(tooltips);
            }

            if (panel == UiPanelKind.Inventory)
            {
                uiState.InventoryOpen = 1;
            }

            return true;
        }

        private static bool ClosePanel(
            ref UiKernelState uiState,
            DynamicBuffer<UiOpenPanel> openPanels,
            DynamicBuffer<UiTooltipEntry> tooltips,
            UiPanelKind panel,
            byte flags)
        {
            if (!TryFindPanel(openPanels, panel, out var panelIndex))
            {
                return false;
            }

            openPanels.RemoveAt(panelIndex);

            if ((flags & UiIntentFlags.KeepTooltips) == 0)
            {
                ClearUnpinnedTooltips(tooltips);
            }

            if (panel == UiPanelKind.Inventory)
            {
                uiState.InventoryOpen = 0;
            }

            return true;
        }

        private static bool CloseTopLayer(
            ref UiKernelState uiState,
            DynamicBuffer<UiOpenPanel> openPanels,
            DynamicBuffer<UiTooltipEntry> tooltips)
        {
            if (tooltips.Length > 0)
            {
                tooltips.RemoveAt(tooltips.Length - 1);
                return true;
            }

            if (openPanels.Length == 0)
            {
                return false;
            }

            var top = openPanels[openPanels.Length - 1];
            return ClosePanel(ref uiState, openPanels, tooltips, top.Panel, top.Flags);
        }

        private static bool PushTooltip(DynamicBuffer<UiTooltipEntry> tooltips, in UiIntent intent)
        {
            if (tooltips.Length >= UiKernelConstants.MaxTooltipDepth)
            {
                // Preserve the lower levels and replace the top-most tooltip.
                tooltips.RemoveAt(tooltips.Length - 1);
            }

            var parentIndex = tooltips.Length > 0 ? (sbyte)(tooltips.Length - 1) : (sbyte)-1;
            var anchor = (UiTooltipAnchor)math.clamp(
                (int)intent.Param0,
                (int)UiTooltipAnchor.Cursor,
                (int)UiTooltipAnchor.World);

            tooltips.Add(new UiTooltipEntry
            {
                Anchor = anchor,
                Mode = UiTooltipMode.Hover,
                Depth = (byte)tooltips.Length,
                ParentIndex = parentIndex,
                Token = intent.Data0,
                Subject = intent.Target,
                PrimaryKey = intent.PrimaryKey,
                SecondaryKey = intent.SecondaryKey,
                ScreenPosition = intent.ScreenPosition,
                WorldPosition = intent.WorldPosition,
                Flags = intent.Flags
            });

            return true;
        }

        private static bool PinTopTooltip(DynamicBuffer<UiTooltipEntry> tooltips)
        {
            if (tooltips.Length == 0)
            {
                return false;
            }

            var top = tooltips[tooltips.Length - 1];
            if (top.Mode == UiTooltipMode.Pinned)
            {
                return false;
            }

            top.Mode = UiTooltipMode.Pinned;
            tooltips[tooltips.Length - 1] = top;
            return true;
        }

        private static bool PopTooltip(DynamicBuffer<UiTooltipEntry> tooltips)
        {
            if (tooltips.Length == 0)
            {
                return false;
            }

            tooltips.RemoveAt(tooltips.Length - 1);
            return true;
        }

        private static bool PopAllTooltips(DynamicBuffer<UiTooltipEntry> tooltips)
        {
            if (tooltips.Length == 0)
            {
                return false;
            }

            tooltips.Clear();
            return true;
        }

        private static void ClearUnpinnedTooltips(DynamicBuffer<UiTooltipEntry> tooltips)
        {
            for (var i = tooltips.Length - 1; i >= 0; i--)
            {
                if (tooltips[i].Mode != UiTooltipMode.Pinned)
                {
                    tooltips.RemoveAt(i);
                }
            }
        }

        private static byte CountPinned(DynamicBuffer<UiTooltipEntry> tooltips)
        {
            byte count = 0;
            for (var i = 0; i < tooltips.Length; i++)
            {
                if (tooltips[i].Mode == UiTooltipMode.Pinned)
                {
                    count++;
                }
            }

            return count;
        }

        private static UiPanelKind ResolveActivePrimaryPanel(DynamicBuffer<UiOpenPanel> openPanels)
        {
            for (var i = openPanels.Length - 1; i >= 0; i--)
            {
                if (openPanels[i].Layer == 0)
                {
                    return openPanels[i].Panel;
                }
            }

            return UiPanelKind.None;
        }

        private static bool HasOpenPanel(DynamicBuffer<UiOpenPanel> openPanels, UiPanelKind panel)
        {
            for (var i = 0; i < openPanels.Length; i++)
            {
                if (openPanels[i].Panel == panel)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindPanel(DynamicBuffer<UiOpenPanel> openPanels, UiPanelKind panel, out int index)
        {
            for (var i = 0; i < openPanels.Length; i++)
            {
                if (openPanels[i].Panel == panel)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }
    }
}
