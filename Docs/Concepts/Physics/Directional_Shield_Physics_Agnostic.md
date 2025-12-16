# Directional Shield Physics Framework (Agnostic)

## Overview

This document provides **project-agnostic mathematical algorithms** for directional shielding with localized surface area damage tracking. These algorithms enable realistic shield failure mechanics where concentrated attacks create breaches while distributed damage weakens overall integrity.

The framework supports two implementation tiers:
1. **Sector-Based** (Godgame): 6 directional sectors with HP pools, low performance cost
2. **Surface-Area Grid** (Space4X): High-resolution 3D grids with per-cell damage, higher fidelity

All formulas use **SI units** and are designed for Unity DOTS ECS architecture.

---

## Core Algorithms

### 1. Sector Determination Algorithm

Calculates which shield sector an attack hits based on impact angle.

#### Input Parameters
```csharp
struct SectorInput
{
    float3 DefenderPosition;        // Shield owner position
    float3 DefenderForward;         // Direction shield is facing (normalized)
    float3 DefenderUp;              // Up vector for elevation calculation
    float3 AttackerPosition;        // Source of attack
}
```

#### Algorithm
```csharp
public static ShieldSector DetermineSector(SectorInput input)
{
    // Calculate attack direction vector
    float3 attackDirection = math.normalize(input.AttackerPosition - input.DefenderPosition);

    // Check elevation first (top/bottom priority)
    float elevation = math.dot(attackDirection, input.DefenderUp);
    float elevationAngle = math.degrees(math.asin(elevation));

    if (elevationAngle > 45f)
        return ShieldSector.Top;
    if (elevationAngle < -45f)
        return ShieldSector.Bottom;

    // Calculate horizontal angle
    float3 defenderRight = math.cross(input.DefenderUp, input.DefenderForward);
    float forwardDot = math.dot(attackDirection, input.DefenderForward);
    float rightDot = math.dot(attackDirection, defenderRight);

    float horizontalAngle = math.degrees(math.atan2(rightDot, forwardDot));

    // Normalize to 0-360 range
    if (horizontalAngle < 0)
        horizontalAngle += 360f;

    // Determine sector based on angle
    if (horizontalAngle < 60f || horizontalAngle >= 300f)
        return ShieldSector.Front;
    else if (horizontalAngle >= 60f && horizontalAngle < 120f)
        return ShieldSector.Right;
    else if (horizontalAngle >= 120f && horizontalAngle < 240f)
        return ShieldSector.Rear;
    else // 240-300
        return ShieldSector.Left;
}

public enum ShieldSector : byte
{
    Front = 0,
    Right = 1,
    Rear = 2,
    Left = 3,
    Top = 4,
    Bottom = 5
}
```

#### Output
- **Shield Sector**: Enum indicating which sector was hit

#### Example
```
Defender at [0, 0, 0], facing [0, 0, 1] (North)
Attacker at [5, 2, 3]
Attack direction: normalize([5, 2, 3]) = [0.67, 0.27, 0.40]

Elevation: dot([0.67, 0.27, 0.40], [0, 1, 0]) = 0.27
Elevation angle: asin(0.27) = 15.66° (not top/bottom)

Forward dot: dot([0.67, 0.27, 0.40], [0, 0, 1]) = 0.40
Right dot: dot([0.67, 0.27, 0.40], [1, 0, 0]) = 0.67
Horizontal angle: atan2(0.67, 0.40) = 59.2°

Result: Front sector (0-60° range)
```

---

### 2. Adjacent Sector Lookup Algorithm

Determines which sectors are adjacent to a given sector for damage spread.

