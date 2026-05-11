
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PokemonAR.Core.Editor
{
    public static class ARSimSetup
    {
        [MenuItem("PokemonAR/Setup AR Simulation in Main Scene")]
        public static void SetupARSimulation()
        {
            // ── Make sure Main.unity is open ─────────────────────────────
            var scene = EditorSceneManager.OpenScene(
                "Assets/Scenes/Main.unity",
                OpenSceneMode.Single);

            // ── Remove old plain camera (we replace it with AR camera rig)
            var oldCam = GameObject.FindWithTag("MainCamera");
            if (oldCam != null && oldCam.name == "Main Camera")
                Object.DestroyImmediate(oldCam);

            // ── 1. AR Session ─────────────────────────────────────────────
            // AR Session + AR Input Manager go on same object
            GameObject existingSession = GameObject.Find("AR Session");
            if (existingSession == null)
            {
                var arSessionGO = new GameObject("AR Session");
                arSessionGO.AddComponent<ARSession>();
                arSessionGO.AddComponent<ARInputManager>();
                Debug.Log("[ARSimSetup] Created AR Session + ARInputManager");
            }

            // ── 2. XR Origin (AR) ─────────────────────────────────────────
            // Structure: XR Origin → Camera Offset → Main Camera (AR Camera)
            GameObject existingOrigin = GameObject.Find("XR Origin");
            if (existingOrigin == null)
            {
                // Root: XR Origin with XROrigin + ARCameraManager + ARPlaneManager
                var xrOriginGO = new GameObject("XR Origin");
                var xrOrigin   = xrOriginGO.AddComponent<XROrigin>();

                // Camera Offset child
                var camOffsetGO = new GameObject("Camera Offset");
                camOffsetGO.transform.SetParent(xrOriginGO.transform, false);

                // Main Camera child (under Camera Offset)
                var arCamGO = new GameObject("Main Camera");
                arCamGO.tag = "MainCamera";
                arCamGO.transform.SetParent(camOffsetGO.transform, false);

                var cam = arCamGO.AddComponent<Camera>();
                cam.clearFlags       = CameraClearFlags.SolidColor;
                cam.backgroundColor  = new Color(0.1f, 0.1f, 0.2f);
                cam.nearClipPlane    = 0.1f;
                cam.farClipPlane     = 20f;

                arCamGO.AddComponent<AudioListener>();
                arCamGO.AddComponent<ARCameraManager>();
                arCamGO.AddComponent<ARCameraBackground>();

                // Wire XROrigin references
                xrOrigin.Camera       = cam;
                xrOrigin.CameraFloorOffsetObject = camOffsetGO;

                // AR plane manager on XR Origin root (Part 4 will use this)
                xrOriginGO.AddComponent<ARPlaneManager>();
                xrOriginGO.AddComponent<ARRaycastManager>();

                Debug.Log("[ARSimSetup] Created XR Origin (AR) rig");
            }

            // ── 3. XR Device Simulator ────────────────────────────────────
            // XR Interaction Toolkit 3.x ships the simulator as a built-in
            // component — no prefab import needed.
            GameObject existingSim = GameObject.Find("XR Device Simulator");
            if (existingSim == null)
            {
                var simGO = new GameObject("XR Device Simulator");
                simGO.AddComponent<XRDeviceSimulator>();
                Debug.Log("[ARSimSetup] Created XR Device Simulator");
            }

            // ── Save ──────────────────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[ARSimSetup] Scene saved. AR simulation setup complete!");
            EditorUtility.DisplayDialog(
                "AR Simulation Setup Complete",
                "Added to Main.unity:\n\n" +
                "✅  AR Session + ARInputManager\n" +
                "✅  XR Origin (AR Camera rig)\n" +
                "   ├─ Camera Offset\n" +
                "   └─ Main Camera (ARCameraManager + ARCameraBackground)\n" +
                "✅  XR Device Simulator\n" +
                "✅  ARPlaneManager + ARRaycastManager\n\n" +
                "NEXT STEP → Edit → Project Settings → XR Plug-in Management\n" +
                "  • Windows, Mac, Linux: enable [XR Simulation] loader\n" +
                "  • Android: enable [ARCore] loader\n" +
                "  • Ensure Automatic Loading is ON for those loaders\n\n" +
                "If UI buttons do not click: menu PokemonAR → Fix EventSystem (Input System).\n\n" +
                "Then press Play and use the XR Device Simulator controls.",
                "Got it!");
        }

        /// <summary>
        /// StandaloneInputModule does not receive pointer input when the project uses
        /// Active Input Handling = Input System Package only.
        /// </summary>
        [MenuItem("PokemonAR/Fix EventSystem (Input System UI)")]
        public static void FixEventSystemForInputSystem()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
            var systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (systems == null || systems.Length == 0)
            {
                EditorUtility.DisplayDialog("Fix EventSystem", "No EventSystem found in Main.unity.", "OK");
                return;
            }

            foreach (var es in systems)
            {
                var standalone = es.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                    Object.DestroyImmediate(standalone);

                if (es.GetComponent<InputSystemUIInputModule>() == null)
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ARSimSetup] Removed StandaloneInputModule and ensured InputSystemUIInputModule on all EventSystems in Main.");
            EditorUtility.DisplayDialog("Fix EventSystem", "EventSystem updated for the Input System package.", "OK");
        }
    }
}
#endif
