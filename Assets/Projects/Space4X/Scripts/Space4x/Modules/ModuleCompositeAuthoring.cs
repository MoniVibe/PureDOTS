using PureDOTS.Authoring.Items;
using UnityEngine;

namespace Space4X.Modules
{
    /// <summary>
    /// Space4X-specific authoring for module composite items.
    /// Modules consist of hull, power cell, targeting system, and heatsink.
    /// </summary>
    [CreateAssetMenu(fileName = "ModuleComposite", menuName = "Space4X/Modules/Module Composite")]
    public class ModuleCompositeAuthoring : CompositeItemAuthoring
    {
        [Header("Module-Specific")]
        [Tooltip("Hull material")]
        public string hullMaterial = "Titanium";

        [Tooltip("Power cell material")]
        public string powerCellMaterial = "Plasma";

        [Tooltip("Targeting system material")]
        public string targetingMaterial = "Crystal";

        [Tooltip("Heatsink material")]
        public string heatsinkMaterial = "Copper";

        private void OnValidate()
        {
            // Auto-populate parts if empty
            if (parts.Count == 0)
            {
                // Hull (PartTypeId 20 = Hull, weight 0.4)
                parts.Add(new PartAuthoring
                {
                    partTypeId = 20, // Hull
                    material = hullMaterial,
                    quality01 = 0.5f,
                    durability01 = 1f,
                    rarityWeight = 40
                });

                // Power Cell (PartTypeId 21 = PowerCell, weight 0.3)
                parts.Add(new PartAuthoring
                {
                    partTypeId = 21, // PowerCell
                    material = powerCellMaterial,
                    quality01 = 0.5f,
                    durability01 = 1f,
                    rarityWeight = 30
                });

                // Targeting System (PartTypeId 22 = Targeting, weight 0.2)
                parts.Add(new PartAuthoring
                {
                    partTypeId = 22, // Targeting
                    material = targetingMaterial,
                    quality01 = 0.5f,
                    durability01 = 1f,
                    rarityWeight = 20
                });

                // Heatsink (PartTypeId 23 = Heatsink, weight 0.1)
                parts.Add(new PartAuthoring
                {
                    partTypeId = 23, // Heatsink
                    material = heatsinkMaterial,
                    quality01 = 0.5f,
                    durability01 = 1f,
                    rarityWeight = 10
                });
            }
        }
    }
}

