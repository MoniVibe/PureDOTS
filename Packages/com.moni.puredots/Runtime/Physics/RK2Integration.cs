using Unity.Burst;
using Unity.Mathematics;
using PureDOTS.Runtime.Math;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Runge-Kutta 2nd order integration for deterministic physics.
    /// Used when analytic solutions are not available.
    /// </summary>
    [BurstCompile]
    public static class RK2Integration
    {
        /// <summary>
        /// Integrates position and velocity using RK2.
        /// </summary>
        [BurstCompile]
        public static void Integrate(
            in float3 position,
            in float3 velocity,
            in float3 acceleration,
            float deltaTime,
            out float3 newPosition,
            out float3 newVelocity)
        {
            // RK2: k1 = f(t, y), k2 = f(t + dt/2, y + dt*k1/2)
            float3 k1Vel = acceleration;
            float3 k1Pos = velocity;

            float halfDt = deltaTime * 0.5f;
            float3 midVel = velocity + k1Vel * halfDt;
            float3 midPos = position + k1Pos * halfDt;

            float3 k2Vel = acceleration; // Assuming constant acceleration
            float3 k2Pos = midVel;

            newVelocity = velocity + k2Vel * deltaTime;
            newPosition = position + k2Pos * deltaTime;
        }

        /// <summary>
        /// Integrates rotation using RK2.
        /// </summary>
        [BurstCompile]
        public static void IntegrateRotation(
            in quaternion rotation,
            in float3 angularVelocity,
            float deltaTime,
            out quaternion newRotation)
        {
            // Simplified rotation integration
            float3 axis = math.normalize(angularVelocity);
            float angle = math.length(angularVelocity) * deltaTime;
            quaternion deltaRot = quaternion.AxisAngle(axis, angle);
            newRotation = math.mul(rotation, deltaRot);
        }
    }
}

