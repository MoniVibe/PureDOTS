using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Godgame.Roads
{
    /// <summary>
    /// Global configuration for Godgame road spawning, visuals, and auto-build heuristics.
    /// </summary>
    public struct GodgameRoadConfig : IComponentData
    {
        public float DefaultRoadWidth;
        public float InitialStretchLength;
        public float HandleMass;
        public float HandleFollowLerp;
        public float HeatCellSize;
        public float HeatDecayPerSecond;
        public float HeatBuildThreshold;
        public float AutoBuildLength;
        public float RoadMeshBaseLength;
        public Hash128 RoadDescriptor;
        public Hash128 HandleDescriptor;
    }

    /// <summary>
    /// Tag + data for a village center that should be wrapped by a starter road loop.
    /// </summary>
    public struct GodgameVillageCenter : IComponentData
    {
        public float RoadRingRadius;
        public float BaseHeight;
    }

    public struct GodgameVillageRoadInitializedTag : IComponentData { }

    /// <summary>
    /// Core road segment data. Start/End are world positions describing the segment line.
    /// </summary>
    public struct GodgameRoadSegment : IComponentData
    {
        public Entity VillageCenter;
        public float3 Start;
        public float3 End;
        public float Width;
        public byte Flags;
    }

    public static class GodgameRoadFlags
    {
        public const byte AutoBuilt = 1 << 0;
    }

    /// <summary>
    /// Applied to small pickable endpoints that the divine hand can drag to stretch roads.
    /// </summary>
    public struct GodgameRoadHandle : IComponentData
    {
        public Entity Road;
        public byte Endpoint; // 0 = start, 1 = end
    }

    /// <summary>
    /// Singleton marker storing road heatmap data.
    /// </summary>
    public struct GodgameRoadHeatMap : IComponentData { }

    /// <summary>
    /// Buffer entry describing accumulated heat for a quantized world cell.
    /// DirectionSum stores the aggregate movement direction.
    /// </summary>
    public struct GodgameRoadHeatCell : IBufferElementData
    {
        public int3 Cell;
        public float Heat;
        public float2 DirectionSum;
    }
}
