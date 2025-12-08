using Unity.Entities;
using PureDOTS.Runtime.Core;
using PureDOTS.Systems;

namespace PureDOTS.Runtime.Bridges
{
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public sealed partial class MindECSUpdateSystem : SystemBase
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
