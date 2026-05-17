using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PokemonAR.Core
{
    // =========================================================================
    //  UIStateManager
    //
    //  One HUD root with two sub-groups that swap visibility per scene:
    //    PuzzleHUD     — Score, Progress (1/3), Wrong attempts
    //    CollectionHUD — Pokeballs, Collected count
    //
    //  All labels poll GameManager every frame while active — no events needed.
    // =========================================================================
    public class UIStateManager : MonoBehaviour
    {
        // ── Panels ────────────────────────────────────────────────────────────
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject puzzlePanel;
        [SerializeField] private GameObject collectionPanel;
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject gameEndPanel;
        [SerializeField] private GameObject gameOverPanel;

        // ── HUD root ──────────────────────────────────────────────────────────
        [Header("HUD Root")]
        [SerializeField] private GameObject hudRoot;

        // ── Puzzle HUD group ──────────────────────────────────────────────────
        [Header("Puzzle HUD")]
        [SerializeField] private GameObject puzzleHUD;
        [SerializeField] private Text puzzleScoreLabel;     // "Score: 0"
        [SerializeField] private Text puzzleProgressLabel;  // "Progress: 1/3"
        [SerializeField] private Text puzzleWrongLabel;     // "Wrong: 0"

        // ── Collection HUD group ──────────────────────────────────────────────
        [Header("Collection HUD")]
        [SerializeField] private GameObject collectionHUD;
        [SerializeField] private Text pokeballLabel;        // "Pokeballs: 3"
        [SerializeField] private Text collectedLabel;       // "Collected: 2"

        // ── Result labels ─────────────────────────────────────────────────────
        [Header("Result Labels (optional)")]
        [SerializeField] private Text finalScoreLabel;
        [SerializeField] private Text gameOverScoreLabel;

        // ── Buttons ───────────────────────────────────────────────────────────
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button restartButtonGameEnd;
        [SerializeField] private Button restartButtonGameOver;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (startButton           != null) startButton.onClick.AddListener(OnStartPressed);
            if (restartButtonGameEnd  != null) restartButtonGameEnd.onClick.AddListener(OnRestartPressed);
            if (restartButtonGameOver != null) restartButtonGameOver.onClick.AddListener(OnRestartPressed);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
            else
            {
                ShowOnly(mainMenuPanel);
                SetHUDVisible(false);
                StartCoroutine(WaitForGameManager());
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private IEnumerator WaitForGameManager()
        {
            while (GameManager.Instance == null) yield return null;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            HandleStateChanged(GameManager.Instance.CurrentState);
        }

        // ── State → panel + HUD group ─────────────────────────────────────────
        private void HandleStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    ShowOnly(mainMenuPanel);
                    SetHUDVisible(false);
                    break;

                case GameState.Puzzle:
                    ShowOnly(puzzlePanel);
                    SetHUDVisible(true);
                    SetHUDGroup(showPuzzle: true);
                    break;

                case GameState.Collection:
                    ShowOnly(collectionPanel);
                    SetHUDVisible(true);
                    SetHUDGroup(showPuzzle: false);
                    break;

                case GameState.Battle:
                    ShowOnly(battlePanel);
                    SetHUDVisible(true);
                    // Battle teammate can add their own HUD group here
                    SetHUDGroup(showPuzzle: false);
                    break;

                case GameState.GameEnd:
                    ShowOnly(gameEndPanel);
                    SetHUDVisible(false);
                    RefreshFinalScore(finalScoreLabel);
                    break;

                case GameState.GameOver:
                    ShowOnly(gameOverPanel);
                    SetHUDVisible(false);
                    RefreshFinalScore(gameOverScoreLabel);
                    break;
            }
        }

        // ── Live label refresh ────────────────────────────────────────────────
        private void Update()
        {
            if (GameManager.Instance == null) return;

            GameState s = GameManager.Instance.CurrentState;

            if (s == GameState.Puzzle)
            {
                if (puzzleScoreLabel    != null) puzzleScoreLabel.text    = "Score: "    + GameManager.Instance.Score;
                if (puzzleProgressLabel != null) puzzleProgressLabel.text = "Progress: " + GetPuzzleProgress();
                if (puzzleWrongLabel    != null) puzzleWrongLabel.text    = "Wrong: "    + GetWrongAttempts();
            }
            else if (s == GameState.Collection || s == GameState.Battle)
            {
                if (pokeballLabel  != null) pokeballLabel.text  = "Pokeballs: " + GameManager.Instance.PokeballCount;
                if (collectedLabel != null) collectedLabel.text = "Score: "     + GameManager.Instance.Score;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void ShowOnly(GameObject target)
        {
            SetActive(mainMenuPanel,   mainMenuPanel   == target);
            SetActive(puzzlePanel,     puzzlePanel     == target);
            SetActive(collectionPanel, collectionPanel == target);
            SetActive(battlePanel,     battlePanel     == target);
            SetActive(gameEndPanel,    gameEndPanel    == target);
            SetActive(gameOverPanel,   gameOverPanel   == target);
        }

        private void SetHUDVisible(bool v)
        {
            if (hudRoot != null) hudRoot.SetActive(v);
        }

        private void SetHUDGroup(bool showPuzzle)
        {
            SetActive(puzzleHUD,     showPuzzle);
            SetActive(collectionHUD, !showPuzzle);
        }

        private void SetActive(GameObject go, bool v) { if (go != null) go.SetActive(v); }

        private void RefreshFinalScore(Text label)
        {
            if (label != null && GameManager.Instance != null)
                label.text = "Final Score: " + GameManager.Instance.Score;
        }

        // Pull live data from PuzzleManager if it's in the scene
        private string GetPuzzleProgress()
        {
            var pm = FindObjectOfType<Puzzle.PuzzleManager>();
            return pm != null ? $"{pm.CurrentIndex}/{pm.SequenceLength}" : "0/0";
        }

        private int GetWrongAttempts()
        {
            var pm = FindObjectOfType<Puzzle.PuzzleManager>();
            return pm != null ? pm.WrongAttempts : 0;
        }

        private void OnStartPressed()   => GameManager.Instance?.StartGame();
        private void OnRestartPressed() => GameManager.Instance?.RestartGame();
    }
}
