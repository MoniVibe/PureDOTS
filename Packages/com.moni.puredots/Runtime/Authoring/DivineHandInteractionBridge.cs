using PureDOTS.Input;
using PureDOTS.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PureDOTS.Authoring
{
    [DisallowMultipleComponent]
    public sealed class DivineHandInteractionBridge : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] HandCameraInputRouter inputRouter;
        [SerializeField] StorehouseDumpRmbHandler storehouseHandler;
        [SerializeField] PileSiphonRmbHandler pileHandler;
        [SerializeField] GroundDripRmbHandler groundHandler;

        World _world;
        EntityQuery _handQuery;
        bool _queryValid;
        Entity _currentHighlightEntity = Entity.Null;
        Vector3 _currentHighlightPosition;
        Vector3 _currentHighlightNormal = Vector3.up;

        void Awake()
        {
            EnsureRouter();
            EnsureHandlers();
            EnsureWorld();
        }

        void OnEnable()
        {
            EnsureRouter();
            EnsureHandlers();
            EnsureWorld();
            SubscribeHandlers();
        }

        void OnDisable()
        {
            UnsubscribeHandlers();
            DisposeQuery();
        }

        void OnDestroy()
        {
            UnsubscribeHandlers();
            DisposeQuery();
        }

        void EnsureRouter()
        {
            if (inputRouter != null) return;
            inputRouter = GetComponent<HandCameraInputRouter>();
            if (inputRouter == null)
            {
                inputRouter = FindFirstObjectByType<HandCameraInputRouter>();
            }
        }

        void EnsureHandlers()
        {
            if (storehouseHandler == null)
            {
                storehouseHandler = FindFirstObjectByType<StorehouseDumpRmbHandler>();
            }

            if (pileHandler == null)
            {
                pileHandler = FindFirstObjectByType<PileSiphonRmbHandler>();
            }

            if (groundHandler == null)
            {
                groundHandler = FindFirstObjectByType<GroundDripRmbHandler>();
            }
        }

        void SubscribeHandlers()
        {
            if (storehouseHandler != null)
            {
                storehouseHandler.RmbInvoked += HandleStorehouseRmb;
            }

            if (pileHandler != null)
            {
                pileHandler.RmbInvoked += HandlePileRmb;
            }

            if (groundHandler != null)
            {
                groundHandler.RmbInvoked += HandleGroundRmb;
            }
        }

        void UnsubscribeHandlers()
        {
            if (storehouseHandler != null)
            {
                storehouseHandler.RmbInvoked -= HandleStorehouseRmb;
            }

            if (pileHandler != null)
            {
                pileHandler.RmbInvoked -= HandlePileRmb;
            }

            if (groundHandler != null)
            {
                groundHandler.RmbInvoked -= HandleGroundRmb;
            }
        }

        bool EnsureWorld()
        {
            var newWorld = World.DefaultGameObjectInjectionWorld;
            if (newWorld == null || !newWorld.IsCreated)
            {
                DisposeQuery();
                _world = null;
                return false;
            }

            if (_world == newWorld && _queryValid)
            {
                return true;
            }

            DisposeQuery();
            _world = newWorld;
            _handQuery = _world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<DivineHandTag>(),
                ComponentType.ReadWrite<DivineHandCommand>(),
                ComponentType.ReadOnly<DivineHandState>());
            _queryValid = true;
            return true;
        }

        void DisposeQuery()
        {
            _queryValid = false;
            _handQuery = default;
        }

        void HandleStorehouseRmb(RmbContext context, RmbPhase phase) => HandleCommand(DivineHandCommandType.DumpToStorehouse, context, phase, storehouseHandler != null ? storehouseHandler.Priority : HandRoutePriority.DumpToStorehouse);

        void HandlePileRmb(RmbContext context, RmbPhase phase) => HandleCommand(DivineHandCommandType.SiphonPile, context, phase, pileHandler != null ? pileHandler.Priority : HandRoutePriority.ResourceSiphon);

        void HandleGroundRmb(RmbContext context, RmbPhase phase) => HandleCommand(DivineHandCommandType.GroundDrip, context, phase, groundHandler != null ? groundHandler.Priority : HandRoutePriority.GroundDrip);

        void HandleCommand(DivineHandCommandType type, in RmbContext context, RmbPhase phase, int requestedPriority)
        {
            if (!EnsureWorld() || !_queryValid || _handQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            if (phase == RmbPhase.Started)
            {
                if ((type == DivineHandCommandType.DumpToStorehouse || type == DivineHandCommandType.GroundDrip) && !context.HandHasCargo)
                {
                    return;
                }

                if (type == DivineHandCommandType.SiphonPile && context.HandHasCargo)
                {
                    return;
                }
            }

            var entityManager = _world.EntityManager;
            Entity handEntity;
            try
            {
                handEntity = _handQuery.GetSingletonEntity();
            }
            catch
            {
                return;
            }

            if (!entityManager.HasBuffer<HandInputRouteRequest>(handEntity))
            {
                return;
            }

            var requests = entityManager.GetBuffer<HandInputRouteRequest>(handEntity);
            var position = new float3(context.WorldPoint.x, context.WorldPoint.y, context.WorldPoint.z);
            var normal = context.HasWorldHit
                ? new float3(context.WorldHit.normal.x, context.WorldHit.normal.y, context.WorldHit.normal.z)
                : new float3(0f, 1f, 0f);

            var phaseConverted = phase switch
            {
                RmbPhase.Started => HandRoutePhase.Started,
                RmbPhase.Performed => HandRoutePhase.Performed,
                RmbPhase.Canceled => HandRoutePhase.Canceled,
                _ => HandRoutePhase.Performed
            };

            var priority = (byte)math.clamp(requestedPriority, 0, 255);
            requests.Add(HandInputRouteRequest.Create(
                HandRouteSource.AuthoringBridge,
                phaseConverted,
                priority,
                type,
                Entity.Null,
                position,
                normal));
        }

        void LateUpdate()
        {
            if (!EnsureWorld() || !_queryValid || _handQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var entityManager = _world.EntityManager;
            Entity handEntity;
            try
            {
                handEntity = _handQuery.GetSingletonEntity();
            }
            catch
            {
                return;
            }

            var context = inputRouter != null ? inputRouter.CurrentContext : default;
            var handState = entityManager.GetComponentData<DivineHandState>(handEntity);

            if (inputRouter != null)
            {
                var hasCargo = handState.HeldEntity != Entity.Null && entityManager.Exists(handState.HeldEntity);
                inputRouter.ReportHandCargo(hasCargo);
            }

            var highlight = entityManager.GetComponentData<DivineHandHighlight>(handEntity);

            if (context.PointerOverUI)
            {
                highlight.Type = HandHighlightType.None;
                highlight.TargetEntity = Entity.Null;
            }
            else if (context.HitStorehouse)
            {
                highlight.Type = HandHighlightType.Storehouse;
                highlight.TargetEntity = Entity.Null;
            }
            else if (context.HitPile)
            {
                highlight.Type = HandHighlightType.Pile;
                highlight.TargetEntity = Entity.Null;
            }
            else if (context.HitDraggable)
            {
                highlight.Type = HandHighlightType.Draggable;
                highlight.TargetEntity = Entity.Null;
            }
            else if (context.HitGround)
            {
                highlight.Type = HandHighlightType.Ground;
                highlight.TargetEntity = Entity.Null;
            }
            else
            {
                highlight.Type = HandHighlightType.None;
                highlight.TargetEntity = Entity.Null;
            }

            var worldPoint = context.WorldPoint;
            highlight.Position = new float3(worldPoint.x, worldPoint.y, worldPoint.z);
            if (context.HasWorldHit)
            {
                var normal = context.WorldHit.normal;
                highlight.Normal = new float3(normal.x, normal.y, normal.z);
            }
            else
            {
                highlight.Normal = new float3(0f, 1f, 0f);
            }

            entityManager.SetComponentData(handEntity, highlight);
        }
    }
}
