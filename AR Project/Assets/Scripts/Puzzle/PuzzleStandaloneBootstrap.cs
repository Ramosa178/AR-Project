using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace PokemonAR.Puzzle
{
    public static class PuzzleStandaloneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            if (SceneManager.GetActiveScene().name != "Puzzle")
                return;

            var runner = new GameObject("PuzzleStandaloneBootstrapRunner");
            Object.DontDestroyOnLoad(runner);
            runner.AddComponent<Runner>();
        }

        private class Runner : MonoBehaviour
        {
            private IEnumerator Start()
            {
                // Keep this as an iterator on all platforms/symbol sets.
                yield return null;

                ARSession arSession = FindFirstObjectByType<ARSession>(FindObjectsInactive.Include);
                Camera arCamera = Camera.main;
                if (arCamera != null)
                {
                    // Avoid permanent black frame if camera permission is pending/denied.
                    arCamera.clearFlags = CameraClearFlags.Skybox;
                    arCamera.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 1f);
                }

#if UNITY_ANDROID && !UNITY_EDITOR
                // Hard fix: ensure XR loader is initialized/started even in Puzzle-only builds.
                if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
                {
                    var xrManager = XRGeneralSettings.Instance.Manager;
                    if (xrManager.activeLoader == null)
                    {
                        Debug.Log("[PuzzleStandaloneBootstrap] Initializing XR loader...");
                        yield return xrManager.InitializeLoader();
                    }
                    if (xrManager.activeLoader != null)
                    {
                        Debug.Log("[PuzzleStandaloneBootstrap] Starting XR subsystems...");
                        xrManager.StartSubsystems();
                    }
                    else
                    {
                        Debug.LogError("[PuzzleStandaloneBootstrap] XR loader failed to initialize.");
                    }
                }

                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                {
                    UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);

                    float timeout = 10f;
                    while (timeout > 0f &&
                           !UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                    {
                        timeout -= Time.unscaledDeltaTime;
                        yield return null;
                    }
                }

                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                {
                    Debug.LogError("[PuzzleStandaloneBootstrap] Camera permission denied.");
                    // Continue running so user can still interact with non-camera content.
                    // Ask user to enable camera permission from app settings for AR feed.
                }
#endif
                if (arSession != null)
                {
                    arSession.enabled = true;
                    arSession.Reset();
                }

                Destroy(gameObject);
                yield break;
            }
        }
    }
}
