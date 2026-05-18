using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokemonAR.Core
{
    public class SimpleGameManager : MonoBehaviour
    {
        public static SimpleGameManager Instance { get; private set; }

        public int Score { get; private set; }
        public int PokeballCount { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddScore(int amount) { Score += amount; }
        public void AddPokeballs(int count) { PokeballCount += count; }
        public bool UsePokeball()
        {
            if (PokeballCount <= 0) return false;
            PokeballCount--;
            return true;
        }

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}