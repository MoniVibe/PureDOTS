#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Space4X.CameraControls;
using Space4X.CameraComponents;
using PureDOTS.Runtime.Components;
using Unity.Mathematics;

namespace Space4X.Editor
{
    /// <summary>
    /// Diagnostic tool to check camera and entity rendering setup.
    /// </summary>
    public static class CameraDiagnostic
    {
        [MenuItem("Space4X/Diagnose Camera & Entities")]
        public static void DiagnoseCameraAndEntities()
        {
            Debug.Log("=== Camera & Entity Rendering Diagnostic ===");

            // Check render pipeline first (CRITICAL for Entities Graphics)
            Debug.Log("\n🔍 Render Pipeline Check:");
            var defaultPipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            var qualityPipeline = QualitySettings.renderPipeline;
            
            if (defaultPipeline == null && qualityPipeline == null)
            {
                Debug.LogError("❌ CRITICAL: NO RENDER PIPELINE ASSIGNED!");
                Debug.LogError("   Entities Graphics requires a Scriptable Render Pipeline (URP/HDRP)");
                Debug.LogError("   Run: Space4X > Fix Render Pipeline (URP Required)");
            }
            else
            {
                var activePipeline = qualityPipeline ?? defaultPipeline;
                Debug.Log($"✅ Render Pipeline: {activePipeline.name} (Type: {activePipeline.GetType().Name})");
                
                // Check if it's URP
                if (activePipeline.GetType().Name.Contains("UniversalRenderPipelineAsset"))
                {
                    Debug.Log("✅ Universal Render Pipeline detected - Entities Graphics should work");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Pipeline type: {activePipeline.GetType().Name} - verify Entities Graphics compatibility");
                }
            }

            // Check camera
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("❌ Main Camera not found! Create a Camera GameObject tagged 'MainCamera'");
                return;
            }

            Debug.Log($"✅ Main Camera found: {camera.name}");
            Debug.Log($"   Position: {camera.transform.position}");
            Debug.Log($"   Rotation: {camera.transform.rotation.eulerAngles}");
            Debug.Log($"   Forward: {camera.transform.forward}");
            Debug.Log($"   Enabled: {camera.enabled}");
            Debug.Log($"   Clear Flags: {camera.clearFlags}");
            Debug.Log($"   Culling Mask: {camera.cullingMask} ({(camera.cullingMask == -1 ? "Everything" : "Custom")})");
            Debug.Log($"   Near: {camera.nearClipPlane}, Far: {camera.farClipPlane}");
            Debug.Log($"   FOV: {camera.fieldOfView}");

            // Check if camera is pointing correctly (should look down toward scene center)
            var forward = camera.transform.forward;
            var expectedForward = new Vector3(0, -0.866f, 0.5f).normalized; // For 60-degree rotation
            var dot = Vector3.Dot(forward, expectedForward);
            
            if (dot < 0.9f)
            {
                Debug.LogWarning($"⚠️ Camera forward vector may be incorrect!");
                Debug.LogWarning($"   Current forward: {forward}");
                Debug.LogWarning($"   Expected forward (approx): {expectedForward}");
                Debug.LogWarning($"   Consider setting rotation to (60, 0, 0)");
            }
            else
            {
                Debug.Log($"✅ Camera orientation looks correct");
            }

            Debug.Log("\n🕹 Input Bridge:");
            if (Space4XCameraInputBridge.TryGetSnapshot(out var bridgeSnapshot))
            {
                Debug.Log($"   Sample Rate: {Space4XCameraInputBridge.SampleRateHz:F1} Hz (Δt {Space4XCameraInputBridge.LastSampleDeltaTime * 1000f:F2} ms)");
                Debug.Log($"   Pan: {bridgeSnapshot.Pan}, Vertical: {bridgeSnapshot.VerticalPan}, Zoom: {bridgeSnapshot.Zoom}, Frame: {bridgeSnapshot.Frame}");
            }
            else
            {
                Debug.Log("   Space4XCameraInputBridge snapshot unavailable (bridge not initialized).");
            }

            if (Space4XCameraMouseController.TryGetLatestState(out var latestState))
            {
                Debug.Log($"   Mono Controller: Position {latestState.Position}, Pitch {math.degrees(latestState.Pitch):F1}°, Yaw {math.degrees(latestState.Yaw):F1}°, Perspective={latestState.PerspectiveMode}");
            }
            else
            {
                Debug.Log("   Mono Controller: inactive (DOTS systems control camera).");
            }

