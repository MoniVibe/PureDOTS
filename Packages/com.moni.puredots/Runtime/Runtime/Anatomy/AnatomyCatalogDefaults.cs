using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Anatomy
{
    public static class AnatomyCatalogDefaults
    {
        public static BlobAssetReference<AnatomyCatalogBlob> BuildDefaultCatalog()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<AnatomyCatalogBlob>();
            var definitions = builder.Allocate(ref root.Definitions, 1);

            definitions[0].AnatomyId = new FixedString64Bytes("humanoid");
            var parts = builder.Allocate(ref definitions[0].Parts, 8);

            parts[0] = new BodyPartDefinition
            {
                PartId = new FixedString64Bytes("head"),
                ParentId = new FixedString64Bytes("torso"),
                Kind = BodyPartKind.Limb,
                Flags = BodyPartFlags.Vital | BodyPartFlags.Sensor,
                MaxHealth = 35f,
                RegenRate = 0f,
                DamageMultiplier = 1.2f
            };
            parts[1] = new BodyPartDefinition
            {
                PartId = new FixedString64Bytes("torso"),
                ParentId = new FixedString64Bytes(string.Empty),
                Kind = BodyPartKind.Limb,
                Flags = BodyPartFlags.Vital,
                MaxHealth = 80f,
                RegenRate = 0f,
                DamageMultiplier = 1f
            };
            parts[2] = new BodyPartDefinition
            {
                PartId = new FixedString64Bytes("left_arm"),
                ParentId = new FixedString64Bytes("torso"),
                Kind = BodyPartKind.Limb,
                Flags = BodyPartFlags.Manipulator | BodyPartFlags.Paired,
                MaxHealth = 45f,
                RegenRate = 0f,
                DamageMultiplier = 1f
            };
            parts[3] = new BodyPartDefinition
            {
                PartId = new FixedString64Bytes("right_arm"),
                ParentId = new FixedString64Bytes("torso"),
                Kind = BodyPartKind.Limb,
                Flags = BodyPartFlags.Manipulator | BodyPartFlags.Paired,
                MaxHealth = 45f,
                RegenRate = 0f,
                DamageMultiplier = 1f
            };
            parts[4] = new BodyPartDefinition
            {
                PartId = new FixedString64Bytes("left_leg"),
                ParentId = new FixedString64Bytes("torso"),
                Kind = BodyPartKind.Limb,
                Flags = BodyPartFlags.Locomotion | BodyPartFlags.Paired,
                MaxHealth = 55f,
                RegenRate = 0f,
                DamageMultiplier = 1f
            };
            parts[5] = new BodyPartDefinition
            {
                PartId = new FixedString64Bytes("right_leg"),
                ParentId = new FixedString64Bytes("torso"),
                Kind = BodyPartKind.Limb,
                Flags = BodyPartFlags.Locomotion | BodyPartFlags.Paired,
                MaxHealth = 55f,
                RegenRate = 0f,
                DamageMultiplier = 1f
            };
            parts[6] = new BodyPartDefinition
            {
                PartId = new FixedString64Bytes("heart"),
                ParentId = new FixedString64Bytes("torso"),
                Kind = BodyPartKind.Organ,
                Flags = BodyPartFlags.Vital,
                MaxHealth = 20f,
                RegenRate = 0f,
                DamageMultiplier = 1.5f
            };
            parts[7] = new BodyPartDefinition
            {
                PartId = new FixedString64Bytes("brain"),
                ParentId = new FixedString64Bytes("head"),
                Kind = BodyPartKind.Organ,
                Flags = BodyPartFlags.Vital | BodyPartFlags.Sensor,
                MaxHealth = 18f,
                RegenRate = 0f,
                DamageMultiplier = 1.8f
            };

            return builder.CreateBlobAssetReference<AnatomyCatalogBlob>(Allocator.Persistent);
        }
    }
}