#### Algorithm
```csharp
public static FixedList32Bytes<ShieldSector> GetAdjacentSectors(ShieldSector sector)
{
    var adjacent = new FixedList32Bytes<ShieldSector>();

    switch (sector)
    {
        case ShieldSector.Front:
            adjacent.Add(ShieldSector.Right);
            adjacent.Add(ShieldSector.Left);
            break;
        case ShieldSector.Right:
            adjacent.Add(ShieldSector.Front);
            adjacent.Add(ShieldSector.Rear);
            break;
        case ShieldSector.Rear:
            adjacent.Add(ShieldSector.Right);
            adjacent.Add(ShieldSector.Left);
            break;
        case ShieldSector.Left:
            adjacent.Add(ShieldSector.Front);
            adjacent.Add(ShieldSector.Rear);
            break;
        case ShieldSector.Top:
            // Top is adjacent to all horizontal sectors
            adjacent.Add(ShieldSector.Front);
            adjacent.Add(ShieldSector.Right);
            adjacent.Add(ShieldSector.Rear);
            adjacent.Add(ShieldSector.Left);
            break;
        case ShieldSector.Bottom:
            // Bottom is adjacent to all horizontal sectors
            adjacent.Add(ShieldSector.Front);
            adjacent.Add(ShieldSector.Right);
            adjacent.Add(ShieldSector.Rear);
            adjacent.Add(ShieldSector.Left);
            break;
    }

    return adjacent;
}
```

#### Output
- **Adjacent Sectors**: List of 2-4 adjacent sectors

#### Example
```
Input: ShieldSector.Front
Output: [Right, Left]

Input: ShieldSector.Top
Output: [Front, Right, Rear, Left]
```

---

### 3. Damage Distribution Algorithm

Calculates how damage is distributed across sectors based on damage type.

#### Input Parameters
```csharp
struct DamageDistributionInput
{
    float TotalDamage;
    DamageType Type;                // Piercing or Blunt
    ShieldSector HitSector;
}

public enum DamageType : byte
{
    Piercing = 0,   // 100% to hit sector
    Blunt = 1,      // 60% hit, 20% each adjacent
    Energy = 2,     // 80% hit, 10% each adjacent
    Explosive = 3   // 40% hit, 15% each adjacent, 10% all others
}
```

#### Algorithm
```csharp
public struct SectorDamageResult
{
    public ShieldSector Sector;
    public float Damage;
}

public static NativeArray<SectorDamageResult> DistributeDamage(
    DamageDistributionInput input,
    Allocator allocator)
{
    var results = new NativeList<SectorDamageResult>(6, allocator);

    switch (input.Type)
    {
        case DamageType.Piercing:
            // 100% to hit sector
            results.Add(new SectorDamageResult
            {
                Sector = input.HitSector,
                Damage = input.TotalDamage
            });
            break;

        case DamageType.Blunt:
            // 60% to hit sector, 20% to each adjacent
            results.Add(new SectorDamageResult
            {
                Sector = input.HitSector,
                Damage = input.TotalDamage * 0.6f
            });

            var adjacentBlunt = GetAdjacentSectors(input.HitSector);
            foreach (var adj in adjacentBlunt)
            {
                results.Add(new SectorDamageResult
                {
                    Sector = adj,
                    Damage = input.TotalDamage * 0.2f
                });
            }
            break;

        case DamageType.Energy:
            // 80% to hit sector, 10% to each adjacent
            results.Add(new SectorDamageResult
            {
                Sector = input.HitSector,
                Damage = input.TotalDamage * 0.8f
            });

            var adjacentEnergy = GetAdjacentSectors(input.HitSector);
            foreach (var adj in adjacentEnergy)
            {
                results.Add(new SectorDamageResult
                {
                    Sector = adj,
                    Damage = input.TotalDamage * 0.1f
                });
            }
            break;

        case DamageType.Explosive:
            // 40% to hit sector, 15% to each adjacent, 10% to all others
            results.Add(new SectorDamageResult
            {
                Sector = input.HitSector,
                Damage = input.TotalDamage * 0.4f
            });

            var adjacentExplosive = GetAdjacentSectors(input.HitSector);
            foreach (var adj in adjacentExplosive)
            {
                results.Add(new SectorDamageResult
                {
                    Sector = adj,
                    Damage = input.TotalDamage * 0.15f
                });
            }

            // Remaining sectors (not hit, not adjacent) get 10%
            for (int i = 0; i < 6; i++)
            {
                ShieldSector sector = (ShieldSector)i;
                if (sector == input.HitSector) continue;
                if (adjacentExplosive.Contains(sector)) continue;

                results.Add(new SectorDamageResult
                {
                    Sector = sector,
                    Damage = input.TotalDamage * 0.1f
                });
            }
            break;
    }

    return results.AsArray();
}
```

#### Output
- **Sector Damage Results**: Array of sector/damage pairs

