using UnityEngine;
using PokemonAR.Core;

namespace PokemonAR.Battle
{
    // =========================================================================
    //  BattleController  (Phase 4 — Battle Scene)
    //
    //  YOUR JOB AS INTEGRATOR:
    //    - AR throw detection  → ThrowPokeball()
    //    - Hit detection       → RegisterHit()  (or RegisterHit(customScore))
    //    - Boss HP = 0         → CompletePhase()
    //    - Pokéballs = 0       → TriggerGameOver()  (auto-detected on throw)
    //
    //  HOW YOUR TEAMMATES CALL INTO THIS:
    //    BattleController.Instance.ThrowPokeball();   // consumes 1 ball, returns false if empty
    //    BattleController.Instance.RegisterHit();     // adds scorePerHit points
    //    BattleController.Instance.CompletePhase();   // win
    //    BattleController.Instance.TriggerGameOver(); // lose
    // =========================================================================
    public class BattleController : MonoBehaviour
    {
        public static BattleController Instance { get; private set; }

        [Header("Points added per hit (editable in Inspector)")]
        [SerializeField] private int scorePerHit = 50;

        [Header("Debug — auto-complete after N seconds (0 = disabled)")]
        [SerializeField] private float autoCompleteAfterSeconds = 0f;

        private bool _completed = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Debug.Log("[BattleController] Battle phase started.");

            if (autoCompleteAfterSeconds > 0f)
                Invoke(nameof(CompletePhase), autoCompleteAfterSeconds);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // =====================================================================
        //  PUBLIC API — bridge your teammates call into
        // =====================================================================

        /// Call when the player throws a Pokéball.
        /// Returns false and triggers Game Over automatically if no balls remain.
        public bool ThrowPokeball()
        {
            if (_completed) return false;

            bool success = GameManager.Instance != null && GameManager.Instance.UsePokeball();
            if (!success)
            {
                Debug.Log("[BattleController] No Pokéballs left — Game Over.");
                TriggerGameOver();
            }
            return success;
        }

        /// Call when a thrown ball hits the target.
        /// Pass customScore to override the Inspector value, or leave -1 to use it.
        public void RegisterHit(int customScore = -1)
        {
            if (_completed) return;
            int points = customScore >= 0 ? customScore : scorePerHit;
            GameManager.Instance?.AddScore(points);
            Debug.Log($"[BattleController] Hit registered. +{points} score.");
        }

        /// Call when the boss / enemy is defeated — advances to GameEnd.
        public void CompletePhase()
        {
            if (_completed) return;
            _completed = true;
            Debug.Log("[BattleController] Battle won — handing off to GameManager.");
            GameManager.Instance?.CompletePhase(GameState.Battle);
        }

        /// Call when the player runs out of Pokéballs or time runs out.
        public void TriggerGameOver()
        {
            if (_completed) return;
            _completed = true;
            Debug.Log("[BattleController] Game Over triggered.");
            GameManager.Instance?.TriggerGameOver();
        }
    }
}
