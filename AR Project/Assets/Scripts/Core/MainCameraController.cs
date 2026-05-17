using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokemonAR.Core
{
    // =========================================================================
    //  MainCameraController
    //
    //  Sits on the Main Camera in Main.unity.
    //  Disables itself whenever an additive scene that has its own camera
    //  (Puzzle, Collection, Battle) is loaded, and re-enables when they unload.
    //  This prevents "No cameras rendering" on the main menu and avoids
    //  camera conflicts during gameplay scenes.
    // =========================================================================
    [RequireComponent(typeof(Camera))]
    public class MainCameraController : MonoBehaviour
    {
        private Camera _cam;

        // Scene names that own their own AR/game camera
        private static readonly string[] _scenesWithCamera =
            { "Puzzle", "Collection", "Battle" };

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded   += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            RefreshCamera();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded   -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshCamera();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            RefreshCamera();
        }

        // Enable Main Camera only when no gameplay scene is loaded
        private void RefreshCamera()
        {
            bool gameplaySceneActive = false;
            for (int i = 0; i < SceneManager.loadedSceneCount; i++)
            {
                string name = SceneManager.GetSceneAt(i).name;
                foreach (string s in _scenesWithCamera)
                {
                    if (name == s) { gameplaySceneActive = true; break; }
                }
                if (gameplaySceneActive) break;
            }

            if (_cam != null)
                _cam.enabled = !gameplaySceneActive;

            // Also toggle AudioListener to avoid duplicate warnings
            var listener = GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = !gameplaySceneActive;
        }
    }
}
