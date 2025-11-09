using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using PureDOTS.Runtime.Components;
using Unity.Mathematics;
using Space4X.Registry;
using Space4X.Runtime.Transport;

public class DualMiningDeterminismTests
{
    private const string DemoSceneName = "MiningDemo_Dual";
    private const int WarmupFrames = 5;

    [UnityTest]
    public IEnumerator DualLoop_ProducesIdenticalStateHashAcrossRuns()
    {
        ulong firstHash = 0;
        ulong secondHash = 0;

        yield return LoadSceneAndSimulateTicks(DemoSceneName, 180, value => firstHash = value);
        yield return LoadSceneAndSimulateTicks(DemoSceneName, 180, value => secondHash = value);

        Assert.AreEqual(firstHash, secondHash, "Dual mining loop diverged between runs.");
    }

    private static IEnumerator LoadSceneAndSimulateTicks(string sceneName, int tickCount, Action<ulong> onComplete)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

        for (int i = 0; i < WarmupFrames; i++)
        {
            yield return null;
        }

        for (int i = 0; i < tickCount; i++)
        {
            yield return null;
        }

        var world = World.DefaultGameObjectInjectionWorld;
        Assert.IsNotNull(world, "DOTS world not initialized.");

        var hash = ComputeWorldHash(world.EntityManager);
        onComplete?.Invoke(hash);
    }

    private static ulong ComputeWorldHash(EntityManager entityManager)
    {
        ulong hash = 1469598103934665603UL; // FNV offset basis

        hash = Combine(hash, SampleVillagerJobs(entityManager));
        hash = Combine(hash, SampleVillagerTickets(entityManager));
        hash = Combine(hash, SampleVillagerStates(entityManager));
        hash = Combine(hash, SampleMinerLoads(entityManager));

        return hash;
    }

    private static ulong SampleVillagerJobs(EntityManager entityManager)
    {
        ulong hash = 1099511628211UL;
        using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerJob>());
        using var jobs = query.ToComponentDataArray<VillagerJob>(Allocator.Temp);
        foreach (var job in jobs)
        {
            hash = Combine(hash, (byte)job.Type);
            hash = Combine(hash, (byte)job.Phase);
        }
        return hash;
    }

    private static ulong SampleVillagerTickets(EntityManager entityManager)
    {
        ulong hash = 1099511628211UL;
        using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerJobTicket>());
        using var tickets = query.ToComponentDataArray<VillagerJobTicket>(Allocator.Temp);
        foreach (var ticket in tickets)
        {
            hash = Combine(hash, (byte)ticket.JobType);
            hash = Combine(hash, (byte)ticket.Phase);
            hash = Combine(hash, (ulong)ticket.ResourceEntity.Index);
        }
        return hash;
    }

    private static ulong SampleVillagerStates(EntityManager entityManager)
    {
        ulong hash = 1099511628211UL;
        using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerAIState>());
        using var states = query.ToComponentDataArray<VillagerAIState>(Allocator.Temp);
        foreach (var state in states)
        {
            hash = Combine(hash, (byte)state.CurrentGoal);
            hash = Combine(hash, (byte)state.CurrentState);
            hash = Combine(hash, (ulong)state.TargetEntity.Index);
        }
        return hash;
    }

    private static ulong SampleMinerLoads(EntityManager entityManager)
    {
        ulong hash = 1099511628211UL;
        using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MinerVessel>());
        using var vessels = query.ToComponentDataArray<MinerVessel>(Allocator.Temp);
        foreach (var vessel in vessels)
        {
            hash = Combine(hash, (ulong)math.asuint(vessel.Load));
            hash = Combine(hash, (ulong)math.asuint(vessel.Capacity));
        }
        return hash;
    }

    private static ulong Combine(ulong hash, ulong value)
    {
        const ulong prime = 1099511628211UL;
        hash ^= value;
        hash *= prime;
        return hash;
    }

    private static ulong Combine(ulong hash, byte value)
    {
        return Combine(hash, (ulong)value);
    }
}


