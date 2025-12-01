using Unity.Burst;
using Unity.Entities;
using PresentationSystemGroup = PureDOTS.Systems.PresentationSystemGroup;

namespace Space4X.Presentation
{
    /// <summary>
    /// System that handles hot-swapping between Minimal and Fancy bindings.
    /// Toggle via command or debug menu.
    /// DISABLED: Using Unity default objects for now. Re-enable when custom visuals are ready.
    /// </summary>
    [DisableAutoCreation]
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(Space4XPresentationAdapterSystem))]
    public partial struct Space4XBindingSwapSystem : ISystem
    {
        private bool _swapRequested;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Load initial bindings (Minimal by default)
            Space4XBindingLoader.LoadBindings(minimal: true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Check for swap request (would come from input/debug command)
            // For now, this is a placeholder structure
            
            if (_swapRequested)
            {
                Space4XBindingLoader.SwapBindings();
                _swapRequested = false;
                
                // Mark all presentation entities as dirty to respawn with new bindings
                // Would add PresentationDirtyTag to relevant entities
            }
        }

        /// <summary>
        /// Request a binding swap (called from non-Burst code).
        /// </summary>
        public static void RequestSwap()
        {
            Space4XBindingLoader.SwapBindings();
        }
    }
}

