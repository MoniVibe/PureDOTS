# Unity Input System Integration with ECS

**Unity Input System Version**: 1.7.0+
**Last Updated**: 2025-12-01
**Maintainer**: PureDOTS Framework Team

---

## Project Context

**This guide applies to all three projects:**

- **PureDOTS**: Framework input patterns (command components, deterministic input)
- **Space4X**: `C:\Users\Moni\Documents\claudeprojects\unity\Space4x` - Game-specific input actions
- **Godgame**: `C:\Users\Moni\Documents\claudeprojects\unity\Godgame` - Game-specific input actions

**⚠️ Note:** Input action assets and game-specific input handling belong in game projects. PureDOTS provides the framework pattern (command components, input reading systems).

---

## Overview

This guide covers integrating Unity's new Input System with ECS for deterministic simulation. The key principle: **separate input reading (managed) from simulation (Burst-compiled)**.

**Why Input System vs Legacy Input:**
- ✅ Better device support (gamepad, touch, etc.)
- ✅ Action-based (not hardcoded keys)
- ✅ Runtime rebinding support
- ✅ Multiplayer-friendly architecture
- ✅ Event-driven or polling modes

**Version Compatibility:**
- Unity 2022.3+ (Input System 1.7.0+)
- Works with DOTS 1.4.x
- Compatible with Burst-compiled simulation

---

## Architecture Pattern

### Separation of Concerns

```
Player Input Actions (managed)
    ↓
Input Reading System (managed, early in frame)
    ↓
Command Components (ECS)
    ↓
Simulation Systems (Burst, deterministic)
```

**Key Principle:** Input reading happens in managed code (SystemBase), simulation happens in Burst-compiled code (ISystem).

---

## Installation & Setup

### Package Manager

1. Open **Window → Package Manager**
2. Select **Unity Registry**
3. Install **Input System** package (1.7.0+)

### Project Settings

**Edit → Project Settings → Player → Other Settings:**
- **Active Input Handling**: 
  - `Both` (during migration from legacy Input)
  - `Input System Package (New)` (after migration complete)

**Edit → Project Settings → Input System Package:**
- **Update Mode**: `Dynamic Update` (default) or `Fixed Update` (for deterministic)
- **Background Behavior**: Configure as needed

---

## Input Actions Setup

### Creating Action Assets

1. **Right-click in Project → Create → Input Actions**
2. Name it (e.g., `GameplayActions.inputactions`)
3. Double-click to open Input Actions editor

### Action Maps

Create separate action maps for different contexts:
- **Gameplay** - In-game controls
- **UI** - Menu navigation
- **Debug** - Developer tools

### Action Types

| Type | Use Case | Example |
|------|----------|---------|
| **Value** | Continuous input | Movement, look, throttle |
| **Button** | Discrete actions | Jump, attack, interact |
| **Pass-Through** | Raw input | Mouse delta, touch position |

### Generated C# Class

After creating `.inputactions` asset, Unity generates a C# class:

```csharp
// Generated from GameplayActions.inputactions
public class GameplayActions : IInputActionCollection2
{
    public InputAction Move { get; }
    public InputAction Look { get; }
    public InputAction Jump { get; }
    public InputAction Attack { get; }
    public InputAction Sprint { get; }
    
    // ... implementation details
}
```

**Enable C# Generation:**
- Select `.inputactions` asset
- Inspector → **Generate C# Class** checkbox
- Set **Namespace** (e.g., `Space4X.Input` for Space4X project, `Godgame.Input` for Godgame project)

**⚠️ Project Location:** Input action assets belong in **game projects**, not PureDOTS framework. PureDOTS provides the integration pattern, but each game defines its own actions.

---

## ECS Integration Pattern

### Input Command Components

**Command components** bridge input reading → simulation:

