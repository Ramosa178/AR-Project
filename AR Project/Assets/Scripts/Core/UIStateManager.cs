using UnityEngine;
using UnityEngine.UI;   // covers both Button and Text — no extra packages needed

namespace PokemonAR.Core
{
    // =========================================================================
    //  UIStateManager  (Part 1 — Core Systems & Game Manager)
    //
    //  Uses standard UnityEngine.UI.Text — no TextMeshPro required.
    //
    //  SETUP IN INSPECTOR:
    //    1. Add this script to the Canvas GameObject in Main.unity
    //    2. Drag each Panel into its matching slot
    //    3. Drag the two Text objects (score / pokeball) into their slots
    //    4. Drag Start and Restart buttons into the button slots
    // =========================================================================
    public class UIStateManager : MonoBehaviour
    {
        // ── Panels ────────────────────────────────────────────────────────────
        [Header("Panels — one per GameState, assign in Inspector")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject puzzlePanel;
        [SerializeField] private GameObject collectionPanel;
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject gameEndPanel;
        [SerializeField] private GameObject gameOverPanel;

        // ── HUD ───────────────────────────────────────────────────────────────
        [Header("HUD — visible during Puzzle / Collection / Battle")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private Text       scoreLabel;       // UI > Text
        [SerializeField] private Text       pokeballLabel;    // UI > Text

        // ── Buttons ───────────────────────────────────────────────────────────
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button restartButtonGameEnd;
        [SerializeField] private Button restartButtonGameOver;

        // ── Result screen labels ───────────────────────────────────────────────
        [Header("Result screen labels (optional)")]
        [SerializeField] private Text finalScoreLabel;      // inside GameEndPanel
        [SerializeField] private Text gameOverScoreLabel;   // inside GameOverPanel


        // =========================================================================
        //  PART 2 NOTE — Puzzle UI
        //  Add your puzzle UI children (number grid, feedback overlays, timer)
        //  inside the PuzzlePanel GameObject in the Hierarchy.
        //  This script handles showing/hiding the panel — you don't touch that.
        // =========================================================================

        // =========================================================================
        //  PART 3 NOTE — Collection UI
        //  Add your timer or ball-count display as children of CollectionPanel.
        //  The HUD already shows PokeballCount live — no extra counter needed
        //  unless you want a different visual style.
        // =========================================================================

        // =========================================================================
        //  PART 4 NOTE — Battle UI
        //  Add your boss HP bar, throw button, Pokémon selector etc. as children
        //  of BattlePanel. The HUD tracks Score and PokeballCount automatically.
        //  GameEndPanel and GameOverPanel display the final score automatically.
        // =========================================================================


        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (startButton           != null) startButton.onClick.AddListener(OnStartPressed);
            if (restartButtonGameEnd  != null) restartButtonGameEnd.onClick.AddListener(OnRestartPressed);
            if (restartButtonGameOver != null) restartButtonGameOver.onClick.AddListener(OnRestartPressed);
        }

private void OnEnable()
        {
            // Subscribe defensively — GameManager may or may not exist yet
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

private void Start()
        {
            // Re-subscribe here so we never miss it even if OnEnable fired too early.
            // Remove first to prevent double-subscription if OnEnable already ran.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
            else
            {
                ShowOnly(mainMenuPanel);
                // Poll until GameManager initialises (handles DontDestroyOnLoad ordering edge cases)
                StartCoroutine(WaitForGameManager());
            }
        }

private System.Collections.IEnumerator WaitForGameManager()
        {
            while (GameManager.Instance == null)
                yield return null;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            HandleStateChanged(GameManager.Instance.CurrentState);
        }


        // ── State → panel ─────────────────────────────────────────────────────
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
                    break;
                case GameState.Collection:
                    ShowOnly(collectionPanel);
                    SetHUDVisible(true);
                    break;
                case GameState.Battle:
                    ShowOnly(battlePanel);
                    SetHUDVisible(true);
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

        // ── HUD live refresh ──────────────────────────────────────────────────
        private void Update()
        {
            if (GameManager.Instance == null) return;

            GameState s = GameManager.Instance.CurrentState;
            bool active = s == GameState.Puzzle ||
                          s == GameState.Collection ||
                          s == GameState.Battle;
            if (!active) return;

            if (scoreLabel    != null) scoreLabel.text    = "Score: "     + GameManager.Instance.Score;
            if (pokeballLabel != null) pokeballLabel.text = "Pokeballs: " + GameManager.Instance.PokeballCount;
        }

        // ── Button callbacks ──────────────────────────────────────────────────
        private void OnStartPressed()   => GameManager.Instance?.StartGame();
        private void OnRestartPressed() => GameManager.Instance?.RestartGame();

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

        private void SetHUDVisible(bool v)              { if (hudRoot != null) hudRoot.SetActive(v); }
        private void SetActive(GameObject go, bool v)   { if (go != null) go.SetActive(v); }

        private void RefreshFinalScore(Text label)
        {
            if (label != null && GameManager.Instance != null)
                label.text = "Final Score: " + GameManager.Instance.Score;
        }
    }
}