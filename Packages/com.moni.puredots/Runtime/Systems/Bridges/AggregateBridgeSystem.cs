using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;
using Unity.Entities;

namespace PureDOTS.Systems.Bridges
{
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(VillagerSystemGroup))]
    public sealed partial class AggregateBridgeSystem : SystemBase
    {
        protected override void OnCreate()
        {
            Enabled = false; // AI layer not present; disable system.
        }

        protected override void OnUpdate()
        {
        }
    }
}