#### Example
```
Input:
- Total damage: 100
- Type: Blunt
- Hit sector: Front

Output:
- Front: 60 damage (60%)
- Right: 20 damage (20%)
- Left: 20 damage (20%)
Total: 100 damage distributed
```

---

### 4. Sector HP Update Algorithm

Applies distributed damage to sector HP values and checks for breach.

#### Input Parameters
```csharp
struct SectorHPUpdateInput
{
    float FrontHP;
    float RightHP;
    float RearHP;
    float LeftHP;
    float TopHP;
    float BottomHP;
    NativeArray<SectorDamageResult> DamageResults;
}
```

#### Algorithm
```csharp
public struct SectorHPState
{
    public float FrontHP;
    public float RightHP;
    public float RearHP;
    public float LeftHP;
    public float TopHP;
    public float BottomHP;

    public byte BreachedSectorBitmask;  // Bits 0-5 represent sectors
    public float OverflowDamage;        // Damage that penetrated breached sector
}

public static SectorHPState UpdateSectorHP(SectorHPUpdateInput input)
{
    var state = new SectorHPState
    {
        FrontHP = input.FrontHP,
        RightHP = input.RightHP,
        RearHP = input.RearHP,
        LeftHP = input.LeftHP,
        TopHP = input.TopHP,
        BottomHP = input.BottomHP,
        BreachedSectorBitmask = 0,
        OverflowDamage = 0
    };

    foreach (var damageResult in input.DamageResults)
    {
        float overflow = ApplySectorDamage(ref state, damageResult.Sector, damageResult.Damage);
        state.OverflowDamage += overflow;
    }

    return state;
}

private static float ApplySectorDamage(ref SectorHPState state, ShieldSector sector, float damage)
{
    float overflow = 0;

    switch (sector)
    {
        case ShieldSector.Front:
            state.FrontHP -= damage;
            if (state.FrontHP <= 0)
            {
                overflow = math.abs(state.FrontHP);
                state.FrontHP = 0;
                state.BreachedSectorBitmask |= (1 << 0);
            }
            break;

        case ShieldSector.Right:
            state.RightHP -= damage;
            if (state.RightHP <= 0)
            {
                overflow = math.abs(state.RightHP);
                state.RightHP = 0;
                state.BreachedSectorBitmask |= (1 << 1);
            }
            break;

        case ShieldSector.Rear:
            state.RearHP -= damage;
            if (state.RearHP <= 0)
            {
                overflow = math.abs(state.RearHP);
                state.RearHP = 0;
                state.BreachedSectorBitmask |= (1 << 2);
            }
            break;

        case ShieldSector.Left:
            state.LeftHP -= damage;
            if (state.LeftHP <= 0)
            {
                overflow = math.abs(state.LeftHP);
                state.LeftHP = 0;
                state.BreachedSectorBitmask |= (1 << 3);
            }
            break;

        case ShieldSector.Top:
            state.TopHP -= damage;
            if (state.TopHP <= 0)
            {
                overflow = math.abs(state.TopHP);
                state.TopHP = 0;
                state.BreachedSectorBitmask |= (1 << 4);
            }
            break;

        case ShieldSector.Bottom:
            state.BottomHP -= damage;
            if (state.BottomHP <= 0)
            {
                overflow = math.abs(state.BottomHP);
                state.BottomHP = 0;
                state.BreachedSectorBitmask |= (1 << 5);
            }
            break;
    }

    return overflow;
}
```

#### Output
- **Sector HP State**: Updated HP values for all sectors
- **Breached Bitmask**: Bits indicating which sectors are breached
- **Overflow Damage**: Damage that penetrated shield

#### Example
```
Input:
- Front HP: 120
- Right HP: 120
- Left HP: 120
- Damage: [Front: 150]

Output:
- Front HP: 0 (120 - 150 = -30)
- Right HP: 120 (unchanged)
- Left HP: 120 (unchanged)
- Breached bitmask: 0b000001 (bit 0 set = Front breached)
- Overflow damage: 30 (penetrated shield)
```

---

### 5. Shield Regeneration Algorithm

Calculates shield HP recovery over time.

#### Input Parameters
```csharp
struct RegenerationInput
{
    float CurrentHP;
    float MaxHP;
    float RegenRatePerSecond;
    float DeltaTime;
    float LastDamageTimestamp;
    float CurrentTimestamp;
    float RegenDelaySeconds;       // Time to wait after damage before regen starts
    bool IsSectorBreached;         // Cannot regen if breached
}
```