```csharp
// Continuous command (always present)
public struct MoveCommand : IComponentData
{
    public float2 Direction;  // Normalized movement direction
    public bool Sprint;        // Sprint modifier
}

// Discrete command (enableable component)
public struct AttackCommand : IComponentData, IEnableableComponent
{
    public float2 TargetDirection;  // Attack direction
    public float DamageMultiplier;  // Optional modifier
}

// Look command (continuous)
public struct LookCommand : IComponentData
{
    public float2 Delta;  // Mouse/touch delta
}
```

### Input Reading System (Managed)

**Reads Unity Input System, writes to ECS command components:**

```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
public partial class InputReadingSystem : SystemBase
{
    private GameplayActions _actions;

    protected override void OnCreate()
    {
        // Create and enable actions
        _actions = new GameplayActions();
        _actions.Enable();
    }

    protected override void OnUpdate()
    {
        // Read input from Unity Input System
        var moveInput = _actions.Move.ReadValue<Vector2>();
        var lookDelta = _actions.Look.ReadValue<Vector2>();
        var sprintPressed = _actions.Sprint.IsPressed();
        var attackPressed = _actions.Attack.WasPressedThisFrame();
        var jumpPressed = _actions.Jump.WasPressedThisFrame();

        // Write to ECS command components (singleton pattern)
        SystemAPI.SetSingleton(new MoveCommand
        {
            Direction = new float2(moveInput.x, moveInput.y),
            Sprint = sprintPressed
        });

        SystemAPI.SetSingleton(new LookCommand
        {
            Delta = new float2(lookDelta.x, lookDelta.y)
        });

        // Enable discrete commands (enableable components)
        var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
        
        if (attackPressed)
        {
            SystemAPI.SetComponentEnabled<AttackCommand>(playerEntity, true);
            // Set command data
            SystemAPI.SetComponent(playerEntity, new AttackCommand
            {
                TargetDirection = math.normalize(new float2(moveInput.x, moveInput.y)),
                DamageMultiplier = sprintPressed ? 1.5f : 1f
            });
        }

        if (jumpPressed)
        {
            SystemAPI.SetComponentEnabled<JumpCommand>(playerEntity, true);
        }
    }

    protected override void OnDestroy()
    {
        // Cleanup
        _actions?.Dispose();
    }
}
```

### Simulation System (Burst)

**Processes commands deterministically:**

```csharp
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var moveCommand = SystemAPI.GetSingleton<MoveCommand>();
        var deltaTime = SystemAPI.Time.DeltaTime;

        // Process move command deterministically
        foreach (var (transform, speed) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>>()
                .WithAll<PlayerTag>())
        {
            if (math.lengthsq(moveCommand.Direction) > 0.01f)
            {
                var dir = math.normalize(moveCommand.Direction);
                var speedMultiplier = moveCommand.Sprint ? 2f : 1f;
                
                transform.ValueRW.Position += new float3(dir.x, 0, dir.y) *
                    speed.ValueRO.Value * speedMultiplier * deltaTime;
            }
        }
    }
}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct AttackSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Process enabled attack commands
        foreach (var (attack, entity) in
            SystemAPI.Query<RefRO<AttackCommand>>().WithEntityAccess())
        {
            // Process attack...
            var target = FindTargetInDirection(attack.ValueRO.TargetDirection);
            if (target != Entity.Null)
            {
                DealDamage(target, attack.ValueRO.DamageMultiplier);
            }

            // Disable command (one-shot)
            SystemAPI.SetComponentEnabled<AttackCommand>(entity, false);
        }
    }
}
```

---

## Determinism Considerations

### Input Recording for Replay

**Record input for deterministic replay:**

