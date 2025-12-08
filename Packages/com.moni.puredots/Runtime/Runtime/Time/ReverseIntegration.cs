using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Time
{
    /// <summary>
    /// Reverse integration for continuous systems (physics, movement, orbit).
    /// Forward: xₙ₊₁ = xₙ + v*dt
    /// Reverse: xₙ₋₁ = xₙ - vₙ₋₁ * dt
    /// No need to re-simulate full physics backwards; just apply negative deltas from history.
    /// </summary>
    [BurstCompile]
    public static class ReverseIntegration
    {
        /// <summary>
        /// Integrate position forward using velocity.
        /// </summary>
        [BurstCompile]
        public static void IntegratePositionForward(in float3 position, in float3 velocity, float dt, out float3 result)
        {
            result = position + velocity * dt;
        }

        /// <summary>
        /// Integrate position backward using stored velocity history.
        /// </summary>
        [BurstCompile]
        public static void IntegratePositionBackward(in float3 position, in float3 velocityAtPreviousTick, float dt, out float3 result)
        {
            result = position - velocityAtPreviousTick * dt;
        }

        /// <summary>
        /// Integrate rotation forward using angular velocity.
        /// </summary>
        [BurstCompile]
        public static void IntegrateRotationForward(in quaternion rotation, in float3 angularVelocity, float dt, out quaternion result)
        {
            // Simplified rotation integration
            float3 axis = math.normalize(angularVelocity);
            float angle = math.length(angularVelocity) * dt;
            quaternion deltaRotation = quaternion.AxisAngle(axis, angle);
            result = math.mul(rotation, deltaRotation);
        }

        /// <summary>
        /// Integrate rotation backward using stored angular velocity history.
        /// </summary>
        [BurstCompile]
        public static void IntegrateRotationBackward(in quaternion rotation, in float3 angularVelocityAtPreviousTick, float dt, out quaternion result)
        {
            // Reverse rotation integration
            float3 axis = math.normalize(angularVelocityAtPreviousTick);
            float angle = math.length(angularVelocityAtPreviousTick) * dt;
            quaternion deltaRotation = quaternion.AxisAngle(axis, -angle); // Negative angle for reverse
            result = math.mul(rotation, deltaRotation);
        }

        /// <summary>
        /// Store velocity history for reverse integration.
        /// </summary>
        public struct VelocityHistory
        {
            private TemporalBuffer<float3> _linearVelocities;
            private TemporalBuffer<float3> _angularVelocities;

            public bool IsCreated => _linearVelocities.IsCreated;

            public VelocityHistory(int capacity, Allocator allocator)
            {
                _linearVelocities = new TemporalBuffer<float3>(capacity, allocator);
                _angularVelocities = new TemporalBuffer<float3>(capacity, allocator);
            }

            public void Dispose()
            {
                _linearVelocities.Dispose();
                _angularVelocities.Dispose();
            }

            /// <summary>
            /// Record velocity at current tick.
            /// </summary>
            public void Record(uint tick, float3 linearVelocity, float3 angularVelocity)
            {
                _linearVelocities.Add(tick, linearVelocity);
                _angularVelocities.Add(tick, angularVelocity);
            }

            /// <summary>
            /// Get velocity at or before target tick for reverse integration.
            /// </summary>
            public bool TryGetVelocity(uint targetTick, out float3 linearVelocity, out float3 angularVelocity, out uint actualTick)
            {
                bool hasLinear = _linearVelocities.TryGetNearest(targetTick, out linearVelocity, out actualTick);
                bool hasAngular = _angularVelocities.TryGetNearest(targetTick, out angularVelocity, out uint angularTick);

                if (hasLinear && hasAngular)
                {
                    // Use the earlier tick
                    actualTick = math.min(actualTick, angularTick);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Prune history older than minTick.
            /// </summary>
            public void PruneOlderThan(uint minTick)
            {
                _linearVelocities.PruneOlderThan(minTick);
                _angularVelocities.PruneOlderThan(minTick);
            }
        }

        /// <summary>
        /// Rewind position using velocity history.
        /// </summary>
        [BurstCompile]
        public static bool RewindPosition(
            ref float3 position,
            uint currentTick,
            uint targetTick,
            float dt,
            in VelocityHistory velocityHistory,
            out uint actualRewoundTick)
        {
            actualRewoundTick = currentTick;

            if (currentTick <= targetTick)
            {
                return false;
            }

            // Get velocity at target tick
            if (!velocityHistory.TryGetVelocity(targetTick, out float3 velocity, out float3 _, out uint velocityTick))
            {
                return false;
            }

            // Calculate how many ticks to rewind
            uint ticksToRewind = currentTick - targetTick;
            float totalTime = ticksToRewind * dt;

            // Apply reverse integration
            IntegratePositionBackward(in position, in velocity, totalTime, out position);
            actualRewoundTick = targetTick;

            return true;
        }

        /// <summary>
        /// Rewind rotation using angular velocity history.
        /// </summary>
        [BurstCompile]
        public static bool RewindRotation(
            ref quaternion rotation,
            uint currentTick,
            uint targetTick,
            float dt,
            in VelocityHistory velocityHistory,
            out uint actualRewoundTick)
        {
            actualRewoundTick = currentTick;

            if (currentTick <= targetTick)
            {
                return false;
            }

            // Get angular velocity at target tick
            if (!velocityHistory.TryGetVelocity(targetTick, out float3 _, out float3 angularVelocity, out uint velocityTick))
            {
                return false;
            }

            // Calculate how many ticks to rewind
            uint ticksToRewind = currentTick - targetTick;
            float totalTime = ticksToRewind * dt;

            // Apply reverse integration
            IntegrateRotationBackward(in rotation, in angularVelocity, totalTime, out rotation);
            actualRewoundTick = targetTick;

            return true;
        }
    }
}

