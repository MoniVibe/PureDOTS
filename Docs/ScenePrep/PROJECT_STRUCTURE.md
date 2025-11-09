# Project Structure After Split

## Overview

Following the project split, **Godgame** and **Space4x** are now completely independent Unity projects, each referencing the shared **PureDOTS** package.

## Structure

```
unity/
├── PureDOTS/              # Shared DOTS package source
│   ├── Packages/
│   │   └── com.moni.puredots/  # Package consumed by both projects
│   └── Docs/              # Package documentation
│
├── Godgame/               # Independent Unity project
│   ├── Assets/
│   │   ├── Scenes/        # Godgame-specific scenes
│   │   └── Scripts/       # Godgame-specific code
│   └── Packages/
│       └── manifest.json  # References PureDOTS package
│
└── Space4x/               # Independent Unity project
    ├── Assets/
    │   ├── Scenes/        # Space4x-specific scenes
    │   └── Scripts/       # Space4x-specific code
    └── Packages/
        └── manifest.json  # References PureDOTS package
```

## Package Reference

Both projects reference PureDOTS via:
```json
"com.moni.puredots": "file:../../PureDOTS/Packages/com.moni.puredots"
```

This allows both projects to use the same shared DOTS framework while maintaining independence.

## Development Workflow

### Working on Godgame
1. Open `Godgame/` as Unity project
2. PureDOTS package is automatically available
3. Create scenes, prefabs, and scripts in `Godgame/Assets/`
4. Use PureDOTS systems as needed

### Working on Space4x
1. Open `Space4x/` as Unity project
2. PureDOTS package is automatically available
3. Create scenes, prefabs, and scripts in `Space4x/Assets/`
4. Use PureDOTS systems as needed

### Working on PureDOTS Package
1. Open `PureDOTS/` as Unity project (for testing package itself)
2. Make changes to `Packages/com.moni.puredots/`
3. Both Godgame and Space4x will pick up changes automatically

## Key Principles

1. **No Cross-Project Dependencies**: Projects don't reference each other
2. **Shared Package Only**: PureDOTS is the only shared code
3. **Independent Assets**: Each project has its own scenes, prefabs, configs
4. **Reusable Systems**: PureDOTS systems work in both projects independently

## Archived Items

- Hybrid showcase scenes (in `Space4x/Assets/Scenes/Hybrid/` - marked archived)
- Hybrid documentation (in `PureDOTS/Docs/ScenePrep/Archived/`)
- Hybrid setup scripts (removed)

## Migration Notes

If you were using hybrid showcase features:
- Extract content to project-specific scenes
- Use PureDOTS package systems (`HybridControlCoordinator`, etc.) in each project independently
- Create project-specific bootstrap scripts if needed

See `HybridShowcaseDecision.md` for more context.


