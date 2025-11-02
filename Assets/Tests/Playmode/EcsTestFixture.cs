using NUnit.Framework;
using Unity.Entities;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// Minimal ECS world bootstrap for playmode tests that need direct access to an EntityManager.
    /// </summary>
    public abstract class EcsTestFixture
    {
        protected World World { get; private set; } = null!;
        protected EntityManager EntityManager { get; private set; }
            = default;

        [SetUp]
        public virtual void SetUp()
        {
            World = new World("PureDOTS Test World");
            World.DefaultGameObjectInjectionWorld = World;
            EntityManager = World.EntityManager;
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (World.DefaultGameObjectInjectionWorld == World)
            {
                World.DefaultGameObjectInjectionWorld = null;
            }

            if (World.IsCreated)
            {
                World.Dispose();
            }
        }
    }
}

