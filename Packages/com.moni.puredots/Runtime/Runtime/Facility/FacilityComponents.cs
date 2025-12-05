using Unity.Collections;
using Unity.Entities;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Facility
{
    /// <summary>
    /// Facility archetype IDs for different facility types.
    /// </summary>
    public enum FacilityArchetypeId : int
    {
        None = 0,
        Lumberyard = 1,
        Smelter = 2,
        Granary = 3,
        Refinery = 4,
        Factory = 5
    }

    /// <summary>
    /// Component attached to facility entities that can craft/process resources.
    /// </summary>
    public struct Facility : IComponentData
    {
        public FacilityArchetypeId ArchetypeId;
        public int CurrentRecipeId;
        public float WorkProgress; // 0..1
    }

    /// <summary>
    /// Recipe definition for facility crafting.
    /// For demo, stored as static data. Can be moved to blob assets later.
    /// </summary>
    public struct FacilityRecipeDef
    {
        public int RecipeId;
        public FacilityArchetypeId FacilityArchetypeId;
        public FixedList32Bytes<int> InputResourceIds;
        public FixedList32Bytes<float> InputAmounts;
        public FixedList32Bytes<int> OutputResourceIds;
        public FixedList32Bytes<float> OutputAmounts;
        public float WorkRequired; // seconds
    }

    /// <summary>
    /// Static recipe catalog for demo facilities.
    /// </summary>
    public static class FacilityRecipes
    {
        private static FacilityRecipeDef[] _recipes;

        static FacilityRecipes()
        {
            _recipes = new FacilityRecipeDef[3];

            // Lumberyard: Wood -> Planks
            _recipes[0] = new FacilityRecipeDef
            {
                RecipeId = 0,
                FacilityArchetypeId = FacilityArchetypeId.Lumberyard,
                InputResourceIds = new FixedList32Bytes<int> { ResourceIds.Wood },
                InputAmounts = new FixedList32Bytes<float> { 10f },
                OutputResourceIds = new FixedList32Bytes<int> { ResourceIds.Planks },
                OutputAmounts = new FixedList32Bytes<float> { 5f },
                WorkRequired = 5f // 5 seconds
            };

            // Smelter: Ore -> Ingots
            _recipes[1] = new FacilityRecipeDef
            {
                RecipeId = 1,
                FacilityArchetypeId = FacilityArchetypeId.Smelter,
                InputResourceIds = new FixedList32Bytes<int> { ResourceIds.Ore },
                InputAmounts = new FixedList32Bytes<float> { 10f },
                OutputResourceIds = new FixedList32Bytes<int> { ResourceIds.Ingots },
                OutputAmounts = new FixedList32Bytes<float> { 5f },
                WorkRequired = 8f // 8 seconds
            };

            // Refinery: Ore -> RefinedOre (for Space4X)
            _recipes[2] = new FacilityRecipeDef
            {
                RecipeId = 2,
                FacilityArchetypeId = FacilityArchetypeId.Refinery,
                InputResourceIds = new FixedList32Bytes<int> { ResourceIds.Ore },
                InputAmounts = new FixedList32Bytes<float> { 10f },
                OutputResourceIds = new FixedList32Bytes<int> { ResourceIds.RefinedOre },
                OutputAmounts = new FixedList32Bytes<float> { 8f },
                WorkRequired = 6f // 6 seconds
            };
        }

        /// <summary>
        /// Gets a recipe by facility archetype ID.
        /// Returns the first recipe for that archetype (assumes 1 recipe per archetype for demo).
        /// </summary>
        public static bool TryGetRecipe(FacilityArchetypeId archetypeId, out FacilityRecipeDef recipe)
        {
            for (int i = 0; i < _recipes.Length; i++)
            {
                if (_recipes[i].FacilityArchetypeId == archetypeId)
                {
                    recipe = _recipes[i];
                    return true;
                }
            }

            recipe = default;
            return false;
        }

        /// <summary>
        /// Gets a recipe by recipe ID.
        /// </summary>
        public static bool TryGetRecipeById(int recipeId, out FacilityRecipeDef recipe)
        {
            for (int i = 0; i < _recipes.Length; i++)
            {
                if (_recipes[i].RecipeId == recipeId)
                {
                    recipe = _recipes[i];
                    return true;
                }
            }

            recipe = default;
            return false;
        }
    }
}

