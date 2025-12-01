using PureDOTS.Runtime.Config;

namespace PureDOTS.Runtime.Camera
{
    public static class Space4XCameraConfigVars
    {
        [RuntimeConfigVar("camera.ecs.enabled", "0", Flags = RuntimeConfigFlags.Save, Description = "Enable the ECS-based Space4X camera pipeline.")]
        public static RuntimeConfigVar EcsModeEnabled;
    }
}
