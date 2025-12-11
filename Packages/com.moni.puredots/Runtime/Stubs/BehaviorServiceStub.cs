// [TRI-STUB] This is an ahead-of-time stub. Safe to compile, does nothing at runtime.
using Unity.Entities;

namespace PureDOTS.Runtime.Behavior
{
    public static class BehaviorServiceStub
    {
        public static void ApplyProfile(in Entity entity, int profileId) { }

        public static void RegisterNeed(in Entity entity, byte needType) { }

        public static void ReportSatisfaction(in Entity entity, byte needType, float value) { }
    }
}
