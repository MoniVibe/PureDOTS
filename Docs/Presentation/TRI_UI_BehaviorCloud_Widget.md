# TRI — Behavior Cloud Widget (UI + RenderTexture)
**Status:** Advisor plan captured (tailored to TRI: PureDOTS + Space4X + Godgame)  
**Last Updated:** 2025-12-21  
**Goal:** Lock a stable, deterministic mapping from “profile” data → a small set of 3D points/lines, then render it via a **card shell + RenderTexture** so UI can get fancy without ever touching PureDOTS simulation.

---

## 0) Why this exists (TRI-specific intent)

We’ve been headless-first (proof/telemetry). This widget is the first “real UI” slice that:
- Surfaces **alignment/behavior state** in a readable, scalable way.
- Keeps a **hard sim/presentation boundary**: PureDOTS outputs a tiny buffer; game UI renders it.
- Works in both games:
  - **Space4X:** UI-first (crew/pops are UI-only) → this is a primary way to “see people.”
  - **Godgame:** 3D world + selection → this becomes a villager/settlement inspect panel and tooltip.

Core rule: **Define the 3D meaning once and never change it.** No “moving goalposts” UI.

---

## 1) What the cloud represents (data → visual channels)

Treat the cloud as a constellation around a **core**.

### 1.1 Core (Alignment: 3 axes)
Moral (Evil↔Good), Order (Chaotic↔Lawful), Purity (Corrupt↔Pure).

This is the anchor orientation/position of the whole widget and also the **true coordinate frame** (see §2).

### 1.2 Outlook petals (8 axes)
Aggression, Honor, Order, Empathy, Greed, Xenophobia, Pride, Mercy.

Eight points orbiting the core.
- Magnitude = intensity (`abs(value)`).
- Sign = style side (`sign(value)`), not direction (prevents point overlap).

### 1.3 Personality / temperament pair (2 axes)
Vengeful↔Forgiving, Bold↔Craven.

Two points closer to the core (temperament rather than culture).

> **TRI note:** In PureDOTS today, these are already modeled as `VillagerBehavior.VengefulScore` and `VillagerBehavior.BoldScore` (see `puredots/Packages/com.moni.puredots/Runtime/Runtime/Villagers/VillagerBehavior.cs`).  
> Godgame also has `Godgame.Villagers.VillagerPersonality` (duplicate concept); we should treat “temperament pair” as one shared logical channel, even if names differ per repo today.

### 1.4 Disposition ring (5 axes)
Loyalty, Fear, Love, Trust, Respect.

Five points at close–mid radius (contextual/social relationship layer).

### 1.5 Traits glyphs (bitfield: 7 flags + combos)
Lawful, Chaotic, Good, Evil, Warlike, Peaceful, Corrupt.

Show as discrete glyph points (on/off), plus optional combo links (purely presentation):
- Honor-bound
- Raiders
- Pacifist monks
- Berserkers

This gives a readable widget with ~23 markers:
`1 core + 8 outlook + 2 temperament + 5 disposition + 7 traits = 23` (cheap to render, easy to pick).

---

## 2) Spatial mapping (stable 3D layout)

### 2.1 Coordinate frame (Alignment is the real 3D meaning)
Define axes exactly once and never change them.

**Axis definition (normalized -1..+1):**
- `X = OrderAxis / 100` (Chaotic -1 … +1 Lawful)
- `Y = MoralAxis / 100` (Evil -1 … +1 Good)
- `Z = PurityAxis / 100` (Corrupt -1 … +1 Pure)

**Core position:**
```
A = float3(Order, Moral, Purity) / 100f;
CorePos = A * CoreRadius;
```

> **TRI note:** PureDOTS uses `sbyte MoralAxis/OrderAxis/PurityAxis` already; conversion to float is `axis * 0.01f`.

### 2.2 Petal points around the core (fixed directions)
Each dimension gets a fixed unit direction `D[i]` (stored in a BlobAsset).

**Position rule:**
```
v = clamp(value / 100f, -1f, +1f);
Pos = CorePos + D[i] * (abs(v) * AxisRadius);
Sign = sign(v);          // styling (dash/flip/alpha), not direction overlap
Intensity = abs(v);      // size/emissive
```

Why `abs()` for radius:
- Avoids “negative overlaps” (points colliding because directions are antipodal).
- Sign is still encoded via styling/label.