```csharp
public struct InputHistory : IComponentData
{
    public BlobAssetReference<InputFrameBlob> RecordedInputs;
}

public struct InputFrameBlob
{
    public struct Frame
    {
        public uint Tick;
        public float2 MoveDirection;
        public float2 LookDelta;
        public bool AttackPressed;
        public bool JumpPressed;
        public bool SprintPressed;
    }

    public BlobArray<Frame> Frames;
}

// Recording system
public partial class InputRecordingSystem : SystemBase
{
    private NativeList<InputFrameBlob.Frame> _recordedFrames;

    protected override void OnUpdate()
    {
        if (!SystemAPI.TryGetSingleton<RewindState>(out var rewind) ||
            rewind.Mode != RewindMode.Record)
            return;

        var tick = SystemAPI.GetSingleton<GameTick>().Value;
        var moveCommand = SystemAPI.GetSingleton<MoveCommand>();
        var lookCommand = SystemAPI.GetSingleton<LookCommand>();
        var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
        var attackEnabled = SystemAPI.IsComponentEnabled<AttackCommand>(playerEntity);

        _recordedFrames.Add(new InputFrameBlob.Frame
        {
            Tick = tick,
            MoveDirection = moveCommand.Direction,
            LookDelta = lookCommand.Delta,
            AttackPressed = attackEnabled,
            JumpPressed = SystemAPI.IsComponentEnabled<JumpCommand>(playerEntity),
            SprintPressed = moveCommand.Sprint
        });
    }

    protected override void OnDestroy()
    {
        // Build blob asset from recorded frames
        if (_recordedFrames.Length > 0)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<InputFrameBlob>();
            var framesArray = builder.Allocate(ref blob.Frames, _recordedFrames.Length);
            
            for (int i = 0; i < _recordedFrames.Length; i++)
            {
                framesArray[i] = _recordedFrames[i];
            }

            var blobRef = builder.CreateBlobAssetReference<InputFrameBlob>(Allocator.Persistent);
            builder.Dispose();

            // Store blob reference (implementation specific)
            // ...
        }

        _recordedFrames.Dispose();
    }
}
```

### Replay System

**Replay recorded input:**

```csharp
[BurstCompile]
public partial struct InputReplaySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<ReplayMode>(out var replay) || 
            !replay.IsReplaying)
            return;

        if (!SystemAPI.TryGetSingleton<InputHistory>(out var history))
            return;

        var currentTick = SystemAPI.GetSingleton<GameTick>().Value;

        // Read from recorded input instead of real input
        ref var inputs = ref history.RecordedInputs.Value;
        
        for (int i = 0; i < inputs.Frames.Length; i++)
        {
            if (inputs.Frames[i].Tick == currentTick)
            {
                var frame = inputs.Frames[i];
                
                // Apply recorded input to command components
                SystemAPI.SetSingleton(new MoveCommand
                {
                    Direction = frame.MoveDirection,
                    Sprint = frame.SprintPressed
                });

                SystemAPI.SetSingleton(new LookCommand
                {
                    Delta = frame.LookDelta
                });

                var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
                SystemAPI.SetComponentEnabled<AttackCommand>(playerEntity, frame.AttackPressed);
                SystemAPI.SetComponentEnabled<JumpCommand>(playerEntity, frame.JumpPressed);
                
                break;
            }
        }
    }
}
```

---

## Multiplayer Patterns

### Client Input → Server Authority

**Client reads input, sends to server. Server applies to simulation:**

```csharp
// Client: Read input, send to server
public partial class ClientInputSystem : SystemBase
{
    private GameplayActions _actions;
    private NetworkClient _networkClient;

    protected override void OnUpdate()
    {
        var moveInput = _actions.Move.ReadValue<Vector2>();
        var currentTick = SystemAPI.GetSingleton<GameTick>().Value;

        // Send input to server (NOT directly to simulation)
        _networkClient.SendInput(new InputPacket
        {
            Tick = currentTick,
            MoveDirection = new float2(moveInput.x, moveInput.y),
            AttackPressed = _actions.Attack.WasPressedThisFrame(),
            // ... other inputs
        });
    }
}

// Server: Receive input, apply to simulation
[BurstCompile]
public partial struct ServerInputApplicationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Read from network buffer
        var inputBuffer = SystemAPI.GetSingletonBuffer<ReceivedInputPacket>();

        foreach (var packet in inputBuffer)
        {
            if (TryGetPlayerEntity(packet.PlayerId, out var entity))
            {
                // Apply input to player's command components
                SystemAPI.SetComponent(entity, new MoveCommand
                {
                    Direction = packet.MoveDirection,
                    Sprint = packet.SprintPressed
                });

                if (packet.AttackPressed)
                {
                    SystemAPI.SetComponentEnabled<AttackCommand>(entity, true);
                }
            }
        }

        // Clear processed packets
        inputBuffer.Clear();
    }
}
```

