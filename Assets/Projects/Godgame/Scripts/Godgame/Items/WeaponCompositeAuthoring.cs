using PureDOTS.Authoring.Items;
using UnityEngine;

namespace Godgame.Items
{
    /// <summary>
    /// Godgame-specific authoring for weapon composite items.
    /// Weapons consist of blade, handle, and pommel.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponComposite", menuName = "Godgame/Items/Weapon Composite")]
    public class WeaponCompositeAuthoring : CompositeItemAuthoring
    {
        [Header("Weapon-Specific")]
        [Tooltip("Blade material")]
        public string bladeMaterial = "Iron";

        [Tooltip("Handle material")]
        public string handleMaterial = "Wood";

        [Tooltip("Pommel material (optional)")]
        public string pommelMaterial = "Iron";

        [Tooltip("Include pommel?")]
        public bool hasPommel = true;

        private void OnValidate()
        {
            // Auto-populate parts if empty
            if (parts.Count == 0)
            {
                // Blade (PartTypeId 10 = Blade, weight 0.6)
                parts.Add(new PartAuthoring
                {
                    partTypeId = 10, // Blade
                    material = bladeMaterial,
                    quality01 = 0.5f,
                    durability01 = 1f,
                    rarityWeight = 60
                });

                // Handle (PartTypeId 11 = Handle, weight 0.3)
                parts.Add(new PartAuthoring
                {
                    partTypeId = 11, // Handle
                    material = handleMaterial,
                    quality01 = 0.5f,
                    durability01 = 1f,
                    rarityWeight = 30
                });

                // Pommel (PartTypeId 12 = Pommel, weight 0.1, optional)
                if (hasPommel)
                {
                    parts.Add(new PartAuthoring
                    {
                        partTypeId = 12, // Pommel
                        material = pommelMaterial,
                        quality01 = 0.5f,
                        durability01 = 1f,
                        rarityWeight = 10
                    });
                }
            }
        }
    }
}

