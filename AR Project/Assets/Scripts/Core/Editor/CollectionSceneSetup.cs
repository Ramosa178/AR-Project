
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PokemonAR.Core.Editor
{
    public static class CollectionSceneSetup
    {
        [MenuItem("PokemonAR/Fix Collection Scene XR Origin")]
        public static void FixCollectionScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Collection.unity", OpenSceneMode.Single);

            // Find XR Origin
            var xrOrigin = Object.FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("[CollectionSceneSetup] XROrigin not found in Collection scene!");
                EditorUtility.DisplayDialog("Error", "XROrigin not found in Collection scene.", "OK");
                return;
            }

            // Find Camera Offset
            var cameraOffsetTransform = xrOrigin.transform.Find("Camera Offset");
            if (cameraOffsetTransform == null)
            {
                Debug.LogError("[CollectionSceneSetup] 'Camera Offset' child not found under XR Origin!");
                EditorUtility.DisplayDialog("Error", "'Camera Offset' child not found under XR Origin.", "OK");
                return;
            }

            // Find Main Camera under Camera Offset
            var mainCamTransform = cameraOffsetTransform.Find("Main Camera");
            if (mainCamTransform == null)
            {
                Debug.LogError("[CollectionSceneSetup] 'Main Camera' not found under Camera Offset!");
                EditorUtility.DisplayDialog("Error", "'Main Camera' not found under Camera Offset.", "OK");
                return;
            }

            var cam = mainCamTransform.GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("[CollectionSceneSetup] Camera component missing on Main Camera!");
                EditorUtility.DisplayDialog("Error", "Camera component missing on Main Camera.", "OK");
                return;
            }

            // Ensure AR components exist on the camera
            if (mainCamTransform.GetComponent<ARCameraManager>() == null)
                mainCamTransform.gameObject.AddComponent<ARCameraManager>();
            if (mainCamTransform.GetComponent<ARCameraBackground>() == null)
                mainCamTransform.gameObject.AddComponent<ARCameraBackground>();

            // Wire XROrigin via SerializedObject (the only reliable way in-editor)
            var so = new SerializedObject(xrOrigin);
            so.FindProperty("m_Camera").objectReferenceValue = cam;
            so.FindProperty("m_CameraFloorOffsetObject").objectReferenceValue = cameraOffsetTransform.gameObject;
            so.FindProperty("m_RequestedTrackingOriginMode").intValue = 1; // Floor
            so.ApplyModifiedProperties();

            // Make sure camera tag is set
            mainCamTransform.gameObject.tag = "MainCamera";

            // Ensure ARTrackedImageManager is on XR Origin
            var trackedImageMgr = xrOrigin.GetComponent<ARTrackedImageManager>();
            if (trackedImageMgr == null)
            {
                trackedImageMgr = xrOrigin.gameObject.AddComponent<ARTrackedImageManager>();
                Debug.Log("[CollectionSceneSetup] Added ARTrackedImageManager to XR Origin.");
            }

            // Ensure PokeballSpawner is on XR Origin
            var spawner = xrOrigin.GetComponent<PokemonAR.Collection.PokeballSpawner>();
            if (spawner == null)
            {
                xrOrigin.gameObject.AddComponent<PokemonAR.Collection.PokeballSpawner>();
                Debug.Log("[CollectionSceneSetup] Added PokeballSpawner to XR Origin.");
            }

            // Ensure CollectionController exists
            var cc = Object.FindObjectOfType<PokemonAR.Collection.CollectionController>();
            if (cc == null)
            {
                var go = new GameObject("CollectionController");
                go.AddComponent<PokemonAR.Collection.CollectionController>();
                Debug.Log("[CollectionSceneSetup] Created CollectionController GameObject.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[CollectionSceneSetup] Collection scene fixed and saved.");
            EditorUtility.DisplayDialog(
                "Collection Scene Fixed",
                "XROrigin.Camera and CameraFloorOffsetObject are now wired.\n\n" +
                "Still to do manually in the Inspector:\n" +
                "1. Select XR Origin -> ARTrackedImageManager -> assign PokeballMarkerLibrary\n" +
                "2. Select XR Origin -> PokeballSpawner -> assign Pokeball prefab\n\n" +
                "Then build and test.",
                "OK");
        }
    }
}
#endif
