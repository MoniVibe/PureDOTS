using NUnit.Framework;
using PureDOTS.Runtime.Movement;
using Unity.Collections;

namespace PureDOTS.Tests.EditMode
{
    public class MovementSpecSanityTests
    {
        [Test]
        public void MovementSpec_Sanity_ValidatesCorrectly()
        {
            var validSpec = new MovementModelSpec
            {
                Id = new FixedString32Bytes("test.movement.valid"),
                Kind = MovementKind.Omni3D,
                Caps = MovementCaps.Forward | MovementCaps.Strafe | MovementCaps.Vertical | MovementCaps.TurnYaw,
                Dim = 3,
                JerkClamp = 10f,
                EnergyPerAccel = 1f,
                HeatPerAccel = 0.5f,
                MaxSlopeDeg = 0f, // Not used for 3D
                GroundFriction = 0f // Not used for 3D
            };

            Assert.IsTrue(ValidateMovementSpec(validSpec), "Valid spec should pass sanity check.");

            // Test invalid Dim
            var invalidDim = validSpec;
            invalidDim.Dim = 1; // Invalid dimension
            Assert.IsFalse(ValidateMovementSpec(invalidDim), "Invalid dimension should fail.");

            invalidDim.Dim = 4; // Invalid dimension
            Assert.IsFalse(ValidateMovementSpec(invalidDim), "Invalid dimension should fail.");

            // Test 2D spec with terrain constraints
            var valid2D = validSpec;
            valid2D.Dim = 2;
            valid2D.MaxSlopeDeg = 45f;
            valid2D.GroundFriction = 0.8f;
            Assert.IsTrue(ValidateMovementSpec(valid2D), "Valid 2D spec should pass.");

            // Test 3D spec with terrain constraints (should fail)
            var invalid3D = validSpec;
            invalid3D.Dim = 3;
            invalid3D.MaxSlopeDeg = 45f; // Should be 0 for 3D
            Assert.IsFalse(ValidateMovementSpec(invalid3D), "3D spec with terrain constraints should fail.");
        }

        private bool ValidateMovementSpec(MovementModelSpec spec)
        {
            // Dim must be 2 or 3
            if (spec.Dim != 2 && spec.Dim != 3)
            {
                return false;
            }

            // Terrain constraints only valid for 2D
            if (spec.Dim == 3)
            {
                if (spec.MaxSlopeDeg != 0f || spec.GroundFriction != 0f)
                {
                    return false;
                }
            }

            // JerkClamp must be positive
            if (spec.JerkClamp <= 0f)
            {
                return false;
            }

            // Energy/Heat costs should be non-negative
            if (spec.EnergyPerAccel < 0f || spec.HeatPerAccel < 0f)
            {
                return false;
            }

            // MaxSlopeDeg should be in valid range [0, 90]
            if (spec.MaxSlopeDeg < 0f || spec.MaxSlopeDeg > 90f)
            {
                return false;
            }

            // GroundFriction should be in valid range [0, 1]
            if (spec.GroundFriction < 0f || spec.GroundFriction > 1f)
            {
                return false;
            }

            return true;
        }
    }
}

