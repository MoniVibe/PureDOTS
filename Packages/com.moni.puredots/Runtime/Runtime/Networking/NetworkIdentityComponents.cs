using Unity.Entities;

namespace PureDOTS.Runtime.Networking
{
    /// <summary>
    /// Network identity for persistent entities in multiplayer-ready architecture.
    /// Deterministic, Burst-safe GUID for entity identification across network.
    /// Authority byte determines ownership model (Server/Client/Hybrid).
    /// </summary>
    /// <remarks>
    /// <para><b>Authority-Free Reconciliation Model:</b></para>
    /// <para>
    /// PureDOTS is deterministic, enabling peer-to-peer lockstep or server authoritative modes.
    /// No hardcoded "Server/Client" assumptions - everything keys off Authority byte in NetworkId.
    /// </para>
    /// <para>
    /// Authority values:
    /// - 0 = Server (server-authoritative, server owns entity)
    /// - 1 = Client (client-authoritative, client owns entity)
    /// - 2 = Hybrid (shared authority, both can modify)
    /// </para>
    /// <para>
    /// Because PureDOTS is deterministic, you can run peer-to-peer lockstep or server authoritative
    /// interchangeably. The Authority byte determines ownership and replication authority without
    /// touching ECS logic. Systems check Authority to determine if they should process an entity.
    /// </para>
    /// <para>
    /// Example: In lockstep mode, all clients have Authority=Server (0) and process identically.
    /// In client-server mode, player entities have Authority=Client (1) on their owning client.
    /// </para>
    /// </remarks>
    public struct NetworkId : IComponentData
    {
        /// <summary>
        /// Deterministic GUID for entity identification (Burst-safe).
        /// </summary>
        public ulong Guid;

        /// <summary>
        /// Authority byte: 0=Server, 1=Client, 2=Hybrid.
        /// Determines ownership and replication authority without touching ECS logic.
        /// </summary>
        public byte Authority;
    }

    /// <summary>
    /// Tag component marking entities that have network identity.
    /// </summary>
    public struct NetworkIdentityTag : IComponentData { }

    /// <summary>
    /// Authority constants for NetworkId.Authority field.
    /// </summary>
    public static class NetworkAuthority
    {
        public const byte Server = 0;
        public const byte Client = 1;
        public const byte Hybrid = 2;
    }
}