---

## Runtime Rebinding

### Rebinding UI Pattern

**Allow players to rebind controls at runtime:**

```csharp
public class RebindUI : MonoBehaviour
{
    private GameplayActions _actions;

    private void Start()
    {
        _actions = new GameplayActions();
        _actions.Enable();
        LoadBindings();
    }

    public void StartRebind(InputAction action, int bindingIndex)
    {
        action.Disable();

        var rebind = action.PerformInteractiveRebinding(bindingIndex)
            .OnMatchWaitForAnother(0.1f)  // Wait for additional input
            .OnComplete(operation =>
            {
                // Save new binding
                var json = action.SaveBindingOverridesAsJson();
                PlayerPrefs.SetString("InputBindings", json);
                PlayerPrefs.Save();

                action.Enable();
                operation.Dispose();
            })
            .OnCancel(operation =>
            {
                action.Enable();
                operation.Dispose();
            })
            .Start();
    }

    public void LoadBindings()
    {
        var json = PlayerPrefs.GetString("InputBindings", "");
        if (!string.IsNullOrEmpty(json))
        {
            _actions.LoadBindingOverridesFromJson(json);
        }
    }

    private void OnDestroy()
    {
        _actions?.Dispose();
    }
}
```

---

## Testing Input

### Simulated Input in Tests

**Test systems with simulated input:**

```csharp
[Test]
public void MovementSystem_RespondsToInput()
{
    // Setup test world
    using var world = new World("TestWorld");
    var entity = world.EntityManager.CreateEntity(
        typeof(LocalTransform), 
        typeof(MoveSpeed),
        typeof(PlayerTag));

    world.EntityManager.SetComponentData(entity, new MoveSpeed { Value = 5f });
    world.EntityManager.SetComponentData(entity, LocalTransform.Identity);

    // Inject simulated input (bypass Input System)
    world.EntityManager.CreateEntity(typeof(MoveCommand));
    world.EntityManager.SetComponentData(
        SystemAPI.GetSingletonEntity<MoveCommand>(),
        new MoveCommand
        {
            Direction = new float2(1, 0),  // Move right
            Sprint = false
        });

    // Update movement system
    var movementSystem = world.GetOrCreateSystemManaged<MovementSystem>();
    movementSystem.Update(world.Unmanaged);

    // Assert
    var transform = world.EntityManager.GetComponentData<LocalTransform>(entity);
    Assert.Greater(transform.Position.x, 0, "Entity should have moved right");

    world.Dispose();
}
```

---

## Performance Considerations

### Input Polling vs Events

| Approach | Use Case | Performance |
|----------|----------|-------------|
| **Polling** (Update) | Continuous input (movement, look) | Lower overhead, simpler |
| **Events** (callbacks) | Discrete actions (button presses) | More efficient for rare events |
| **Hybrid** | Both continuous + discrete | Recommended |

**Recommended:** Use polling for continuous input, events for discrete actions.

### Memory Allocations

