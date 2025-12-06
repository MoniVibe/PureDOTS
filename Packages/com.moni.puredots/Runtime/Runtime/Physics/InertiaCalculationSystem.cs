using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Runtime.Physics
{
    /// <summary>
    /// Calculates inertia tensors for composite entities using parallel reduction.
    /// Implements diagonalized tensor calculation: Ixx = Σm(y²+z²), Iyy = Σm(x²+z²), Izz = Σm(x²+y²)
    /// Updates inertia for composite hulls with additional cargo mass.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(MassAggregationSystem))]
    public partial struct InertiaCalculationSystem : ISystem
    {
        private EntityQuery _compositeQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TickTimeState>();
            state.RequireForUpdate<RewindState>();

            _compositeQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<MassComponent>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<Parent>()
            );
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (_compositeQuery.CalculateEntityCount() == 0)
            {
                return;
            }

            var transformLookup = state.GetComponentLookup<LocalTransform>(true);
            var massLookup = state.GetComponentLookup<MassComponent>(true);

            var calculateJob = new CalculateInertiaJob
            {
                TransformLookup = transformLookup,
                MassLookup = massLookup
            };

            state.Dependency = calculateJob.ScheduleParallel(_compositeQuery, state.Dependency);
        }

        [BurstCompile]
        private struct CalculateInertiaJob : IJobChunk
        {
            [ReadOnly]
            public ComponentLookup<LocalTransform> TransformLookup;

            [ReadOnly]
            public ComponentLookup<MassComponent> MassLookup;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var massComponents = chunk.GetNativeArray(ref chunk.GetRequiredComponentTypeHandle<MassComponent>(false));
                var transforms = chunk.GetNativeArray(ref chunk.GetRequiredComponentTypeHandle<LocalTransform>(true));
                var parents = chunk.GetNativeArray(ref chunk.GetRequiredComponentTypeHandle<Parent>(true));
                var entities = chunk.GetEntityArray();

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    var mass = massComponents[i];
                    var transform = transforms[i];
                    var parent = parents[i];

                    if (parent.Value == Entity.Null || !MassLookup.HasComponent(parent.Value))
                    {
                        continue;
                    }

                    // Calculate relative position to parent center of mass
                    var parentMass = MassLookup[parent.Value];
                    var relativePos = transform.Position - parentMass.CenterOfMass;

                    // Calculate inertia tensor components using parallel axis theorem
                    // Ixx = Σm(y²+z²), Iyy = Σm(x²+z²), Izz = Σm(x²+y²)
                    var x = relativePos.x;
                    var y = relativePos.y;
                    var z = relativePos.z;
                    var xSq = x * x;
                    var ySq = y * y;
                    var zSq = z * z;

                    var inertia = new float3(
                        mass.Mass * (ySq + zSq),  // Ixx
                        mass.Mass * (xSq + zSq),  // Iyy
                        mass.Mass * (xSq + ySq)   // Izz
                    );

                    // Add local inertia tensor
                    inertia += mass.InertiaTensor;

                    mass.InertiaTensor = inertia;
                    massComponents[i] = mass;
                }
            }
        }
    }
}

