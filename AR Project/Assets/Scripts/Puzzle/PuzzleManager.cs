using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PokemonAR.Core;

namespace PokemonAR.Puzzle
{
    public class PuzzleManager : MonoBehaviour
    {
        [System.Serializable]
        public class IntIntEvent : UnityEvent<int, int> { }
        [System.Serializable]
        public class IntEvent : UnityEvent<int> { }

        [Header("Sequence (assignment expects 3-5 values)")]
        [SerializeField] private List<int> correctSequence = new() { 1, 2, 3 };

        [Header("Behavior")]
        [SerializeField] private float wrongFlashDurationSeconds = 0.5f;
        [SerializeField] private bool lockInputWhenSolved = true;
        [SerializeField] private float delayBeforeTransition = 1.5f;
        [SerializeField] private bool completeOnAnyTap = false;

        [Header("Scoring")]
        [SerializeField] private int pointsPerCorrectTap = 10;
        [SerializeField] private int penaltyPerWrongTap = 5;
        [SerializeField] private bool clampScoreAtZero = true;
        [SerializeField] private bool syncScoreToGameManager = true;

        [Header("Hint")]
        [SerializeField] private string hintMessage = "Arrange the life stages from youngest to oldest: Egg -> Baby Dragon -> Fairy Dragon.";

        [Header("Events")]
        [SerializeField] private UnityEvent onPuzzleCompleted;
        [SerializeField] private IntIntEvent onProgressChanged;
        [SerializeField] private UnityEvent onWrongInput;
        [SerializeField] private IntEvent onScoreChanged;

        private readonly HashSet<NumberObjectController> _activeNumbers = new();
        private Coroutine _wrongRoutine;
        private int _currentIndex;
        private int _score;
        private int _wrongAttempts;
        private bool _inputLocked;
        private bool _isComplete;

        public bool IsComplete => _isComplete;
        public int CurrentIndex => _currentIndex;
        public int SequenceLength => correctSequence?.Count ?? 0;
        public int Score => _score;
        public int WrongAttempts => _wrongAttempts;
        public string HintMessage => hintMessage;
        public UnityEvent OnPuzzleCompleted => onPuzzleCompleted;

        private void Start()
        {
            NotifyProgress();
        }

        private void OnValidate()
        {
            if (correctSequence == null) return;
            if (correctSequence.Count < 3 || correctSequence.Count > 5)
                Debug.LogWarning("[PuzzleManager] Sequence should usually contain 3-5 values for this assignment.");
        }

        public void RegisterNumber(NumberObjectController numberObject)
        {
            if (numberObject == null) return;
            _activeNumbers.Add(numberObject);
            numberObject.SetIdle();
            Debug.Log($"[PuzzleManager] Registered puzzle object '{numberObject.name}' value={numberObject.NumberValue}. Active count={_activeNumbers.Count}");
        }

        public void UnregisterNumber(NumberObjectController numberObject)
        {
            if (numberObject == null) return;
            _activeNumbers.Remove(numberObject);
        }

        public bool TryInput(NumberObjectController tappedNumber)
        {
            if (tappedNumber == null || _inputLocked || _isComplete) return false;
            if (correctSequence == null || correctSequence.Count == 0)
            {
                Debug.LogWarning("[PuzzleManager] correctSequence is empty.");
                return false;
            }

            if (completeOnAnyTap)
            {
                tappedNumber.ShowCorrect();
                _currentIndex = Mathf.Min(1, correctSequence.Count);
                ApplyScoreDelta(pointsPerCorrectTap);
                NotifyProgress();
                CompletePuzzle();
                return true;
            }

            int expected = correctSequence[_currentIndex];
            if (tappedNumber.NumberValue == expected)
            {
                tappedNumber.ShowCorrect();
                _currentIndex++;
                ApplyScoreDelta(pointsPerCorrectTap);
                NotifyProgress();

                if (_currentIndex >= correctSequence.Count)
                    CompletePuzzle();

                return true;
            }

            if (_wrongRoutine != null)
                StopCoroutine(_wrongRoutine);
            _wrongRoutine = StartCoroutine(HandleWrongInput());
            return false;
        }

        public void ResetPuzzleState()
        {
            if (_wrongRoutine != null)
            {
                StopCoroutine(_wrongRoutine);
                _wrongRoutine = null;
            }

            _inputLocked = false;
            _isComplete = false;
            _currentIndex = 0;
            _score = 0;
            _wrongAttempts = 0;

            foreach (NumberObjectController numberObject in _activeNumbers)
                if (numberObject != null)
                    numberObject.SetIdle();

            NotifyProgress();
        }

        private IEnumerator HandleWrongInput()
        {
            _inputLocked = true;
            _wrongAttempts++;
            ApplyScoreDelta(-penaltyPerWrongTap);
            onWrongInput?.Invoke();

            foreach (NumberObjectController numberObject in _activeNumbers)
                if (numberObject != null)
                    StartCoroutine(numberObject.FlashWrongAndReset(wrongFlashDurationSeconds));

            yield return new WaitForSeconds(wrongFlashDurationSeconds);
            _currentIndex = 0;
            _inputLocked = false;
            NotifyProgress();
        }

        private void CompletePuzzle()
        {
            _isComplete = true;
            if (lockInputWhenSolved) _inputLocked = true;

            Debug.Log("[PuzzleManager] Puzzle solved! Firing onPuzzleCompleted and handing off to PuzzleController.");
            onPuzzleCompleted?.Invoke();

            // Always advance to Collection — give the player a moment to see the solved state.
            StartCoroutine(DelayedComplete());
        }

        private IEnumerator DelayedComplete()
        {
            yield return new WaitForSeconds(delayBeforeTransition);
            PuzzleController.Instance?.CompletePhase();
        }

        private void NotifyProgress() => onProgressChanged?.Invoke(_currentIndex, correctSequence?.Count ?? 0);

        private void ApplyScoreDelta(int delta)
        {
            _score += delta;
            if (clampScoreAtZero && _score < 0) _score = 0;

            if (syncScoreToGameManager && delta > 0)
                GameManager.Instance?.AddScore(delta);

            onScoreChanged?.Invoke(_score);
        }
    }
}
