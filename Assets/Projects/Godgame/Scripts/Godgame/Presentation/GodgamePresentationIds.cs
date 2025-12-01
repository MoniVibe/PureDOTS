using Unity.Collections;

namespace Godgame.Presentation
{
    /// <summary>
    /// Centralised identifiers for Godgame presentation bindings.
    /// </summary>
        public static class GodgamePresentationIds
        {
            public const int MiraclePingEffectId = 1001;
            public static FixedString64Bytes MiraclePingStyle => new FixedString64Bytes("fx.miracle.ping");
        }
}
