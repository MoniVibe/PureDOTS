using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using PureDOTS.Runtime.IntergroupRelations;
using PureDOTS.Runtime.Components;

namespace PureDOTS.Runtime.Operations
{
    // Precomputed role names to avoid constructing FixedStrings inside Burst code.
    internal static class OperationRoleNames
    {
        public static readonly FixedString32Bytes Blockade = "BlockadePatrol";
        public static readonly FixedString32Bytes Siege = "SiegeRing";
        public static readonly FixedString32Bytes Occupy = "Occupier";
        public static readonly FixedString32Bytes Protest = "ProtestCrowd";
        public static readonly FixedString32Bytes Riot = "RiotCrowd";
        public static readonly FixedString32Bytes Cult = "Cultist";
        public static readonly FixedString32Bytes Funeral = "Mourner";
        public static readonly FixedString32Bytes Festival = "Attendee";
        public static readonly FixedString32Bytes Circus = "Performer";
        public static readonly FixedString32Bytes Deserter = "Deserter";
        public static readonly FixedString32Bytes Default = "Participant";
    }

    /// <summary>
    /// Creates operation entities and initializes state from planner decisions.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(OperationPlannerSystem))]
    public partial struct OperationInitSystem : ISystem
    {
        private ComponentLookup<OrgPersona> _personaLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<OperationRequest>();

            _personaLookup = state.GetComponentLookup<OrgPersona>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentTick = SystemAPI.GetSingleton<TimeState>().Tick;
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            _personaLookup.Update(ref state);

            // Process all operation requests
            foreach (var (request, requestEntity) in SystemAPI.Query<RefRO<OperationRequest>>()
                .WithEntityAccess())
            {
                var req = request.ValueRO;

                // Check if initiator org still exists and has persona
                if (!SystemAPI.Exists(req.InitiatorOrg) || !_personaLookup.HasComponent(req.InitiatorOrg))
                {
                    ecb.DestroyEntity(requestEntity);
                    continue;
                }

                // Check if initiator already has an active operation
                if (HasActiveOperation(state, req.InitiatorOrg))
                {
                    ecb.DestroyEntity(requestEntity);
                    continue;
                }

                var persona = _personaLookup[req.InitiatorOrg];

                // Create operation entity
                var operationEntity = ecb.CreateEntity();
                ecb.AddComponent(operationEntity, new OperationTag());

                // Initialize core operation
                var operation = new Operation
                {
                    Kind = req.Kind,
                    InitiatorOrg = req.InitiatorOrg,
                    TargetOrg = req.TargetOrg,
                    TargetLocation = req.TargetLocation,
                    State = OperationState.Active,
                    StartedTick = currentTick,
                    LastUpdateTick = currentTick
                };
                ecb.AddComponent(operationEntity, operation);

                // Initialize rules based on persona
                var rules = OperationHelpers.CreateDefaultRules(
                    persona.VengefulForgiving,
                    persona.CravenBold,
                    req.Kind);
                ecb.AddComponent(operationEntity, rules);

                // Initialize progress
                var progress = new OperationProgress
                {
                    ElapsedTicks = 0,
                    SuccessMetric = 0.5f, // Start neutral
                    Casualties = 0,
                    Unrest = 0f,
                    SiegeSupplyLevel = 1f, // Start at full supply
                    TargetSupplyLevel = 1f,
                    TargetMorale = 0.5f
                };
                ecb.AddComponent(operationEntity, progress);

                // Initialize kind-specific parameters
                InitializeKindSpecificParams(ecb, operationEntity, req.Kind, rules, persona);

                // Initialize participants buffer and add initiator as first participant
                var participants = ecb.AddBuffer<OperationParticipant>(operationEntity);
                participants.Add(new OperationParticipant
                {
                    ParticipantEntity = req.InitiatorOrg,
                    Role = GetDefaultRole(req.Kind),
                    Contribution = 1f
                });

                // Remove request entity
                ecb.DestroyEntity(requestEntity);
            }
        }

        [BurstCompile]
        private bool HasActiveOperation(SystemState state, Entity org)
        {
            foreach (var operation in SystemAPI.Query<RefRO<Operation>>()
                .WithAll<OperationTag>())
            {
                if (operation.ValueRO.InitiatorOrg == org && 
                    operation.ValueRO.State != OperationState.Ended)
                {
                    return true;
                }
            }
            return false;
        }

        [BurstCompile]
        private FixedString32Bytes GetDefaultRole(OperationKind kind)
        {
            switch (kind)
            {
                case OperationKind.Blockade: return OperationRoleNames.Blockade;
                case OperationKind.Siege: return OperationRoleNames.Siege;
                case OperationKind.Occupation: return OperationRoleNames.Occupy;
                case OperationKind.Protest: return OperationRoleNames.Protest;
                case OperationKind.Riot: return OperationRoleNames.Riot;
                case OperationKind.CultRitual: return OperationRoleNames.Cult;
                case OperationKind.Funeral: return OperationRoleNames.Funeral;
                case OperationKind.Festival: return OperationRoleNames.Festival;
                case OperationKind.Circus: return OperationRoleNames.Circus;
                case OperationKind.DeserterSettlement: return OperationRoleNames.Deserter;
                default: return OperationRoleNames.Default;
            }
        }

        [BurstCompile]
        private void InitializeKindSpecificParams(
            EntityCommandBuffer ecb,
            Entity operationEntity,
            OperationKind kind,
            OperationRules rules,
            OrgPersona persona)
        {
            switch (kind)
            {
                case OperationKind.Blockade:
                    var blockadeParams = OperationHelpers.CreateBlockadeParams(rules.Severity, rules.Stance);
                    ecb.AddComponent(operationEntity, blockadeParams);
                    break;

                case OperationKind.Siege:
                    var siegeParams = OperationHelpers.CreateSiegeParams(rules.Severity, rules.Stance);
                    ecb.AddComponent(operationEntity, siegeParams);
                    break;

                case OperationKind.Occupation:
                    var occupationParams = OperationHelpers.CreateOccupationParams(rules.Stance);
                    ecb.AddComponent(operationEntity, occupationParams);
                    break;

                case OperationKind.Protest:
                case OperationKind.Riot:
                    // Calculate grievance level (simplified)
                    float grievanceLevel = 0.5f + (persona.VengefulForgiving * 0.3f);
                    var protestParams = OperationHelpers.CreateProtestRiotParams(grievanceLevel);
                    ecb.AddComponent(operationEntity, protestParams);
                    break;

                case OperationKind.CultRitual:
                    // Default sacrifice count based on org size (simplified)
                    int sacrificeCount = 5; // Default
                    var ritualParams = OperationHelpers.CreateCultRitualParams(sacrificeCount);
                    ecb.AddComponent(operationEntity, ritualParams);
                    break;

                case OperationKind.Festival:
                case OperationKind.Circus:
                    byte festivalType = kind == OperationKind.Circus ? (byte)0 : (byte)1;
                    uint durationTicks = 216000; // 1 hour default
                    var festivalParams = OperationHelpers.CreateFestivalParams(festivalType, durationTicks);
                    ecb.AddComponent(operationEntity, festivalParams);
                    break;
            }
        }
    }
}

