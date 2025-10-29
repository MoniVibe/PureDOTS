using Unity.Entities;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Stub system for future faction/empire registry logic.
    /// </summary>
    public partial struct FactionRegistrySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // TODO: implement faction registry rebuild
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state) { }
    }

    /// <summary>
    /// Stub system for future climate/hazard registry.
    /// </summary>
    public partial struct ClimateHazardRegistrySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // TODO: implement climate hazard registry rebuild
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state) { }
    }

    /// <summary>
    /// Stub system for future area effect registry.
    /// </summary>
    public partial struct AreaEffectRegistrySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // TODO: implement area effect registry rebuild
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state) { }
    }

    /// <summary>
    /// Stub system for future culture/alignment registry.
    /// </summary>
    public partial struct CultureAlignmentRegistrySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // TODO: implement culture alignment registry rebuild
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state) { }
    }
}
