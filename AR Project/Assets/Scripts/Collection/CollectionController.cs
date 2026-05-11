using UnityEngine;
using PokemonAR.Core;

namespace PokemonAR.Collection
{
    // =========================================================================
    //  CollectionController  (Phase 3 — Collection Scene)
    //
    //  YOUR JOB AS INTEGRATOR:
    //    - Your AR teammate calls AddPokeball() each time a ball is grabbed
    //    - Call CompletePhase() when collection ends (timer up, all balls found,
    //      or whatever the win condition is)
    //
    //  HOW YOUR TEAMMATES CALL INTO THIS:
    //    CollectionController.Instance.AddPokeball();      // each ball grabbed
    //    CollectionController.Instance.CompletePhase();    // phase done
    //
    //  AUTO-ADVANCE:
    //    Set targetPokeballCount > 0 → auto-completes once that many are collected.
    //    Set autoCompleteAfterSeconds > 0 → auto-completes after a timer.
    // =========================================================================
    public class CollectionController : MonoBehaviour
    {
        public static CollectionController Instance { get; private set; }

        [Header("Auto-complete when this many Pokéballs collected (0 = manual)")]
        [SerializeField] private int targetPokeballCount = 0;

        [Header("Debug — auto-complete after N seconds (0 = disabled)")]
        [SerializeField] private float autoCompleteAfterSeconds = 0f;

        private int  _collectedThisPhase = 0;
        private bool _completed          = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Debug.Log("[CollectionController] Collection phase started.");

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

        /// Call every time the player grabs a Pokéball.
        public void AddPokeball(int count = 1)
        {
            if (_completed) return;

            GameManager.Instance?.AddPokeballs(count);
            _collectedThisPhase += count;
            Debug.Log($"[CollectionController] +{count} ball(s). Phase total: {_collectedThisPhase}");

            if (targetPokeballCount > 0 && _collectedThisPhase >= targetPokeballCount)
                CompletePhase();
        }

        /// Call when the collection phase is finished.
        public void CompletePhase()
        {
            if (_completed) return;
            _completed = true;
            Debug.Log("[CollectionController] Collection complete — handing off to GameManager.");
            GameManager.Instance?.CompletePhase(GameState.Collection);
        }
    }
}
