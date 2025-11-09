# PureDOTS Core Version Lock

## Pinned Advisory: Version Baseline

**CRITICAL**: PureDOTS is locked to specific Unity package versions. All documentation, onboarding notes, and agent prompts must reference this baseline.

### Locked Versions

- **Unity Entities**: `1.4.2` (NOT 1.5+)
- **Unity NetCode**: `1.8` (integration on hold until single-player runtime is stable)
- **Unity Input System**: `1.7.0` (NOT legacy `UnityEngine.Input`)

### Compatibility Constraints

1. **Entities 1.4.2 Only**
   - Do NOT use Entities 1.5+ APIs
   - Do NOT introduce patterns that require 1.5+ features
   - Flag any attempts to pull 1.5+ APIs in code reviews
   - Update this document if baseline changes

2. **NetCode Integration**
   - NetCode integration stays **on hold** until single-player runtime is stable
   - Do NOT add NetCode dependencies until this advisory is updated
   - Single-player determinism and rewind must pass all tests first

3. **Input System**
   - Use **Unity Input System 1.7.0** (`com.unity.inputsystem`)
   - Do NOT use legacy `UnityEngine.Input` class
   - Flag any legacy input usage in code reviews
   - All input must flow through DOTS-friendly command buffers

### Enforcement

- All new systems must compile against Entities 1.4.2
- All input handling must use Input System 1.7.0
- NetCode features are blocked until advisory is updated
- Documentation must reference these versions

### Updating This Advisory

If the baseline needs to change:
1. Update this document with new versions
2. Update `package.json` dependencies
3. Update `PureDOTS_TODO.md` pinned advisory
4. Test compatibility with both Godgame and Space4x projects
5. Update all relevant documentation

**Last Updated**: 2025-01-XX (Project Split)


