using UnityEngine;
using TMPro;

namespace PokemonAR.Core
{
    public class PersistentHUD : MonoBehaviour
    {
        public static PersistentHUD Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI pokeballText;
        [SerializeField] private GameObject hudRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (SimpleGameManager.Instance == null) return;
            if (scoreText != null)
                scoreText.text = "Score: " + SimpleGameManager.Instance.Score;
            if (pokeballText != null)
                pokeballText.text = "Balls: " + SimpleGameManager.Instance.PokeballCount;
        }

        public void Show() { if (hudRoot != null) hudRoot.SetActive(true); }
        public void Hide() { if (hudRoot != null) hudRoot.SetActive(false); }
    }
}