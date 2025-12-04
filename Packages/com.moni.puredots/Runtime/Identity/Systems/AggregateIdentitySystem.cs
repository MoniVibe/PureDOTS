using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Runtime.Identity
{
    /// <summary>
    /// Computes aggregate identity components (Alignment, Outlook, Persona, Power Profile) for groups
    /// from their member entities. Updates when members change.
    /// </summary>
    [BurstCompile]
    public partial struct AggregateIdentitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AggregateMember>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Process all entities with AggregateMember buffers
            foreach (var (entity, members) in SystemAPI.Query<RefRO<AggregateMember>>()
                .WithEntityAccess())
            {
                if (members.ValueRO.Length == 0)
                    continue;

                // Compute aggregate alignment
                ComputeAggregateAlignment(ref state, entity, members.ValueRO);

                // Compute aggregate outlook
                ComputeAggregateOutlook(ref state, entity, members.ValueRO);

                // Compute group persona
                ComputeGroupPersona(ref state, entity, members.ValueRO);

                // Compute aggregate power profile
                ComputeAggregatePowerProfile(ref state, entity, members.ValueRO);
            }
        }

        [BurstCompile]
        private static void ComputeAggregateAlignment(ref SystemState state, Entity groupEntity, DynamicBuffer<AggregateMember> members)
        {
            float totalMoral = 0f;
            float totalOrder = 0f;
            float totalPurity = 0f;
            float totalWeight = 0f;
            float minMoral = float.MaxValue, maxMoral = float.MinValue;
            float minOrder = float.MaxValue, maxOrder = float.MinValue;
            float minPurity = float.MaxValue, maxPurity = float.MinValue;

            foreach (var member in members)
            {
                if (!state.EntityManager.HasComponent<EntityAlignment>(member.MemberEntity))
                    continue;

                var alignment = state.EntityManager.GetComponentData<EntityAlignment>(member.MemberEntity);
                float weight = member.InfluenceWeight > 0f ? member.InfluenceWeight : 1f;

                totalMoral += alignment.Moral * weight;
                totalOrder += alignment.Order * weight;
                totalPurity += alignment.Purity * weight;
                totalWeight += weight;

                minMoral = math.min(minMoral, alignment.Moral);
                maxMoral = math.max(maxMoral, alignment.Moral);
                minOrder = math.min(minOrder, alignment.Order);
                maxOrder = math.max(maxOrder, alignment.Order);
                minPurity = math.min(minPurity, alignment.Purity);
                maxPurity = math.max(maxPurity, alignment.Purity);
            }

            if (totalWeight == 0f)
                return;

            var aggregateAlignment = new AggregateAlignment
            {
                Moral = totalMoral / totalWeight,
                Order = totalOrder / totalWeight,
                Purity = totalPurity / totalWeight,
                // Cohesion: inverse of spread (higher spread = lower cohesion)
                Cohesion = 1f - math.max(
                    (maxMoral - minMoral) / 200f,
                    (maxOrder - minOrder) / 200f,
                    (maxPurity - minPurity) / 200f
                ),
                DriftRate = 0.5f // Default, can be adjusted per game
            };

            state.EntityManager.SetComponentData(groupEntity, aggregateAlignment);
        }

        [BurstCompile]
        private static void ComputeAggregateOutlook(ref SystemState state, Entity groupEntity, DynamicBuffer<AggregateMember> members)
        {
            var outlookCounts = new NativeHashMap<OutlookType, int>(16, Allocator.Temp);

            foreach (var member in members)
            {
                if (!state.EntityManager.HasComponent<EntityOutlook>(member.MemberEntity))
                    continue;

                var outlook = state.EntityManager.GetComponentData<EntityOutlook>(member.MemberEntity);
                
                // Count primary (weight 3), secondary (weight 2), tertiary (weight 1)
                if (outlook.Primary != OutlookType.None)
                    outlookCounts.TryAdd(outlook.Primary, 0);
                outlookCounts[outlook.Primary] = outlookCounts[outlook.Primary] + 3;

                if (outlook.Secondary != OutlookType.None)
                    outlookCounts.TryAdd(outlook.Secondary, 0);
                outlookCounts[outlook.Secondary] = outlookCounts[outlook.Secondary] + 2;

                if (outlook.Tertiary != OutlookType.None)
                    outlookCounts.TryAdd(outlook.Tertiary, 0);
                outlookCounts[outlook.Tertiary] = outlookCounts[outlook.Tertiary] + 1;
            }

            // Find top 3 outlooks by count
            OutlookType primary = OutlookType.None, secondary = OutlookType.None, tertiary = OutlookType.None;
            int primaryCount = 0, secondaryCount = 0, tertiaryCount = 0;

            foreach (var kvp in outlookCounts)
            {
                if (kvp.Value > primaryCount)
                {
                    tertiary = secondary;
                    tertiaryCount = secondaryCount;
                    secondary = primary;
                    secondaryCount = primaryCount;
                    primary = kvp.Key;
                    primaryCount = kvp.Value;
                }
                else if (kvp.Value > secondaryCount)
                {
                    tertiary = secondary;
                    tertiaryCount = secondaryCount;
                    secondary = kvp.Key;
                    secondaryCount = kvp.Value;
                }
                else if (kvp.Value > tertiaryCount)
                {
                    tertiary = kvp.Key;
                    tertiaryCount = kvp.Value;
                }
            }

            // Calculate uniformity: how many members share the primary outlook
            int membersWithPrimary = 0;
            int totalMembers = 0;
            foreach (var member in members)
            {
                if (!state.EntityManager.HasComponent<EntityOutlook>(member.MemberEntity))
                    continue;

                totalMembers++;
                var outlook = state.EntityManager.GetComponentData<EntityOutlook>(member.MemberEntity);
                if (outlook.Primary == primary || outlook.Secondary == primary || outlook.Tertiary == primary)
                    membersWithPrimary++;
            }

            var aggregateOutlook = new AggregateOutlook
            {
                DominantPrimary = primary,
                DominantSecondary = secondary,
                DominantTertiary = tertiary,
                CulturalUniformity = totalMembers > 0 ? (float)membersWithPrimary / totalMembers : 0f
            };

            state.EntityManager.SetComponentData(groupEntity, aggregateOutlook);
            outlookCounts.Dispose();
        }

        [BurstCompile]
        private static void ComputeGroupPersona(ref SystemState state, Entity groupEntity, DynamicBuffer<AggregateMember> members)
        {
            float totalVengeful = 0f;
            float totalBold = 0f;
            float totalWeight = 0f;
            float minVengeful = float.MaxValue, maxVengeful = float.MinValue;
            float minBold = float.MaxValue, maxBold = float.MinValue;

            foreach (var member in members)
            {
                if (!state.EntityManager.HasComponent<PersonalityAxes>(member.MemberEntity))
                    continue;

                var personality = state.EntityManager.GetComponentData<PersonalityAxes>(member.MemberEntity);
                float weight = member.InfluenceWeight > 0f ? member.InfluenceWeight : 1f;

                totalVengeful += personality.VengefulForgiving * weight;
                totalBold += personality.CravenBold * weight;
                totalWeight += weight;

                minVengeful = math.min(minVengeful, personality.VengefulForgiving);
                maxVengeful = math.max(maxVengeful, personality.VengefulForgiving);
                minBold = math.min(minBold, personality.CravenBold);
                maxBold = math.max(maxBold, personality.CravenBold);
            }

            if (totalWeight == 0f)
                return;

            var groupPersona = new GroupPersona
            {
                AvgVengefulForgiving = totalVengeful / totalWeight,
                AvgCravenBold = totalBold / totalWeight,
                // Cohesion: inverse of spread
                Cohesion = 1f - math.max(
                    (maxVengeful - minVengeful) / 200f,
                    (maxBold - minBold) / 200f
                )
            };

            state.EntityManager.SetComponentData(groupEntity, groupPersona);
        }

        [BurstCompile]
        private static void ComputeAggregatePowerProfile(ref SystemState state, Entity groupEntity, DynamicBuffer<AggregateMember> members)
        {
            float totalAxis = 0f;
            float totalWeight = 0f;
            int militaryCount = 0;
            int totalCount = 0;

            foreach (var member in members)
            {
                totalCount++;
                
                if (!state.EntityManager.HasComponent<MightMagicAffinity>(member.MemberEntity))
                    continue;

                var affinity = state.EntityManager.GetComponentData<MightMagicAffinity>(member.MemberEntity);
                float weight = member.InfluenceWeight > 0f ? member.InfluenceWeight : 1f;

                totalAxis += affinity.Axis * weight;
                totalWeight += weight;

                // Simple heuristic: if entity has combat components, count as military
                // This is a placeholder - games should override with their own logic
                if (state.EntityManager.HasComponent<Unity.Transforms.LocalTransform>(member.MemberEntity))
                    militaryCount++;
            }

            if (totalWeight == 0f)
                return;

            var powerProfile = new AggregatePowerProfile
            {
                AvgMightMagicAxis = totalAxis / totalWeight,
                MilitaryWeight = totalCount > 0 ? (float)militaryCount / totalCount : 0f,
                MagitechBlend = 0.5f // Default, games can compute based on tech/magic assets
            };

            state.EntityManager.SetComponentData(groupEntity, powerProfile);
        }
    }
}

