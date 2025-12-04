using PureDOTS.Runtime.Components;
using PureDOTS.Runtime.Config;
using PureDOTS.Runtime.Presentation;
using Unity.Collections;
using Unity.Entities;

namespace PureDOTS.Systems
{
    /// <summary>
    /// Seeds a sample presentation binding set based on runtime config to make graybox visuals available in demos and headless runs.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(PresentationBootstrapSystem))]
    public partial struct PresentationBindingSampleBootstrapSystem : ISystem
    {
        private FixedString64Bytes _lastApplied;
        private bool _dirty;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PresentationCommandQueue>();
            RuntimeConfigRegistry.Initialize();
            _dirty = false;

            if (PresentationBindingConfigVars.BindingSample != null)
            {
                PresentationBindingConfigVars.BindingSample.ValueChanged += OnConfigChanged;
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            if (PresentationBindingConfigVars.BindingSample != null)
            {
                PresentationBindingConfigVars.BindingSample.ValueChanged -= OnConfigChanged;
            }

            // Don't dispose blob assets in OnDestroy - they're owned by components and will be cleaned up by EntityManager
            // Disposing them here can cause "already disposed" errors during world shutdown
            // The EntityManager will handle cleanup of component data including blob references
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!TryEnsureBindingEntity(ref state, out var bindingEntity))
            {
                return;
            }

            var bindingRef = state.EntityManager.GetComponentData<PresentationBindingReference>(bindingEntity);

            if (!bindingRef.Binding.IsCreated)
            {
                _dirty = true;
            }

            var desiredKey = ResolveDesiredKey();
            bool hasExternalBinding = bindingRef.Binding.IsCreated && _lastApplied.IsEmpty;

            if (hasExternalBinding && !_dirty)
            {
                return;
            }

            if (!_dirty && bindingRef.Binding.IsCreated && _lastApplied.Equals(desiredKey))
            {
                return;
            }

            if (!PresentationBindingSamples.TryBuild(desiredKey.ToString(), Allocator.Persistent, out var blob, out var appliedKey))
            {
                return;
            }

            if (bindingRef.Binding.IsCreated)
            {
                bindingRef.Binding.Dispose();
            }

            bindingRef.Binding = blob;
            state.EntityManager.SetComponentData(bindingEntity, bindingRef);
            _lastApplied = appliedKey;
            _dirty = false;
        }

        private bool TryEnsureBindingEntity(ref SystemState state, out Entity bindingEntity)
        {
            if (SystemAPI.TryGetSingletonEntity<PresentationBindingReference>(out bindingEntity))
            {
                return true;
            }

            bindingEntity = state.EntityManager.CreateEntity(typeof(PresentationBindingReference));
            return true;
        }

        private static FixedString64Bytes ResolveDesiredKey()
        {
            var value = PresentationBindingConfigVars.BindingSample != null
                ? PresentationBindingConfigVars.BindingSample.Value
                : "graybox-minimal";

            return new FixedString64Bytes((value ?? string.Empty).ToLowerInvariant());
        }

        private void OnConfigChanged(RuntimeConfigVar _)
        {
            _dirty = true;
        }
    }
}
