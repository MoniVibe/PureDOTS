using PureDOTS.Runtime.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace PureDOTS.Debugging
{
    /// <summary>
    /// Opt-in MonoBehaviour bridge that reads DebugDisplayData singleton and updates Unity UI.
    /// Attach this to a Canvas GameObject in playmode builds for runtime debug visualization.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public sealed class DebugDisplayReader : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text component displaying time state (optional)")]
        public Text timeStateText;
        
        [Tooltip("Text component displaying rewind state (optional)")]
        public Text rewindStateText;
        
        [Tooltip("Text component displaying villager count (optional)")]
        public Text villagerCountText;
        
        [Tooltip("Text component displaying resource totals (optional)")]
        public Text resourceTotalText;

        [Header("Update Settings")]
        [Tooltip("Update frequency in seconds (0 = every frame)")]
        public float updateInterval = 0.1f;

        [Tooltip("Start with HUD visible")]
        public bool startVisible = true;

        private World _world;
        private float _lastUpdateTime;
        private Canvas _canvas;
        private EntityQuery _commandQuery;
        private EntityQuery _debugDataQuery;
        private bool _hasCommandQuery;
        private bool _hasDebugDataQuery;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
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
                if (_world != null)
                {
                    Debug.LogWarning("DebugDisplayReader lost connection to DefaultGameObjectInjectionWorld.", this);
                }
                if (_hasCommandQuery)
                {
                    _commandQuery.Dispose();
                    _hasCommandQuery = false;
                }
                if (_hasDebugDataQuery)
                {
                    _debugDataQuery.Dispose();
                    _hasDebugDataQuery = false;
                }
                _world = null;
                enabled = false;
                return;
            }

            // Dispose old queries if world changed
            if (_world != null && _world != newWorld)
            {
                if (_hasCommandQuery)
                {
                    _commandQuery.Dispose();
                    _hasCommandQuery = false;
                }
                if (_hasDebugDataQuery)
                {
                    _debugDataQuery.Dispose();
                    _hasDebugDataQuery = false;
                }
            }

            _world = newWorld;

            // Cache queries for new world
            var entityManager = _world.EntityManager;
            _commandQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugCommandSingletonTag>());
            _debugDataQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugDisplayData>());
            _hasCommandQuery = true;
            _hasDebugDataQuery = true;

            // Set initial visibility
            if (_canvas != null)
            {
                _canvas.enabled = startVisible;
            }
        }

        private void OnDestroy()
        {
            // Dispose cached queries (EntityQuery is a struct, check IsCreated)
            if (_hasCommandQuery)
            {
                _commandQuery.Dispose();
                _hasCommandQuery = false;
            }
            if (_hasDebugDataQuery)
            {
                _debugDataQuery.Dispose();
                _hasDebugDataQuery = false;
            }
        }

        private void Update()
        {
            if (_world == null || !_world.IsCreated)
            {
                return;
            }

            // Throttle updates for performance
            if (updateInterval > 0f && Time.time - _lastUpdateTime < updateInterval)
            {
                return;
            }

            _lastUpdateTime = Time.time;

            // Check for debug commands
            ProcessDebugCommands();

            // Update UI from ECS singleton
            UpdateUI();
        }

        private void ProcessDebugCommands()
        {
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
            
            for (int i = 0; i < commands.Length; i++)
            {
                var cmd = commands[i];
                switch (cmd.Type)
                {
                    case DebugCommand.CommandType.ToggleHUD:
                        if (_canvas != null)
                        {
                            _canvas.enabled = !_canvas.enabled;
                        }
                        break;
                    case DebugCommand.CommandType.ShowHUD:
                        if (_canvas != null)
                        {
                            _canvas.enabled = true;
                        }
                        break;
                    case DebugCommand.CommandType.HideHUD:
                        if (_canvas != null)
                        {
                            _canvas.enabled = false;
                        }
                        break;
                }
            }

            // Clear processed commands
            commands.Clear();
        }

        private void UpdateUI()
        {
            if (_debugDataQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var debugData = _debugDataQuery.GetSingleton<DebugDisplayData>();

            // Update UI elements if assigned
            if (timeStateText != null)
            {
                timeStateText.text = debugData.TimeStateText.ToString();
            }

            if (rewindStateText != null)
            {
                rewindStateText.text = debugData.RewindStateText.ToString();
            }

            if (villagerCountText != null)
            {
                villagerCountText.text = $"Villagers: {debugData.VillagerCount}";
            }

            if (resourceTotalText != null)
            {
                resourceTotalText.text = $"Resources: {debugData.TotalResourcesStored:F1}";
            }
        }

        /// <summary>
        /// Public API for toggling HUD visibility from other MonoBehaviour scripts.
        /// </summary>
        public void ToggleHUD()
        {
            if (_canvas != null)
            {
                _canvas.enabled = !_canvas.enabled;
            }
        }

        /// <summary>
        /// Public API for showing HUD.
        /// </summary>
        public void ShowHUD()
        {
            if (_canvas != null)
            {
                _canvas.enabled = true;
            }
        }

        /// <summary>
        /// Public API for hiding HUD.
        /// </summary>
        public void HideHUD()
        {
            if (_canvas != null)
            {
                _canvas.enabled = false;
            }
        }
    }
}
