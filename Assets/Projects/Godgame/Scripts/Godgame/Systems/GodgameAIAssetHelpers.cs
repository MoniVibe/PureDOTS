using PureDOTS.Runtime.AI;
using Unity.Collections;

namespace Godgame.Systems
{
    /// <summary>
    /// Helper utilities for creating default AI archetype blobs for villagers.
    /// </summary>
    public static class GodgameAIAssetHelpers
    {
        /// <summary>
        /// Creates a default villager AI utility archetype blob with 4 actions:
        /// 0: SatisfyHunger (responds to low hunger sensor reading)
        /// 1: Rest (responds to low energy sensor reading)
        /// 2: ImproveMorale (responds to low morale sensor reading)
        /// 3: Work (responds to job availability)
        /// </summary>
        public static BlobAssetReference<AIUtilityArchetypeBlob> CreateDefaultVillagerArchetypeBlob(Unity.Collections.Allocator allocator)
        {
            var builder = new BlobBuilder(allocator);
            ref var root = ref builder.ConstructRoot<AIUtilityArchetypeBlob>();

            // Create 4 actions
            var actions = builder.Allocate(ref root.Actions, 4);

            // Action 0: SatisfyHunger - responds to hunger need (sensor reading 0 = hunger urgency)
            ref var action0 = ref actions[0];
            var factors0 = builder.Allocate(ref action0.Factors, 1);
            factors0[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 0, // Hunger urgency (normalized 0-1, higher = more urgent)
                Threshold = 0.3f, // Trigger when hunger below 30%
                Weight = 2f, // High priority
                ResponsePower = 2f, // Quadratic response
                MaxValue = 1f
            };

            // Action 1: Rest - responds to energy need (sensor reading 1 = energy urgency)
            ref var action1 = ref actions[1];
            var factors1 = builder.Allocate(ref action1.Factors, 1);
            factors1[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 1, // Energy urgency
                Threshold = 0.2f, // Trigger when energy below 20%
                Weight = 1.5f, // Medium-high priority
                ResponsePower = 1.5f,
                MaxValue = 1f
            };

            // Action 2: ImproveMorale - responds to morale need (sensor reading 2 = morale urgency)
            ref var action2 = ref actions[2];
            var factors2 = builder.Allocate(ref action2.Factors, 1);
            factors2[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 2, // Morale urgency
                Threshold = 0.4f, // Trigger when morale below 40%
                Weight = 1f, // Medium priority
                ResponsePower = 1f, // Linear response
                MaxValue = 1f
            };

            // Action 3: Work - responds to job availability (sensor reading 3 = nearest resource node)
            ref var action3 = ref actions[3];
            var factors3 = builder.Allocate(ref action3.Factors, 1);
            factors3[0] = new AIUtilityCurveBlob
            {
                SensorIndex = 3, // Nearest resource node proximity
                Threshold = 0f, // Always consider work if resources available
                Weight = 0.8f, // Lower priority than needs
                ResponsePower = 1f,
                MaxValue = 1f
            };

            var blob = builder.CreateBlobAssetReference<AIUtilityArchetypeBlob>(allocator);
            builder.Dispose();
            return blob;
        }
    }
}

