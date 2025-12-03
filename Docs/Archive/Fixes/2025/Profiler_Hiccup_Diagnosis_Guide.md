# Profiler Hiccup Diagnosis Guide

This guide helps diagnose performance hiccups from Unity Profiler capture files.

## Quick Start

1. **Open Unity Profiler**: Window > Analysis > Profiler
2. **Load Capture**: Click "Load" button, select `test.data` (or any `.data` file)
3. **Identify Hiccups**: Look for spikes in the CPU Usage graph (yellow/red bars)

## Analysis Steps

### 1. Frame Time Analysis

In the CPU Usage module:
- **Yellow bars**: Frames exceeding ~33ms (30 FPS threshold)
- **Red bars**: Frames exceeding ~66ms (15 FPS threshold)
- **Hover over spikes** to see exact frame time

### 2. System Group Analysis

Check which system groups are causing the hiccups:

1. **Select a problematic frame** (click on spike in timeline)
2. **Expand "Hierarchy View"** in CPU Usage module
3. **Look for system groups exceeding budget**:
   - `Environment` (budget: 2ms)
   - `Spatial` (budget: 1ms) - **Common hiccup source**
   - `Villager` (budget: 2ms)
   - `Resource` (budget: 1.5ms)
   - `History` (budget: 1ms) - **Rewind catch-up can cause spikes**

### 3. Common Hiccup Causes in DOTS

#### Spatial Grid Rebuilds
- **Symptom**: `SpatialGridBuildSystem` taking >1ms
- **Solution**: Reduce rebuild frequency, optimize grid size
- **Check**: Look for "LastRebuildMilliseconds" spikes

#### Structural Changes
- **Symptom**: Large spikes in "Entities Structural Changes" module
- **Common causes**:
  - Entity creation/destruction in bursts
  - Component addition/removal
  - Chunk allocation
- **Solution**: Batch operations, use command buffers

#### GC Allocations
- **Symptom**: Memory spikes in "Memory" module
- **Solution**: 
  - Use `NativeArray` instead of managed arrays
  - Avoid string concatenation in hot paths
  - Pool objects

#### Rewind Catch-Up
- **Symptom**: Frames with `CatchUp` flag in frame timing
- **Indication**: Multiple frames processed in one update
- **Solution**: Optimize systems that run during catch-up

#### System Budget Exceeded
- **Symptom**: System group exceeding its budget threshold
- **Check**: Frame timing data shows `BudgetExceeded` flag
- **Solution**: Optimize systems in that group

### 4. Frame Timing Integration

The codebase includes `FrameTimingRecorderSystem` which tracks:
- Per-group timing (Time, Environment, Spatial, AI, Villager, etc.)
- Budget violations
- GC collections
- Memory allocations

Look for these patterns in the capture:
- **Consistent budget violations** → System needs optimization
- **Intermittent spikes** → Look for triggering conditions
- **GC spikes** → Memory allocation issues

### 5. Frame-by-Frame Analysis

For each hiccup frame:

1. **Note the frame number** (e.g., Frame 3421)
2. **Check what systems ran**:
   - Look at "Hierarchy View" for that frame
   - Identify which system took the most time
3. **Check system context**:
   - Was it a catch-up frame?
   - How many entities were being processed?
   - Was there a structural change?

### 6. Entities Module Analysis

Enable "Entities" module to see:
- **Structural Changes**: Entity creation/destruction patterns
- **Memory**: Archetype memory usage
- Look for:
  - Bursts of entity creation
  - Large archetype counts
  - Memory spikes

## Automated Analysis Tool

Use the `ProfilerHiccupAnalyzer` tool:
1. Tools > Space4X > Analyze Profiler Hiccups
2. Set capture file path
3. Set hiccup threshold (default: 33.33ms for 30 FPS)
4. Click "Analyze Capture File"

**Note**: Unity Profiler API requires the capture to be loaded in the Profiler window first.

## Interpreting Results

### Threshold Guidelines

- **30 FPS threshold**: 33.33ms per frame
- **60 FPS threshold**: 16.67ms per frame
- **Hiccup**: Frame time spike > threshold

### Severity Levels

- **Minor**: 1-2 frames above threshold
- **Moderate**: 3-5 frames above threshold
- **Severe**: >5 frames or >100ms spike

## Common Patterns

### Pattern 1: Periodic Spikes
- **Pattern**: Regular intervals (e.g., every 60 frames)
- **Likely cause**: Periodic system (e.g., spatial rebuild, history save)
- **Solution**: Spread work across frames, reduce frequency

### Pattern 2: Burst of Spikes
- **Pattern**: Multiple consecutive frames spiking
- **Likely cause**: Rewind catch-up, large structural change
- **Solution**: Optimize catch-up systems, batch structural changes

### Pattern 3: Gradual Degradation
- **Pattern**: Frame time slowly increasing over time
- **Likely cause**: Memory leak, unbounded growth
- **Solution**: Check for resource leaks, entity growth

### Pattern 4: Sudden Spikes
- **Pattern**: Isolated large spikes
- **Likely cause**: One-time event (e.g., large entity spawn, scene load)
- **Solution**: Identify trigger, optimize or defer

## Next Steps

After identifying the cause:

1. **Reproduce**: Try to reproduce the hiccup in a controlled test
2. **Profile Specific System**: Add ProfilerMarkers to suspect systems
3. **Measure Impact**: Quantify how much the fix improves performance
4. **Verify**: Re-run capture to confirm fix

## Resources

- Unity Profiler Documentation: https://docs.unity3d.com/Manual/Profiler.html
- DOTS Performance Guide: https://docs.unity3d.com/Packages/com.unity.entities@latest/manual/performance-entities.html
- Frame Timing System: `Packages/com.moni.puredots/Runtime/Systems/FrameTimingRecorderSystem.cs`

