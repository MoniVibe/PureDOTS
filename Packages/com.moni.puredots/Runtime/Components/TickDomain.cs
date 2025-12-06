using Unity.Entities;

namespace PureDOTS.Runtime.Components
{
    /// <summary>
    /// Component defining tick domain and ratio for heterogeneous tick rates.
    /// Each subsystem owns its own clock (Physics 60Hz, Cognitive 0.5-5Hz, Economy 0.1Hz).
    /// </summary>
    public struct TickDomain : IComponentData
    {
        public TickDomainType DomainType;
        public uint TickRatio; // Ratio relative to base tick (e.g., 120 for 0.5Hz when base is 60Hz)
        public uint LastTick;
        public uint NextTick;
    }

    /// <summary>
    /// Tick domain types for different subsystems.
    /// </summary>
    public enum TickDomainType : byte
    {
        Physics = 0,      // 60 Hz (1:1 ratio)
        Cognitive = 1,    // 0.5-5 Hz (adaptive)
        Economy = 2,      // 0.1 Hz
        Custom = 255
    }
}

