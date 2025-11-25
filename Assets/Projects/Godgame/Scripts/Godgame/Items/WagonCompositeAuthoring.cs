using PureDOTS.Authoring.Items;
using UnityEngine;

namespace Godgame.Items
{
    /// <summary>
    /// Godgame-specific authoring for wagon composite items.
    /// Wagons consist of wheels, axle, frame, and bolts.
    /// </summary>
    [CreateAssetMenu(fileName = "WagonComposite", menuName = "Godgame/Items/Wagon Composite")]
    public class WagonCompositeAuthoring : CompositeItemAuthoring
    {
        [Header("Wagon-Specific")]
        [Tooltip("Number of wheels (typically 4)")]
        [Range(2, 8)]
        public int wheelCount = 4;

        [Tooltip("Wheel material")]
        public string wheelMaterial = "Wood";

        [Tooltip("Axle material")]
        public string axleMaterial = "Iron";

        [Tooltip("Frame material")]
        public string frameMaterial = "Wood";

        [Tooltip("Bolt material")]
        public string boltMaterial = "Iron";

        private void OnValidate()
        {
            // Auto-populate parts if empty
            if (parts.Count == 0)
            {
                // Wheels (PartTypeId 1 = Wheel, weight 0.3 total)
                float wheelWeight = 0.3f / wheelCount;
                for (int i = 0; i < wheelCount; i++)
                {
                    parts.Add(new PartAuthoring
                    {
                        partTypeId = 1, // Wheel
                        material = wheelMaterial,
                        quality01 = 0.5f,
                        durability01 = 1f,
                        rarityWeight = 30
                    });
                }

                // Axle (PartTypeId 2 = Axle, weight 0.4)
                parts.Add(new PartAuthoring
                {
                    partTypeId = 2, // Axle
                    material = axleMaterial,
                    quality01 = 0.5f,
                    durability01 = 1f,
                    rarityWeight = 40
                });

                // Frame (PartTypeId 3 = Frame, weight 0.2)
                parts.Add(new PartAuthoring
                {
                    partTypeId = 3, // Frame
                    material = frameMaterial,
                    quality01 = 0.5f,
                    durability01 = 1f,
                    rarityWeight = 20
                });

                // Bolts (PartTypeId 4 = Bolts, weight 0.1 total)
                parts.Add(new PartAuthoring
                {
                    partTypeId = 4, // Bolts
                    material = boltMaterial,
                    quality01 = 0.5f,
                    durability01 = 1f,
                    rarityWeight = 10
                });
            }
        }
    }
}