#### Algorithm
```csharp
public static float CalculateRegeneratedHP(RegenerationInput input)
{
    // Cannot regenerate breached sectors
    if (input.IsSectorBreached)
        return input.CurrentHP;

    // Check if regen delay has passed
    float timeSinceDamage = input.CurrentTimestamp - input.LastDamageTimestamp;
    if (timeSinceDamage < input.RegenDelaySeconds)
        return input.CurrentHP; // Still in delay period

    // Calculate regen amount
    float regenAmount = input.RegenRatePerSecond * input.DeltaTime;
    float newHP = input.CurrentHP + regenAmount;

    // Clamp to max HP
    return math.min(newHP, input.MaxHP);
}
```

#### Output
- **Regenerated HP**: New HP value after regeneration

#### Example
```
Current HP: 80
Max HP: 120
Regen rate: 5 HP/s
Delta time: 0.1s (1 frame @ 10 FPS)
Last damage: 3.0 seconds ago
Current time: 5.0 seconds
Regen delay: 2.0 seconds
Breached: false

Time since damage: 5.0 - 3.0 = 2.0 seconds (equals delay, regen starts)
Regen amount: 5 × 0.1 = 0.5 HP
New HP: 80 + 0.5 = 80.5 HP
```

---

### 6. Surface Area Grid Algorithm (Space4X)

For high-fidelity surface damage tracking, subdivide sectors into grids.

#### Input Parameters
```csharp
struct SurfaceGridInput
{
    int GridResolution;            // 10×10 = 100 cells per sector
    float SectorMaxHP;             // Total HP for entire sector
    float3 ImpactPoint;            // World position of impact
    float3 SectorCenterPoint;      // Center of sector surface
    float SectorRadius;            // Radius of sector surface (meters)
}
```

#### Algorithm
```csharp
public struct GridCell
{
    public int CellX;              // 0 to GridResolution-1
    public int CellY;              // 0 to GridResolution-1
    public float HP;               // HP for this cell
}

public static GridCell DetermineHitCell(SurfaceGridInput input)
{
    // Calculate impact position relative to sector center
    float3 relativeImpact = input.ImpactPoint - input.SectorCenterPoint;

    // Project onto sector surface (assume flat approximation)
    float2 surfaceUV = new float2(
        (relativeImpact.x / input.SectorRadius) * 0.5f + 0.5f,
        (relativeImpact.y / input.SectorRadius) * 0.5f + 0.5f
    );

    // Clamp to 0-1 range
    surfaceUV = math.clamp(surfaceUV, 0f, 1f);

    // Convert to grid coordinates
    int cellX = (int)(surfaceUV.x * input.GridResolution);
    int cellY = (int)(surfaceUV.y * input.GridResolution);

    // Clamp to grid bounds
    cellX = math.clamp(cellX, 0, input.GridResolution - 1);
    cellY = math.clamp(cellY, 0, input.GridResolution - 1);

    // Calculate HP per cell (evenly distributed)
    float hpPerCell = input.SectorMaxHP / (input.GridResolution * input.GridResolution);

    return new GridCell
    {
        CellX = cellX,
        CellY = cellY,
        HP = hpPerCell
    };
}
```

#### Output
- **Grid Cell**: X/Y coordinates and HP value

#### Example
```
Grid resolution: 10×10 (100 cells)
Sector max HP: 1,000
Sector radius: 5 meters
Impact point: [1.5, 2.0, 0] (relative to sector center)

Surface UV:
- X: (1.5 / 5) × 0.5 + 0.5 = 0.65
- Y: (2.0 / 5) × 0.5 + 0.5 = 0.7

Grid coordinates:
- Cell X: 0.65 × 10 = 6 (clamped)
- Cell Y: 0.7 × 10 = 7 (clamped)

HP per cell: 1,000 / 100 = 10 HP

Result: Cell [6, 7] with 10 HP
```

---

### 7. Grid Cell Damage Propagation Algorithm

Spreads damage from impact cell to adjacent cells.

#### Input Parameters
```csharp
struct CellDamagePropagationInput
{
    int ImpactCellX;
    int ImpactCellY;
    float TotalDamage;
    float PropagationRadius;       // How many cells away damage spreads
    DamageType Type;
}
```

