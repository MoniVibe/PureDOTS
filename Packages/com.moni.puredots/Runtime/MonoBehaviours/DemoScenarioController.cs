using PureDOTS.Runtime;
using Unity.Entities;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityDebug = UnityEngine.Debug;

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

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(typeof(DemoScenarioState));
            
            if (!query.TryGetSingletonEntity<DemoScenarioState>(out var e))
                return;

            var state = em.GetComponentData<DemoScenarioState>(e);
            bool changed = false;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                state.Current = DemoScenario.AllSystemsShowcase;
                changed = true;
                UnityDebug.Log("[DemoScenarioController] Switched to AllSystemsShowcase (F1)");
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                state.Current = DemoScenario.Space4XPhysicsOnly;
                changed = true;
                UnityDebug.Log("[DemoScenarioController] Switched to Space4XPhysicsOnly (F2)");
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                state.Current = DemoScenario.GodgamePhysicsOnly;
                changed = true;
                UnityDebug.Log("[DemoScenarioController] Switched to GodgamePhysicsOnly (F3)");
            }
            else if (Input.GetKeyDown(KeyCode.F4))
            {
                state.Current = DemoScenario.HandThrowSandbox;
                changed = true;
                UnityDebug.Log("[DemoScenarioController] Switched to HandThrowSandbox (F4)");
            }

            if (changed)
            {
                em.SetComponentData(e, state);
            }
        }
    }
}




