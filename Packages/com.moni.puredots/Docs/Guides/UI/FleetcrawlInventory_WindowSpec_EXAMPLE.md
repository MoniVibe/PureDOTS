# Fleetcrawl Inventory Window Spec (Example)

This is a concrete example based on current Fleetcrawl inventory behavior.

## 1) Identity

- Window name: Fleetcrawl Inventory
- Owning game(s): `space4x`
- Status: in-progress (kernel-wired input path)

## 2) Kernel Mapping

- `UiPanelKind`: `Inventory`
- Opens via intents: `UiIntentKind.TogglePanel`
- Closes via intents: `UiIntentKind.CloseTopLayer`
- Modal: no
- Keep tooltips: no

## 3) Player Entry Points

- Primary: `I` key
- Close: `Esc`
- Tab hotkeys: `1..4`

## 4) Tab Model

| Tab Id | Label | Purpose |
|---|---|---|
| 0 | Cargo/Logistics | Resource and cargo context |
| 1 | Crew | Crew summary/state |
| 2 | Captain | Captain-focused status |
| 3 | Hangar | Strike craft and hangar info |

Tab switch intent:
- `UiIntentKind.SetInventoryTab`
- `Data0` carries tab id.

## 5) View Model Contracts

Current implementation reads directly in overlay; target state is to split into dedicated view model producers.

Near-term contracts to extract:
- inventory summary
- crew summary
- captain summary
- hangar summary

## 6) Actions And Commands

| UI Action | Intent Kind | Panel | Data0 |
|---|---|---|---|
| Toggle Inventory | TogglePanel | Inventory | 0 |
| Close Top Layer | CloseTopLayer | None | 0 |
| Set Cargo Tab | SetInventoryTab | Inventory | 0 |
| Set Crew Tab | SetInventoryTab | Inventory | 1 |
| Set Captain Tab | SetInventoryTab | Inventory | 2 |
| Set Hangar Tab | SetInventoryTab | Inventory | 3 |

## 7) Tooltip Plan

Current Fleetcrawl overlay has no kernel tooltip chain yet.

Planned:
- hover row -> `PushTooltip` (summary)
- pin detail -> `PinTopTooltip`
- nested terms -> additional `PushTooltip` with parent index

## 8) Implementation Reference

- `Assets/Scripts/Space4x/Scenario/Space4XFleetcrawlUiOverlayMono.cs`
- `Packages/com.moni.puredots/Runtime/Runtime/UI/UiKernelComponents.cs`
- `Packages/com.moni.puredots/Runtime/Systems/UI/UiKernelIntentSystem.cs`