#### Algorithm
```csharp
public struct CellDamageResult
{
    public int CellX;
    public int CellY;
    public float Damage;
}

public static NativeArray<CellDamageResult> PropagateCellDamage(
    CellDamagePropagationInput input,
    Allocator allocator)
{
    var results = new NativeList<CellDamageResult>(25, allocator);

    // Calculate propagation pattern based on damage type
    float centralDamagePercent = 0.7f;  // 70% to center cell
    float adjacentDamagePercent = 0.075f; // 7.5% to each of 4 adjacent cells

    if (input.Type == DamageType.Blunt)
    {
        // Blunt spreads more
        centralDamagePercent = 0.5f;
        adjacentDamagePercent = 0.125f; // 12.5% to each adjacent
    }
    else if (input.Type == DamageType.Piercing)
    {
        // Piercing focuses more
        centralDamagePercent = 0.9f;
        adjacentDamagePercent = 0.025f; // 2.5% to each adjacent
    }

    // Apply damage to impact cell
    results.Add(new CellDamageResult
    {
        CellX = input.ImpactCellX,
        CellY = input.ImpactCellY,
        Damage = input.TotalDamage * centralDamagePercent
    });

    // Apply damage to adjacent cells (4-connected)
    int[] dx = { -1, 1, 0, 0 };
    int[] dy = { 0, 0, -1, 1 };

    for (int i = 0; i < 4; i++)
    {
        int adjX = input.ImpactCellX + dx[i];
        int adjY = input.ImpactCellY + dy[i];

        // Check bounds (assuming 10×10 grid)
        if (adjX >= 0 && adjX < 10 && adjY >= 0 && adjY < 10)
        {
            results.Add(new CellDamageResult
            {
                CellX = adjX,
                CellY = adjY,
                Damage = input.TotalDamage * adjacentDamagePercent
            });
        }
    }

    return results.AsArray();
}
```

#### Output
- **Cell Damage Results**: Array of cell coordinates and damage amounts

#### Example
```
Impact cell: [5, 5]
Total damage: 100
Type: Blunt (50% center, 12.5% adjacent)

Results:
- Cell [5, 5]: 50 damage (center)
- Cell [4, 5]: 12.5 damage (left)
- Cell [6, 5]: 12.5 damage (right)
- Cell [5, 4]: 12.5 damage (down)
- Cell [5, 6]: 12.5 damage (up)

Total: 100 damage distributed across 5 cells
```

---

### 8. Shield Integrity Calculation Algorithm

Calculates overall shield health as percentage.

#### Input Parameters
```csharp
struct IntegrityInput
{
    float FrontHP;
    float RightHP;
    float RearHP;
    float LeftHP;
    float TopHP;
    float BottomHP;
    float MaxSectorHP;
}
```

#### Algorithm
```csharp
public static float CalculateShieldIntegrity(IntegrityInput input)
{
    float totalCurrentHP = input.FrontHP + input.RightHP + input.RearHP +
                          input.LeftHP + input.TopHP + input.BottomHP;

    float totalMaxHP = input.MaxSectorHP * 6; // 6 sectors

    return totalCurrentHP / totalMaxHP;
}
```

#### Output
- **Shield Integrity**: 0-1 value (0 = destroyed, 1 = full health)

#### Example
```
Front: 50 / 120 HP
Right: 120 / 120 HP
Rear: 120 / 120 HP
Left: 80 / 120 HP
Top: 120 / 120 HP
Bottom: 120 / 120 HP

Total current: 610 HP
Total max: 720 HP
Integrity: 610 / 720 = 0.847 (84.7%)
```

---

### 9. Optimal Shield Facing Algorithm

Calculates best rotation to protect weakest sector.

#### Input Parameters
```csharp
struct OptimalFacingInput
{
    float FrontHP;
    float RightHP;
    float RearHP;
    float LeftHP;
    float TopHP;
    float BottomHP;
    float3 CurrentForward;
    float3 ThreatDirection;       // Direction of highest threat
}
```

