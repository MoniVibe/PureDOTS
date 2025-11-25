using PureDOTS.Runtime.Spells;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PureDOTS.Systems.Spells
{
    /// <summary>
    /// Processes hybridization requests to combine two spells at 400% mastery.
    /// Creates new hybrid spells that combine effects from both parents.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(SignatureUnlockSystem))]
    public partial struct HybridizationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var currentTick = timeState.Tick;

            // Get spell catalog
            if (!SystemAPI.TryGetSingleton<SpellCatalogRef>(out var spellCatalogRef) ||
                !spellCatalogRef.Blob.IsCreated)
            {
                return;
            }

            var spellCatalog = spellCatalogRef.Blob.Value;

            // Get signature catalog
            var signatureCatalogRef = SystemAPI.GetSingleton<SpellSignatureCatalogRef>();
            var signatureCatalog = signatureCatalogRef.Blob.IsCreated ? signatureCatalogRef.Blob.Value : default;

            var ecbSingleton = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ProcessHybridizationRequestsJob
            {
                SpellCatalog = spellCatalog,
                SignatureCatalog = signatureCatalog,
                CurrentTick = currentTick,
                Ecb = ecb
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ProcessHybridizationRequestsJob : IJobEntity
        {
            [ReadOnly]
            public SpellDefinitionBlob SpellCatalog;

            [ReadOnly]
            public SpellSignatureBlob SignatureCatalog;

            public uint CurrentTick;
            public EntityCommandBuffer.ParallelWriter Ecb;

            void Execute(
                Entity entity,
                [EntityIndexInQuery] int entityInQueryIndex,
                ref DynamicBuffer<HybridizationRequest> requests,
                ref DynamicBuffer<ExtendedSpellMastery> mastery,
                ref DynamicBuffer<SignatureSelection> selections,
                ref DynamicBuffer<HybridSpell> hybridSpells,
                ref DynamicBuffer<HybridSpellCreatedEvent> events)
            {
                for (int i = requests.Length - 1; i >= 0; i--)
                {
                    var request = requests[i];

                    // Validate both spells exist
                    SpellEntry spellA = default, spellB = default;
                    bool foundA = false, foundB = false;

                    for (int j = 0; j < SpellCatalog.Spells.Length; j++)
                    {
                        if (SpellCatalog.Spells[j].SpellId.Equals(request.SpellA))
                        {
                            spellA = SpellCatalog.Spells[j];
                            foundA = true;
                        }
                        if (SpellCatalog.Spells[j].SpellId.Equals(request.SpellB))
                        {
                            spellB = SpellCatalog.Spells[j];
                            foundB = true;
                        }
                    }

                    if (!foundA || !foundB)
                    {
                        requests.RemoveAt(i);
                        continue; // Invalid spells
                    }

                    // Validate both spells are at 400% mastery with hybridization signature
                    bool canHybridizeA = false, canHybridizeB = false;
                    bool hasHybridizationA = false, hasHybridizationB = false;

                    for (int j = 0; j < mastery.Length; j++)
                    {
                        if (mastery[j].SpellId.Equals(request.SpellA))
                        {
                            canHybridizeA = mastery[j].MasteryProgress >= 4.0f;
                            hasHybridizationA = (mastery[j].Signatures & SpellSignatureFlags.HybridizationUnlocked) != 0;
                        }
                        if (mastery[j].SpellId.Equals(request.SpellB))
                        {
                            canHybridizeB = mastery[j].MasteryProgress >= 4.0f;
                            hasHybridizationB = (mastery[j].Signatures & SpellSignatureFlags.HybridizationUnlocked) != 0;
                        }
                    }

                    if (!canHybridizeA || !canHybridizeB || !hasHybridizationA || !hasHybridizationB)
                    {
                        requests.RemoveAt(i);
                        continue; // Prerequisites not met
                    }

                    // Check if hybridization signature is selected
                    bool hasHybridizationSignatureA = false, hasHybridizationSignatureB = false;
                    for (int j = 0; j < selections.Length; j++)
                    {
                        if (selections[j].SpellId.Equals(request.SpellA) && selections[j].MilestoneIndex == 2)
                        {
                            // Check if this signature is hybridization type
                            for (int k = 0; k < SignatureCatalog.Signatures.Length; k++)
                            {
                                if (SignatureCatalog.Signatures[k].SignatureId.Equals(selections[j].SignatureId) &&
                                    SignatureCatalog.Signatures[k].Type == SignatureType.Hybridization)
                                {
                                    hasHybridizationSignatureA = true;
                                    break;
                                }
                            }
                        }
                        if (selections[j].SpellId.Equals(request.SpellB) && selections[j].MilestoneIndex == 2)
                        {
                            for (int k = 0; k < SignatureCatalog.Signatures.Length; k++)
                            {
                                if (SignatureCatalog.Signatures[k].SignatureId.Equals(selections[j].SignatureId) &&
                                    SignatureCatalog.Signatures[k].Type == SignatureType.Hybridization)
                                {
                                    hasHybridizationSignatureB = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!hasHybridizationSignatureA || !hasHybridizationSignatureB)
                    {
                        requests.RemoveAt(i);
                        continue; // Hybridization signature not selected
                    }

                    // Generate hybrid spell ID
                    var hybridId = new FixedString64Bytes($"{request.SpellA}_{request.SpellB}_{entity.Index}_{CurrentTick}");

                    // Determine derived school (combine or inherit)
                    SpellSchool derivedSchool = CombineSchools(spellA.School, spellB.School);

                    // Create hybrid spell entry
                    hybridSpells.Add(new HybridSpell
                    {
                        HybridSpellId = hybridId,
                        ParentSpellA = request.SpellA,
                        ParentSpellB = request.SpellB,
                        CreatorEntity = entity,
                        CreatedTick = CurrentTick,
                        DerivedSchool = derivedSchool,
                        DisplayName = new FixedString64Bytes($"{spellA.DisplayName}+{spellB.DisplayName}")
                    });

                    // Update mastery entries to mark hybridization
                    for (int j = 0; j < mastery.Length; j++)
                    {
                        if (mastery[j].SpellId.Equals(request.SpellA))
                        {
                            var entry = mastery[j];
                            entry.HybridWithSpellId = request.SpellB;
                            mastery[j] = entry;
                        }
                        if (mastery[j].SpellId.Equals(request.SpellB))
                        {
                            var entry = mastery[j];
                            entry.HybridWithSpellId = request.SpellA;
                            mastery[j] = entry;
                        }
                    }

                    // Emit event
                    events.Add(new HybridSpellCreatedEvent
                    {
                        HybridSpellId = hybridId,
                        ParentSpellA = request.SpellA,
                        ParentSpellB = request.SpellB,
                        CreatorEntity = entity,
                        CreatedTick = CurrentTick
                    });

                    requests.RemoveAt(i);
                }
            }

            [BurstCompile]
            private SpellSchool CombineSchools(SpellSchool schoolA, SpellSchool schoolB)
            {
                // Simple combination: if same school, return it; otherwise return Elemental as default hybrid
                if (schoolA == schoolB) return schoolA;
                return SpellSchool.Elemental; // Default hybrid school
            }
        }
    }
}

