# Editor Reload Safety Guidelines

## Problem

Unity editor can freeze during script reload with "Hold on… Application.UpdateScene – Waiting for Unity's code to finish executing". This occurs when blocking code runs during domain/scene reload.

## Root Causes

### 1. Static Constructors with Heavy Operations

**Anti-pattern:**
```csharp
static MyService()
{
    RuntimeConfigRegistry.Initialize(); // Scans ALL assemblies - can hang during reload
    HeavyOperation();
}
```

**Solution:** Use lazy initialization
```csharp
private static RuntimeConfigVar? s_configVar;

public static bool IsEnabled
{
    get
    {
        if (s_configVar == null)
        {
            RuntimeConfigRegistry.Initialize(); // Only called when first accessed
            s_configVar = SomeConfigVars.Enabled;
        }
        return s_configVar.BoolValue;
    }
}
```

### 2. Assembly Scanning During Domain Reload

**Anti-pattern:**
```csharp
public static void Initialize()
{
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
        var types = assembly.GetTypes(); // Can hang if assembly is problematic
        // ... scan types ...
    }
}
```

**Solution:** Guard against compilation/reload state
```csharp
#if UNITY_EDITOR
using UnityEditor;
#endif

public static void Initialize()
{
    if (s_initialized)
        return;

#if UNITY_EDITOR
    // Skip heavy operations during domain reload/compilation
    if (EditorApplication.isCompiling || EditorApplication.isUpdating)
    {
        return;
    }
#endif

    ScanAssemblies();
    s_initialized = true;
}
```

### 3. Editor-World Systems Without Guards

**Anti-pattern:**
```csharp
[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
public partial struct MySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Runs during reload - can access invalid world state
        var entities = SystemAPI.Query<MyComponent>();
    }
}
```

**Solution:** Add world validity checks
```csharp
public void OnUpdate(ref SystemState state)
{
    // Guard: Skip if world is not ready (during domain reload)
    if (!state.WorldUnmanaged.IsCreated)
        return;

    // Safe to proceed
    var entities = SystemAPI.Query<MyComponent>();
}
```

### 4. Blocking Operations in Editor Hooks

**Anti-patterns:**
- `Task.Wait()` / `.Result` in static constructors
- `ManualResetEvent.WaitOne()` in `OnEnable`/`OnValidate`
- `Thread.Sleep()` in editor scripts
- `Resources.FindObjectsOfTypeAll()` loops in `OnValidate`
- Network/database calls in `[InitializeOnLoad]` methods

**Solution:** Defer heavy work or use async/coroutines
```csharp
[InitializeOnLoadMethod]
static void Initialize()
{
    // Lightweight setup only
    EditorApplication.update += OnEditorUpdate;
}

static void OnEditorUpdate()
{
    // Heavy work happens over multiple frames
    if (ShouldDoWork())
    {
        DoChunkOfWork();
    }
}
```

## Code Patterns That Run During Reload

These Unity callbacks run during domain/scene reload and must be lightweight:

1. **Static constructors** (`static MyClass()`)
2. **`[InitializeOnLoad]` / `[InitializeOnLoadMethod]`**
3. **`OnEnable` / `Awake` / `OnValidate` / `Reset`** in:
   - Editor scripts
   - `[ExecuteInEditMode]` / `[ExecuteAlways]` MonoBehaviours
4. **`ISerializationCallbackReceiver.OnAfterDeserialize`**
5. **`EditorApplication.update` delegates**
6. **DOTS editor-world systems:**
   - `[WorldSystemFilter(WorldSystemFilterFlags.Editor)]`
   - `[UpdateInGroup(typeof(EditorSimulationSystemGroup))]`

## Best Practices

### ✅ DO:
- Use lazy initialization for heavy operations
- Guard assembly scanning with `EditorApplication.isCompiling` checks
- Add `state.WorldUnmanaged.IsCreated` checks in editor-world systems
- Use `RequireForUpdate` to prevent systems from running when dependencies aren't ready
- Defer heavy work to runtime (after domain reload completes)
- Use async/await or coroutines for long operations

### ❌ DON'T:
- Call `RuntimeConfigRegistry.Initialize()` in static constructors
- Scan assemblies without checking compilation state
- Use blocking waits (`Task.Wait()`, `ManualResetEvent.WaitOne()`) in editor hooks
- Do heavy file I/O or network calls in static constructors
- Access Unity APIs that might be invalid during reload
- Create infinite loops or heavy recursion in editor-time code

## Testing

After making changes:
1. Make a small code change to trigger recompile
2. Monitor for "Hold on" dialog
3. Check `Editor.log` for last executed method before hang
4. Use Unity Profiler to capture call stack during freeze

## References

- Unity Domain Reload: https://docs.unity3d.com/Manual/DomainReloading.html
- Editor Scripting: https://docs.unity3d.com/Manual/EditorScripting.html
- DOTS World System Filter: https://docs.unity3d.com/Packages/com.unity.entities@latest/api/Unity.Entities.WorldSystemFilterFlags.html

