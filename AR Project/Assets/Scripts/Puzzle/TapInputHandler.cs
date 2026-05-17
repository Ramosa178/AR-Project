using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PokemonAR.Puzzle
{
    public class TapInputHandler : MonoBehaviour
    {
        [SerializeField] private Camera arCamera;
        [SerializeField] private PuzzleManager puzzleManager;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField] private float maxRaycastDistance = 20f;
        [SerializeField] private float fallbackScreenPickRadiusPixels = 520f;
        [SerializeField] private bool alwaysPickNearestOnFallback = true;
        [SerializeField] private bool logTapDebug = true;

        private void Awake()
        {
            if (arCamera == null)
                arCamera = Camera.main;
        }

        private void Update()
        {
            if (arCamera == null || puzzleManager == null || puzzleManager.IsComplete) return;
            if (TryGetPointerPosition(out Vector2 pointerPosition))
                TryHandleTap(pointerPosition);
        }

        private bool TryGetPointerPosition(out Vector2 pointerPosition)
        {
            pointerPosition = default;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var primaryTouch = Touchscreen.current.primaryTouch;
                if (primaryTouch.press.wasPressedThisFrame)
                {
                    pointerPosition = primaryTouch.position.ReadValue();
                    return true;
                }
            }

#if UNITY_EDITOR
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerPosition = Mouse.current.position.ReadValue();
                return true;
            }
#endif
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    pointerPosition = touch.position;
                    return true;
                }
            }

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                pointerPosition = Input.mousePosition;
                return true;
            }
#endif
#endif
            return false;
        }

        private void TryHandleTap(Vector2 screenPosition)
        {
            // Deterministic mobile fallback: pick nearest active puzzle object on screen.
            // This avoids fragile collider behavior across imported AR models.
            NumberObjectController numberObject = PickByScreenProximity(screenPosition);
            if (numberObject == null)
                return;

            bool accepted = puzzleManager.TryInput(numberObject);
            if (logTapDebug)
                Debug.Log($"[TapInputHandler] Tapped '{numberObject.name}' value={numberObject.NumberValue} accepted={accepted}");
        }

        private NumberObjectController PickByScreenProximity(Vector2 pointerPosition)
        {
            NumberObjectController[] candidates = FindObjectsOfType<NumberObjectController>(false);
            if (candidates == null || candidates.Length == 0) return null;

            NumberObjectController best = null;
            float bestDistance = float.MaxValue;
            float maxDist = Mathf.Max(1f, fallbackScreenPickRadiusPixels);

            for (int i = 0; i < candidates.Length; i++)
            {
                NumberObjectController c = candidates[i];
                if (c == null || !c.gameObject.activeInHierarchy) continue;

                Vector3 worldPickPoint = GetPickPoint(c);
                Vector3 screen = arCamera.WorldToScreenPoint(worldPickPoint);
                if (screen.z <= 0f) continue; // behind camera

                float d = Vector2.Distance(pointerPosition, new Vector2(screen.x, screen.y));
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = c;
                }
            }

            if (best != null && (bestDistance <= maxDist || alwaysPickNearestOnFallback))
                return best;
            return null;
        }

        private static Vector3 GetPickPoint(NumberObjectController number)
        {
            if (number == null) return Vector3.zero;
            return number.transform.position;
        }
    }
}
