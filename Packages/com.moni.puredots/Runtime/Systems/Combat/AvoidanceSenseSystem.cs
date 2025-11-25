using PureDOTS.Runtime.Combat;
using PureDOTS.Runtime.Components;
using PureDOTS.Systems;
using Space4X.Knowledge;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Combat
{
    /// <summary>
    /// Samples hazard grid gradient and computes avoidance steering vector.
    /// Per ship: sample grid gradient (±1 cells), compute V_avoid = -∇Risk.
    /// Applies ReactionSec delay via ring buffer of sampled risks.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSystemGroup))]
    [UpdateAfter(typeof(AccumulateHazardGridSystem))]
    public partial struct AvoidanceSenseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<HazardGridSingleton>(out var gridSingleton) ||
                !SystemAPI.HasComponent<HazardGrid>(gridSingleton.GridEntity))
            {
                return;
            }

            var grid = SystemAPI.GetComponent<HazardGrid>(gridSingleton.GridEntity);
            if (!grid.Risk.IsCreated)
            {
                return;
            }

            var job = new AvoidanceSenseJob
            {
                Grid = grid,
                RiskData = grid.Risk.Value
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct AvoidanceSenseJob : IJobEntity
        {
            [ReadOnly] public HazardGrid Grid;
            [ReadOnly] public BlobArray<float> RiskData;

            void Execute(
                Entity entity,
                ref HazardAvoidanceState avoidanceState,
                in AvoidanceProfile profile,
                in LocalTransform transform)
            {
                float3 pos = transform.Position;

                // Sample risk gradient around ship position
                int3 cell = CellOf(pos, Grid);

                // Sample neighboring cells (±1 in each axis)
                float rx = SampleRisk(cell + new int3(1, 0, 0)) - SampleRisk(cell + new int3(-1, 0, 0));
                float ry = SampleRisk(cell + new int3(0, 1, 0)) - SampleRisk(cell + new int3(0, -1, 0));
                float rz = Grid.Size.z > 1
                    ? SampleRisk(cell + new int3(0, 0, 1)) - SampleRisk(cell + new int3(0, 0, -1))
                    : 0f; // 2D grid

                // Compute avoidance vector (negative gradient)
                float3 gradient = new float3(rx, ry, rz);
                float gradientLength = math.length(gradient);

                if (gradientLength > 1e-6f)
                {
                    float3 vAvoid = -math.normalize(gradient);

                    // Scale by risk magnitude
                    float currentRisk = SampleRisk(cell);
                    float avoidanceWeight = math.saturate(currentRisk / profile.BreakFormationThresh);

                    avoidanceState.CurrentAdjustment = vAvoid * avoidanceWeight;
                    avoidanceState.AvoidanceUrgency = avoidanceWeight;
                }
                else
                {
                    avoidanceState.CurrentAdjustment = float3.zero;
                    avoidanceState.AvoidanceUrgency = 0f;
                }

                // TODO: Apply ReactionSec delay via ring buffer
                // For now, immediate response
            }

            private float SampleRisk(int3 cell)
            {
                // Clamp cell to grid bounds
                cell = math.clamp(cell, int3.zero, Grid.Size - 1);

                int index = Flatten(cell, Grid);
                if (index >= 0 && index < RiskData.Length)
                {
                    return RiskData[index];
                }

                return 0f;
            }

            private static int3 CellOf(float3 pos, HazardGrid grid)
            {
                float3 local = (pos - grid.Origin) / grid.Cell;
                return (int3)math.floor(local);
            }

            private static int Flatten(int3 cell, HazardGrid grid)
            {
                return (cell.z * grid.Size.y + cell.y) * grid.Size.x + cell.x;
            }
        }
    }
}