**✅ DO:**
- Cache action instances (don't recreate each frame)
- Dispose properly in `OnDestroy`
- Use value-type reads (`ReadValue<T>`) over object allocation

**❌ DON'T:**
- Create new `GameplayActions` each frame
- Use `ReadValueAsObject()` (allocates)
- Forget to dispose actions

---

## Common Patterns

### Local Multiplayer

**Per-player input:**

```csharp
public struct PlayerInputComponent : IComponentData
{
    public int PlayerIndex;
}

public partial class MultiplayerInputSystem : SystemBase
{
    private GameplayActions[] _playerActions;

    protected override void OnCreate()
    {
        _playerActions = new GameplayActions[4]; // Up to 4 players
        
        for (int i = 0; i < 4; i++)
        {
            _playerActions[i] = new GameplayActions();
            // Filter devices by player index
            _playerActions[i].devices = InputSystem.devices
                .Where(d => IsDeviceForPlayer(d, i));
            _playerActions[i].Enable();
        }
    }

    protected override void OnUpdate()
    {
        foreach (var (playerInput, moveCommand) in
            SystemAPI.Query<RefRO<PlayerInputComponent>, RefRW<MoveCommand>>())
        {
            var playerIndex = playerInput.ValueRO.PlayerIndex;
            var actions = _playerActions[playerIndex];
            
            moveCommand.ValueRW.Direction = new float2(
                actions.Move.ReadValue<Vector2>().x,
                actions.Move.ReadValue<Vector2>().y
            );
        }
    }

    protected override void OnDestroy()
    {
        foreach (var actions in _playerActions)
        {
            actions?.Dispose();
        }
    }
}
```

### UI Navigation

**Separate UI action map:**

```csharp
// Separate UI action map
public partial class UIInputSystem : SystemBase
{
    private GameplayActions.UIActions _uiActions;

    protected override void OnCreate()
    {
        var actions = new GameplayActions();
        _uiActions = actions.UI;
        _uiActions.Enable();
    }

    protected override void OnUpdate()
    {
        if (_uiActions.Submit.WasPressedThisFrame())
        {
            // Handle UI submit
            SystemAPI.SetSingleton(new UIClickCommand { Action = "Submit" });
        }

        if (_uiActions.Cancel.WasPressedThisFrame())
        {
            // Handle UI cancel
            SystemAPI.SetSingleton(new UIClickCommand { Action = "Cancel" });
        }
    }
}
```

---

## Migration from Legacy Input

### Parallel Support

**Support both during migration:**

```csharp
public partial class LegacyInputBridge : SystemBase
{
    protected override void OnUpdate()
    {
#if ENABLE_INPUT_SYSTEM
        // New Input System
        var actions = new GameplayActions();
        actions.Enable();
        var move = actions.Move.ReadValue<Vector2>();
#else
        // Legacy Input (fallback)
        var move = new Vector2(
            UnityEngine.Input.GetAxis("Horizontal"),
            UnityEngine.Input.GetAxis("Vertical")
        );
#endif

        SystemAPI.SetSingleton(new MoveCommand
        {
            Direction = new float2(move.x, move.y)
        });
    }
}
```

---

## Best Practices Summary

1. ✅ **Read input in managed system** (early in frame, InitializationSystemGroup)
2. ✅ **Write to command components** (ECS bridge)
3. ✅ **Process commands in Burst systems** (SimulationSystemGroup)
4. ✅ **Use enableable components** for discrete actions (one-shot commands)
5. ✅ **Record input** for deterministic replay
6. ✅ **Separate client input from server authority** (multiplayer)
7. ✅ **Cache action instances** (avoid allocations)
8. ✅ **Test with simulated input** (bypass Input System in tests)
9. ❌ **Don't read Input System directly in Burst systems**
10. ❌ **Don't mix input reading and simulation logic**

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| **Input not working** | Check action is enabled (`_actions.Enable()`) |
| **Input delayed** | Check system update order (InputReadingSystem before Simulation) |
| **Non-deterministic** | Ensure input goes through command components, not direct Unity API |
| **Multiplayer desync** | Verify server authority pattern (client sends, server applies) |
| **Rebinding not saving** | Check `PlayerPrefs.Save()` is called |
| **Memory leaks** | Dispose actions in `OnDestroy` |

---

## Additional Resources

- [Unity Input System Manual](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest)
- [Input System Samples](https://github.com/Unity-Technologies/InputSystem)
- [DOTS Best Practices](BestPractices/DOTS_1_4_Patterns.md)
- [Determinism Checklist](BestPractices/DeterminismChecklist.md)

---

*Last Updated: 2025-12-01*
*Maintainer: PureDOTS Framework Team*

