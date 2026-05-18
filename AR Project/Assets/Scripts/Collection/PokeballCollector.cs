using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using PokemonAR.Core;

namespace PokemonAR.Collection
{
    public class PokeballCollector : MonoBehaviour
    {
        [SerializeField] private float swipeThreshold = 100f;

        private Vector2 _touchStart;
        private bool _tracking = false;
        private Camera _cam;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Start()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (Touch.activeTouches.Count == 0) return;

            var touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Ray ray = _cam.ScreenPointToRay(touch.screenPosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        _touchStart = touch.screenPosition;
                        _tracking = true;
                        Debug.Log("[PokeballCollector] Touch started on pokeball!");
                    }
                }
            }

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended && _tracking)
            {
                _tracking = false;
                Vector2 swipeDelta = touch.screenPosition - _touchStart;

                if (swipeDelta.y > swipeThreshold &&
                    Mathf.Abs(swipeDelta.y) > Mathf.Abs(swipeDelta.x))
                {
                    Collect();
                }
            }
        }

        private void Collect()
        {
            Debug.Log("[PokeballCollector] Pokeball collected!");
            FindObjectOfType<SceneTransition>()?.CollectPokeball();
            Destroy(gameObject);
        }
    }
}