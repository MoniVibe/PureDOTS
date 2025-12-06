# Genetic Balancer Guide

**Purpose**: Guide for automated parameter tuning via genetic algorithm.

## Overview

CI genetic optimizer mutates parameters (cooldowns, focus regen, morale loss) each night. Keeps best results (performance + behavior diversity). Outputs JSON patches to Blob manifests.

## Core Components

### GeneticBalancer

```csharp
public struct GeneticBalancer
{
    private NativeList<ParameterSet> _population;
    private NativeList<float> _fitnessScores;
    
    public void Evolve();
    public ParameterSet GetBestParameters();
}
```

Genetic balancer system for automated parameter tuning.

### ParameterSet

```csharp
public struct ParameterSet
{
    public float CooldownMultiplier;
    public float FocusRegenRate;
    public float MoraleLossRate;
    // Add more parameters as needed
}
```

Parameter set for genetic balancing.

### ParameterSpace

```csharp
public struct ParameterSpace : IComponentData
{
    public float CooldownMin;
    public float CooldownMax;
    public float FocusRegenMin;
    public float FocusRegenMax;
    public float MoraleLossMin;
    public float MoraleLossMax;
}
```

Parameter space definition for genetic balancing.

## Usage Pattern

### Defining Parameter Space

```csharp
var parameterSpace = new ParameterSpace
{
    CooldownMin = 0.5f,
    CooldownMax = 2.0f,
    FocusRegenMin = 1.0f,
    FocusRegenMax = 5.0f,
    MoraleLossMin = 0.1f,
    MoraleLossMax = 1.0f
};
```

### Running Genetic Balancer

```csharp
var balancer = new GeneticBalancer(populationSize: 50, Allocator.Persistent);

// Evolve population
balancer.Evolve();

// Get best parameters
var bestParams = balancer.GetBestParameters();

// Output JSON patches to Blob manifests
OutputJsonPatches(bestParams);
```

### Evaluating Fitness

```csharp
float EvaluateFitness(ParameterSet parameters)
{
    // Run simulation with parameters
    RunSimulation(parameters);
    
    // Evaluate fitness (performance + behavior diversity)
    float performanceScore = EvaluatePerformance();
    float diversityScore = EvaluateDiversity();
    
    return performanceScore * 0.7f + diversityScore * 0.3f;
}
```

## CI Integration

```csharp
// CI runs genetic balancer nightly
// Mutates parameters
// Keeps best results
// Outputs JSON patches to Blob manifests
// Commits patches to repository
```

## Best Practices

1. **Define parameter space**: Set min/max bounds for each parameter
2. **Evaluate fitness**: Consider both performance and behavior diversity
3. **Evolve population**: Run mutation, crossover, and selection
4. **Output patches**: Generate JSON patches for Blob manifests
5. **CI integration**: Run nightly for continuous optimization

## Performance Impact

- **Automated tuning**: Simulation tunes itself over time
- **Best results**: Keeps parameters with best performance + diversity
- **Continuous optimization**: CI runs nightly for ongoing improvement

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/Balancing/GeneticBalancer.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/Systems/Balancing/GeneticBalancerSystem.cs`

