got a new hard crash that’s different from the Rewind/buffer issues:

ArgumentException: System.String EntityComponentStore::AppendRemovedComponentRecordError(Entity, ComponentType)
called from EntityQueryImpl.GetSingleton<TimeState>() inside a Burst-compiled system.

Translated:
Some Burst system is asking for TimeState via GetSingleton<TimeState>(), and the underlying query is in a bad state (almost always because there is no valid TimeState singleton when it runs, or the singleton query is being used while structural changes are happening).

We’ll stabilize this the same way we stabilized Rewind:

Guarantee there is always exactly one TimeState singleton in the sim world.

Gate all systems that depend on TimeState so they don’t run until it exists (and don’t explode in editor worlds).

1. Add a small Time bootstrap (like RewindBootstrap)

You already did this for RewindState. Do the same for TimeState in PureDOTS.

File: Packages/com.moni.puredots/Runtime/Systems/Time/TimeBootstrapSystem.cs

using Unity.Burst;
using Unity.Entities;
using PureDOTS.Runtime.Components; // where TimeState lives

namespace PureDOTS.Systems.Time
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TimeBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // If TimeState already exists, do nothing
            if (SystemAPI.TryGetSingleton<TimeState>(out _))
                return;

            var e = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(e, new TimeState
            {
                Tick           = 0,
                BaseScale      = 1f,
                TimeScale      = 1f,
                FixedDeltaTime = 1f / 60f,
                Mode           = TimeMode.SinglePlayer, // or whatever your enum uses
                // leave other fields at default if any
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            // one-shot bootstrap
            state.Enabled = false;
        }
    }
}


Agent checklist:

Make sure this system’s asmdef (PureDOTS.Systems.asmdef) is in the Default World (it should already be via PureDotsWorldBootstrap).

In play mode, in Entities Hierarchy → Default World, confirm one entity has TimeState right after initialization.

This ensures that even if some system hits GetSingleton<TimeState>() very early, there’s at least a dummy TimeState present.

Note: your existing CoreSingletonBootstrapSystem that sets up configs can later update this TimeState with real values. Because we only create it when none exists (TryGetSingleton), there will still be exactly one.

2. Gate systems that use TimeState

Now we make sure no system blows up if, for some reason, TimeState isn’t there yet or is absent in editor worlds.

2.1. For ISystem systems

Search for all usages of SystemAPI.GetSingleton<TimeState>() in com.moni.puredots and Space4x.* code.

For each ISystem using TimeState, use this pattern:

[BurstCompile]
public partial struct SomeTimeAwareSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TimeState>();
        // Often also:
        // state.RequireForUpdate<ScenarioState>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var time = SystemAPI.GetSingleton<TimeState>();
        // var legacy = SystemAPI.GetSingleton<ScenarioState>();

        // Optional: if you want to skip in certain modes
        // if (time.Mode == TimeMode.Playback) return;

        // ... system logic ...
    }
}


This ensures:

The system won’t run at all until TimeState exists.

GetSingleton<TimeState>() will not throw, because RequireForUpdate guarantees exactly one.

2.2. For SystemBase systems

For SystemBase types using GetSingleton<TimeState>():

public partial class SomeTimeAwareSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<TimeState>();
        // RequireForUpdate<ScenarioState>();
    }

    protected override void OnUpdate()
    {
        var time = GetSingleton<TimeState>();
        // var legacy = GetSingleton<ScenarioState>();
        // ... logic ...
    }
}


Same idea as above.

Agent directive:
Run a project-wide search for "GetSingleton<TimeState>".
For each system:

Add RequireForUpdate<TimeState>(); in OnCreate (or state.RequireForUpdate<TimeState>() for ISystem).

If useful, also require ScenarioState so the system doesn’t run before the scenario is initialized.

Keep GetSingleton<TimeState>() in OnUpdate; it will then be safe.

3. Ignore the weird Burst string in the stacktrace

The scary bit:

System.ArgumentException: System.String EntityComponentStore::AppendRemovedComponentRecordError(Entity, ComponentType)


is just how Burst wraps the underlying failure when GetSingleton<TimeState>() can’t resolve properly (no matching entity / wrong filter), and then tries to build an error string.

Once:

TimeBootstrapSystem guarantees a TimeState, and

All TimeState consumers are RequireForUpdate-gated,

that path won’t be hit and this whole error will disappear.