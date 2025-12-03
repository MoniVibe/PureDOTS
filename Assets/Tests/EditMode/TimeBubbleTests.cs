using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Time;

namespace PureDOTS.Tests.EditMode
{
    /// <summary>
    /// Unit tests for time bubble components and logic.
    /// </summary>
    [TestFixture]
    public class TimeBubbleTests
    {
        [Test]
        public void TimeBubbleVolume_CreateSphere_SetsCorrectValues()
        {
            var center = new float3(10, 5, 20);
            float radius = 15f;
            
            var volume = TimeBubbleVolume.CreateSphere(center, radius);
            
            Assert.AreEqual(center, volume.Center);
            Assert.AreEqual(radius, volume.Radius);
            Assert.AreEqual(TimeBubbleVolumeType.Sphere, volume.VolumeType);
        }

        [Test]
        public void TimeBubbleVolume_CreateCylinder_SetsCorrectValues()
        {
            var center = new float3(10, 5, 20);
            float radius = 15f;
            float height = 30f;
            
            var volume = TimeBubbleVolume.CreateCylinder(center, radius, height);
            
            Assert.AreEqual(center, volume.Center);
            Assert.AreEqual(radius, volume.Radius);
            Assert.AreEqual(height, volume.Height);
            Assert.AreEqual(TimeBubbleVolumeType.Cylinder, volume.VolumeType);
            Assert.IsFalse(volume.IgnoreY);
        }

        [Test]
        public void TimeBubbleVolume_CreateBox_SetsCorrectValues()
        {
            var center = new float3(10, 5, 20);
            var halfExtents = new float3(5, 10, 15);
            
            var volume = TimeBubbleVolume.CreateBox(center, halfExtents);
            
            Assert.AreEqual(center, volume.Center);
            Assert.AreEqual(halfExtents, volume.HalfExtents);
            Assert.AreEqual(TimeBubbleVolumeType.Box, volume.VolumeType);
        }

        [Test]
        public void TimeBubbleVolume_Sphere_Contains_PointInside()
        {
            var volume = TimeBubbleVolume.CreateSphere(new float3(0, 0, 0), 10f);
            
            Assert.IsTrue(volume.Contains(new float3(0, 0, 0)), "Center should be inside");
            Assert.IsTrue(volume.Contains(new float3(5, 0, 0)), "Point within radius should be inside");
            Assert.IsTrue(volume.Contains(new float3(9.9f, 0, 0)), "Point at edge should be inside");
        }

        [Test]
        public void TimeBubbleVolume_Sphere_DoesNotContain_PointOutside()
        {
            var volume = TimeBubbleVolume.CreateSphere(new float3(0, 0, 0), 10f);
            
            Assert.IsFalse(volume.Contains(new float3(15, 0, 0)), "Point beyond radius should be outside");
            Assert.IsFalse(volume.Contains(new float3(100, 100, 100)), "Distant point should be outside");
        }

        [Test]
        public void TimeBubbleVolume_Box_Contains_PointInside()
        {
            var volume = TimeBubbleVolume.CreateBox(new float3(0, 0, 0), new float3(10, 10, 10));
            
            Assert.IsTrue(volume.Contains(new float3(0, 0, 0)), "Center should be inside");
            Assert.IsTrue(volume.Contains(new float3(5, 5, 5)), "Point within extents should be inside");
            Assert.IsTrue(volume.Contains(new float3(-9, -9, -9)), "Point near corner should be inside");
        }

        [Test]
        public void TimeBubbleVolume_Box_DoesNotContain_PointOutside()
        {
            var volume = TimeBubbleVolume.CreateBox(new float3(0, 0, 0), new float3(10, 10, 10));
            
            Assert.IsFalse(volume.Contains(new float3(15, 0, 0)), "Point beyond X extents should be outside");
            Assert.IsFalse(volume.Contains(new float3(0, 15, 0)), "Point beyond Y extents should be outside");
            Assert.IsFalse(volume.Contains(new float3(0, 0, 15)), "Point beyond Z extents should be outside");
        }

