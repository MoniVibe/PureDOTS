using System;
using NUnit.Framework;
using PureDOTS.Runtime.Movement;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Tests.EditMode
{
    public class MovementSpecSanityTests
    {
        [Test]
        public void MovementSpec_Sanity_ValidatesCorrectly()
        {
            using var validSpecBlob = CreateMovementSpecBlob(3, 0f, 0f);
            ref var validSpec = ref validSpecBlob.Value;
            Assert.IsTrue(ValidateMovementSpec(ref validSpec), "Valid spec should pass sanity check.");
            
            using var invalidDimLowBlob = CreateMovementSpecBlob(1, 0f, 0f);
            ref var invalidDimLow = ref invalidDimLowBlob.Value;
            Assert.IsFalse(ValidateMovementSpec(ref invalidDimLow), "Invalid dimension should fail.");
            
            using var invalidDimHighBlob = CreateMovementSpecBlob(4, 0f, 0f);
            ref var invalidDimHigh = ref invalidDimHighBlob.Value;
            Assert.IsFalse(ValidateMovementSpec(ref invalidDimHigh), "Invalid dimension should fail.");
            
            using var valid2DBlob = CreateMovementSpecBlob(2, 45f, 0.8f);
            ref var valid2D = ref valid2DBlob.Value;
            Assert.IsTrue(ValidateMovementSpec(ref valid2D), "Valid 2D spec should pass.");
            
            using var invalid3DBlob = CreateMovementSpecBlob(3, 45f, 0f);
            ref var invalid3D = ref invalid3DBlob.Value;
            Assert.IsFalse(ValidateMovementSpec(ref invalid3D), "3D spec with terrain constraints should fail.");
        }
        
        private static bool ValidateMovementSpec(ref MovementModelSpec spec)
        {
            if (spec.Dim != 2 && spec.Dim != 3)
            {
                return false;
            }
            
            if (spec.Dim == 3)
            {
                if (spec.MaxSlopeDeg != 0f || spec.GroundFriction != 0f)
                {
                    return false;
                }
            }
            
            if (spec.JerkClamp <= 0f)
            {
                return false;
            }
            
            if (spec.EnergyPerAccel < 0f || spec.HeatPerAccel < 0f)
            {
                return false;
            }
            
            if (spec.MaxSlopeDeg < 0f || spec.MaxSlopeDeg > 90f)
            {
                return false;
            }
            
            if (spec.GroundFriction < 0f || spec.GroundFriction > 1f)
            {
                return false;
            }
            
            return true;
        }
        
        private static BlobAssetReference<MovementModelSpec> CreateMovementSpecBlob(byte dim, float maxSlopeDeg, float groundFriction)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var spec = ref builder.ConstructRoot<MovementModelSpec>();

            spec.Id = new FixedString32Bytes("test.movement");
            spec.Kind = MovementKind.Omni3D;
            spec.Caps = MovementCaps.Forward | MovementCaps.Strafe | MovementCaps.Vertical | MovementCaps.TurnYaw;
            spec.Dim = dim;
            spec.JerkClamp = 10f;
            spec.EnergyPerAccel = 1f;
            spec.HeatPerAccel = 0.5f;
            spec.MaxSlopeDeg = maxSlopeDeg;
            spec.GroundFriction = groundFriction;

            var blob = builder.CreateBlobAssetReference<MovementModelSpec>(Allocator.Temp);
            builder.Dispose();
            return blob;
        }
    }
}
