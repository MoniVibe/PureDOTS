using UnityEngine;
using PureDOTS.Core;

namespace PureDOTS.Runtime.Core
{
    /// <summary>
    /// MonoBehaviour wrapper to call SimulationRunner.UpdateAllWorlds from Unity's Update loop.
    /// </summary>
    public class SimulationRunnerMono : MonoBehaviour
    {
        private void Update()
        {
            SimulationRunner.UpdateAllWorlds(Time.deltaTime);
        }
    }
}

