# PureDOTS Rendering Contracts (Shared Layer)

Minimal, game-agnostic rendering scaffolding for the Body world. PureDOTS does **not** know about meshes or materials; games bind those via their own render catalogs.

## Components
- `RenderKey { ushort ArchetypeId; byte LOD; }` – stable identifier/LOD authored by simulation.
- `RenderFlags { byte Visible; byte ShadowCaster; byte HighlightMask; }` – lightweight presentation toggles.
- `RenderOwner { Entity Owner; }` – optional link for proxies/impostors.

## Guard Systems (Body world)
- `EnsureRenderTransformSystem` – adds `LocalTransform.Identity` to any `RenderKey` entity missing one.
- `RenderSanitySystem` – warns once if visible `RenderKey` entities exist but `MaterialMeshInfo` count is zero (broken catalog/bootstrap).

## Expected Flow
1) Sim spawners attach `RenderKey` + `RenderFlags` (+ `LocalTransform`).
2) Game-side ApplyRenderCatalogSystem resolves `RenderKey.ArchetypeId` into `MaterialMeshInfo`/bounds from a baked `RenderMeshArray`.
3) Entities Graphics `RenderingSystemGroup` runs in **Body** world only; Mind/Aggregate worlds never carry render components.
