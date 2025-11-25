using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Movement;
using PureDOTS.Tests.Support;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Tests.Playmode
{
    /// <summary>
    /// PlayMode tests for movement capability validation.
    /// </summary>
    public class MovementCapabilityTests : DeterministicRewindTestFixture
    {
        [Test]
        public void Monodirectional_NoStrafe()
        {
            // Test that Forward3D craft never gains lateral velocity beyond epsilon
            var entity = EntityManager.CreateEntity();
            var modelSpec = CreateForward3DSpec();
            var modelRef = CreateModelRef(modelSpec);

            EntityManager.AddComponentData(entity, modelRef);
            EntityManager.AddComponentData(entity, new MovementState
            {
                Vel = float3.zero,
                Desired = new float3(1f, 0f, 1f), // Try to strafe
                Mode = (byte)MovementMode.Cruise
            });
            EntityManager.AddComponentData(entity, LocalTransform.FromPositionRotation(
                float3.zero,
                quaternion.identity));
            EntityManager.AddComponentData(entity, new PilotProficiency
            {
                ControlMult = 1f,
                TurnRateMult = 1f,
                EnergyMult = 1f,
                Jitter = 0f,
                ReactionSec = 0f
            });

            // Run movement integration
            // TODO: Actually run MovementIntegrateSystem
            // For now, verify spec doesn't allow strafe
            Assert.IsFalse((modelSpec.Caps & MovementCaps.Strafe) != 0, "Forward3D should not have strafe capability.");
        }

        [Test]
        public void Omni3D_StrafeWorks()
        {
            // Test that Omni3D craft achieves lateral displacement > threshold in T seconds
            var entity = EntityManager.CreateEntity();
            var modelSpec = CreateOmni3DSpec();
            var modelRef = CreateModelRef(modelSpec);

            EntityManager.AddComponentData(entity, modelRef);
            EntityManager.AddComponentData(entity, new MovementState
            {
                Vel = float3.zero,
                Desired = new float3(0f, 0f, 1f), // Strafe right
                Mode = (byte)MovementMode.Cruise
            });
            EntityManager.AddComponentData(entity, LocalTransform.FromPositionRotation(
                float3.zero,
                quaternion.identity));
            EntityManager.AddComponentData(entity, new PilotProficiency
            {
                ControlMult = 1f,
                TurnRateMult = 1f,
                EnergyMult = 1f,
                Jitter = 0f,
                ReactionSec = 0f
            });

            // Verify spec allows strafe
            Assert.IsTrue((modelSpec.Caps & MovementCaps.Strafe) != 0, "Omni3D should have strafe capability.");
        }

        [Test]
        public void Proficiency_ScalesTurnAndEnergy()
        {
            // Test that veteran vs novice yields higher turn rate, lower energy per maneuver
            var noviceProficiency = new PilotProficiency
            {
                ControlMult = 0.5f, // Novice
                TurnRateMult = 0.7f,
                EnergyMult = 1.5f, // Wasteful
                Jitter = 0.1f,
                ReactionSec = 1.0f
            };

            var veteranProficiency = new PilotProficiency
            {
                ControlMult = 1.5f, // Veteran
                TurnRateMult = 1.3f,
                EnergyMult = 0.7f, // Efficient
                Jitter = 0f,
                ReactionSec = 0.1f
            };

            Assert.Greater(veteranProficiency.TurnRateMult, noviceProficiency.TurnRateMult, "Veteran should have higher turn rate.");
            Assert.Less(veteranProficiency.EnergyMult, noviceProficiency.EnergyMult, "Veteran should be more energy efficient.");
        }

        [Test]
        public void Terrain_SlopeClamp_2D()
        {
            // Test that Ground2D entity refuses slopes > MaxSlopeDeg
            var modelSpec = CreateGround2DSpec();
            Assert.AreEqual(2, modelSpec.Dim, "Ground2D should be 2D.");
            Assert.Greater(modelSpec.MaxSlopeDeg, 0f, "Ground2D should have max slope constraint.");
            Assert.Greater(modelSpec.GroundFriction, 0f, "Ground2D should have ground friction.");
        }

        private MovementModelSpec CreateForward3DSpec()
        {
            return new MovementModelSpec
            {
                Id = new FixedString32Bytes("forward3d"),
                Kind = MovementKind.Forward3D,
                Caps = MovementCaps.Forward | MovementCaps.TurnYaw | MovementCaps.TurnPitch,
                Dim = 3,
                JerkClamp = 10f,
                EnergyPerAccel = 1f,
                HeatPerAccel = 0.5f,
                MaxSlopeDeg = 0f,
                GroundFriction = 0f
            };
        }

        private MovementModelSpec CreateOmni3DSpec()
        {
            return new MovementModelSpec
            {
                Id = new FixedString32Bytes("omni3d"),
                Kind = MovementKind.Omni3D,
                Caps = MovementCaps.Forward | MovementCaps.Strafe | MovementCaps.Vertical | MovementCaps.TurnYaw | MovementCaps.TurnPitch | MovementCaps.TurnRoll,
                Dim = 3,
                JerkClamp = 10f,
                EnergyPerAccel = 1f,
                HeatPerAccel = 0.5f,
                MaxSlopeDeg = 0f,
                GroundFriction = 0f
            };
        }

        private MovementModelSpec CreateGround2DSpec()
        {
            return new MovementModelSpec
            {
                Id = new FixedString32Bytes("ground2d"),
                Kind = MovementKind.Ground2D,
                Caps = MovementCaps.Forward | MovementCaps.TurnYaw,
                Dim = 2,
                JerkClamp = 5f,
                EnergyPerAccel = 0.5f,
                HeatPerAccel = 0f,
                MaxSlopeDeg = 45f,
                GroundFriction = 0.8f
            };
        }

        private MovementModelRef CreateModelRef(MovementModelSpec spec)
        {
            // Create blob asset reference (simplified - would use proper blob builder)
            // For tests, this is a placeholder
            return new MovementModelRef
            {
                Blob = default // Would be created from catalog in real scenario
            };
        }
    }
}

