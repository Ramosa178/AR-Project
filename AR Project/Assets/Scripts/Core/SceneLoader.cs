using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokemonAR.Core
{
    // =========================================================================
    //  SceneLoader  (Scene Bridge)
    //
    //  WHAT THIS DOES:
    //    - Listens to GameManager.OnStateChanged
    //    - Loads the correct scene additively when a new phase starts
    //    - Unloads the previous scene cleanly before loading the next
    //    - Main.unity (this scene) NEVER unloads
    //
    //  SCENE MAP:
    //    MainMenu   → nothing extra loaded (just Main.unity)
    //    Puzzle     → load Puzzle.unity additively
    //    Collection → unload Puzzle, load Collection.unity additively
    //    Battle     → unload Collection, load Battle.unity additively
    //    GameEnd    → unload Battle (show GameEnd UI in Main canvas)
    //    GameOver   → unload any active phase scene (show GameOver UI in Main canvas)
    //
    //  HOW TO TRIGGER FROM A SCENE CONTROLLER:
    //    GameManager.Instance.CompletePhase(GameState.Puzzle);
    // =========================================================================
    public class SceneLoader : MonoBehaviour
    {
        // ── Scene name constants — must match your actual .unity file names ──
        private const string SCENE_PUZZLE     = "Puzzle";
        private const string SCENE_COLLECTION = "Collection";
        private const string SCENE_BATTLE     = "Battle";

        // ── Internal state ────────────────────────────────────────────────────
        private string _activePhaseScene = null;

        // ── Lifecycle ─────────────────────────────────────────────────────────
private void OnEnable() { /* intentionally empty — subscription happens in Start after all Awake() calls */ }

private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= OnStateChanged;
                GameManager.Instance.OnStateChanged += OnStateChanged;
                Debug.Log("[SceneLoader] Subscribed to GameManager.");
            }
            else
            {
                Debug.LogWarning("[SceneLoader] GameManager not ready — starting poll.");
                StartCoroutine(WaitAndSubscribe());
            }
        }

        private System.Collections.IEnumerator WaitAndSubscribe()
        {
            while (GameManager.Instance == null) yield return null;
            GameManager.Instance.OnStateChanged -= OnStateChanged;
            GameManager.Instance.OnStateChanged += OnStateChanged;
            Debug.Log("[SceneLoader] Subscribed to GameManager (delayed).");
        }


        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnStateChanged;
        }

        // ── State handler ─────────────────────────────────────────────────────
        private void OnStateChanged(GameState newState)
        {
            Debug.Log($"[SceneLoader] State changed to {newState}");

            switch (newState)
            {
                case GameState.Puzzle:
                    StartCoroutine(TransitionTo(SCENE_PUZZLE));
                    break;

                case GameState.Collection:
                    StartCoroutine(TransitionTo(SCENE_COLLECTION));
                    break;

                case GameState.Battle:
                    StartCoroutine(TransitionTo(SCENE_BATTLE));
                    break;

                case GameState.GameEnd:
                case GameState.GameOver:
                    StartCoroutine(UnloadActiveScene());
                    break;

                case GameState.MainMenu:
                    StartCoroutine(UnloadActiveScene());
                    break;
            }
        }

        // ── Coroutines ────────────────────────────────────────────────────────

        /// Unload current phase scene (if any), then load the next one additively.
        private IEnumerator TransitionTo(string sceneName)
        {
            yield return UnloadActiveScene();

            Debug.Log($"[SceneLoader] Loading '{sceneName}' additively...");
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return load;

            _activePhaseScene = sceneName;
            Debug.Log($"[SceneLoader] '{sceneName}' loaded.");
        }

        /// Unload _activePhaseScene if one is loaded.
        private IEnumerator UnloadActiveScene()
        {
            if (string.IsNullOrEmpty(_activePhaseScene)) yield break;

            Scene scene = SceneManager.GetSceneByName(_activePhaseScene);
            if (!scene.isLoaded) { _activePhaseScene = null; yield break; }

            Debug.Log($"[SceneLoader] Unloading '{_activePhaseScene}'...");
            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            yield return unload;

            _activePhaseScene = null;
            Debug.Log("[SceneLoader] Unload complete.");
        }
    }
}
