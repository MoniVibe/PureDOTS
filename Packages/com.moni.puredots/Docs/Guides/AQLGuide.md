# AI Query Language (AQL) Guide

**Purpose**: Guide for using declarative query DSL for Mind ECS cognition.

## Overview

AQL allows cognitive systems to query the world declaratively without writing raw code. Queries are translated to pre-compiled DOTS queries with cached handles.

## Query Syntax

```
FIND enemies WHERE distance < 30 AND morale > 0.5
```

- **FIND**: Entity type to query
- **WHERE**: Conditions (field operator value)
- **AND/OR**: Logical operators

## Core Components

### AQLParser

```csharp
var query = AQLParser.Parse("FIND enemies WHERE distance < 30 AND morale > 0.5");
```

Parses query string into structured `AQLQuery`.

### AQLQuery

```csharp
public struct AQLQuery
{
    public FixedString64Bytes EntityType;
    public NativeList<AQLCondition> Conditions;
}
```

Structured query representation.

### CompiledAQLQuery

```csharp
public struct CompiledAQLQuery
{
    public EntityQuery QueryHandle;
    public uint CacheVersion;
}
```

Pre-compiled query with cached EntityQuery handle.

## Usage Pattern

### Parsing Queries

```csharp
var queryString = new FixedString512Bytes("FIND enemies WHERE distance < 30");
var query = AQLParser.Parse(queryString);
```

### Compiling Queries

```csharp
var compiledQuery = AQLExecutorSystem.CompileQuery(query);
// Returns cached EntityQuery handle
```

### Executing Queries

```csharp
// In MindECS system
var results = AQLExecutorSystem.ExecuteQuery(compiledQuery);
// Returns entities matching query conditions
```

### Caching Results

Query results are cached for performance:

```csharp
// First execution compiles and caches
var results1 = ExecuteQuery(query); // Compiles + caches

// Subsequent executions use cached handle
var results2 = ExecuteQuery(query); // Uses cache
```

## Integration with MindECS

AQL queries integrate with MindECS cognitive systems:

```csharp
// In PerceptionInterpreterSystem
var enemyQuery = AQLParser.Parse("FIND enemies WHERE distance < 30");
var enemies = AQLExecutorSystem.ExecuteQuery(enemyQuery);

// Process cognitive decisions based on query results
foreach (var enemy in enemies)
{
    EvaluateThreat(enemy);
}
```

## Best Practices

1. **Use declarative syntax**: Write queries in AQL, not raw DOTS code
2. **Cache compiled queries**: Reuse `CompiledAQLQuery` handles
3. **Integrate with cognition**: Use queries in MindECS systems
4. **Performance**: Pre-compiled queries are fast and cached

## Performance Impact

- **Declarative**: Powerful cognition without raw code
- **Cached**: Pre-compiled queries with cached handles
- **Fast**: Efficient translation to DOTS queries

## References

- `PureDOTS/Packages/com.moni.puredots/Runtime/AI/AQL/AQLParser.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/AI/AQL/AQLQuery.cs`
- `PureDOTS/Packages/com.moni.puredots/Runtime/AI/AQL/AQLExecutorSystem.cs`

