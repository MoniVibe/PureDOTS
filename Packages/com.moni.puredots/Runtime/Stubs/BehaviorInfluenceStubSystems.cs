// [TRI-STUB] This is an ahead-of-time stub. Safe to compile, does nothing at runtime.
using Unity.Burst;
using Unity.Entities;

namespace PureDOTS.Runtime.Behavior
{
    [BurstCompile]
    public partial struct BehaviorInfluenceStubSystem : ISystem
    {
        [BurstCompile] public void OnCreate(ref SystemState state) { }
        [BurstCompile] public void OnDestroy(ref SystemState state) { }
        [BurstCompile] public void OnUpdate(ref SystemState state) { }
    }
}
