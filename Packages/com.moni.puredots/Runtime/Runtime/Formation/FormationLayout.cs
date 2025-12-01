using Unity.Mathematics;
using Unity.Burst;

namespace PureDOTS.Runtime.Formation
{
    /// <summary>
    /// Static helpers for calculating formation layouts.
    /// </summary>
    [BurstCompile]
    public static class FormationLayout
    {
        /// <summary>
        /// Gets the local offset for a slot in a line formation.
        /// </summary>
        public static float3 GetLineOffset(int slotIndex, int totalSlots, float spacing)
        {
            float halfWidth = (totalSlots - 1) * spacing * 0.5f;
            return new float3(slotIndex * spacing - halfWidth, 0, 0);
        }

        /// <summary>
        /// Gets the local offset for a slot in a column formation.
        /// </summary>
        public static float3 GetColumnOffset(int slotIndex, int totalSlots, float spacing)
        {
            return new float3(0, 0, -slotIndex * spacing);
        }

        /// <summary>
        /// Gets the local offset for a slot in a wedge/V formation.
        /// </summary>
        public static float3 GetWedgeOffset(int slotIndex, int totalSlots, float spacing)
        {
            if (slotIndex == 0)
                return float3.zero; // Leader at point

            int row = (int)math.ceil(math.sqrt(slotIndex));
            int posInRow = slotIndex - (row * row - row) / 2;
            
            float x = (posInRow - row * 0.5f) * spacing;
            float z = -row * spacing;
            
            return new float3(x, 0, z);
        }

        /// <summary>
        /// Gets the local offset for a slot in a circle formation.
        /// </summary>
        public static float3 GetCircleOffset(int slotIndex, int totalSlots, float spacing)
        {
            if (totalSlots <= 1)
                return float3.zero;

            float radius = (totalSlots * spacing) / (2f * math.PI);
            float angle = (slotIndex / (float)totalSlots) * 2f * math.PI;
            
            return new float3(math.cos(angle) * radius, 0, math.sin(angle) * radius);
        }

        /// <summary>
        /// Gets the local offset for a slot in a square formation.
        /// </summary>
        public static float3 GetSquareOffset(int slotIndex, int totalSlots, float spacing)
        {
            int side = (int)math.ceil(math.sqrt(totalSlots));
            int row = slotIndex / side;
            int col = slotIndex % side;
            
            float halfSide = (side - 1) * spacing * 0.5f;
            
            return new float3(col * spacing - halfSide, 0, -row * spacing + halfSide);
        }

        /// <summary>
        /// Gets the local offset for a slot in an echelon formation.
        /// </summary>
        public static float3 GetEchelonOffset(int slotIndex, int totalSlots, float spacing, bool leftEchelon = true)
        {
            float direction = leftEchelon ? -1f : 1f;
            return new float3(slotIndex * spacing * direction, 0, -slotIndex * spacing);
        }

        /// <summary>
        /// Gets the local offset for a slot in a diamond formation.
        /// </summary>
        public static float3 GetDiamondOffset(int slotIndex, int totalSlots, float spacing)
        {
            if (slotIndex == 0)
                return new float3(0, 0, spacing); // Point

            int remaining = slotIndex - 1;
            int ring = 1;
            int ringStart = 1;
            
            while (remaining >= ring * 4)
            {
                remaining -= ring * 4;
                ringStart += ring * 4;
                ring++;
            }

            int side = remaining / ring;
            int posOnSide = remaining % ring;
            
            float3 offset = float3.zero;
            float ringSpacing = ring * spacing;
            
            switch (side)
            {
                case 0: // Right
                    offset = new float3(ringSpacing, 0, ringSpacing - posOnSide * spacing);
                    break;
                case 1: // Bottom
                    offset = new float3(ringSpacing - posOnSide * spacing, 0, -ringSpacing);
                    break;
                case 2: // Left
                    offset = new float3(-ringSpacing, 0, -ringSpacing + posOnSide * spacing);
                    break;
                case 3: // Top
                    offset = new float3(-ringSpacing + posOnSide * spacing, 0, ringSpacing);
                    break;
            }
            
            return offset;
        }

        /// <summary>
        /// Gets the slot offset for any formation type.
        /// </summary>
        public static float3 GetSlotOffset(FormationType type, int slotIndex, int totalSlots, float spacing)
        {
            return type switch
            {
                FormationType.Line => GetLineOffset(slotIndex, totalSlots, spacing),
                FormationType.Column => GetColumnOffset(slotIndex, totalSlots, spacing),
                FormationType.Wedge => GetWedgeOffset(slotIndex, totalSlots, spacing),
                FormationType.Circle => GetCircleOffset(slotIndex, totalSlots, spacing),
                FormationType.Square => GetSquareOffset(slotIndex, totalSlots, spacing),
                FormationType.Echelon => GetEchelonOffset(slotIndex, totalSlots, spacing),
                FormationType.Diamond => GetDiamondOffset(slotIndex, totalSlots, spacing),
                FormationType.Phalanx => GetSquareOffset(slotIndex, totalSlots, spacing * 0.7f), // Tighter
                FormationType.Skirmish => GetCircleOffset(slotIndex, totalSlots, spacing * 1.5f), // Looser
                FormationType.Defensive => GetCircleOffset(slotIndex, totalSlots, spacing * 0.8f),
                FormationType.Offensive => GetWedgeOffset(slotIndex, totalSlots, spacing),
                FormationType.Vanguard => GetWedgeOffset(slotIndex, totalSlots, spacing),
                FormationType.Rearguard => GetWedgeOffset(slotIndex, totalSlots, spacing), // Inverted
                FormationType.Screen => GetLineOffset(slotIndex, totalSlots, spacing * 2f), // Wide
                FormationType.Scatter => GetCircleOffset(slotIndex, totalSlots, spacing * 3f), // Very loose
                _ => GetSquareOffset(slotIndex, totalSlots, spacing)
            };
        }

