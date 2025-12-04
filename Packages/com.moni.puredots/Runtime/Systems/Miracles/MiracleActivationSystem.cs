using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Miracles;
using PureDOTS.Runtime.Time;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PureDOTS.Systems.Miracles
{
    /// <summary>
    /// Consumes miracle activation requests and spawns effect entities.
    /// Checks cooldowns and respects miracle specifications from catalog.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(MiracleEffectSystemGroup), OrderFirst = true)]
    public partial struct MiracleActivationSystem : ISystem
    {
        private TimeAwareController _controller;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MiracleConfigState>();
            state.RequireForUpdate<TimeState>();
            state.RequireForUpdate<RewindState>();
            _controller = new TimeAwareController(
                TimeAwareExecutionPhase.Record | TimeAwareExecutionPhase.CatchUp,
                TimeAwareExecutionOptions.SkipWhenPaused);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var timeState = SystemAPI.GetSingleton<TimeState>();
            var rewindState = SystemAPI.GetSingleton<RewindState>();

            if (!_controller.TryBegin(timeState, rewindState, out var context))
            {
                return;
            }

            // Only process in Record mode (not during playback)
            if (rewindState.Mode != RewindMode.Record)
            {
                return;
            }

            if (!SystemAPI.TryGetSingleton<MiracleConfigState>(out var configState))
            {
                return;
            }

            ref var catalog = ref configState.Catalog.Value;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var cooldownLookup = state.GetBufferLookup<MiracleCooldown>(false);
            cooldownLookup.Update(ref state);
            
            // Track which entities need cooldown entries added (for new buffers created this frame)
            var pendingCooldowns = new NativeHashMap<Entity, NativeList<MiracleCooldown>>(16, Allocator.TempJob);

            // Process all activation requests
            foreach (var (requests, entity) in SystemAPI.Query<DynamicBuffer<MiracleActivationRequest>>().WithEntityAccess())
            {
                DynamicBuffer<MiracleCooldown> cooldowns = default;
                bool hasCooldownBuffer = cooldownLookup.HasBuffer(entity);
                if (hasCooldownBuffer)
                {
                    cooldowns = cooldownLookup[entity];
                }

                for (int i = requests.Length - 1; i >= 0; i--)
                {
                    var request = requests[i];
                    
                    // Find spec in catalog
                    bool foundSpec = false;
                    MiracleSpec spec = default;
                    for (int j = 0; j < catalog.Specs.Length; j++)
                    {
                        if (catalog.Specs[j].Id == request.Id)
                        {
                            spec = catalog.Specs[j];
                            foundSpec = true;
                            break;
                        }
                    }

                    if (!foundSpec)
                    {
                        requests.RemoveAt(i);
                        continue;
                    }

                    // Check cooldown
                    bool canActivate = false;
                    
                    if (hasCooldownBuffer)
                    {
                        for (int j = 0; j < cooldowns.Length; j++)
                        {
                            if (cooldowns[j].Id == request.Id)
                            {
                                if (cooldowns[j].RemainingSeconds <= 0f && cooldowns[j].ChargesAvailable > 0)
                                {
                                    canActivate = true;
                                    // Reduce charge and set cooldown
                                    var cooldown = cooldowns[j];
                                    if (cooldown.ChargesAvailable > 0)
                                    {
                                        cooldown.ChargesAvailable--;
                                    }
                                    cooldown.RemainingSeconds = spec.BaseCooldownSeconds * configState.GlobalCooldownScale;
                                    cooldowns[j] = cooldown;
                                }
                                break;
                            }
                        }
                    }

                    // If no cooldown entry exists, allow activation and create cooldown
                    if (!canActivate)
                    {
                        bool hasCooldown = false;
                        if (hasCooldownBuffer)
                        {
                            for (int j = 0; j < cooldowns.Length; j++)
                            {
                                if (cooldowns[j].Id == request.Id)
                                {
                                    hasCooldown = true;
                                    break;
                                }
                            }
                        }
                        
                        if (!hasCooldown)
                        {
                            // First time using this miracle - allow activation
                            canActivate = true;
                            if (!hasCooldownBuffer)
                            {
                                // Create cooldown buffer
                                ecb.AddBuffer<MiracleCooldown>(entity);
                                // Track that we need to add cooldown entry after playback
                                if (!pendingCooldowns.ContainsKey(entity))
                                {
                                    pendingCooldowns.Add(entity, new NativeList<MiracleCooldown>(4, Allocator.TempJob));
                                }
                                pendingCooldowns[entity].Add(new MiracleCooldown
                                {
                                    Id = request.Id,
                                    RemainingSeconds = spec.BaseCooldownSeconds * configState.GlobalCooldownScale,
                                    ChargesAvailable = (byte)(spec.MaxCharges - 1)
                                });
                            }
                            else
                            {
                                // Buffer exists but no entry - add it now
                                cooldowns.Add(new MiracleCooldown
                                {
                                    Id = request.Id,
                                    RemainingSeconds = spec.BaseCooldownSeconds * configState.GlobalCooldownScale,
                                    ChargesAvailable = (byte)(spec.MaxCharges - 1)
                                });
                            }
                        }
                    }

                    if (!canActivate)
                    {
                        requests.RemoveAt(i);
                        continue;
                    }

                    // MVP: Ignore prayer costs (spec has BasePrayerCost but we don't validate it)

                    // Spawn effect entity
                    var effectEntity = ecb.CreateEntity();
                    
                    // Add generic miracle effect component
                    ecb.AddComponent(effectEntity, new MiracleEffectNew
                    {
                        Id = request.Id,
                        RemainingSeconds = 60f, // Default duration, can be overridden by miracle-specific systems
                        Intensity = 1.0f,
                        Origin = request.TargetPoint,
                        Radius = math.clamp(request.TargetRadius, spec.BaseRadius, spec.MaxRadius)
                    });

                    // Add LocalTransform
                    ecb.AddComponent(effectEntity, LocalTransform.FromPosition(request.TargetPoint));

                    // Add miracle-specific components based on ID
                    // This will be extended by game-specific systems (Rain, Temporal Veil, etc.)
                    // For now, we just spawn the generic effect entity

                    // Remove processed request
                    requests.RemoveAt(i);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            
            // Add cooldown entries for entities that just got buffers created
            cooldownLookup.Update(ref state);
            foreach (var kvp in pendingCooldowns)
            {
                var entity = kvp.Key;
                var cooldownsToAdd = kvp.Value;
                
                if (cooldownLookup.HasBuffer(entity))
                {
                    var cooldowns = cooldownLookup[entity];
                    for (int i = 0; i < cooldownsToAdd.Length; i++)
                    {
                        cooldowns.Add(cooldownsToAdd[i]);
                    }
                }
                
                cooldownsToAdd.Dispose();
            }
            
            pendingCooldowns.Dispose();
        }
    }
}
