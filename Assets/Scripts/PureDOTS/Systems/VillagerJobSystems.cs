using PureDOTS.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(CoreSingletonBootstrapSystem))]
    public partial struct VillagerJobBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var entityManager = state.EntityManager;

            EnsureEventStream(entityManager);
            EnsureRequestQueue(entityManager);
            EnsureDeliveryQueue(entityManager);

            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        private static void EnsureEventStream(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerJobEventStream>());
            Entity eventEntity;
            if (query.IsEmptyIgnoreFilter)
            {
                eventEntity = entityManager.CreateEntity(typeof(VillagerJobEventStream), typeof(VillagerJobTicketSequence));
                entityManager.AddBuffer<VillagerJobEvent>(eventEntity);
                entityManager.SetComponentData(eventEntity, new VillagerJobTicketSequence { Value = 0 });
            }
            else
            {
                eventEntity = query.GetSingletonEntity();
                if (!entityManager.HasComponent<VillagerJobTicketSequence>(eventEntity))
                {
                    entityManager.AddComponentData(eventEntity, new VillagerJobTicketSequence { Value = 0 });
                }
                if (!entityManager.HasBuffer<VillagerJobEvent>(eventEntity))
                {
                    entityManager.AddBuffer<VillagerJobEvent>(eventEntity);
                }
            }
        }

        private static void EnsureRequestQueue(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerJobRequestQueue>());
            if (query.IsEmptyIgnoreFilter)
            {
                var entity = entityManager.CreateEntity(typeof(VillagerJobRequestQueue));
                entityManager.AddBuffer<VillagerJobRequest>(entity);
            }
            else
            {
                var entity = query.GetSingletonEntity();
                if (!entityManager.HasBuffer<VillagerJobRequest>(entity))
                {
                    entityManager.AddBuffer<VillagerJobRequest>(entity);
                }
            }
        }

        private static void EnsureDeliveryQueue(EntityManager entityManager)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerJobDeliveryQueue>());
            if (query.IsEmptyIgnoreFilter)
            {
                var entity = entityManager.CreateEntity(typeof(VillagerJobDeliveryQueue));
                entityManager.AddBuffer<VillagerJobDeliveryCommand>(entity);
            }
            else
            {
                var entity = query.GetSingletonEntity();
                if (!entityManager.HasBuffer<VillagerJobDeliveryCommand>(entity))
                {
                    entityManager.AddBuffer<VillagerJobDeliveryCommand>(entity);
                }
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(VillagerJobFixedStepGroup), OrderFirst = true)]
    public partial struct VillagerJobInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VillagerJob>();
            state.RequireForUpdate<VillagerJobRequestQueue>();
            state.RequireForUpdate<VillagerJobDeliveryQueue>();
            state.RequireForUpdate<VillagerJobEventStream>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var requestEntity = SystemAPI.GetSingletonEntity<VillagerJobRequestQueue>();
            var requestBuffer = state.EntityManager.GetBuffer<VillagerJobRequest>(requestEntity);
            requestBuffer.Clear();

            var deliveryEntity = SystemAPI.GetSingletonEntity<VillagerJobDeliveryQueue>();
            var deliveryBuffer = state.EntityManager.GetBuffer<VillagerJobDeliveryCommand>(deliveryEntity);
            deliveryBuffer.Clear();

            var timeState = SystemAPI.GetSingleton<TimeState>();
            var ecbSingleton = SystemAPI.GetSingletonRW<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (job, entity) in SystemAPI.Query<RefRW<VillagerJob>>()
                         .WithNone<VillagerJobTicket>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new VillagerJobTicket
                {
                    TicketId = 0,
                    JobType = job.ValueRO.Type,
                    ResourceTypeIndex = ushort.MaxValue,
                    ResourceEntity = Entity.Null,
                    StorehouseEntity = Entity.Null,
                    Priority = 0,
                    Phase = (byte)VillagerJob.JobPhase.Idle,
                    ReservedUnits = 0f,
                    AssignedTick = 0,
                    LastProgressTick = 0
                });
            }

            foreach (var (job, entity) in SystemAPI.Query<RefRW<VillagerJob>>()
                         .WithNone<VillagerJobProgress>()
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new VillagerJobProgress
                {
                    Gathered = 0f,
                    Delivered = 0f,
                    TimeInPhase = 0f,
                    LastUpdateTick = timeState.Tick
                });
            }

            foreach (var (_, entity) in SystemAPI.Query<RefRW<VillagerJob>>()
                         .WithNone<VillagerJobCarryItem>()
                         .WithEntityAccess())
            {
                ecb.AddBuffer<VillagerJobCarryItem>(entity);
            }

            foreach (var (_, entity) in SystemAPI.Query<RefRW<VillagerJob>>()
                         .WithNone<VillagerJobHistorySample>()
                         .WithEntityAccess())
            {
                ecb.AddBuffer<VillagerJobHistorySample>(entity);
            }

            foreach (var job in SystemAPI.Query<RefRW<VillagerJob>>())
            {
                if (job.ValueRO.Type == VillagerJob.JobType.None)
                {
                    job.ValueRW.Phase = VillagerJob.JobPhase.Idle;
                    job.ValueRW.ActiveTicketId = 0;
                    continue;
                }

                if (job.ValueRO.Phase == 0)
                {
                    job.ValueRW.Phase = VillagerJob.JobPhase.Idle;
                    job.ValueRW.LastStateChangeTick = timeState.Tick;
                }
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(VillagerJobFixedStepGroup))]
    [UpdateAfter(typeof(VillagerJobInitializationSystem))]
    public partial struct VillagerJobRequestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VillagerJob>();
            state.RequireForUpdate<VillagerJobTicket>();
            state.RequireForUpdate<VillagerAvailability>();
            state.RequireForUpdate<VillagerJobRequestQueue>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var requestEntity = SystemAPI.GetSingletonEntity<VillagerJobRequestQueue>();
            var requests = state.EntityManager.GetBuffer<VillagerJobRequest>(requestEntity);

            foreach (var (job, ticket, availability, entity) in SystemAPI.Query<RefRW<VillagerJob>, RefRW<VillagerJobTicket>, RefRO<VillagerAvailability>>()
                         .WithEntityAccess())
            {
                if (job.ValueRO.Type == VillagerJob.JobType.None)
                {
                    continue;
                }

                switch (job.ValueRO.Phase)
                {
                    case VillagerJob.JobPhase.Idle:
                    case VillagerJob.JobPhase.Completed:
                    case VillagerJob.JobPhase.Interrupted:
                        break;
                    default:
                        continue;
                }

                if (availability.ValueRO.IsAvailable == 0)
                {
                    continue;
                }

                ticket.ValueRW.JobType = job.ValueRO.Type;
                ticket.ValueRW.Priority = (byte)math.select((int)ticket.ValueRO.Priority, 1, availability.ValueRO.IsReserved != 0);
                ticket.ValueRW.Phase = (byte)VillagerJob.JobPhase.Idle;
                ticket.ValueRW.ResourceEntity = Entity.Null;
                ticket.ValueRW.StorehouseEntity = Entity.Null;
                ticket.ValueRW.ResourceTypeIndex = ushort.MaxValue;
                ticket.ValueRW.ReservedUnits = 0f;
                ticket.ValueRW.LastProgressTick = timeState.Tick;

                job.ValueRW.Phase = VillagerJob.JobPhase.Idle;
                job.ValueRW.ActiveTicketId = 0;
                job.ValueRW.LastStateChangeTick = timeState.Tick;

                requests.Add(new VillagerJobRequest
                {
                    Villager = entity,
                    JobType = job.ValueRO.Type,
                    Priority = ticket.ValueRO.Priority
                });
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(VillagerJobFixedStepGroup))]
    [UpdateAfter(typeof(VillagerJobRequestSystem))]
    public partial struct VillagerJobAssignmentSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<ResourceJobReservation> _resourceReservationLookup;
        private BufferLookup<ResourceActiveTicket> _resourceActiveTicketLookup;
        private ComponentLookup<StorehouseJobReservation> _storehouseReservationLookup;
        private BufferLookup<StorehouseReservationItem> _storehouseReservationItems;
        private ComponentLookup<ResourceSourceConfig> _resourceConfigLookup;
        
        
        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _resourceReservationLookup = state.GetComponentLookup<ResourceJobReservation>(false);
            _resourceActiveTicketLookup = state.GetBufferLookup<ResourceActiveTicket>(false);
            _storehouseReservationLookup = state.GetComponentLookup<StorehouseJobReservation>(false);
            _storehouseReservationItems = state.GetBufferLookup<StorehouseReservationItem>(false);
            _resourceConfigLookup = state.GetComponentLookup<ResourceSourceConfig>(true);
            
            state.RequireForUpdate<VillagerJobRequestQueue>();
            state.RequireForUpdate<ResourceRegistry>();
            state.RequireForUpdate<ResourceTypeIndex>();
            state.RequireForUpdate<VillagerJobEventStream>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _transformLookup.Update(ref state);
            _resourceReservationLookup.Update(ref state);
            _resourceActiveTicketLookup.Update(ref state);
            _storehouseReservationLookup.Update(ref state);
            _storehouseReservationItems.Update(ref state);
            _resourceConfigLookup.Update(ref state);
            
            var requestEntity = SystemAPI.GetSingletonEntity<VillagerJobRequestQueue>();
            var requests = state.EntityManager.GetBuffer<VillagerJobRequest>(requestEntity);
            if (requests.Length == 0)
            {
                return;
            }

            var registryEntity = SystemAPI.GetSingletonEntity<ResourceRegistry>();
            var resourceEntries = state.EntityManager.GetBuffer<ResourceRegistryEntry>(registryEntity);
            if (resourceEntries.Length == 0)
            {
                requests.Clear();
                return;
            }

            var eventEntity = SystemAPI.GetSingletonEntity<VillagerJobEventStream>();
            var events = state.EntityManager.GetBuffer<VillagerJobEvent>(eventEntity);
            var ticketSequence = SystemAPI.GetComponentRW<VillagerJobTicketSequence>(eventEntity);

            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                if (!_transformLookup.HasComponent(request.Villager))
                {
                    continue;
                }

                var villagerPos = _transformLookup[request.Villager].Position;
                var bestIndex = -1;
                var bestScore = float.MaxValue;
                var bestReservation = default(ResourceJobReservation);
                var targetConfig = default(ResourceSourceConfig);

                for (int r = 0; r < resourceEntries.Length; r++)
                {
                    var entry = resourceEntries[r];
                    if (entry.UnitsRemaining <= 0f)
                    {
                        continue;
                    }

                    if (!_transformLookup.HasComponent(entry.SourceEntity))
                    {
                        continue;
                    }

                    var reservation = _resourceReservationLookup.HasComponent(entry.SourceEntity)
                        ? _resourceReservationLookup[entry.SourceEntity]
                        : new ResourceJobReservation();

                    var config = _resourceConfigLookup.HasComponent(entry.SourceEntity)
                        ? _resourceConfigLookup[entry.SourceEntity]
                        : new ResourceSourceConfig { GatherRatePerWorker = 10f, MaxSimultaneousWorkers = 1 };

                    if (config.MaxSimultaneousWorkers <= 0)
                    {
                        continue;
                    }

                    if (reservation.ActiveTickets >= config.MaxSimultaneousWorkers)
                    {
                        continue;
                    }

                    if ((reservation.ClaimFlags & ResourceRegistryClaimFlags.PlayerClaim) != 0)
                    {
                        continue;
                    }

                    var availableUnits = entry.UnitsRemaining - reservation.ReservedUnits;
                    if (availableUnits <= 0f)
                    {
                        continue;
                    }

                    var distSq = math.distancesq(villagerPos, entry.Position);
                    var score = distSq + (reservation.ActiveTickets * 5f);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = r;
                        bestReservation = reservation;
                        targetConfig = config;
                    }
                }

                if (bestIndex < 0)
                {
                    continue;
                }

                var resourceEntry = resourceEntries[bestIndex];
                var villagerJob = SystemAPI.GetComponentRW<VillagerJob>(request.Villager);
                var villagerTicket = SystemAPI.GetComponentRW<VillagerJobTicket>(request.Villager);
                var villagerProgress = SystemAPI.GetComponentRW<VillagerJobProgress>(request.Villager);

                var newTicketId = ticketSequence.ValueRW.Value + 1u;
                if (newTicketId == 0u)
                {
                    newTicketId = 1u;
                }
                ticketSequence.ValueRW.Value = newTicketId;

                var reservedUnits = math.min(resourceEntry.UnitsRemaining, targetConfig.GatherRatePerWorker * timeState.FixedDeltaTime * 5f);
                bestReservation.ActiveTickets = (byte)math.min(255, bestReservation.ActiveTickets + 1);
                bestReservation.ReservedUnits += reservedUnits;
                bestReservation.LastMutationTick = timeState.Tick;
                bestReservation.ClaimFlags |= ResourceRegistryClaimFlags.VillagerReserved;

                _resourceReservationLookup[resourceEntry.SourceEntity] = bestReservation;

                if (_resourceActiveTicketLookup.HasBuffer(resourceEntry.SourceEntity))
                {
                    var activeTickets = _resourceActiveTicketLookup[resourceEntry.SourceEntity];
                    activeTickets.Add(new ResourceActiveTicket
                    {
                        Villager = request.Villager,
                        TicketId = newTicketId,
                        ReservedUnits = reservedUnits
                    });
                }

                villagerJob.ValueRW.Phase = VillagerJob.JobPhase.Assigned;
                villagerJob.ValueRW.ActiveTicketId = newTicketId;
                villagerJob.ValueRW.LastStateChangeTick = timeState.Tick;

                villagerTicket.ValueRW.TicketId = newTicketId;
                villagerTicket.ValueRW.JobType = request.JobType;
                villagerTicket.ValueRW.ResourceTypeIndex = resourceEntry.ResourceTypeIndex;
                villagerTicket.ValueRW.ResourceEntity = resourceEntry.SourceEntity;
                villagerTicket.ValueRW.StorehouseEntity = Entity.Null;
                villagerTicket.ValueRW.Priority = request.Priority;
                villagerTicket.ValueRW.Phase = (byte)VillagerJob.JobPhase.Assigned;
                villagerTicket.ValueRW.ReservedUnits = reservedUnits;
                villagerTicket.ValueRW.AssignedTick = timeState.Tick;
                villagerTicket.ValueRW.LastProgressTick = timeState.Tick;

                villagerProgress.ValueRW.TimeInPhase = 0f;

                events.Add(new VillagerJobEvent
                {
                    Tick = timeState.Tick,
                    Villager = request.Villager,
                    EventType = VillagerJobEventType.JobAssigned,
                    ResourceTypeIndex = resourceEntry.ResourceTypeIndex,
                    Amount = 0f,
                    TicketId = newTicketId
                });
            }

            requests.Clear();
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(VillagerJobFixedStepGroup))]
    [UpdateAfter(typeof(VillagerJobAssignmentSystem))]
    public partial struct VillagerJobExecutionSystem : ISystem
    {
        private ComponentLookup<ResourceSourceState> _resourceStateLookup;
        private ComponentLookup<ResourceSourceConfig> _resourceConfigLookup;
        private ComponentLookup<ResourceJobReservation> _resourceReservationLookup;
        private BufferLookup<ResourceActiveTicket> _resourceActiveTicketLookup;
        private ComponentLookup<LocalTransform> _transformLookup;

        public void OnCreate(ref SystemState state)
        {
            _resourceStateLookup = state.GetComponentLookup<ResourceSourceState>(false);
            _resourceConfigLookup = state.GetComponentLookup<ResourceSourceConfig>(true);
            _resourceReservationLookup = state.GetComponentLookup<ResourceJobReservation>(false);
            _resourceActiveTicketLookup = state.GetBufferLookup<ResourceActiveTicket>(false);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<VillagerJobTicket>();
            state.RequireForUpdate<VillagerJobEventStream>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _resourceStateLookup.Update(ref state);
            _resourceConfigLookup.Update(ref state);
            _resourceReservationLookup.Update(ref state);
            _resourceActiveTicketLookup.Update(ref state);
            _transformLookup.Update(ref state);

            var gatherDistanceSq = 9f;
            var deltaTime = timeState.FixedDeltaTime;
            var eventEntity = SystemAPI.GetSingletonEntity<VillagerJobEventStream>();
            var events = state.EntityManager.GetBuffer<VillagerJobEvent>(eventEntity);

            foreach (var (job, ticket, progress, needs, transform, carry, entity) in SystemAPI.Query<RefRW<VillagerJob>, RefRW<VillagerJobTicket>, RefRW<VillagerJobProgress>, RefRO<VillagerNeeds>, RefRO<LocalTransform>, DynamicBuffer<VillagerJobCarryItem>>()
                         .WithEntityAccess())
            {
                if (job.ValueRO.Type == VillagerJob.JobType.None)
                {
                    continue;
                }

                if (ticket.ValueRO.ResourceEntity == Entity.Null)
                {
                    continue;
                }

                if (!_resourceStateLookup.HasComponent(ticket.ValueRO.ResourceEntity) ||
                    !_transformLookup.HasComponent(ticket.ValueRO.ResourceEntity))
                {
                    continue;
                }

                var resourceState = _resourceStateLookup[ticket.ValueRO.ResourceEntity];
                var resourceTransform = _transformLookup[ticket.ValueRO.ResourceEntity];
                var distSq = math.distancesq(transform.ValueRO.Position, resourceTransform.Position);

                if (job.ValueRO.Phase == VillagerJob.JobPhase.Assigned)
                {
                    job.ValueRW.Phase = VillagerJob.JobPhase.Gathering;
                    job.ValueRW.LastStateChangeTick = timeState.Tick;
                    ticket.ValueRW.Phase = (byte)VillagerJob.JobPhase.Gathering;
                    ticket.ValueRW.LastProgressTick = timeState.Tick;
                }

                if (job.ValueRO.Phase != VillagerJob.JobPhase.Gathering)
                {
                    continue;
                }

                if (distSq > gatherDistanceSq)
                {
                    progress.ValueRW.TimeInPhase += deltaTime;
                    continue;
                }

                var config = _resourceConfigLookup.HasComponent(ticket.ValueRO.ResourceEntity)
                    ? _resourceConfigLookup[ticket.ValueRO.ResourceEntity]
                    : new ResourceSourceConfig { GatherRatePerWorker = 10f };

                var gatherRate = math.max(0.1f, config.GatherRatePerWorker);
                var gatherAmount = gatherRate * job.ValueRO.Productivity * deltaTime;
                // Extension point: job-type specific modifiers can adjust gatherAmount here.
                var energyMultiplier = math.saturate(needs.ValueRO.Energy / 50f);
                gatherAmount *= energyMultiplier;
                gatherAmount = math.min(gatherAmount, resourceState.UnitsRemaining);

                if (gatherAmount <= 0f)
                {
                    progress.ValueRW.TimeInPhase += deltaTime;
                    continue;
                }

                resourceState.UnitsRemaining -= gatherAmount;
                _resourceStateLookup[ticket.ValueRO.ResourceEntity] = resourceState;

                if (_resourceReservationLookup.HasComponent(ticket.ValueRO.ResourceEntity))
                {
                    var reservation = _resourceReservationLookup[ticket.ValueRO.ResourceEntity];
                    reservation.ReservedUnits = math.max(0f, reservation.ReservedUnits - gatherAmount);
                    reservation.LastMutationTick = timeState.Tick;
                    _resourceReservationLookup[ticket.ValueRO.ResourceEntity] = reservation;
                }

                var ticketReserved = ticket.ValueRO.ReservedUnits;
                ticket.ValueRW.ReservedUnits = math.max(0f, ticketReserved - gatherAmount);

                var carryIndex = -1;
                var mutableCarry = carry;
                for (int i = 0; i < mutableCarry.Length; i++)
                {
                    if (mutableCarry[i].ResourceTypeIndex == ticket.ValueRO.ResourceTypeIndex)
                    {
                        carryIndex = i;
                        break;
                    }
                }

                if (carryIndex >= 0)
                {
                    var tmp = mutableCarry[carryIndex];
                    tmp.Amount = tmp.Amount + gatherAmount;
                    mutableCarry[carryIndex] = tmp;
                }
                else
                {
                    mutableCarry.Add(new VillagerJobCarryItem
                    {
                        ResourceTypeIndex = ticket.ValueRO.ResourceTypeIndex,
                        Amount = gatherAmount
                    });
                }

                progress.ValueRW.Gathered += gatherAmount;
                progress.ValueRW.TimeInPhase += deltaTime;
                progress.ValueRW.LastUpdateTick = timeState.Tick;
                ticket.ValueRW.LastProgressTick = timeState.Tick;

                events.Add(new VillagerJobEvent
                {
                    Tick = timeState.Tick,
                    Villager = entity,
                    EventType = VillagerJobEventType.JobProgress,
                    ResourceTypeIndex = ticket.ValueRO.ResourceTypeIndex,
                    Amount = gatherAmount,
                    TicketId = ticket.ValueRO.TicketId
                });

                if (resourceState.UnitsRemaining <= 0f ||
                    GetCarryAmount(carry, ticket.ValueRO.ResourceTypeIndex) >= 40f)
                {
                    job.ValueRW.Phase = VillagerJob.JobPhase.Delivering;
                    job.ValueRW.LastStateChangeTick = timeState.Tick;
                    ticket.ValueRW.Phase = (byte)VillagerJob.JobPhase.Delivering;
                }
            }
        }

        private static float GetCarryAmount(DynamicBuffer<VillagerJobCarryItem> carry, ushort resourceTypeIndex)
        {
            for (int i = 0; i < carry.Length; i++)
            {
                if (carry[i].ResourceTypeIndex == resourceTypeIndex)
                {
                    return carry[i].Amount;
                }
            }
            return 0f;
        }

    }

    [BurstCompile]
    [UpdateInGroup(typeof(VillagerJobFixedStepGroup))]
    [UpdateAfter(typeof(VillagerJobExecutionSystem))]
    public partial struct VillagerJobDeliverySystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<ResourceJobReservation> _resourceReservationLookup;
        private BufferLookup<ResourceActiveTicket> _resourceActiveTicketLookup;
        private ComponentLookup<StorehouseJobReservation> _storehouseReservationLookup;
        private BufferLookup<StorehouseReservationItem> _storehouseReservationItems;

        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _resourceReservationLookup = state.GetComponentLookup<ResourceJobReservation>(false);
            _resourceActiveTicketLookup = state.GetBufferLookup<ResourceActiveTicket>(false);
            _storehouseReservationLookup = state.GetComponentLookup<StorehouseJobReservation>(false);
            _storehouseReservationItems = state.GetBufferLookup<StorehouseReservationItem>(false);

            state.RequireForUpdate<StorehouseRegistry>();
            state.RequireForUpdate<VillagerJobTicket>();
            state.RequireForUpdate<VillagerJobEventStream>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _transformLookup.Update(ref state);
            _resourceReservationLookup.Update(ref state);
            _resourceActiveTicketLookup.Update(ref state);
            _storehouseReservationLookup.Update(ref state);
            _storehouseReservationItems.Update(ref state);

            var storehouseEntity = SystemAPI.GetSingletonEntity<StorehouseRegistry>();
            var storehouseEntries = state.EntityManager.GetBuffer<StorehouseRegistryEntry>(storehouseEntity);
            if (storehouseEntries.Length == 0)
            {
                return;
            }

            var eventEntity = SystemAPI.GetSingletonEntity<VillagerJobEventStream>();
            var events = state.EntityManager.GetBuffer<VillagerJobEvent>(eventEntity);

            foreach (var (job, ticket, progress, carry, aiState, transform, entity) in SystemAPI.Query<RefRW<VillagerJob>, RefRW<VillagerJobTicket>, RefRW<VillagerJobProgress>, DynamicBuffer<VillagerJobCarryItem>, RefRW<VillagerAIState>, RefRO<LocalTransform>>()
                         .WithEntityAccess())
            {
                if (job.ValueRO.Phase != VillagerJob.JobPhase.Delivering)
                {
                    continue;
                }

                var carriedAmount = GetCarryAmount(carry, ticket.ValueRO.ResourceTypeIndex);
                if (carriedAmount <= 0f)
                {
                    CompleteJob(ref job.ValueRW, ref ticket.ValueRW, ref progress.ValueRW, carry, timeState.Tick);
                    events.Add(new VillagerJobEvent
                    {
                        Tick = timeState.Tick,
                        Villager = entity,
                        EventType = VillagerJobEventType.JobCompleted,
                        ResourceTypeIndex = ticket.ValueRO.ResourceTypeIndex,
                        Amount = 0f,
                        TicketId = ticket.ValueRO.TicketId
                    });
                    continue;
                }

                if (ticket.ValueRO.StorehouseEntity == Entity.Null)
                {
                    var bestStorehouse = SelectStorehouse(storehouseEntries, ticket.ValueRO.ResourceTypeIndex, transform.ValueRO.Position);
                    ticket.ValueRW.StorehouseEntity = bestStorehouse;
                    aiState.ValueRW.TargetEntity = bestStorehouse;
                    aiState.ValueRW.CurrentState = VillagerAIState.State.Working;
                    aiState.ValueRW.CurrentGoal = VillagerAIState.Goal.Work;

                    if (bestStorehouse != Entity.Null)
                    {
                        ReserveStorehouse(bestStorehouse, ticket.ValueRO.ResourceTypeIndex, carriedAmount, timeState.Tick);
                    }
                }
            }
        }

        private static Entity SelectStorehouse(DynamicBuffer<StorehouseRegistryEntry> entries, ushort resourceTypeIndex, float3 villagerPos)
        {
            var best = Entity.Null;
            var bestScore = float.MaxValue;

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                float available = 0f;
                for (int t = 0; t < entry.TypeSummaries.Length; t++)
                {
                    var summary = entry.TypeSummaries[t];
                    if (summary.ResourceTypeIndex == resourceTypeIndex)
                    {
                        available = summary.Capacity - (summary.Stored + summary.Reserved);
                        break;
                    }
                }

                if (available <= 0f)
                {
                    continue;
                }

                var dist = math.distancesq(villagerPos, entry.Position);
                if (dist < bestScore)
                {
                    bestScore = dist;
                    best = entry.StorehouseEntity;
                }
            }

            return best;
        }

        private void CompleteJob(ref VillagerJob job, ref VillagerJobTicket ticket, ref VillagerJobProgress progress, DynamicBuffer<VillagerJobCarryItem> carry, uint currentTick)
        {
            var resourceEntity = ticket.ResourceEntity;
            var storehouseEntity = ticket.StorehouseEntity;
            var ticketId = ticket.TicketId;
            var deliveredAmount = progress.Gathered;

            job.Phase = VillagerJob.JobPhase.Completed;
            job.ActiveTicketId = 0;
            job.LastStateChangeTick = currentTick;

            ticket.ResourceEntity = Entity.Null;
            ticket.StorehouseEntity = Entity.Null;
            ticket.ReservedUnits = 0f;
            ticket.TicketId = 0;
            ticket.Phase = (byte)VillagerJob.JobPhase.Completed;
            ticket.LastProgressTick = currentTick;

            carry.Clear();
            progress.Delivered += progress.Gathered;
            progress.Gathered = 0f;
            progress.TimeInPhase = 0f;
            progress.LastUpdateTick = currentTick;

            if (resourceEntity != Entity.Null && _resourceReservationLookup.HasComponent(resourceEntity))
            {
                var reservation = _resourceReservationLookup[resourceEntity];
                reservation.ActiveTickets = (byte)math.max(0, reservation.ActiveTickets - 1);
                reservation.ReservedUnits = math.max(0f, reservation.ReservedUnits - deliveredAmount);
                reservation.LastMutationTick = currentTick;
                _resourceReservationLookup[resourceEntity] = reservation;

                if (_resourceActiveTicketLookup.HasBuffer(resourceEntity))
                {
                    var activeTickets = _resourceActiveTicketLookup[resourceEntity];
                    for (int i = activeTickets.Length - 1; i >= 0; i--)
                    {
                        if (activeTickets[i].TicketId == ticketId)
                        {
                            activeTickets.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            if (storehouseEntity != Entity.Null && _storehouseReservationLookup.HasComponent(storehouseEntity))
            {
                var reservation = _storehouseReservationLookup[storehouseEntity];
                reservation.ReservedCapacity = math.max(0f, reservation.ReservedCapacity - deliveredAmount);
                reservation.LastMutationTick = currentTick;
                _storehouseReservationLookup[storehouseEntity] = reservation;

                if (_storehouseReservationItems.HasBuffer(storehouseEntity))
                {
                    var items = _storehouseReservationItems[storehouseEntity];
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (items[i].ResourceTypeIndex == ticket.ResourceTypeIndex)
                        {
                            var item = items[i];
                            item.Reserved = math.max(0f, item.Reserved - deliveredAmount);
                            items[i] = item;
                            break;
                        }
                    }
                }
            }
        }

        private static float GetCarryAmount(DynamicBuffer<VillagerJobCarryItem> carry, ushort resourceTypeIndex)
        {
            for (int i = 0; i < carry.Length; i++)
            {
                if (carry[i].ResourceTypeIndex == resourceTypeIndex)
                {
                    return carry[i].Amount;
                }
            }

            return 0f;
        }

        private void ReserveStorehouse(Entity storehouse, ushort resourceTypeIndex, float amount, uint currentTick)
        {
            if (storehouse == Entity.Null || amount <= 0f)
            {
                return;
            }

            if (_storehouseReservationLookup.HasComponent(storehouse))
            {
                var reservation = _storehouseReservationLookup[storehouse];
                reservation.ReservedCapacity += amount;
                reservation.LastMutationTick = currentTick;
                _storehouseReservationLookup[storehouse] = reservation;
            }

            if (_storehouseReservationItems.HasBuffer(storehouse))
            {
                var items = _storehouseReservationItems[storehouse];
                var updated = false;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].ResourceTypeIndex == resourceTypeIndex)
                    {
                        var item = items[i];
                        item.Reserved += amount;
                        items[i] = item;
                        updated = true;
                        break;
                    }
                }

                if (!updated)
                {
                    items.Add(new StorehouseReservationItem
                    {
                        ResourceTypeIndex = resourceTypeIndex,
                        Reserved = amount
                    });
                }
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(VillagerJobFixedStepGroup))]
    [UpdateAfter(typeof(VillagerJobDeliverySystem))]
    public partial struct VillagerJobInterruptSystem : ISystem
    {
        private ComponentLookup<ResourceJobReservation> _resourceReservationLookup;
        private BufferLookup<ResourceActiveTicket> _resourceActiveTicketLookup;
        private ComponentLookup<StorehouseJobReservation> _storehouseReservationLookup;
        private BufferLookup<StorehouseReservationItem> _storehouseReservationItems;

        public void OnCreate(ref SystemState state)
        {
            _resourceReservationLookup = state.GetComponentLookup<ResourceJobReservation>(false);
            _resourceActiveTicketLookup = state.GetBufferLookup<ResourceActiveTicket>(false);
            _storehouseReservationLookup = state.GetComponentLookup<StorehouseJobReservation>(false);
            _storehouseReservationItems = state.GetBufferLookup<StorehouseReservationItem>(false);

            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            state.RequireForUpdate<VillagerJob>();
            state.RequireForUpdate<VillagerJobTicket>();
            state.RequireForUpdate<VillagerJobEventStream>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (timeState.IsPaused || rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            _resourceReservationLookup.Update(ref state);
            _resourceActiveTicketLookup.Update(ref state);
            _storehouseReservationLookup.Update(ref state);
            _storehouseReservationItems.Update(ref state);

            var eventEntity = SystemAPI.GetSingletonEntity<VillagerJobEventStream>();
            var events = state.EntityManager.GetBuffer<VillagerJobEvent>(eventEntity);

            foreach (var (job, ticket, progress, carry, entity) in SystemAPI.Query<RefRW<VillagerJob>, RefRW<VillagerJobTicket>, RefRW<VillagerJobProgress>, DynamicBuffer<VillagerJobCarryItem>>()
                         .WithEntityAccess())
            {
                if (job.ValueRO.Type == VillagerJob.JobType.None)
                {
                    continue;
                }

                var resourceEntity = ticket.ValueRO.ResourceEntity;
                var storehouseEntity = ticket.ValueRO.StorehouseEntity;
                var resourceTypeIndex = ticket.ValueRO.ResourceTypeIndex;
                var reservedUnits = ticket.ValueRO.ReservedUnits;

                if (resourceEntity == Entity.Null)
                {
                    continue;
                }

                if (!_resourceReservationLookup.HasComponent(resourceEntity))
                {
                    continue;
                }

                var reservation = _resourceReservationLookup[resourceEntity];
                if ((reservation.ClaimFlags & ResourceRegistryClaimFlags.PlayerClaim) == 0)
                {
                    continue;
                }

                reservation.ActiveTickets = (byte)math.max(0, reservation.ActiveTickets - 1);
                reservation.ReservedUnits = math.max(0f, reservation.ReservedUnits - reservedUnits);
                reservation.LastMutationTick = timeState.Tick;
                reservation.ClaimFlags &= unchecked((byte)~ResourceRegistryClaimFlags.VillagerReserved);
                _resourceReservationLookup[resourceEntity] = reservation;

                if (_resourceActiveTicketLookup.HasBuffer(resourceEntity))
                {
                    var buffer = _resourceActiveTicketLookup[resourceEntity];
                    for (int i = buffer.Length - 1; i >= 0; i--)
                    {
                        if (buffer[i].Villager == entity)
                        {
                            buffer.RemoveAt(i);
                            break;
                        }
                    }
                }

                carry.Clear();
                progress.ValueRW.TimeInPhase = 0f;
                progress.ValueRW.LastUpdateTick = timeState.Tick;

                job.ValueRW.Phase = VillagerJob.JobPhase.Interrupted;
                job.ValueRW.ActiveTicketId = 0;
                job.ValueRW.LastStateChangeTick = timeState.Tick;

                var interruptedTicketId = ticket.ValueRO.TicketId;
                ticket.ValueRW.ResourceEntity = Entity.Null;
                ticket.ValueRW.StorehouseEntity = Entity.Null;
                ticket.ValueRW.ReservedUnits = 0f;
                ticket.ValueRW.TicketId = 0;
                ticket.ValueRW.Phase = (byte)VillagerJob.JobPhase.Interrupted;
                ticket.ValueRW.LastProgressTick = timeState.Tick;

                var hasStorehouseReservation = _storehouseReservationLookup.HasComponent(storehouseEntity);
                if (storehouseEntity != Entity.Null && hasStorehouseReservation)
                {
                    var storeReservation = _storehouseReservationLookup[storehouseEntity];
                    storeReservation.ReservedCapacity = math.max(0f, storeReservation.ReservedCapacity - reservedUnits);
                    storeReservation.LastMutationTick = timeState.Tick;
                    _storehouseReservationLookup[storehouseEntity] = storeReservation;

                var hasReservationItems = _storehouseReservationItems.HasBuffer(storehouseEntity);
                if (hasReservationItems)
                    {
                        var items = _storehouseReservationItems[storehouseEntity];
                        for (int i = 0; i < items.Length; i++)
                        {
                            if (items[i].ResourceTypeIndex == resourceTypeIndex)
                            {
                                var item = items[i];
                                item.Reserved = math.max(0f, item.Reserved - reservedUnits);
                                items[i] = item;
                                break;
                            }
                        }
                    }
                }

                events.Add(new VillagerJobEvent
                {
                    Tick = timeState.Tick,
                    Villager = entity,
                    EventType = VillagerJobEventType.JobInterrupted,
                    ResourceTypeIndex = resourceTypeIndex,
                    Amount = 0f,
                    TicketId = interruptedTicketId
                });
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(RecordSimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial struct VillagerJobEventFlushSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VillagerJobEventStream>();
            state.RequireForUpdate<TimeState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var eventEntity = SystemAPI.GetSingletonEntity<VillagerJobEventStream>();
            var events = state.EntityManager.GetBuffer<VillagerJobEvent>(eventEntity);

            // Retain only recent events to avoid unbounded growth.
            var horizonTick = timeState.Tick > 120 ? timeState.Tick - 120u : 0u;
            for (int i = events.Length - 1; i >= 0; i--)
            {
                if (events[i].Tick < horizonTick)
                {
                    events.RemoveAt(i);
                }
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(HistorySystemGroup))]
    public partial struct VillagerJobHistorySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VillagerJobHistorySample>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<HistorySettings>();
            state.RequireForUpdate<RewindState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var historySettings = SystemAPI.GetSingleton<HistorySettings>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            var strideTicks = (uint)math.max(1f, historySettings.DefaultStrideSeconds / math.max(0.0001f, timeState.FixedDeltaTime));
            if (strideTicks == 0 || timeState.Tick % strideTicks != 0)
            {
                return;
            }

            foreach (var (job, ticket, progress, transform, entity) in SystemAPI.Query<RefRO<VillagerJob>, RefRO<VillagerJobTicket>, RefRO<VillagerJobProgress>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                var buffer = state.EntityManager.GetBuffer<VillagerJobHistorySample>(entity);
                buffer.Add(new VillagerJobHistorySample
                {
                    Tick = timeState.Tick,
                    TicketId = ticket.ValueRO.TicketId,
                    Phase = job.ValueRO.Phase,
                    Gathered = progress.ValueRO.Gathered,
                    Delivered = progress.ValueRO.Delivered,
                    TargetPosition = transform.ValueRO.Position
                });
                PruneHistory(ref buffer, timeState.Tick, historySettings.DefaultHorizonSeconds, timeState.FixedDeltaTime);
            }
        }

        private static void PruneHistory(ref DynamicBuffer<VillagerJobHistorySample> buffer, uint currentTick, float horizonSeconds, float fixedDt)
        {
            if (buffer.Length == 0)
            {
                return;
            }

            var horizonTicks = (uint)math.max(1f, horizonSeconds / math.max(0.0001f, fixedDt));
            for (int i = 0; i < buffer.Length; i++)
            {
                if (currentTick - buffer[i].Tick <= horizonTicks)
                {
                    if (i > 0)
                    {
                        buffer.RemoveRange(0, i);
                    }
                    return;
                }
            }

            buffer.Clear();
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PlaybackSimulationSystemGroup))]
    public partial struct VillagerJobPlaybackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VillagerJobHistorySample>();
            state.RequireForUpdate<VillagerJob>();
            state.RequireForUpdate<VillagerJobTicket>();
            state.RequireForUpdate<VillagerJobProgress>();
            state.RequireForUpdate<RewindState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var rewindState = SystemAPI.GetSingleton<RewindState>();
            if (rewindState.Mode != RewindMode.Playback)
            {
                return;
            }

            var targetTick = rewindState.PlaybackTick;
            foreach (var (job, ticket, progress, historyBuffer) in SystemAPI.Query<RefRW<VillagerJob>, RefRW<VillagerJobTicket>, RefRW<VillagerJobProgress>, DynamicBuffer<VillagerJobHistorySample>>())
            {
                if (historyBuffer.Length == 0)
                {
                    continue;
                }

                var sampleIndex = FindSampleIndex(historyBuffer, targetTick);
                if (sampleIndex < 0)
                {
                    continue;
                }

                var sample = historyBuffer[sampleIndex];
                job.ValueRW.Phase = sample.Phase;
                job.ValueRW.ActiveTicketId = sample.TicketId;

                ticket.ValueRW.TicketId = sample.TicketId;
                ticket.ValueRW.Phase = (byte)sample.Phase;

                progress.ValueRW.Gathered = sample.Gathered;
                progress.ValueRW.Delivered = sample.Delivered;
            }
        }

        private static int FindSampleIndex(DynamicBuffer<VillagerJobHistorySample> buffer, uint tick)
        {
            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                if (buffer[i].Tick <= tick)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
