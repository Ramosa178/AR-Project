using UnityEngine;
using PokemonAR.Core;

namespace PokemonAR.Puzzle
{
    // =========================================================================
    //  PuzzleController  (Phase 2 — Puzzle Scene)
    //
    //  YOUR JOB AS INTEGRATOR:
    //    When the puzzle logic from your teammate is done, they call:
    //      PuzzleController.Instance.CompletePhase();
    //    That automatically fires GameManager → SceneLoader → loads Collection.unity
    //
    //  HOW YOUR TEAMMATES CALL INTO THIS:
    //    From anywhere:  PuzzleController.Instance.CompletePhase();
    //    From Inspector: wire a UnityEvent to PuzzleController.CompletePhase
    //
    //  DEBUG SHORTCUT:
    //    Set autoCompleteAfterSeconds > 0 in the Inspector to auto-advance.
    //    Or press [2] in Editor (handled by GameManager keyboard shortcuts).
    // =========================================================================
    public class PuzzleController : MonoBehaviour
    {
        public static PuzzleController Instance { get; private set; }

        [Header("Debug — auto-complete after N seconds (0 = disabled)")]
        [SerializeField] private float autoCompleteAfterSeconds = 0f;

        private bool _completed = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Debug.Log("[PuzzleController] Puzzle phase started.");

            if (autoCompleteAfterSeconds > 0f)
                Invoke(nameof(CompletePhase), autoCompleteAfterSeconds);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // =====================================================================
        //  PUBLIC API — this is the bridge your teammates call into
        // =====================================================================

        /// Call this when the puzzle is solved. Safe to call multiple times.
        public void CompletePhase()
        {
            if (_completed) return;
            _completed = true;
            Debug.Log("[PuzzleController] Puzzle complete — handing off to GameManager.");
        }
    }
}
