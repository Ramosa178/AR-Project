using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace PokemonAR.Collection
{
    // Attach to each pokeball prefab.
    // Detects an upward swipe on this pokeball and calls CollectionController.AddPokeball.
    public class PokeballCollector : MonoBehaviour
    {
        [SerializeField] private float swipeThreshold = 100f;

        private Vector2 _touchStart;
        private bool    _tracking = false;
        private Camera  _cam;

        private void OnEnable()  { EnhancedTouchSupport.Enable();  }
        private void OnDisable() { EnhancedTouchSupport.Disable(); }

        private void Start()
        {
            // Camera.main works as long as the AR camera is tagged MainCamera.
            // Fall back to FindObjectOfType if the tag is missing.
            _cam = Camera.main;
            if (_cam == null)
                _cam = FindObjectOfType<Camera>();

            if (_cam == null)
                Debug.LogWarning("[PokeballCollector] No camera found -- touch collection will not work.");
        }

        private void Update()
        {
            if (_cam == null) return;
            if (Touch.activeTouches.Count == 0) return;

            var touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Ray ray = _cam.ScreenPointToRay(touch.screenPosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider != null && hit.collider.gameObject == gameObject)
                    {
                        _touchStart = touch.screenPosition;
                        _tracking   = true;
                        Debug.Log("[PokeballCollector] Touch started on pokeball.");
                    }
                }
            }

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended && _tracking)
            {
                _tracking = false;
                Vector2 delta = touch.screenPosition - _touchStart;

                // Accept upward swipe
                if (delta.y > swipeThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    Collect();
            }
        }

        private void Collect()
        {
            Debug.Log("[PokeballCollector] Pokeball collected!");
            CollectionController.Instance?.AddPokeball(1);
            Destroy(gameObject);
        }
    }
}