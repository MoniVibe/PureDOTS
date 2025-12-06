using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Spatial
{
    /// <summary>
    /// Array-of-Structure-of-Arrays (AoSoA) packet for SIMD-vectorized cell operations.
    /// Stores 16 entities per packet with aligned arrays for cache-coherent access.
    /// Size: 512 bytes (aligned to cache line boundaries).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 512)]
    public unsafe struct CellPacket
    {
        /// <summary>
        /// X positions for 16 entities (SIMD-aligned).
        /// </summary>
        public fixed float PosX[16];

        /// <summary>
        /// Y positions for 16 entities.
        /// </summary>
        public fixed float PosY[16];

        /// <summary>
        /// Z positions for 16 entities.
        /// </summary>
        public fixed float PosZ[16];

        /// <summary>
        /// X velocities for 16 entities.
        /// </summary>
        public fixed float VelX[16];

        /// <summary>
        /// Y velocities for 16 entities.
        /// </summary>
        public fixed float VelY[16];

        /// <summary>
        /// Z velocities for 16 entities.
        /// </summary>
        public fixed float VelZ[16];

        /// <summary>
        /// Entity references for 16 entities.
        /// </summary>
        public fixed ulong EntityData[16]; // Stored as Index | (Version << 32)

        /// <summary>
        /// Number of valid entities in this packet (0-16).
        /// </summary>
        public byte Count;

        /// <summary>
        /// Padding to align to 512 bytes.
        /// </summary>
        private fixed byte _padding[7];

        /// <summary>
        /// Gets the position for entity at index (0-15).
        /// </summary>
        public readonly float3 GetPosition(int index)
        {
            if (index < 0 || index >= 16)
            {
                return float3.zero;
            }

            unsafe
            {
                fixed (float* px = PosX, py = PosY, pz = PosZ)
                {
                    return new float3(px[index], py[index], pz[index]);
                }
            }
        }

        /// <summary>
        /// Sets the position for entity at index.
        /// </summary>
        public void SetPosition(int index, in float3 position)
        {
            if (index < 0 || index >= 16)
            {
                return;
            }

            unsafe
            {
                fixed (float* px = PosX, py = PosY, pz = PosZ)
                {
                    px[index] = position.x;
                    py[index] = position.y;
                    pz[index] = position.z;
                }
            }
        }

        /// <summary>
        /// Gets the velocity for entity at index.
        /// </summary>
        public readonly float3 GetVelocity(int index)
        {
            if (index < 0 || index >= 16)
            {
                return float3.zero;
            }

            unsafe
            {
                fixed (float* vx = VelX, vy = VelY, vz = VelZ)
                {
                    return new float3(vx[index], vy[index], vz[index]);
                }
            }
        }

        /// <summary>
        /// Sets the velocity for entity at index.
        /// </summary>
        public void SetVelocity(int index, in float3 velocity)
        {
            if (index < 0 || index >= 16)
            {
                return;
            }

            unsafe
            {
                fixed (float* vx = VelX, vy = VelY, vz = VelZ)
                {
                    vx[index] = velocity.x;
                    vy[index] = velocity.y;
                    vz[index] = velocity.z;
                }
            }
        }

        /// <summary>
        /// Gets the entity at index.
        /// </summary>
        public readonly Entity GetEntity(int index)
        {
            if (index < 0 || index >= 16)
            {
                return Entity.Null;
            }

            unsafe
            {
                fixed (ulong* entities = EntityData)
                {
                    var data = entities[index];
                    return new Entity
                    {
                        Index = (int)(data & 0xFFFFFFFFUL),
                        Version = (uint)(data >> 32)
                    };
                }
            }
        }

        /// <summary>
        /// Sets the entity at index.
        /// </summary>
        public void SetEntity(int index, Entity entity)
        {
            if (index < 0 || index >= 16)
            {
                return;
            }

            unsafe
            {
                fixed (ulong* entities = EntityData)
                {
                    entities[index] = (ulong)(uint)entity.Index | ((ulong)entity.Version << 32);
                }
            }
        }

        /// <summary>
        /// Clears all entities in the packet.
        /// </summary>
        public void Clear()
        {
            Count = 0;
            unsafe
            {
                fixed (float* px = PosX, py = PosY, pz = PosZ, vx = VelX, vy = VelY, vz = VelZ)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        px[i] = 0f;
                        py[i] = 0f;
                        pz[i] = 0f;
                        vx[i] = 0f;
                        vy[i] = 0f;
                        vz[i] = 0f;
                    }
                }

                fixed (ulong* entities = EntityData)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        entities[i] = 0;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Hot data component for spatial cells (queried per tick).
    /// Stores entity positions and velocities in AoSoA packets for SIMD operations.
    /// </summary>
    public struct SpatialCellHotData : IComponentData
    {
        /// <summary>
        /// Number of packets allocated.
        /// </summary>
        public int PacketCount;

        /// <summary>
        /// Number of entities currently stored.
        /// </summary>
        public int EntityCount;

        /// <summary>
        /// Last update tick.
        /// </summary>
        public uint LastUpdateTick;
    }

    /// <summary>
    /// Buffer storing AoSoA cell packets (hot data).
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct SpatialCellPacketBuffer : IBufferElementData
    {
        public CellPacket Packet;
    }

    /// <summary>
    /// Cold data component for spatial cells (queried per second+).
    /// Stores density, average mass, average energy, and cached statistics.
    /// </summary>
    public struct SpatialCellColdData : IComponentData
    {
        /// <summary>
        /// Entity density (entities per unit volume).
        /// </summary>
        public float Density;

        /// <summary>
        /// Average mass of entities in this cell.
        /// </summary>
        public float AverageMass;

        /// <summary>
        /// Average energy of entities in this cell.
        /// </summary>
        public float AverageEnergy;

        /// <summary>
        /// Cached statistics version (incremented when stats change).
        /// </summary>
        public uint StatsVersion;

        /// <summary>
        /// Last update tick for cold data.
        /// </summary>
        public uint LastUpdateTick;

        /// <summary>
        /// Update interval in ticks (default: 60 ticks = 1 second at 60 Hz).
        /// </summary>
        public uint UpdateInterval;
    }
}

