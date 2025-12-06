using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Snapshot of physics state for rewind support.
    /// Extends existing snapshot system.
    /// </summary>
    public struct PhysicsStateSnapshot
    {
        public uint Tick;
        public NativeArray<EntityPhysicsState> EntityStates;
    }

    /// <summary>
    /// Physics state for a single entity.
    /// </summary>
    public struct EntityPhysicsState
    {
        public Entity Entity;
        public float3 Position;
        public float3 Velocity;
        public quaternion Rotation;
        public float3 AngularVelocity;
    }
}