### 2.3 Trait flags + combos
Each flag has a fixed direction `T[i]`.

**Position:**
```
Pos = CorePos + T[i] * TraitRadius;
Intensity = isSet ? 1 : 0.15;
```

**Combo visualization:** add link segments between involved traits when the combo predicate is true (presentation-only).

---

## 3) PureDOTS contract (game-agnostic)

### 3.1 Canonical input components (packed, Burst-friendly)
This is the target contract the cloud builder reads. Keep ranges stable (`-100..+100`, or `0..100` for unsigned).

> **Important:** This is a *design contract*. In TRI today, only alignment + temperament exist broadly; outlook/disposition/trait flags may be added later. The builder must **degrade gracefully** when components are missing.

```csharp
using Unity.Entities;
using Unity.Mathematics;

public struct VillagerAlignment : IComponentData
{
    public sbyte MoralAxis;   // -100..100
    public sbyte OrderAxis;   // -100..100
    public sbyte PurityAxis;  // -100..100
}

// Outlook petals (8)
public struct VillagerOutlook0 : IComponentData
{
    public short4 Value; // Aggression, Honor, Order, Empathy  (-100..100)
}

public struct VillagerOutlook1 : IComponentData
{
    public short4 Value; // Greed, Xenophobia, Pride, Mercy    (-100..100)
}

// Temperament pair (2)
public struct VillagerPersonality : IComponentData
{
    public sbyte Vengeful; // -100..100  (- = vengeful, + = forgiving)
    public sbyte Bold;     // -100..100  (- = craven, + = bold)
}

// Disposition ring (5)
public struct VillagerDisposition : IComponentData
{
    public sbyte Loyalty;  // -100..100 (signed)
    public byte Fear;      // 0..100
    public byte Love;      // 0..100
    public byte Trust;     // 0..100
    public byte Respect;   // 0..100
}

public struct TraitFlags : IComponentData
{
    public ushort Bits; // 7 bits used
}
```

### 3.2 Behavior cloud output buffer (what UI consumes)
Keep it tiny and fixed-capacity. The UI resolves `Id` → label/icon locally (no strings in Burst).

```csharp
public enum CloudChannel : byte
{
    AlignmentCore,
    Outlook,
    Personality,
    Disposition,
    Trait,
    ComboLink
}

public struct BehaviorCloudPoint : IBufferElementData
{
    public float3 Position;
    public float  Intensity;  // 0..1
    public sbyte  Sign;       // -1,0,+1 where relevant
    public CloudChannel Channel;
    public ushort Id;         // stable id for picking/labels
}
```

### 3.3 Layout definition (BlobAsset)
Store fixed directions in a Blob so it’s flexible and fast. Build once at boot; share across entities.

```csharp
public struct BehaviorCloudLayout
{
    public BlobArray<float3> OutlookDirs;      // 8
    public BlobArray<float3> PersonalityDirs;  // 2
    public BlobArray<float3> DispositionDirs;  // 5
    public BlobArray<float3> TraitDirs;        // 7
}

public struct BehaviorCloudLayoutRef : IComponentData
{
    public BlobAssetReference<BehaviorCloudLayout> Value;
}
```

**Recurring pitfall:** do not rebuild the Blob per entity/per frame.

---

## 4) Systems order of execution (selection → build → render/pick)

### 4.1 Pipeline (strict responsibilities)
1. **Selection system** (game input) sets `SelectedEntity` on a single “widget” entity.
2. **BehaviorCloudBuildSystem** (PureDOTS, Burst) reads selected entity profile components, writes `DynamicBuffer<BehaviorCloudPoint>` on the widget entity.
3. **BehaviorCloudRenderBridge** (game-side, non-Burst) reads that buffer and updates the RenderTexture rig (instanced draw or pooled renderers).
4. **Tooltip UI** displays the RenderTexture and routes pointer deltas to camera rig + shader props.
5. **Picking** raycasts against the points (CPU), sets `HoveredId` back on the widget entity (optional) so UI can label/highlight.

### 4.2 Recurring pitfalls to actively avoid
- Don’t resize buffers every frame: **pre-size once** (e.g., 64) and overwrite.
- Don’t create/destroy renderers on hover: pool once; update matrices/properties only.
- Don’t query the sim from MonoBehaviour directly (no “DotsDebugHUD style” polling); keep the bridge consuming the buffer only.

