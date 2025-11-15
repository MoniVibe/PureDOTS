using PureDOTS.Runtime.Components;
using PureDOTS.Input;
using PureDOTS.Runtime.Hand;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Authoring
{
    [DisallowMultipleComponent]
    public sealed class DivineHandAuthoring : MonoBehaviour
    {
        public const int LatestSchemaVersion = 1;

        [SerializeField, HideInInspector]
        private int _schemaVersion = LatestSchemaVersion;

        [Header("Hand Settings")]
        [Min(0.1f)] public float pickupRadius = 8f;
        [Min(0.1f)] public float maxGrabDistance = 60f;
        [Range(0.05f, 1f)] public float holdLerp = 0.25f;
        [Min(1f)] public float throwImpulse = 20f;
        [Min(0.1f)] public float throwChargeMultiplier = 12f;
        public float holdHeightOffset = 4f;

        [Header("State Machine")]
        [Min(0f)] public float cooldownAfterThrowSeconds = 0.35f;
        [Min(0f)] public float minChargeSeconds = 0.15f;
        [Min(0f)] public float maxChargeSeconds = 1.25f;
        [Min(0)] public int hysteresisFrames = 3;
        [Min(1)] public int heldCapacity = 500;

        [Header("Resource Flow")]
        [Min(0f)] public float siphonUnitsPerSecond = 50f;
        [Min(0f)] public float dumpUnitsPerSecond = 150f;

        [Header("Initial State")]
        public Vector3 initialCursorWorldPosition = new(0f, 12f, 0f);
        public Vector3 initialAimDirection = new(0f, -1f, 0f);

#if UNITY_EDITOR
        public int SchemaVersion => _schemaVersion;

        public void SetSchemaVersion(int value)
        {
            _schemaVersion = value;
        }
#endif
    }

    public sealed class DivineHandBaker : Baker<DivineHandAuthoring>
    {
        public override void Bake(DivineHandAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

            AddComponent<DivineHandTag>(entity);
            AddComponent(entity, new DivineHandConfig
            {
                PickupRadius = authoring.pickupRadius,
                MaxGrabDistance = authoring.maxGrabDistance,
                HoldLerp = math.clamp(authoring.holdLerp, 0.05f, 1f),
                ThrowImpulse = authoring.throwImpulse,
                ThrowChargeMultiplier = authoring.throwChargeMultiplier,
                HoldHeightOffset = authoring.holdHeightOffset,
                CooldownAfterThrowSeconds = math.max(0f, authoring.cooldownAfterThrowSeconds),
                MinChargeSeconds = math.max(0f, authoring.minChargeSeconds),
                MaxChargeSeconds = math.max(authoring.minChargeSeconds, authoring.maxChargeSeconds),
                HysteresisFrames = math.max(0, authoring.hysteresisFrames),
                HeldCapacity = math.max(1, authoring.heldCapacity),
                SiphonRate = math.max(0f, authoring.siphonUnitsPerSecond),
                DumpRate = math.max(0f, authoring.dumpUnitsPerSecond)
            });

            float3 cursor = new float3(authoring.initialCursorWorldPosition.x, authoring.initialCursorWorldPosition.y, authoring.initialCursorWorldPosition.z);
            float3 aim = math.normalizesafe(new float3(authoring.initialAimDirection.x, authoring.initialAimDirection.y, authoring.initialAimDirection.z), new float3(0f, -1f, 0f));

            AddComponent(entity, new DivineHandState
            {
                HeldEntity = Entity.Null,
                CursorPosition = cursor,
                AimDirection = aim,
                HeldLocalOffset = float3.zero,
                CurrentState = HandState.Empty,
                PreviousState = HandState.Empty,
                ChargeTimer = 0f,
                CooldownTimer = 0f,
                HeldResourceTypeIndex = DivineHandConstants.NoResourceType,
                HeldAmount = 0,
                HeldCapacity = math.max(1, authoring.heldCapacity),
                Flags = 0
            });

            AddComponent(entity, new HandInteractionState
            {
                HandEntity = entity,
                CurrentState = HandState.Empty,
                PreviousState = HandState.Empty,
                ActiveCommand = DivineHandCommandType.None,
                ActiveResourceType = DivineHandConstants.NoResourceType,
                HeldAmount = 0,
                HeldCapacity = math.max(1, authoring.heldCapacity),
                CooldownSeconds = 0f,
                LastUpdateTick = 0,
                Flags = 0
            });

            AddComponent(entity, new ResourceSiphonState
            {
                HandEntity = entity,
                TargetEntity = Entity.Null,
                ResourceTypeIndex = DivineHandConstants.NoResourceType,
                SiphonRate = math.max(0f, authoring.siphonUnitsPerSecond),
                DumpRate = math.max(0f, authoring.dumpUnitsPerSecond),
                AccumulatedUnits = 0f,
                LastUpdateTick = 0,
                Flags = 0
            });

            AddComponent(entity, new DivineHandInput
            {
                SampleTick = 0,
                PlayerId = 0,
                PointerPosition = float2.zero,
                PointerDelta = float2.zero,
                CursorWorldPosition = cursor,
                AimDirection = aim,
                PrimaryHeld = 0,
                SecondaryHeld = 0,
                ThrowCharge = 0f,
                PointerOverUI = 0,
                AppHasFocus = 1,
                QueueModifierHeld = 0,
                ReleaseSingleTriggered = 0,
                ReleaseAllTriggered = 0,
                ToggleThrowModeTriggered = 0,
                ThrowModeIsSlingshot = 1
            });

            AddComponent(entity, new DivineHandCommand
            {
                Type = DivineHandCommandType.None,
                TargetEntity = Entity.Null,
                TargetPosition = cursor,
                TargetNormal = new float3(0f, 1f, 0f),
                TimeSinceIssued = 0f
            });

            AddComponent(entity, new DivineHandHighlight
            {
                Type = HandHighlightType.None,
                TargetEntity = Entity.Null,
                Position = cursor,
                Normal = new float3(0f, 1f, 0f)
            });

            AddComponent(entity, new GodIntent
            {
                LastUpdateTick = 0,
                PlayerId = 0
            });

            AddBuffer<DivineHandEvent>(entity);
            AddBuffer<HandInputRouteRequest>(entity);
            AddComponent(entity, HandInputRouteResult.None);
            AddBuffer<HandQueuedThrowElement>(entity);
            AddBuffer<MiracleReleaseEvent>(entity);
            AddBuffer<MiracleSlotDefinition>(entity);

            AddComponent(entity, new MiracleCasterState
            {
                HandEntity = entity,
                SelectedSlot = 0,
                SustainedCastHeld = 0,
                ThrowCastTriggered = 0
            });
        }
    }
}
