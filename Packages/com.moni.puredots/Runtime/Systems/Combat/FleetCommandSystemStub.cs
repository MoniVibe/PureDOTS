using Unity.Entities;

namespace PureDOTS.Systems.Combat
{
    public partial struct FleetCommandSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {}
        public void OnDestroy(ref SystemState state) {}
        public void OnUpdate(ref SystemState state) {}
    }

    public partial struct ParryReactionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {}
        public void OnDestroy(ref SystemState state) {}
        public void OnUpdate(ref SystemState state) {}
    }

    public partial struct LearningDecaySystem : ISystem
    {
        public void OnCreate(ref SystemState state) {}
        public void OnDestroy(ref SystemState state) {}
        public void OnUpdate(ref SystemState state) {}
    }
}

