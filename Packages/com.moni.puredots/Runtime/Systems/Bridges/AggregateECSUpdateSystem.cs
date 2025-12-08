using PureDOTS.Systems.Bridges;
using PureDOTS.Systems;
using Unity.Entities;

namespace PureDOTS.Systems.Bridges
{
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(AggregateBridgeSystem))]
    public sealed partial class AggregateECSUpdateSystem : SystemBase
    {
        protected override void OnCreate()
        {
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }
    }
}
