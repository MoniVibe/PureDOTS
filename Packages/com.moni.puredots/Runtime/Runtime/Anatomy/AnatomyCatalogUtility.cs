using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Runtime.Anatomy
{
    public static class AnatomyCatalogUtility
    {
        public static bool TryGetDefinitionIndex(
            BlobAssetReference<AnatomyCatalogBlob> catalog,
            in FixedString64Bytes anatomyId,
            out int index)
        {
            index = -1;
            if (!catalog.IsCreated || anatomyId.Length == 0)
            {
                return false;
            }

            ref var definitions = ref catalog.Value.Definitions;
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i].AnatomyId.Equals(anatomyId))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }
    }
}
