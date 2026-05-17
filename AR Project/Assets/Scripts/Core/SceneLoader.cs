using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokemonAR.Core
{
    // =========================================================================
    //  SceneLoader
    //
    //  Listens to GameManager.OnStateChanged and loads/unloads the correct
    //  additive scene for each game phase.
    //
    //  Scene map:
    //    MainMenu   → no additive scene (Main camera handles UI)
    //    Puzzle     → Puzzle.unity  (additive)
    //    Collection → Collection.unity (additive, Puzzle unloaded)
    //    Battle     → Battle.unity  (additive, Collection unloaded)
    //    GameEnd    → no additive scene
    //    GameOver   → no additive scene
    // =========================================================================
    public class SceneLoader : MonoBehaviour
    {
        private const string SCENE_PUZZLE     = "Puzzle";
        private const string SCENE_COLLECTION = "Collection";
        private const string SCENE_BATTLE     = "Battle";

        private string _currentlyLoaded = null;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
            else
            {
                StartCoroutine(WaitForGameManager());
            }
        }

        private IEnumerator WaitForGameManager()
        {
            while (GameManager.Instance == null) yield return null;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState newState)
        {
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
                case GameState.MainMenu:
                case GameState.GameEnd:
                case GameState.GameOver:
                    StartCoroutine(TransitionTo(null));
                    break;
            }
        }

        private IEnumerator TransitionTo(string sceneName)
        {
            // Unload current scene if there is one
            if (!string.IsNullOrEmpty(_currentlyLoaded))
            {
                Scene old = SceneManager.GetSceneByName(_currentlyLoaded);
                if (old.isLoaded)
                {
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(old);
                    while (unload != null && !unload.isDone) yield return null;
                }
                _currentlyLoaded = null;
            }

            // Load new scene if specified
            if (!string.IsNullOrEmpty(sceneName))
            {
                // Check it's not already loaded
                Scene existing = SceneManager.GetSceneByName(sceneName);
                if (!existing.isLoaded)
                {
                    AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    while (load != null && !load.isDone) yield return null;
                }
                _currentlyLoaded = sceneName;
            }
        }
    }
}
