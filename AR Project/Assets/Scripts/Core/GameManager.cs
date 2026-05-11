using System;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace PokemonAR.Core
{
    // =========================================================================
    //  GAME STATES  —  shared enum used by every part of the project.
    //  DO NOT rename or remove values without telling the whole team.
    // =========================================================================
    public enum GameState
    {
        MainMenu,
        Puzzle,
        Collection,
        Battle,
        GameEnd,
        GameOver
    }

    // =========================================================================
    //  GameManager  (Part 1 — Core Systems & Game Manager)
    //
    //  WHAT THIS DOES:
    //    - Owns the current GameState (ONLY class allowed to change it)
    //    - Tracks global variables: Score and PokeballCount
    //    - Fires OnStateChanged so every other system can react without coupling
    //    - Survives scene loads via DontDestroyOnLoad
    //    - Lives on a single persistent GameObject in Main.unity
    //
    //  HOW TO REACH THIS FROM ANY SCRIPT:
    //    GameManager.Instance.SomeMethod();
    // =========================================================================
    public class GameManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Inspector tunables ────────────────────────────────────────────────
        [Header("Starting values — editable in Inspector for balancing")]
        [SerializeField] private int startingPokeballCount = 0;
        [SerializeField] private int startingScore         = 0;

        // ── Global read-only state ────────────────────────────────────────────
        public GameState CurrentState  { get; private set; } = GameState.MainMenu;
        public int        Score         { get; private set; }
        public int        PokeballCount { get; private set; }

        // ── Event system ──────────────────────────────────────────────────────
        // Subscribe:  GameManager.Instance.OnStateChanged += HandleState;
        // Unsubscribe: GameManager.Instance.OnStateChanged -= HandleState;
        public event Action<GameState> OnStateChanged;

        [Header("Optional: wire state callbacks directly in Inspector")]
        public UnityEvent<GameState> OnStateChangedUnityEvent;

        // =========================================================================
        //  PART 2 — Puzzle: call CompletePhase(GameState.Puzzle) when done
        //  PART 3 — Collection: call AddPokeballs(1) per ball, then CompletePhase(GameState.Collection)
        //  PART 4 — Battle: call UsePokeball() before throw, AddScore(n) on hit,
        //           CompletePhase(GameState.Battle) to win, TriggerGameOver() to lose
        // =========================================================================

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResetGlobals();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void StartGame()
        {
            ResetGlobals();
            TransitionTo(GameState.Puzzle);
        }

        public void CompletePhase(GameState completedPhase)
        {
            if (completedPhase != CurrentState)
            {
                Debug.LogWarning($"[GameManager] CompletePhase({completedPhase}) ignored — current state is {CurrentState}.");
                return;
            }
            switch (completedPhase)
            {
                case GameState.Puzzle:     TransitionTo(GameState.Collection); break;
                case GameState.Collection: TransitionTo(GameState.Battle);     break;
                case GameState.Battle:     TransitionTo(GameState.GameEnd);    break;
                default:
                    Debug.LogWarning($"[GameManager] No successor defined for {completedPhase}.");
                    break;
            }
        }

        public void TriggerGameOver() => TransitionTo(GameState.GameOver);
        public void RestartGame()     => StartGame();

        public void AddScore(int amount)
        {
            if (amount <= 0) return;
            Score += amount;
            Debug.Log($"[GameManager] Score +{amount} => {Score}");
        }

        public void AddPokeballs(int count)
        {
            if (count <= 0) return;
            PokeballCount += count;
            Debug.Log($"[GameManager] Pokeballs +{count} => {PokeballCount}");
        }

        public bool UsePokeball()
        {
            if (PokeballCount <= 0)
            {
                Debug.Log("[GameManager] No Pokeballs left!");
                return false;
            }
            PokeballCount--;
            Debug.Log($"[GameManager] Pokeball used => {PokeballCount} remaining");
            return true;
        }

        // ── Internal helpers ──────────────────────────────────────────────────
        private void TransitionTo(GameState newState)
        {
            Debug.Log($"[GameManager] {CurrentState} => {newState}");
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
            OnStateChangedUnityEvent?.Invoke(newState);
        }

        private void ResetGlobals()
        {
            Score         = startingScore;
            PokeballCount = startingPokeballCount;
        }

#if UNITY_EDITOR
        // ── EDITOR-ONLY keyboard shortcuts (new Input System) ─────────────────
        // 1 = Start  2 = Complete Puzzle  3 = Complete Collection (+5 balls)
        // 4 = Complete Battle (+100 score)  0 = Game Over  R = Restart
        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) StartGame();

            if (kb.digit2Key.wasPressedThisFrame && CurrentState == GameState.Puzzle)
                CompletePhase(GameState.Puzzle);

            if (kb.digit3Key.wasPressedThisFrame && CurrentState == GameState.Collection)
            {
                AddPokeballs(5);
                CompletePhase(GameState.Collection);
            }

            if (kb.digit4Key.wasPressedThisFrame && CurrentState == GameState.Battle)
            {
                AddScore(100);
                CompletePhase(GameState.Battle);
            }

            if (kb.digit0Key.wasPressedThisFrame) TriggerGameOver();
            if (kb.rKey.wasPressedThisFrame)      RestartGame();
        }
#endif
    }
}