---

## 5) Burst build system sketch (single-entity, no churn)

Selection component:
```csharp
public struct SelectedEntity : IComponentData { public Entity Value; }
```

Builder system (sketch):
```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct BehaviorCloudBuildSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SelectedEntity>();
        state.RequireForUpdate<BehaviorCloudLayoutRef>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var widget = SystemAPI.GetSingletonEntity<SelectedEntity>();
        var target = SystemAPI.GetComponent<SelectedEntity>(widget).Value;
        var layout = SystemAPI.GetComponent<BehaviorCloudLayoutRef>(widget).Value;
        var buf = SystemAPI.GetBuffer<BehaviorCloudPoint>(widget);

        const int Max = 64;
        if (buf.Length != Max) buf.ResizeUninitialized(Max);
        for (int i = 0; i < Max; i++) buf[i] = default;

        if (target == Entity.Null || !SystemAPI.Exists(target)) return;

        // Degrade gracefully: only write channels that exist.
        bool hasAlign = SystemAPI.HasComponent<VillagerAlignment>(target);
        bool hasO0 = SystemAPI.HasComponent<VillagerOutlook0>(target);
        bool hasO1 = SystemAPI.HasComponent<VillagerOutlook1>(target);
        bool hasPers = SystemAPI.HasComponent<VillagerPersonality>(target);
        bool hasDisp = SystemAPI.HasComponent<VillagerDisposition>(target);
        bool hasTraits = SystemAPI.HasComponent<TraitFlags>(target);

        float3 core = float3.zero;
        if (hasAlign)
        {
            var a = SystemAPI.GetComponent<VillagerAlignment>(target);
            core = new float3(a.OrderAxis, a.MoralAxis, a.PurityAxis) / 100f;
        }

        const float CoreRadius = 0.35f;
        const float AxisRadius = 0.55f;
        const float TraitRadius = 0.85f;
        float3 corePos = core * CoreRadius;

        int w = 0;
        buf[w++] = new BehaviorCloudPoint
        {
            Position = corePos,
            Intensity = math.saturate(math.length(core)),
            Sign = 0,
            Channel = CloudChannel.AlignmentCore,
            Id = 1
        };

        // Outlook, Personality, Disposition, Traits follow the fixed mapping rules (see §2).
    }
}
```

---

## 6) Game/UI rendering (hybrid, minimal, fast)

### 6.1 RenderTexture rig
- Dedicated camera renders a `Tooltip3D` layer → RenderTexture.
- UI displays the RenderTexture inside a “card shell” (UGUI Image / UI Toolkit Image).
- Parallax: pointer delta rotates a pivot transform (or updates shader params).
- Special effects: card image material can do scanline/glow/distortion driven by pointer + hover intensity.

### 6.2 Draw method options
Pick one per game (both obey the same buffer contract):

**Option A (simplest):** pool ~64 GameObjects with MeshRenderer; set transforms from buffer.  
**Option B (clean + cheap):** `Graphics.DrawMeshInstanced` with cached matrices + `MaterialPropertyBlock` for intensity/sign (no transforms, no scene clutter).

### 6.3 Picking (23 spheres)
- Convert pointer position on the tooltip image → viewport UV → ray from tooltip camera.
- Test ray vs spheres for each point (≈23 checks).
- On hit: show label/highlight; optionally write `HoveredId` back to ECS.

---

## 7) Extension: compare mode (factions/relations)

Without changing the pipeline:
- Build **two clouds** (selected entity vs target faction / rival god / reference profile).
- Compute **L1 distance** on outlook axes in PureDOTS (you already use L1 in other metric work).
- UI shows:
  - Single compatibility bar
  - Optional linking lines between matching axes (same `Id`) with thickness = `abs(delta)`

---

## 8) Acceptance criteria (TRI-friendly)

### Stability (definition locked)
- Axis meanings (X/Y/Z) do not change across builds.
- All non-alignment axes use fixed Blob directions and stable ids.

### Boundary (PureDOTS safe)
- PureDOTS only writes `BehaviorCloudPoint` buffer; no UI references, no strings, no GameObjects.
- Game-side bridge does not query arbitrary sim state; it consumes the buffer and renders.

### Performance (scales)
- Fixed buffer capacity; no per-frame allocs.
- Renderer pooling or instancing only; no create/destroy on hover.
