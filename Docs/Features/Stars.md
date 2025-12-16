# Stars Feature

## Feature Overview

Stars are massive celestial bodies that form the centers of planetary systems. They orbit the galactic center, have physical properties (mass, density, stellar class, luminosity), and can be various types (single, binary, trinary, black holes, etc.). Stars parent planets and provide solar yield based on luminosity, which affects planet sunlight intensity.

## Scope

### Core Functionality
- **Star Properties**: Mass, density, radius, temperature, stellar class, luminosity
- **Star Types**: Single, binary, trinary, black holes, magnetars, pulsars, neutron stars
- **Stellar Classification**: O, B, A, F, G, K, M types plus white dwarfs, brown dwarfs, black holes
- **Cluster Membership**: Stars can belong to star clusters for organization and generation
- **Galactic Orbits**: Stars orbit the galactic center using the existing orbit system
- **Planet Parenting**: Stars maintain buffers of orbiting planets
- **Solar Yield Calculation**: Configurable strategies (normalize, logarithmic, custom) to convert luminosity to yield [0..1]
- **Time-of-Day Integration**: Solar yield affects planet sunlight intensity

## Shared vs Game-Specific

### Shared-Core (`PureDOTS.Runtime.Space`)
- `StarType` enum: Single, Binary, Trinary, BlackHole, Magnetar, Pulsar, NeutronStar
- `StellarClass` enum: O, B, A, F, G, K, M, WhiteDwarf, BrownDwarf, BlackHole
- `StarPhysicalProperties`: Mass, density, radius, temperature
- `StarLuminosity`: Luminosity value (relative to Sun or absolute)
- `StarSolarYield`: Calculated yield [0..1] and last calculation tick
- `StarCluster`: Cluster identifier
- `StarPlanet` buffer: Planet entities orbiting this star
- `StarParent`: Reference to parent star (for planets)
- `StarSolarYieldConfig`: Configuration for yield calculation strategy
- `StarSolarYieldSystem`: Burst-compiled system that calculates yield from luminosity
- `StarClusterSystem`: Validates cluster membership and star-planet hierarchies
- `PlanetOrbitHierarchySystem`: Extended to handle star-planet relationships
- `StellarClassCatalog`: ScriptableObject with typical properties per stellar class

### Shared-Core-with-Variants (`PureDOTS.Runtime.Space`)
- Solar yield calculation strategies (normalize, logarithmic, custom)
- Luminosity-to-sunlight mapping (via `TimeOfDaySystem` integration)

### Game-Specific
- Star type definitions and generation logic
- Cluster generation algorithms
- UI for star info panels
- Game-specific stellar class extensions

## Integration Points

### Orbit System
- Stars use `OrbitParameters` with `ParentPlanet = Entity.Null` to orbit the galactic center
- Planets use `StarParent` component to reference their parent star
- `PlanetOrbitHierarchySystem` maintains `StarPlanets` buffers on star entities

### Planet System
- Planets reference parent stars via `StarParent` component
- `PlanetOrbitHierarchySystem` updates star-planet relationships
- Moons still use `PlanetParent` for planet-moon relationships (backward compatible)

### Time-of-Day System
- `TimeOfDaySystem` reads `StarSolarYield` from parent star (via `StarParent`)
- Sunlight intensity is multiplied by solar yield: `sunlight *= starYield.Yield`
- Planets orbiting brighter stars receive more sunlight

### Solar Energy (Future)
- Solar yield can feed into future solar energy generation systems
- Higher yield = more energy production potential

## Component Reference

### Star Components (`PureDOTS.Runtime.Space`)
- `StarTypeComponent`: Star type (single, binary, etc.)
- `StellarClassComponent`: Stellar classification
- `StarPhysicalProperties`: Mass, density, radius, temperature
- `StarLuminosity`: Luminosity value
- `StarSolarYield`: Calculated yield [0..1]
- `StarCluster`: Cluster identifier
- `StarPlanet` (buffer): Planet entities orbiting this star
- `StarParent`: Reference to parent star (for planets)

### Configuration
- `StarSolarYieldConfig`: Singleton component with calculation strategy and parameters
- `StellarClassCatalog`: ScriptableObject with stellar class properties

## System Reference

### Systems (`PureDOTS.Systems.Space`)
- `StarSolarYieldSystem`: Calculates solar yield from luminosity (runs in `EnvironmentSystemGroup`)
- `StarClusterSystem`: Validates cluster membership and star-planet hierarchies (runs in `EnvironmentSystemGroup` after `PlanetOrbitHierarchySystem`)
- `PlanetOrbitHierarchySystem`: Extended to handle `StarParent` and update `StarPlanets` buffers

### Authoring (`PureDOTS.Authoring.Space`)
- `StarAuthoring`: MonoBehaviour with baker that creates all star components
- `StarSolarYieldConfigAsset`: ScriptableObject for configuring yield calculation

## Usage Examples

### Creating a Star
```csharp
// In Unity Editor, add StarAuthoring component to GameObject
var starAuthoring = gameObject.AddComponent<StarAuthoring>();
starAuthoring.Type = StarType.Single;
starAuthoring.StellarClass = StellarClass.G;
starAuthoring.Mass = 1.0f; // Solar masses
starAuthoring.Luminosity = 1.0f; // Relative to Sun
starAuthoring.ClusterId = 0;
```

### Creating a Planet Orbiting a Star
```csharp
// Planet authoring references star via StarParent
var planetAuthoring = gameObject.AddComponent<PlanetAuthoring>();
// ... set planet properties ...

// In baker or system, set StarParent
var starEntity = GetEntity(starGameObject);
AddComponent(entity, new StarParent { ParentStar = starEntity });
```

### Configuring Solar Yield Calculation
```csharp
// Create ScriptableObject asset
var config = ScriptableObject.CreateInstance<StarSolarYieldConfigAsset>();
config.Strategy = SolarYieldStrategy.Normalize;
config.MaxLuminosity = 1000000f;
config.MinLuminosity = 0.0001f;

// Bake into singleton component
var configEntity = CreateEntity();
AddComponent(configEntity, config.ToComponent());
```

## Notes

- All components use blittable types for Burst compatibility
- All struct parameters use `in` keyword for read-only access
- All `DynamicBuffer<T>` parameters use `in DynamicBuffer<T>`
- Namespace: `PureDOTS.Runtime.Space` for components, `PureDOTS.Systems.Space` for systems
- Maintains backward compatibility with existing planet-moon relationships
- Stars orbit galactic center (no parent), planets orbit stars, moons orbit planets
























