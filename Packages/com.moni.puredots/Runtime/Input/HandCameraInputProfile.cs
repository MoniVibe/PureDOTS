using UnityEngine;
using UnityEngine.InputSystem;

namespace PureDOTS.Input
{
    [CreateAssetMenu(
        fileName = "HandCameraInputProfile",
        menuName = "PureDOTS/Input/Hand Camera Input Profile",
        order = 0)]
    public sealed class HandCameraInputProfile : ScriptableObject
    {
        [Header("Input Asset")]
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] string actionMapName = "HandCamera";

        [Header("Router")]
        [SerializeField, Min(0f)] float handlerCooldownSeconds = 0.1f;
        [SerializeField, Min(0)] int hysteresisFrames = 3;
        [SerializeField] bool logTransitions;

        [Header("Masks & Raycast")]
        [SerializeField] LayerMask interactionMask = ~0;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] LayerMask storehouseMask = 0;
        [SerializeField] LayerMask pileMask = 0;
        [SerializeField] LayerMask draggableMask = 0;
        [SerializeField, Min(0.1f)] float maxRayDistance = 800f;

        public InputActionAsset InputActions => inputActions;
        public string ActionMapName => actionMapName;
        public float HandlerCooldownSeconds => Mathf.Max(0f, handlerCooldownSeconds);
        public int HysteresisFrames => Mathf.Max(0, hysteresisFrames);
        public bool LogTransitions => logTransitions;
        public LayerMask InteractionMask => interactionMask;
        public LayerMask GroundMask => groundMask;
        public LayerMask StorehouseMask => storehouseMask;
        public LayerMask PileMask => pileMask;
        public LayerMask DraggableMask => draggableMask;
        public float MaxRayDistance => Mathf.Max(0.1f, maxRayDistance);
    }
}
