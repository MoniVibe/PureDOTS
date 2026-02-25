# UIWindow/Tab Spec Template

Use this template before implementing any new gameplay window or major tab set.

---

## 1) Identity

- Window name:
- Owning game(s): `space4x` / `godgame` / both
- Spec owner:
- Date:
- Status: draft / approved / in-progress / shipped

## 2) Kernel Mapping

- `UiPanelKind`:
- Opens via intents:
- Closes via intents:
- Modal? yes/no
- Keep tooltips on open/close? yes/no

Intent map:
- Open:
- Close:
- Toggle:
- Escape/Back behavior:

## 3) Player Entry Points

- Primary access path (button/menu):
- Hotkeys:
- Contextual access (selection-dependent):
- Lockouts (when unavailable):

## 4) Window Purpose

- Primary player questions this window answers:
- Decisions enabled by this window:
- Non-goals (what this window does not do):

## 5) Tab Model

Define every tab with stable id and data ownership.

| Tab Id | Label | Purpose | Required View Model | Empty State | Primary Actions |
|---|---|---|---|---|---|
| 0 |  |  |  |  |  |
| 1 |  |  |  |  |  |
| 2 |  |  |  |  |  |

Tab switching:
- Source intent(s):
- Persisted in kernel state field:
- Default tab rule:

## 6) View Model Contracts

List UI-ready ECS contracts only (not raw simulation internals).

- Required components:
- Required buffers:
- Producer systems:
- Update group and order:
- Freshness expectation (every frame / throttled / event-driven):

## 7) Actions And Commands

For each button/interaction, map to intent.

| UI Action | Intent Kind | Panel | Data0/Param | Expected Result | Failure/Disabled Rule |
|---|---|---|---|---|---|
|  |  |  |  |  |  |

## 8) Tooltip Plan (Including Sub-Tooltips)

Define explainability chain using `UiTooltipEntry`.

| Token/Key | Trigger Element | Anchor | Mode | Parent Token | Content Source |
|---|---|---|---|---|---|
|  |  | Cursor/Element/World | Hover/Pinned | none |  |

Rules:
- Max depth used:
- Pin behavior:
- Esc behavior with tooltip stack:
- Fallback when data missing:

## 9) Layout And Visual System

- UI technology: UI Toolkit / IMGUI (debug-only)
- UXML path:
- USS path:
- Naming convention for element ids:
- Responsive breakpoints / panel docking:
- Min resolution target:

## 10) Accessibility And Input

- Keyboard navigation path:
- Focus order defined? yes/no
- Hover-only info duplicated for keyboard? yes/no
- Text scaling behavior:
- Contrast constraints:

## 11) Telemetry And Debug

- Metrics/events to emit:
- Debug overlay hooks:
- Validation logs expected:

## 12) Test Plan

Manual checks:
- Open/close flow
- Tab switching
- Tooltip chain and sub-tooltip behavior
- Hotkeys and Esc/back handling
- Resolution/aspect coverage

Automated checks:
- Unit/system tests:
- PlayMode tests:

## 13) Rollout Notes

- Behind flag? yes/no
- Migration impact on existing windows:
- Iterator handoff notes:
- Validator checklist:

---

## Quick Copy Block

```
Window:
PanelKind:
Tabs:
ViewModel Producers:
Open Intent:
Close Intent:
Tooltip Depth:
Hotkeys:
Smoke Scene:
Owner:
```