#### Algorithm
```csharp
public static float3 CalculateOptimalFacing(OptimalFacingInput input)
{
    // Find strongest horizontal sector (top/bottom excluded from rotation)
    float[] horizontalHP = new float[4]
    {
        input.FrontHP,  // 0°
        input.RightHP,  // 90°
        input.RearHP,   // 180°
        input.LeftHP    // 270°
    };

    // Find index of strongest sector
    int strongestIndex = 0;
    float strongestHP = horizontalHP[0];
    for (int i = 1; i < 4; i++)
    {
        if (horizontalHP[i] > strongestHP)
        {
            strongestHP = horizontalHP[i];
            strongestIndex = i;
        }
    }

    // Calculate rotation angle to face strongest sector toward threat
    float targetAngle = strongestIndex * 90f; // 0°, 90°, 180°, or 270°

    // Convert angle to direction vector
    float angleRad = math.radians(targetAngle);
    float3 targetForward = new float3(
        math.sin(angleRad),
        0,
        math.cos(angleRad)
    );

    return targetForward;
}
```

#### Output
- **Optimal Facing**: Direction vector to face strongest sector toward threat

#### Example
```
Front: 20 HP (weak)
Right: 120 HP (strongest)
Rear: 80 HP
Left: 100 HP
Threat direction: [1, 0, 0] (East)

Strongest sector: Right (120 HP, index 1)
Target angle: 1 × 90° = 90°
Target forward: [sin(90°), 0, cos(90°)] = [1, 0, 0]

Result: Rotate to face Right sector toward threat (already aligned with East)
```

---

## ECS Integration Examples

### System 1: Sector Damage Application System

```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SectorDamageApplicationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (shield, transform, entity) in
                 SystemAPI.Query<RefRW<DirectionalShield>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            // Check for damage events
            if (!SystemAPI.HasBuffer<ShieldDamageEvent>(entity)) continue;

            var damageBuffer = SystemAPI.GetBuffer<ShieldDamageEvent>(entity);

            foreach (var damageEvent in damageBuffer)
            {
                // Determine sector
                var sectorInput = new SectorInput
                {
                    DefenderPosition = transform.ValueRO.Position,
                    DefenderForward = transform.ValueRO.Forward(),
                    DefenderUp = transform.ValueRO.Up(),
                    AttackerPosition = damageEvent.ImpactPosition
                };

                ShieldSector hitSector = DetermineSector(sectorInput);

                // Distribute damage
                var distributionInput = new DamageDistributionInput
                {
                    TotalDamage = damageEvent.Damage,
                    Type = damageEvent.Type,
                    HitSector = hitSector
                };

                var damageResults = DistributeDamage(distributionInput, Allocator.Temp);

                // Update sector HP
                var hpInput = new SectorHPUpdateInput
                {
                    FrontHP = shield.ValueRO.FrontSectorHP,
                    RightHP = shield.ValueRO.RightSectorHP,
                    RearHP = shield.ValueRO.RearSectorHP,
                    LeftHP = shield.ValueRO.LeftSectorHP,
                    TopHP = shield.ValueRO.TopSectorHP,
                    BottomHP = shield.ValueRO.BottomSectorHP,
                    DamageResults = damageResults
                };

                var newState = UpdateSectorHP(hpInput);

                // Write back to shield component
                shield.ValueRW.FrontSectorHP = newState.FrontHP;
                shield.ValueRW.RightSectorHP = newState.RightHP;
                shield.ValueRW.RearSectorHP = newState.RearHP;
                shield.ValueRW.LeftSectorHP = newState.LeftHP;
                shield.ValueRW.TopSectorHP = newState.TopHP;
                shield.ValueRW.BottomSectorHP = newState.BottomHP;
                shield.ValueRW.BreachedSectorBitmask = newState.BreachedSectorBitmask;

                // Apply overflow damage to entity HP
                if (newState.OverflowDamage > 0 && SystemAPI.HasComponent<Health>(entity))
                {
                    var health = SystemAPI.GetComponentRW<Health>(entity);
                    health.ValueRW.CurrentHP -= newState.OverflowDamage;
                }

                damageResults.Dispose();
            }

            // Clear damage buffer
            damageBuffer.Clear();
        }
    }
}
```

---

### System 2: Shield Regeneration System

