using UnityEditor;
using UnityEngine;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
using System.Linq;

/// <summary>
/// One-click tool to configure XR Simulation for editor AR testing.
/// Run via: PokemonAR > Setup AR Simulation
/// </summary>
public class ARSimulationSetup
{
public static void SetupARSimulation()
    {
        Debug.Log("[ARSetup] Checking XR Simulation for Editor...");

        var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Unknown);
        if (generalSettings == null)
        {
            EditorUtility.DisplayDialog("AR Setup",
                "Could not find XR General Settings.\n\nPlease go to:\nEdit > Project Settings > XR Plug-in Management > Editor tab\nand check 'XR Simulation'.", "OK");
            return;
        }

        var manager = generalSettings.Manager;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Active XR Loaders (Editor):");
        if (manager != null)
            foreach (var l in manager.activeLoaders)
                sb.AppendLine($"  - {l.GetType().Name}");

        bool hasSimulation = manager != null && manager.activeLoaders.Any(l =>
            l.GetType().Name.Contains("Simulation") || l.GetType().Name.Contains("ARFoundation"));

        if (hasSimulation)
        {
            sb.AppendLine("\nXR Simulation is ACTIVE. You're good to go!");
            EditorUtility.DisplayDialog("AR Setup", sb.ToString(), "OK");
        }
        else
        {
            sb.AppendLine("\nXR Simulation NOT found in active loaders.\n");
            sb.AppendLine("ACTION REQUIRED:\nEdit > Project Settings > XR Plug-in Management\n> Click the MONITOR icon (Editor tab)\n> Check 'XR Simulation'\n> Uncheck 'OpenXR' if checked");
            EditorUtility.DisplayDialog("AR Setup - Action Required", sb.ToString(), "OK");
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management");
        }
    }

    [MenuItem("PokemonAR/Diagnose Scene")]
    public static void DiagnoseScene()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== AR Scene Diagnostic ===\n");

        // XROrigin
        var xrOrigin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        sb.AppendLine("--- XROrigin ---");
        if (xrOrigin == null) { sb.AppendLine("  MISSING!"); }
        else
        {
            sb.AppendLine($"  Camera: {(xrOrigin.Camera != null ? xrOrigin.Camera.name : "NULL ⚠")}");
            sb.AppendLine($"  CameraFloorOffsetObject: {(xrOrigin.CameraFloorOffsetObject != null ? xrOrigin.CameraFloorOffsetObject.name : "NULL ⚠")}");
            sb.AppendLine($"  TrackablesParent: {(xrOrigin.TrackablesParent != null ? xrOrigin.TrackablesParent.name : "NULL")}");
        }

        // AR Session
        var arSession = Object.FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARSession>();
        sb.AppendLine("\n--- ARSession ---");
        sb.AppendLine(arSession == null ? "  MISSING!" : $"  Found on: {arSession.gameObject.name}");

        // XR Device Simulator
        var sim = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>();
        sb.AppendLine("\n--- XR Device Simulator ---");
        if (sim == null) sb.AppendLine("  Not found");
        else sb.AppendLine($"  Found: {sim.gameObject.name} (active={sim.gameObject.activeInHierarchy})");

        // Canvas
        var canvas = Object.FindFirstObjectByType<Canvas>();
        sb.AppendLine("\n--- Canvas ---");
        if (canvas == null) sb.AppendLine("  MISSING!");
        else sb.AppendLine($"  RenderMode: {canvas.renderMode}");

        // EventSystem
        var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        sb.AppendLine("\n--- EventSystem ---");
        sb.AppendLine(es == null ? "  MISSING!" : $"  Found: {es.name}");

        // XR Management
        sb.AppendLine("\n--- XR Management (Editor) ---");
        var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Unknown);
        if (settings?.Manager != null)
        {
            sb.AppendLine("  Active loaders:");
            foreach (var l in settings.Manager.activeLoaders)
                sb.AppendLine($"    - {l.GetType().Name}");
            if (!settings.Manager.activeLoaders.Any())
                sb.AppendLine("    (none) ⚠ XR Simulation not configured!");
        }
        else sb.AppendLine("  XR Settings not found");

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Scene Diagnostic", sb.ToString(), "OK");
    }

    [MenuItem("PokemonAR/Fix XROrigin Camera Reference")]
    public static void FixXROriginCamera()
    {
        var xrOrigin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin == null) { Debug.LogError("XROrigin not found!"); return; }

        var mainCam = Camera.main;
        if (mainCam == null)
        {
            var camGO = GameObject.FindGameObjectWithTag("MainCamera");
            if (camGO != null) mainCam = camGO.GetComponent<Camera>();
        }

        if (mainCam == null) { Debug.LogError("Main Camera not found!"); return; }

        // Use SerializedObject to set the camera field properly
        var so = new SerializedObject(xrOrigin);
        var cameraProp = so.FindProperty("m_Camera");
        var floorProp = so.FindProperty("m_CameraFloorOffsetObject");

        if (cameraProp != null)
        {
            cameraProp.objectReferenceValue = mainCam;
            Debug.Log($"[ARSetup] Set XROrigin.m_Camera = {mainCam.name}");
        }

        // Camera Offset is the parent of Main Camera
        if (floorProp != null && mainCam.transform.parent != null)
        {
            floorProp.objectReferenceValue = mainCam.transform.parent.gameObject;
            Debug.Log($"[ARSetup] Set XROrigin.m_CameraFloorOffsetObject = {mainCam.transform.parent.name}");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(xrOrigin);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(xrOrigin.gameObject.scene);

        Debug.Log("[ARSetup] XROrigin camera reference fixed! Save the scene (Ctrl+S) and press Play.");
    }
}