            if (Application.isPlaying)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null && world.IsCreated)
                {
                    var entityManager = world.EntityManager;
                    using var diagQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Space4XCameraDiagnostics>());
                    if (!diagQuery.IsEmptyIgnoreFilter)
                    {
                        var diagnostics = diagQuery.GetSingleton<Space4XCameraDiagnostics>();
                        Debug.Log($"   Diagnostics: Frame {diagnostics.FrameId}, Ticks {diagnostics.TicksThisFrame} (Catch-up {diagnostics.CatchUpTicks}), StaleTicks {diagnostics.InputStaleTicks}");
                        Debug.Log($"   Ownership: Mono={diagnostics.MonoControllerActive}, MovementHandled={diagnostics.MovementHandledExternally}, RotationHandled={diagnostics.RotationHandledExternally}");
                        Debug.Log($"   Budget: Rotate {diagnostics.PendingRotateBudget}, Zoom {diagnostics.PendingZoomBudget:F3}, TicksRemaining {diagnostics.BudgetTicksRemaining}");
                    }

                    using var villagerDiagQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VillagerJobDiagnostics>());
                    if (!villagerDiagQuery.IsEmptyIgnoreFilter)
                    {
                        var jobDiagnostics = villagerDiagQuery.GetSingleton<VillagerJobDiagnostics>();
                        Debug.Log($"\n🧑‍🌾 Villager Jobs: Frame {jobDiagnostics.Frame}, Total {jobDiagnostics.TotalVillagers}, Idle {jobDiagnostics.IdleVillagers}, Assigned {jobDiagnostics.AssignedVillagers}, Pending {jobDiagnostics.PendingRequests}, Active {jobDiagnostics.ActiveTickets}");
                    }
                }
            }
 
            // Check entities (if in play mode)
            if (Application.isPlaying)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null && world.IsCreated)
                {
                    var entityManager = world.EntityManager;

                    // Count entities with transforms
                    var transformQuery = entityManager.CreateEntityQuery(typeof(LocalToWorld));
                    var totalEntities = transformQuery.CalculateEntityCount();
                    Debug.Log($"\n📊 Entity Statistics:");
                    Debug.Log($"   Total entities with transforms: {totalEntities}");

                    // Count entities with rendering components
                    var renderQuery = entityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<MaterialMeshInfo>(),
                        ComponentType.ReadOnly<LocalToWorld>()
                    );
                    var renderableEntities = renderQuery.CalculateEntityCount();
                    Debug.Log($"   Entities with rendering components: {renderableEntities}");

                    if (renderableEntities == 0 && totalEntities > 0)
                    {
                        Debug.LogError($"❌ CRITICAL: {totalEntities} entities exist but NONE have rendering components!");
                        Debug.LogError("   Entities need MaterialMeshInfo, RenderBounds, etc. to be visible.");
                        Debug.LogError("   Ensure GameObjects have MeshRenderer/MeshFilter before baking.");
                    }
                    else if (renderableEntities > 0)
                    {
                        Debug.Log($"✅ {renderableEntities} entities should be visible");
                    }

                    // Check if entities are in camera view
                    if (renderableEntities > 0)
                    {
                        var entities = renderQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                        int visibleCount = 0;
                        for (int i = 0; i < Mathf.Min(entities.Length, 10); i++)
                        {
                            if (entityManager.HasComponent<LocalToWorld>(entities[i]))
                            {
                                var localToWorld = entityManager.GetComponentData<LocalToWorld>(entities[i]);
                                // Extract position from LocalToWorld matrix (c3 column contains translation)
                                var worldPos = new Vector3(
                                    localToWorld.Value.c3.x,
                                    localToWorld.Value.c3.y,
                                    localToWorld.Value.c3.z
                                );
                                
                                // Check if in camera frustum (rough check)
                                var viewportPos = camera.WorldToViewportPoint(worldPos);
                                if (viewportPos.x >= 0 && viewportPos.x <= 1 && 
                                    viewportPos.y >= 0 && viewportPos.y <= 1 &&
                                    viewportPos.z > 0 && viewportPos.z < camera.farClipPlane)
                                {
                                    visibleCount++;
                                }
                            }
                        }
                        entities.Dispose();
                        Debug.Log($"   Entities potentially in view (sample): {visibleCount}/10");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ DOTS World not initialized (may be normal if just entered play mode)");
                }
            }
            else
            {
                Debug.Log("\n⚠️ Not in Play Mode - Cannot check entities");
                Debug.Log("   Enter Play Mode to see entity statistics");
            }

            Debug.Log("\n=== Diagnostic Complete ===");
        }

        [MenuItem("Space4X/Fix Camera Orientation")]
        public static void FixCameraOrientation()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("Main Camera not found!");
                return;
            }

            Undo.RecordObject(camera.transform, "Fix Camera Orientation");
            camera.transform.position = new Vector3(0, 15, -20);
            camera.transform.rotation = Quaternion.Euler(60, 0, 0);
            
            Debug.Log("✅ Camera orientation fixed:");
            Debug.Log($"   Position: {camera.transform.position}");
            Debug.Log($"   Rotation: {camera.transform.rotation.eulerAngles}");
            Debug.Log($"   Forward: {camera.transform.forward}");
        }
    }
}
#endif