```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ShieldRegenerationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float currentTime = (float)SystemAPI.Time.ElapsedTime;

        foreach (var (shield, regen) in SystemAPI.Query<RefRW<DirectionalShield>, RefRO<ShieldRegeneration>>())
        {
            if (!regen.ValueRO.IsRegenerating) continue;

            // Regenerate each sector
            shield.ValueRW.FrontSectorHP = RegenerateSector(
                shield.ValueRO.FrontSectorHP,
                shield.ValueRO.MaxSectorHP,
                regen.ValueRO.RegenRatePerSecond,
                deltaTime,
                shield.ValueRO.LastDamageTimestamp,
                currentTime,
                regen.ValueRO.RegenDelayAfterDamage,
                (shield.ValueRO.BreachedSectorBitmask & (1 << 0)) != 0
            );

            // Repeat for other sectors...
            // (Similar logic for Right, Rear, Left, Top, Bottom)
        }
    }

    private static float RegenerateSector(
        float currentHP,
        float maxHP,
        float regenRate,
        float deltaTime,
        float lastDamageTime,
        float currentTime,
        float regenDelay,
        bool isBreach)
    {
        var input = new RegenerationInput
        {
            CurrentHP = currentHP,
            MaxHP = maxHP,
            RegenRatePerSecond = regenRate,
            DeltaTime = deltaTime,
            LastDamageTimestamp = lastDamageTime,
            CurrentTimestamp = currentTime,
            RegenDelaySeconds = regenDelay,
            IsSectorBreached = isBreach
        };

        return CalculateRegeneratedHP(input);
    }
}
```

---

## Performance Considerations

### Bitmask Operations for Breach Tracking

Use bitwise operations for efficient breach checks:

```csharp
public static bool IsSectorBreached(byte bitmask, ShieldSector sector)
{
    return (bitmask & (1 << (int)sector)) != 0;
}

public static byte SetSectorBreached(byte bitmask, ShieldSector sector)
{
    return (byte)(bitmask | (1 << (int)sector));
}

public static byte ClearSectorBreached(byte bitmask, ShieldSector sector)
{
    return (byte)(bitmask & ~(1 << (int)sector));
}

public static int CountBreachedSectors(byte bitmask)
{
    int count = 0;
    for (int i = 0; i < 6; i++)
    {
        if ((bitmask & (1 << i)) != 0)
            count++;
    }
    return count;
}
```

**Performance**: Single byte (8 bits) stores breach state for all 6 sectors.

---

### Grid Cell Storage Optimization

For Space4X surface grids, use **sparse storage** instead of full 2D arrays:

```csharp
public struct SparseGridCell
{
    public ushort CellIndex;       // 0-999 for 10×10 grid (packed X/Y)
    public half HP;                // 16-bit float (sufficient precision)
}

public static ushort PackCellIndex(int x, int y, int gridResolution)
{
    return (ushort)(y * gridResolution + x);
}

public static void UnpackCellIndex(ushort index, int gridResolution, out int x, out int y)
{
    x = index % gridResolution;
    y = index / gridResolution;
}
```

**Memory Savings**: Only store damaged cells (< max HP). Undamaged cells assumed at max HP.

---

## Summary

This framework provides **9 core algorithms** for directional shielding:

1. **Sector Determination**: Calculate which sector is hit from attack angle
2. **Adjacent Sector Lookup**: Find neighboring sectors for damage spread
3. **Damage Distribution**: Allocate damage to sectors based on type (piercing/blunt/energy/explosive)
4. **Sector HP Update**: Apply damage and check for breaches
5. **Shield Regeneration**: Restore HP over time with delay
6. **Surface Area Grid**: High-resolution cell tracking (Space4X)
7. **Grid Cell Damage Propagation**: Spread damage across adjacent cells
8. **Shield Integrity**: Calculate overall health percentage
9. **Optimal Shield Facing**: Rotate strongest sector toward threat

**Performance Tiers:**
- **Sector-Based** (Godgame): 6 sectors × 4 bytes HP + 1 byte bitmask = **25 bytes** per shield
- **Surface Grid** (Space4X): 100 cells × 4 bytes = **400 bytes** per shield (16× memory cost)

**Implementation Guidance:**
- Use **sector-based** for low-fidelity (Godgame, mobile, large unit counts)
- Use **surface grid** for high-fidelity (Space4X, capital ships, cinematic combat)
- Both tiers use **identical damage distribution formulas** for consistency

All algorithms are **Burst-compatible** and designed for high-performance ECS execution.
