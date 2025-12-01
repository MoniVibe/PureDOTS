#if PUREDOTS_LEGACY_CAMERA
using PureDOTS.Runtime.Camera;
using Unity.Entities;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Singleton used to toggle manual phase groups on or off at runtime.
    /// </summary>
    public struct ManualPhaseControl : IComponentData
    {
        public bool CameraPhaseEnabled;
        public bool TransportPhaseEnabled;
        public bool HistoryPhaseEnabled;

        public static ManualPhaseControl CreateDefaults(string profileId)
        {
            var defaults = new ManualPhaseControl
            {
                CameraPhaseEnabled = CameraRigService.IsEcsCameraEnabled,
                TransportPhaseEnabled = true,
                HistoryPhaseEnabled = true
            };

            if (profileId == SystemRegistry.BuiltinProfiles.HeadlessId)
            {
                defaults.CameraPhaseEnabled = false;
            }

            return defaults;
        }
    }
}
#endif
