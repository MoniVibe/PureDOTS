# UI Kernel Authoring Guide

This guide defines how to design and implement gameplay UI for TRI projects (`space4x`, `godgame`) using the shared PureDOTS UI kernel.

Primary goals:
- Keep UI iteration fast for iterator lanes.
- Keep gameplay simulation code decoupled from UI framework details.
- Ensure new windows/tabs are portable across games.

## Kernel Sources Of Truth

Shared UI state and intents live in:
- `Packages/com.moni.puredots/Runtime/Runtime/UI/UiKernelComponents.cs`
- `Packages/com.moni.puredots/Runtime/Systems/UI/UiKernelBootstrapSystem.cs`
- `Packages/com.moni.puredots/Runtime/Systems/UI/UiKernelIntentSystem.cs`

Do not bypass these contracts for gameplay UI.

## Authoring Model

Use this model for every gameplay window:

1. **View Model Producer (ECS)**
   - Game systems publish UI-ready data (small, read-focused state).
   - Example: inventory summary, selected entity details, action availability.

2. **Intent Consumer/State (Kernel)**
   - UI writes `UiIntent` only.
   - Kernel updates `UiKernelState`, `UiOpenPanel`, and `UiTooltipEntry`.

3. **Renderer Adapter (UI Toolkit)**
   - Reads kernel state + view model.
   - Binds controls to kernel intents.
   - Must not execute gameplay logic directly.

## Rules For Iterators

- UI code can **read**:
  - `UiKernelState`
  - `UiOpenPanel`
  - `UiTooltipEntry`
  - game-specific view model components/buffers
- UI code can **write**:
  - `UiIntent` buffer on `UiKernelRootTag`
- UI code must **not**:
  - mutate simulation components directly for menu interactions
  - create hidden one-off hotkey state outside kernel for shared panels

## Panel And Tab Conventions

- Each window maps to one canonical `UiPanelKind`.
- Tabs are numeric and stable (0..N-1), carried via `UiIntentKind.SetInventoryTab` style intents.
- Close behavior uses `UiIntentKind.CloseTopLayer` so Esc/back is consistent.
- If a panel is modal, set `UiIntentFlags.Modal`.

## Tooltip And Sub-Tooltip Conventions

- Tooltip stack is represented by `UiTooltipEntry` buffer.
- Max depth defaults to `UiKernelConstants.MaxTooltipDepth`.
- Hover tooltip: `UiTooltipMode.Hover`.
- Pinned/expanded tooltip: `UiTooltipMode.Pinned`.
- Use stable keys:
  - `PrimaryKey`: canonical concept id (`resource.ore`, `crew.morale`)
  - `SecondaryKey`: context id (`facility.12`, `ship.alpha`)

This supports CK/Stellaris-style layered explanations without hardcoding widget trees.

## Implementation Workflow (Per Window)

1. Fill `UIWindowTabSpec_TEMPLATE.md` for the window.
2. Add/confirm view model producers in game code.
3. Build/update UI Toolkit renderer (UXML/USS + adapter script).
4. Wire interactions to `UiIntent`.
5. Verify keyboard/mouse flow and tooltip layering.
6. Add or update smoke scenario coverage.

## Acceptance Checklist

- Window opens/closes only through kernel intents.
- Esc closes top layer first.
- Tab selection round-trips through kernel state.
- No gameplay-side direct mutation from UI event handlers.
- Tooltip chain depth and pin behavior are deterministic.
- Works in both game resolutions used by smoke tests.

## Fleetcrawl Inventory Reference

Current bridge example:
- `Assets/Scripts/Space4x/Scenario/Space4XFleetcrawlUiOverlayMono.cs`

The overlay now emits inventory intents (`TogglePanel`, `SetInventoryTab`, `CloseTopLayer`) and reads kernel state when available.
