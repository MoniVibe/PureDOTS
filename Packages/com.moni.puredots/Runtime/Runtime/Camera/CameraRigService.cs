using System;
using PureDOTS.Runtime.Config;

#nullable enable

namespace PureDOTS.Runtime.Camera
{
    public static class CameraRigService
    {
        private static RuntimeConfigVar? s_ecsCameraVar;
        private static CameraRigState s_currentState;
        private static bool s_hasState;

        static CameraRigService()
        {
            RuntimeConfigRegistry.Initialize();
            s_ecsCameraVar = Space4XCameraConfigVars.EcsModeEnabled;
        }

        public static event Action<CameraRigState>? CameraStateChanged;

        public static bool HasState => s_hasState;

        public static CameraRigState Current => s_currentState;

        public static bool IsEcsCameraEnabled => s_ecsCameraVar != null && s_ecsCameraVar.BoolValue;

        public static void Publish(CameraRigState state)
        {
            s_currentState = state;
            s_hasState = true;
            CameraStateChanged?.Invoke(state);
        }
    }
}


