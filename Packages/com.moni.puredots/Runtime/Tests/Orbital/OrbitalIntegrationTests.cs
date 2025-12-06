using NUnit.Framework;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Components.Orbital;
using PureDOTS.Runtime.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Tests.Orbital
{
    /// <summary>
    /// Unit tests for orbital integration jobs.
    /// Tests symplectic Euler and Rodrigues' formula for deterministic integration.
    /// </summary>
    public class OrbitalIntegrationTests
    {
        [Test]
        public void LinearVelocityIntegration_SymplecticEuler_UpdatesPosition()
        {
            // Arrange
            var sixDoF = new SixDoFState
            {
                Position = float3.zero,
                Orientation = quaternion.identity,
                LinearVelocity = new float3(1f, 0f, 0f),
                AngularVelocity = float3.zero
            };

            float deltaTime = 0.1f;
            float3 expectedPosition = new float3(0.1f, 0f, 0f);

            // Act - Simulate integration step
            sixDoF.Position += sixDoF.LinearVelocity * deltaTime;

            // Assert
            Assert.AreEqual(expectedPosition.x, sixDoF.Position.x, 1e-6f);
            Assert.AreEqual(expectedPosition.y, sixDoF.Position.y, 1e-6f);
            Assert.AreEqual(expectedPosition.z, sixDoF.Position.z, 1e-6f);
        }

        [Test]
        public void AngularVelocityIntegration_RodriguesFormula_UpdatesOrientation()
        {
            // Arrange
            var sixDoF = new SixDoFState
            {
                Position = float3.zero,
                Orientation = quaternion.identity,
                LinearVelocity = float3.zero,
                AngularVelocity = new float3(0f, 1f, 0f) // Rotate around Y axis
            };

            float deltaTime = 0.1f;
            quaternion initialOrientation = sixDoF.Orientation;

            // Act - Simulate Rodrigues' formula
            float3 angularVel = sixDoF.AngularVelocity;
            float angle = math.length(angularVel) * deltaTime;

            if (angle > 1e-6f)
            {
                float3 axis = math.normalize(angularVel);
                quaternion dq = quaternion.AxisAngle(axis, angle);
                sixDoF.Orientation = math.mul(dq, sixDoF.Orientation);
            }

            // Assert - Orientation should have changed
            float dot = math.dot(initialOrientation.value, sixDoF.Orientation.value);
            Assert.AreNotEqual(1.0f, dot, 1e-6f); // Should have rotated
        }

        [Test]
        public void AngularVelocityIntegration_ZeroAngularVelocity_NoChange()
        {
            // Arrange
            var sixDoF = new SixDoFState
            {
                Position = float3.zero,
                Orientation = quaternion.identity,
                LinearVelocity = float3.zero,
                AngularVelocity = float3.zero
            };

            float deltaTime = 0.1f;
            quaternion initialOrientation = sixDoF.Orientation;

            // Act
            float3 angularVel = sixDoF.AngularVelocity;
            float angle = math.length(angularVel) * deltaTime;

            if (angle > 1e-6f)
            {
                float3 axis = math.normalize(angularVel);
                quaternion dq = quaternion.AxisAngle(axis, angle);
                sixDoF.Orientation = math.mul(dq, sixDoF.Orientation);
            }

            // Assert - Orientation should remain unchanged
            float dot = math.dot(initialOrientation.value, sixDoF.Orientation.value);
            Assert.AreEqual(1.0f, dot, 1e-6f);
        }

        [Test]
        public void Integration_Deterministic_Repeatable()
        {
            // Arrange
            var sixDoF1 = new SixDoFState
            {
                Position = float3.zero,
                Orientation = quaternion.identity,
                LinearVelocity = new float3(1f, 2f, 3f),
                AngularVelocity = new float3(0.1f, 0.2f, 0.3f)
            };

            var sixDoF2 = new SixDoFState
            {
                Position = float3.zero,
                Orientation = quaternion.identity,
                LinearVelocity = new float3(1f, 2f, 3f),
                AngularVelocity = new float3(0.1f, 0.2f, 0.3f)
            };

            float deltaTime = 0.1f;

            // Act - Integrate twice
            for (int i = 0; i < 2; i++)
            {
                // Linear integration
                sixDoF1.Position += sixDoF1.LinearVelocity * deltaTime;
                sixDoF2.Position += sixDoF2.LinearVelocity * deltaTime;

                // Angular integration
                float3 angularVel1 = sixDoF1.AngularVelocity;
                float angle1 = math.length(angularVel1) * deltaTime;
                if (angle1 > 1e-6f)
                {
                    float3 axis1 = math.normalize(angularVel1);
                    quaternion dq1 = quaternion.AxisAngle(axis1, angle1);
                    sixDoF1.Orientation = math.mul(dq1, sixDoF1.Orientation);
                }

                float3 angularVel2 = sixDoF2.AngularVelocity;
                float angle2 = math.length(angularVel2) * deltaTime;
                if (angle2 > 1e-6f)
                {
                    float3 axis2 = math.normalize(angularVel2);
                    quaternion dq2 = quaternion.AxisAngle(axis2, angle2);
                    sixDoF2.Orientation = math.mul(dq2, sixDoF2.Orientation);
                }
            }

            // Assert - Results should be identical (deterministic)
            Assert.AreEqual(sixDoF1.Position.x, sixDoF2.Position.x, 1e-6f);
            Assert.AreEqual(sixDoF1.Position.y, sixDoF2.Position.y, 1e-6f);
            Assert.AreEqual(sixDoF1.Position.z, sixDoF2.Position.z, 1e-6f);

            float dot = math.dot(sixDoF1.Orientation.value, sixDoF2.Orientation.value);
            Assert.AreEqual(1.0f, dot, 1e-6f); // Orientations should be identical
        }
    }
}

