using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Resource;
using Unity.Collections;
using Unity.Entities;

namespace Godgame.Interaction
{
    internal static class ResourceTypeCatalogUtility
    {
        public static bool TryGetResourceTypeIndex(ResourceType type, BlobAssetReference<ResourceTypeIndexBlob> catalog, out ushort index)
        {
            index = 0;
            if (!catalog.IsCreated)
            {
                return false;
            }

            ref var blob = ref catalog.Value;
            for (ushort i = 0; i < blob.Ids.Length; i++)
            {
                var id = blob.Ids[i];
                if (Matches(type, id))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        public static bool TryResolveResourceType(ushort resourceTypeIndex, BlobAssetReference<ResourceTypeIndexBlob> catalog, out ResourceType type)
        {
            type = ResourceType.None;
            if (!catalog.IsCreated)
            {
                return false;
            }

            ref var blob = ref catalog.Value;
            if (resourceTypeIndex >= blob.Ids.Length)
            {
                return false;
            }

            var id = blob.Ids[resourceTypeIndex];
            if (Matches(ResourceType.Wood, id))
            {
                type = ResourceType.Wood;
                return true;
            }

            if (Matches(ResourceType.Ore, id))
            {
                type = ResourceType.Ore;
                return true;
            }

            if (Matches(ResourceType.Food, id))
            {
                type = ResourceType.Food;
                return true;
            }

            if (Matches(ResourceType.Worship, id))
            {
                type = ResourceType.Worship;
                return true;
            }

            return false;
        }

        private static bool Matches(ResourceType type, in FixedString64Bytes id)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    return MatchesLiteral(id, "wood");
                case ResourceType.Ore:
                    return MatchesLiteral(id, "ore");
                case ResourceType.Food:
                    return MatchesLiteral(id, "food");
                case ResourceType.Worship:
                    return MatchesLiteral(id, "worship");
                default:
                    return false;
            }
        }

        private static bool MatchesLiteral(in FixedString64Bytes value, string literal)
        {
            if (value.Length != literal.Length)
            {
                return false;
            }

            for (var i = 0; i < literal.Length; i++)
            {
                if (value[i] != literal[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
