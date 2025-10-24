using PureDOTS.Runtime.Components;
using Unity.Entities;
using UnityEngine;

namespace PureDOTS.Debugging
{
    /// <summary>
    /// Optional input handler for debug commands.
    /// Sends debug commands to DOTS command buffer for processing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugInputHandler : MonoBehaviour
    {
        [Header("Keyboard Shortcuts")]
        [Tooltip("Key to toggle debug HUD visibility")]
        public KeyCode toggleHUDKey = KeyCode.F1;

        [Tooltip("Key to show debug HUD")]
        public KeyCode showHUDKey = KeyCode.F2;

        [Tooltip("Key to hide debug HUD")]
        public KeyCode hideHUDKey = KeyCode.F3;

        [Header("Settings")]
        [Tooltip("Enable keyboard shortcuts")]
        public bool enableKeyboardShortcuts = true;

        private World _world;
        private EntityQuery _commandQuery;
        private bool _hasCommandQuery;
        private bool _warnedMissingWorld;

        private void Awake()
        {
            InitializeWorld();
        }

        private void OnEnable()
        {
            // Reinitialize on world reload
            InitializeWorld();
        }

        private void InitializeWorld()
        {
            var newWorld = World.DefaultGameObjectInjectionWorld;
            
            if (newWorld == null || !newWorld.IsCreated)
            {
                if (!_warnedMissingWorld)
                {
                    Debug.LogWarning("DebugInputHandler did not find a DefaultGameObjectInjectionWorld.", this);
                    _warnedMissingWorld = true;
                }
                if (_hasCommandQuery)
                {
                    _commandQuery.Dispose();
                    _hasCommandQuery = false;
                }
                _world = null;
                return;
            }

            // Dispose old query if world changed
            if (_world != null && _world != newWorld)
            {
                if (_hasCommandQuery)
                {
                    _commandQuery.Dispose();
                    _hasCommandQuery = false;
                }
            }

            _world = newWorld;
            _warnedMissingWorld = false;

            // Cache query for new world
            var entityManager = _world.EntityManager;
            _commandQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugCommandSingletonTag>());
            _hasCommandQuery = true;
        }

        private void OnDestroy()
        {
            // Dispose cached query (EntityQuery is a struct, check IsCreated)
            if (_hasCommandQuery)
            {
                _commandQuery.Dispose();
                _hasCommandQuery = false;
            }
        }

        private void Update()
        {
            if (!enableKeyboardShortcuts)
            {
                return;
            }

            if (_world == null || !_world.IsCreated)
            {
                InitializeWorld();
                if (_world == null || !_world.IsCreated)
                {
                    return;
                }
            }

            if (_commandQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var entity = _commandQuery.GetSingletonEntity();
            var entityManager = _world.EntityManager;
            if (!entityManager.HasBuffer<DebugCommand>(entity))
            {
                return;
            }

            var commands = entityManager.GetBuffer<DebugCommand>(entity);

            // Check for keyboard input
            if (Input.GetKeyDown(toggleHUDKey))
            {
                commands.Add(new DebugCommand { Type = DebugCommand.CommandType.ToggleHUD });
            }
            else if (Input.GetKeyDown(showHUDKey))
            {
                commands.Add(new DebugCommand { Type = DebugCommand.CommandType.ShowHUD });
            }
            else if (Input.GetKeyDown(hideHUDKey))
            {
                commands.Add(new DebugCommand { Type = DebugCommand.CommandType.HideHUD });
            }
        }
    }
}
