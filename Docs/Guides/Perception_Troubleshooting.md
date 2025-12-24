# Perception Troubleshooting Guide

Common issues and solutions for Phase B perception upgrades.

## LOS Checks Always Unknown

**Symptoms**: `LosChecksUnknownThisTick` is high, entities have reduced confidence when detecting targets.

**Causes**:
- No physics world available
- Obstacle grid not enabled or not populated

**Solutions**:
1. **Enable obstacle grid**:
   - Add `ObstacleGridConfig` to spatial grid entity with `Enabled = 1`
   - Run `ObstacleGridBootstrapSystem` to populate grid from `ObstacleTag` entities
   - Or use `ObstacleGridAuthoring` component on GameObjects

2. **Add physics world**:
   - If using Unity Physics, ensure `PhysicsWorldSingleton` is present
   - `PerceptionUpdateSystem` will use physics raycasts as primary LOS method

3. **Accept reduced confidence**:
   - Unknown LOS applies 0.5x confidence multiplier
   - This is acceptable for non-critical scenarios

## Signal Sampling Too Expensive

**Symptoms**: `SignalCellsSampledThisTick` exceeds budget (1000 cells/tick), tick time increases.

**Causes**:
- Too many sensors with large sampling radius
- `MaxSamplingRadiusCells` too high
- Tier multipliers too aggressive

**Solutions**:
1. **Reduce sampling radius**:
   - Lower `SignalFieldConfig.MaxSamplingRadiusCells` (default: 5)
   - Adjust `SenseCapability.Range` to reduce base radius

2. **Adjust tier multipliers**:
   - Reduce `TierSamplingRadiusMultiplier` for higher tiers
   - Tier0 should use 0.5x multiplier, Tier3 can use 2.0x

3. **Reduce sensor count**:
   - Use LOD/TierProfiles to disable perception for low-tier entities
   - Increase `SenseCapability.UpdateInterval` to reduce update frequency

## Miracles Not Detected

**Symptoms**: Miracle entities exist but don't appear in `PerceivedEntity` or `AISensorReading` buffers.

**Causes**:
- Missing `LocalTransform` on miracle entity
- Missing `SensorSignature` or `SensorySignalEmitter`
- Miracle outside sensor range/FOV
- Sensor doesn't have matching `SenseCapability` channel

**Solutions**:
1. **Verify miracle components**:
   - Ensure `LocalTransform` is present (required for spatial queries)
   - Add `SensorSignature` for direct detection (Vision/EM channels)
   - Or add `SensorySignalEmitter` for field-based detection (Smell/Sound)

2. **Check sensor configuration**:
   - Verify `SenseCapability.EnabledChannels` includes miracle's channel
   - Check `SenseCapability.Range` covers miracle distance
   - Verify `SenseCapability.FieldOfView` includes miracle direction

3. **Run validation system**:
   - `MiracleDetectabilityBootstrapSystem` logs warnings for invalid miracles
   - Check Unity console for validation errors

## Obstacle Grid Not Populated

**Symptoms**: `ObstacleGridCell` buffer is empty or all cells have `BlockingHeight = 0`.

**Causes**:
- `ObstacleGridBootstrapSystem` not running
- No entities with `ObstacleTag`
- `ObstacleGridConfig.Enabled = 0`

**Solutions**:
1. **Enable obstacle grid**:
   - Add `ObstacleGridConfig` to spatial grid entity with `Enabled = 1`
   - Ensure `ObstacleGridBootstrapSystem` is in `PerceptionSystemGroup`

2. **Mark obstacles**:
   - Add `ObstacleTag` component to obstacle entities
   - Use `ObstacleGridAuthoring` component on GameObjects (adds tag at bake time)
   - Or manually add `ObstacleTag` + `ObstacleHeight` components

3. **Trigger rebuild**:
   - Add `ObstacleGridRebuildRequest` component to grid entity
   - System will rebuild on next update

## Performance Budget Violations

**Symptoms**: Telemetry shows budget failures for perception metrics.

**Common Violations**:
- `perception.losChecks.total > 24`: Too many LOS checks per tick
- `perception.signalCellsSampled > 1000`: Too many signal cells sampled
- `perception.losChecks.unknown > 12`: Too many unknown LOS (should enable obstacle grid)

**Solutions**:
1. **Review budgets**:
   - Check `PerformanceBudgets.md` for current limits
   - Adjust budgets if requirements changed

2. **Optimize systems**:
   - Reduce sensor count via LOD/TierProfiles
   - Increase `SenseCapability.UpdateInterval` to reduce update frequency
   - Use spatial queries to limit candidate entities

3. **Enable fallbacks**:
   - Enable obstacle grid to reduce unknown LOS checks
   - Use physics world if available for deterministic LOS

## Determinism Issues

**Symptoms**: Same scenario produces different perception results on rerun.

**Causes**:
- Non-deterministic random number generation
- Floating-point precision issues
- Order-dependent operations

**Solutions**:
1. **Use deterministic RNG**:
   - Ensure `Unity.Mathematics.Random` uses fixed seed
   - Check that all systems use same RNG state

2. **Verify obstacle grid**:
   - Obstacle grid LOS is deterministic (Bresenham/DDA line stepping)
   - Ensure obstacle grid is populated before perception runs

3. **Check rewind compatibility**:
   - Perception state should be restored correctly on rewind
   - Verify `PerceptionState.LastUpdateTick` is reset properly

## See Also

- `Docs/Architecture/Senses_And_Comms_Medium_First.md` - Perception architecture
- `Docs/QA/PerformanceBudgets.md` - Performance budgets and thresholds
- `AI_IMPLEMENTATION_ROADMAP.md` - Phase B implementation details