        [Test]
        public void TimeBubbleVolume_Cylinder_Contains_PointInside()
        {
            var volume = TimeBubbleVolume.CreateCylinder(new float3(0, 0, 0), 10f, 20f);
            
            Assert.IsTrue(volume.Contains(new float3(0, 0, 0)), "Center should be inside");
            Assert.IsTrue(volume.Contains(new float3(5, 5, 0)), "Point within radius and height should be inside");
        }

        [Test]
        public void TimeBubbleVolume_Cylinder_DoesNotContain_PointOutside()
        {
            var volume = TimeBubbleVolume.CreateCylinder(new float3(0, 0, 0), 10f, 20f);
            
            Assert.IsFalse(volume.Contains(new float3(15, 0, 0)), "Point beyond radius should be outside");
            Assert.IsFalse(volume.Contains(new float3(0, 15, 0)), "Point beyond height should be outside");
        }

        [Test]
        public void TimeBubbleParams_CreateScale_SetsCorrectValues()
        {
            var bubbleParams = TimeBubbleParams.CreateScale(1, 0.5f, 100);
            
            Assert.AreEqual(1u, bubbleParams.BubbleId);
            Assert.AreEqual(TimeBubbleMode.Scale, bubbleParams.Mode);
            Assert.AreEqual(0.5f, bubbleParams.Scale);
            Assert.AreEqual(100, bubbleParams.Priority);
            Assert.AreEqual(0, bubbleParams.OwnerPlayerId, "SP default should be 0");
            Assert.IsFalse(bubbleParams.AffectsOwnedEntitiesOnly, "SP default should be false");
            Assert.IsTrue(bubbleParams.IsActive);
            Assert.IsTrue(bubbleParams.AllowMembershipChanges);
        }

        [Test]
        public void TimeBubbleParams_CreatePause_SetsCorrectValues()
        {
            var bubbleParams = TimeBubbleParams.CreatePause(2, 150);
            
            Assert.AreEqual(2u, bubbleParams.BubbleId);
            Assert.AreEqual(TimeBubbleMode.Pause, bubbleParams.Mode);
            Assert.AreEqual(0f, bubbleParams.Scale);
            Assert.AreEqual(150, bubbleParams.Priority);
        }

        [Test]
        public void TimeBubbleParams_CreateStasis_SetsCorrectValues()
        {
            var bubbleParams = TimeBubbleParams.CreateStasis(3, 200);
            
            Assert.AreEqual(3u, bubbleParams.BubbleId);
            Assert.AreEqual(TimeBubbleMode.Stasis, bubbleParams.Mode);
            Assert.AreEqual(0f, bubbleParams.Scale);
            Assert.AreEqual(200, bubbleParams.Priority);
            Assert.IsFalse(bubbleParams.AllowMembershipChanges, "Stasis should not allow membership changes");
        }

        [Test]
        public void TimeBubbleParams_CreateRewind_SetsCorrectValues()
        {
            var bubbleParams = TimeBubbleParams.CreateRewind(4, 100, 175);
            
            Assert.AreEqual(4u, bubbleParams.BubbleId);
            Assert.AreEqual(TimeBubbleMode.Rewind, bubbleParams.Mode);
            Assert.AreEqual(100, bubbleParams.RewindOffsetTicks);
            Assert.AreEqual(175, bubbleParams.Priority);
            Assert.IsFalse(bubbleParams.AllowMembershipChanges, "Rewind should not allow membership changes");
        }

        [Test]
        public void TimeBubbleId_Create_WithoutName_SetsIdOnly()
        {
            var bubbleId = TimeBubbleId.Create(42);
            
            Assert.AreEqual(42u, bubbleId.Id);
            Assert.AreEqual(default(FixedString32Bytes), bubbleId.Name);
        }

        [Test]
        public void TimeBubbleId_Create_WithName_SetsBothValues()
        {
            var name = new FixedString32Bytes("TestBubble");
            var bubbleId = TimeBubbleId.Create(42, name);
            
            Assert.AreEqual(42u, bubbleId.Id);
            Assert.AreEqual(name, bubbleId.Name);
        }

