using PureDOTS.Runtime;
using Unity.Entities;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PureDOTS.Runtime.MonoBehaviours
{
    /// <summary>
    /// MonoBehaviour controller for switching demo scenarios via F1-F4 hotkeys.
    /// Attach to a GameObject in the showcase scene.
    /// </summary>
    public class DemoScenarioController : MonoBehaviour
    {
        private void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;
#else
            return;
#endif

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(DemoScenarioState));
            
            if (!query.TryGetSingletonEntity<DemoScenarioState>(out var e))
                return;

            var state = em.GetComponentData<DemoScenarioState>(e);
            bool changed = false;

#if ENABLE_INPUT_SYSTEM
            if (kb.f1Key.wasPressedThisFrame)
            {
                state.Current = DemoScenario.AllSystemsShowcase;
                changed = true;
            }
            else if (kb.f2Key.wasPressedThisFrame)
            {
                state.Current = DemoScenario.Space4XPhysicsOnly;
                changed = true;
            }
            else if (kb.f3Key.wasPressedThisFrame)
            {
                state.Current = DemoScenario.GodgamePhysicsOnly;
                changed = true;
            }
            else if (kb.f4Key.wasPressedThisFrame)
            {
                state.Current = DemoScenario.HandThrowSandbox;
                changed = true;
            }
#endif

            if (changed)
            {
                em.SetComponentData(e, state);
            }
        }
    }
}

