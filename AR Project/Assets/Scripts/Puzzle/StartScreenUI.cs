using UnityEngine;
using UnityEngine.UI;

namespace PokemonAR.Core
{
    public class StartScreenUI : MonoBehaviour
    {
        [SerializeField] private GameObject startPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private GameObject puzzleContent;

        private void Awake()
        {
            if (startPanel != null) startPanel.SetActive(true);
            if (puzzleContent != null) puzzleContent.SetActive(false);
        }

        private void Start()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartPressed);
        }

        private void OnStartPressed()
        {
            if (startPanel != null) startPanel.SetActive(false);
            if (puzzleContent != null) puzzleContent.SetActive(true);
            PersistentHUD.Instance?.Show();
        }
    }
}