        [Test]
        public void TimeHelpers_GetEffectiveDelta_ReturnsGlobalDelta_WhenNotInBubble()
        {
            var tickTimeState = new TickTimeState { FixedDeltaTime = 1f / 60f, CurrentSpeedMultiplier = 2f };
            var timeState = new TimeState { IsPaused = false, CurrentSpeedMultiplier = 2f };
            var membership = new TimeBubbleMembership { BubbleId = 0 }; // Not in any bubble
            
            float delta = TimeHelpers.GetEffectiveDelta(tickTimeState, timeState, membership);
            
            Assert.That(delta, Is.EqualTo(tickTimeState.FixedDeltaTime * 2f).Within(0.0001f));
        }

        [Test]
        public void TimeHelpers_GetEffectiveDelta_ReturnsZero_WhenInStasis()
        {
            var tickTimeState = new TickTimeState { FixedDeltaTime = 1f / 60f, CurrentSpeedMultiplier = 1f };
            var timeState = new TimeState { IsPaused = false, CurrentSpeedMultiplier = 1f };
            var membership = new TimeBubbleMembership { BubbleId = 1, LocalMode = TimeBubbleMode.Stasis, LocalScale = 0f };
            
            float delta = TimeHelpers.GetEffectiveDelta(tickTimeState, timeState, membership);
            
            Assert.AreEqual(0f, delta, "Stasis mode should return zero delta");
        }

        [Test]
        public void TimeHelpers_GetEffectiveDelta_ReturnsScaledDelta_WhenInScaleBubble()
        {
            var tickTimeState = new TickTimeState { FixedDeltaTime = 1f / 60f, CurrentSpeedMultiplier = 1f };
            var timeState = new TimeState { IsPaused = false, CurrentSpeedMultiplier = 1f };
            var membership = new TimeBubbleMembership { BubbleId = 1, LocalMode = TimeBubbleMode.Scale, LocalScale = 0.5f };
            
            float delta = TimeHelpers.GetEffectiveDelta(tickTimeState, timeState, membership);
            
            Assert.That(delta, Is.EqualTo(tickTimeState.FixedDeltaTime * 0.5f).Within(0.0001f));
        }

        [Test]
        public void TimeHelpers_IsInStasis_ReturnsTrue_WhenInStasisBubble()
        {
            var membership = new TimeBubbleMembership { BubbleId = 1, LocalMode = TimeBubbleMode.Stasis };
            
            Assert.IsTrue(TimeHelpers.IsInStasis(membership));
        }

        [Test]
        public void TimeHelpers_IsInStasis_ReturnsFalse_WhenNotInBubble()
        {
            var membership = new TimeBubbleMembership { BubbleId = 0 };
            
            Assert.IsFalse(TimeHelpers.IsInStasis(membership));
        }

        [Test]
        public void TimeHelpers_IsInStasis_ReturnsFalse_WhenInScaleBubble()
        {
            var membership = new TimeBubbleMembership { BubbleId = 1, LocalMode = TimeBubbleMode.Scale };
            
            Assert.IsFalse(TimeHelpers.IsInStasis(membership));
        }

        [Test]
        public void TimeHelpers_ShouldUpdate_ReturnsFalse_WhenInStasis()
        {
            var timeState = new TimeState { IsPaused = false };
            var rewindState = new RewindState { Mode = RewindMode.Record };
            var membership = new TimeBubbleMembership { BubbleId = 1, LocalMode = TimeBubbleMode.Stasis };
            
            Assert.IsFalse(TimeHelpers.ShouldUpdate(timeState, rewindState, membership));
        }

        [Test]
        public void TimeHelpers_ShouldUpdate_ReturnsTrue_WhenInScaleBubble()
        {
            var timeState = new TimeState { IsPaused = false };
            var rewindState = new RewindState { Mode = RewindMode.Record };
            var membership = new TimeBubbleMembership { BubbleId = 1, LocalMode = TimeBubbleMode.Scale, LocalScale = 0.5f };
            
            Assert.IsTrue(TimeHelpers.ShouldUpdate(timeState, rewindState, membership));
        }

        [Test]
        public void TimeHelpers_ShouldUpdate_ReturnsFalse_WhenGloballyPausedInRecordMode()
        {
            var timeState = new TimeState { IsPaused = true };
            var rewindState = new RewindState { Mode = RewindMode.Record };
            var membership = new TimeBubbleMembership { BubbleId = 0 };
            
            Assert.IsFalse(TimeHelpers.ShouldUpdate(timeState, rewindState, membership));
        }
    }
}

