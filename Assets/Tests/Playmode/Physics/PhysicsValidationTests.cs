using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace PureDOTS.Tests.Physics
{
    public class PhysicsValidationTests
    {
        private const float FixedDeltaTime = 1f / 60f;
        private static readonly float3 Gravity = new(0f, -9.81f, 0f);

        [Test]
        public void FallingSphereSettlesAboveGround()
        {
            float finalHeight = RunSphereDropSimulation(initialRadius: 0.5f);
            Assert.That(finalHeight, Is.InRange(0.45f, 0.55f), "Sphere should rest on the ground within tolerance.");
        }

        [Test]
        public void RuntimeColliderScaleChangeIsDeterministic()
        {
            float first = RunSphereDropSimulation(initialRadius: 0.5f, scaleChangeStep: 45, scaleMultiplier: 1.5f);
            float second = RunSphereDropSimulation(initialRadius: 0.5f, scaleChangeStep: 45, scaleMultiplier: 1.5f);
            Assert.AreEqual(first, second, 1e-4f, "Collider scale adjustments should yield deterministic results.");
        }

        private static float RunSphereDropSimulation(float initialRadius, int scaleChangeStep = -1, float scaleMultiplier = 1f)
        {
            using var world = new World("PhysicsValidationWorld");

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, new[]
            {
                typeof(PhysicsSystemGroup)
            });

            var simulationGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
            var entityManager = world.EntityManager;

            // Ensure singleton requirements are present.
            entityManager.CreateEntity(typeof(SimulationSingleton));
            var physicsStepEntity = entityManager.CreateEntity(typeof(PhysicsStep));
            entityManager.SetComponentData(physicsStepEntity, new PhysicsStep
            {
                SimulationType = SimulationType.UnityPhysics,
                Gravity = Gravity,
                SolverIterationCount = 4
            });

            var groundCollider = Unity.Physics.BoxCollider.Create(new BoxGeometry
            {
                Center = float3.zero,
                Orientation = quaternion.identity,
                Size = new float3(20f, 1f, 20f),
                BevelRadius = 0f
            });

            var groundEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(groundEntity,
                LocalTransform.FromPositionRotationScale(new float3(0f, -0.5f, 0f), quaternion.identity, 1f));
            entityManager.AddComponentData(groundEntity, new PhysicsCollider { Value = groundCollider });

            var currentCollider = Unity.Physics.SphereCollider.Create(new SphereGeometry
            {
                Center = float3.zero,
                Radius = initialRadius
            });

            var sphereEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(sphereEntity,
                LocalTransform.FromPositionRotationScale(new float3(0f, 2f, 0f), quaternion.identity, 1f));
            entityManager.AddComponentData(sphereEntity, new PhysicsCollider { Value = currentCollider });
            entityManager.AddComponentData(sphereEntity, PhysicsMass.CreateDynamic(currentCollider.Value.MassProperties, 1f));
            entityManager.AddComponentData(sphereEntity, PhysicsVelocity.Zero);
            entityManager.AddComponentData(sphereEntity, new PhysicsDamping { Linear = 0.01f, Angular = 0.05f });
            entityManager.AddComponentData(sphereEntity, new PhysicsGravityFactor { Value = 1f });

            const int steps = 180; // 3 seconds simulated
            for (int step = 0; step < steps; step++)
            {
                if (scaleChangeStep >= 0 && step == scaleChangeStep)
                {
                    var newCollider = Unity.Physics.SphereCollider.Create(new SphereGeometry
                    {
                        Center = float3.zero,
                        Radius = initialRadius * scaleMultiplier
                    });

                    var oldCollider = currentCollider;
                    currentCollider = newCollider;
                    entityManager.SetComponentData(sphereEntity, new PhysicsCollider { Value = newCollider });
                    entityManager.SetComponentData(sphereEntity, PhysicsMass.CreateDynamic(newCollider.Value.MassProperties, 1f));
                    oldCollider.Dispose();
                }

                float elapsed = (step + 1) * FixedDeltaTime;
                world.SetTime(new TimeData(elapsed, FixedDeltaTime));
                simulationGroup.Update();
            }

            var finalTransform = entityManager.GetComponentData<LocalTransform>(sphereEntity);

            currentCollider.Dispose();
            groundCollider.Dispose();

            return finalTransform.Position.y;
        }
    }
}