        /// <summary>
        /// Gets the slot role for a position in a formation.
        /// </summary>
        public static FormationSlotRole GetSlotRole(FormationType type, int slotIndex, int totalSlots)
        {
            if (slotIndex == 0)
                return FormationSlotRole.Leader;

            return type switch
            {
                FormationType.Line => slotIndex < totalSlots / 2 
                    ? FormationSlotRole.Flank 
                    : FormationSlotRole.Flank,
                FormationType.Column => slotIndex < 3 
                    ? FormationSlotRole.Front 
                    : slotIndex >= totalSlots - 2 
                        ? FormationSlotRole.Rear 
                        : FormationSlotRole.Center,
                FormationType.Wedge => slotIndex < 3 
                    ? FormationSlotRole.Front 
                    : FormationSlotRole.Flank,
                FormationType.Circle => FormationSlotRole.Center,
                FormationType.Square => GetSquareRole(slotIndex, totalSlots),
                FormationType.Phalanx => slotIndex < totalSlots / 3 
                    ? FormationSlotRole.Front 
                    : FormationSlotRole.Support,
                FormationType.Skirmish => FormationSlotRole.Scout,
                FormationType.Defensive => slotIndex < totalSlots / 2 
                    ? FormationSlotRole.Front 
                    : FormationSlotRole.Support,
                FormationType.Offensive => slotIndex < 4 
                    ? FormationSlotRole.Front 
                    : FormationSlotRole.Support,
                _ => FormationSlotRole.Any
            };
        }

        private static FormationSlotRole GetSquareRole(int slotIndex, int totalSlots)
        {
            int side = (int)math.ceil(math.sqrt(totalSlots));
            int row = slotIndex / side;
            int col = slotIndex % side;
            
            if (row == 0)
                return FormationSlotRole.Front;
            if (row == side - 1)
                return FormationSlotRole.Rear;
            if (col == 0 || col == side - 1)
                return FormationSlotRole.Flank;
            return FormationSlotRole.Center;
        }

        /// <summary>
        /// Transforms a local offset to world position.
        /// </summary>
        public static float3 LocalToWorld(float3 localOffset, float3 anchorPosition, quaternion anchorRotation, float scale)
        {
            return anchorPosition + math.mul(anchorRotation, localOffset * scale);
        }

        /// <summary>
        /// Gets the recommended slot count for a formation type.
        /// </summary>
        public static int GetRecommendedSlotCount(FormationType type)
        {
            return type switch
            {
                FormationType.Line => 10,
                FormationType.Column => 10,
                FormationType.Wedge => 15,
                FormationType.Circle => 12,
                FormationType.Square => 16,
                FormationType.Phalanx => 25,
                FormationType.Skirmish => 8,
                FormationType.Diamond => 13,
                FormationType.Echelon => 10,
                FormationType.Vanguard => 7,
                FormationType.Rearguard => 7,
                FormationType.Screen => 5,
                FormationType.Scatter => 10,
                _ => 10
            };
        }

        /// <summary>
        /// Gets combat bonuses for a formation type.
        /// </summary>
        public static void GetFormationBonuses(
            FormationType type,
            out float attackBonus,
            out float defenseBonus,
            out float speedBonus)
        {
            attackBonus = 0f;
            defenseBonus = 0f;
            speedBonus = 0f;

            switch (type)
            {
                case FormationType.Line:
                    attackBonus = 0.1f;
                    break;
                case FormationType.Wedge:
                    attackBonus = 0.2f;
                    speedBonus = 0.1f;
                    break;
                case FormationType.Phalanx:
                    defenseBonus = 0.3f;
                    speedBonus = -0.2f;
                    break;
                case FormationType.Skirmish:
                    speedBonus = 0.2f;
                    defenseBonus = -0.1f;
                    break;
                case FormationType.Defensive:
                    defenseBonus = 0.25f;
                    attackBonus = -0.1f;
                    break;
                case FormationType.Offensive:
                    attackBonus = 0.25f;
                    defenseBonus = -0.1f;
                    break;
                case FormationType.Circle:
                    defenseBonus = 0.15f;
                    break;
                case FormationType.Scatter:
                    speedBonus = 0.15f;
                    defenseBonus = -0.2f;
                    break;
            }
        }
    }
}

