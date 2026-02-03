using PureDOTS.Runtime.Core;
using Unity.Entities;
using UnityDebug = UnityEngine.Debug;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Disables entire system groups for bug-hunt isolation.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class BugHuntGroupGateSystem : SystemBase
    {
        protected override void OnCreate()
        {
            if (!BugHuntGate.IsEnabled)
            {
                Enabled = false;
            }
        }

        protected override void OnUpdate()
        {
            DisableGroup<HandSystemGroup>("hand");
            DisableGroup<ThrownObjectPrePhysicsSystemGroup>("thrown_objects");
            DisableGroup<CombatSystemGroup>("combat");
            DisableGroup<VegetationSystemGroup>("vegetation");
            Enabled = false;
        }

        private void DisableGroup<T>(string token) where T : ComponentSystemBase
        {
            if (!BugHuntGate.IsDisabled(token))
            {
                return;
            }

            var group = World.GetExistingSystemManaged<T>();
            if (group == null)
            {
                return;
            }

            group.Enabled = false;
            UnityDebug.Log($"[BugHuntGate] Disabled group {typeof(T).Name} (token={token}).");
        }
    }
}
