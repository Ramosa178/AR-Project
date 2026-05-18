using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokemonAR.Core
{
    public class SceneTransition : MonoBehaviour
    {
        [SerializeField] private string nextSceneName;
        [SerializeField] private int pokeballsRequiredToComplete = 0;

        private void Start()
        {
            Debug.Log($"[SceneTransition] Ready. Next scene: '{nextSceneName}'");
        }

        public void GoToNextScene()
        {
            Debug.Log($"[SceneTransition] GoToNextScene called! Loading '{nextSceneName}'");
            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }

        public void CollectPokeball()
        {
            SimpleGameManager.Instance?.AddPokeballs(1);
            SimpleGameManager.Instance?.AddScore(10);

            if (pokeballsRequiredToComplete > 0 &&
                SimpleGameManager.Instance?.PokeballCount >= pokeballsRequiredToComplete)
                GoToNextScene();
        }
    }
}