using PureDOTS.Runtime.Physics;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Authoring.Physics
{
    /// <summary>
    /// Authoring component for collision properties.
    /// Bakes CollisionProperties component with radius and mass values.
    /// </summary>
    public class CollisionPropertiesAuthoring : MonoBehaviour
    {
        [Tooltip("Collision radius in meters. Determines collision regime (< 100m = Micro, 100m-10km = Meso, > 10km = Macro).")]
        public float Radius = 1f;

        [Tooltip("Mass in kilograms. Used for Q-law energy calculations and momentum conservation.")]
        public float Mass = 1000f;

        [Tooltip("Initial structural integrity for Micro regime entities (0.0 = destroyed, 1.0 = pristine).")]
        [Range(0f, 1f)]
        public float InitialIntegrity = 1f;
    }

    /// <summary>
    /// Baker for CollisionPropertiesAuthoring.
    /// </summary>
    public class CollisionPropertiesBaker : Baker<CollisionPropertiesAuthoring>
    {
        public override void Bake(CollisionPropertiesAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new CollisionProperties
            {
                Radius = authoring.Radius,
                Mass = authoring.Mass,
                RegimeThreshold = 0f, // Will be computed by CollisionRegimeSelectorSystem
                Regime = CollisionRegime.Micro // Initial value, will be updated
            });

            // Add StructuralIntegrity for Micro regime entities
            if (authoring.Radius < 100f)
            {
                AddComponent(entity, new StructuralIntegrity
                {
                    Value = authoring.InitialIntegrity,
                    MaxValue = authoring.InitialIntegrity
                });
            }
        }
    }
}

