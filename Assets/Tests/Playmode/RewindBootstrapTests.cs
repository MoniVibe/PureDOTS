using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;
using Unity.Entities;

namespace PureDOTS.Tests.Playmode
{
    public class RewindBootstrapTests
    {
        [Test]
        public void RewindBootstrap_CreatesSingleStateFromConfig()
        {
            using var world = new World("RewindBootstrap_CreatesSingleStateFromConfig");
            var em = world.EntityManager;

            var configEntity = em.CreateEntity(typeof(RewindConfig));
            em.SetComponentData(configEntity, new RewindConfig
            {
                TickDuration = 0.02f,
                MaxHistoryTicks = 1234,
                InitialMode = RewindMode.Paused
            });

            var system = world.CreateSystem<RewindBootstrapSystem>();
            system.Update(world.Unmanaged);

            var rewindQuery = em.CreateEntityQuery(ComponentType.ReadOnly<RewindState>());
            Assert.AreEqual(1, rewindQuery.CalculateEntityCount(), "Expected exactly one RewindState");

            var rewind = em.GetComponentData<RewindState>(rewindQuery.GetSingletonEntity());
            Assert.AreEqual(RewindMode.Paused, rewind.Mode);
            Assert.AreEqual(0.02f, rewind.TickDuration);
            Assert.AreEqual(1234, rewind.MaxHistoryTicks);
        }
    }
}

















