using UnityEngine;
using UnityEngine.InputSystem;

namespace PokemonAR.Core
{
#if UNITY_EDITOR
    /// <summary>
    /// Editor-only free-look camera using the New Input System.
    /// Attach to Main Camera. Stripped from device builds automatically.
    /// CONTROLS (click the Game view once to focus it first):
    ///   WASD / Arrows    move   |   Q/E  down/up
    ///   Hold Right Mouse look   |   Shift  3x speed   |   Scroll  adjust speed
    /// </summary>
    public class EditorCameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed       = 2f;
        [SerializeField] private float fastMultiplier  = 3f;
        [SerializeField] private float mouseSensitivity = 0.15f;

        private float _pitch;
        private float _yaw;

        private void Start()
        {
            _yaw   = transform.eulerAngles.y;
            _pitch = transform.eulerAngles.x;
        }

        private void Update()
        {
            var kb    = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            // ── Look (hold right mouse button) ────────────────────────────────
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _yaw   += delta.x * mouseSensitivity;
                _pitch -= delta.y * mouseSensitivity;
                _pitch  = Mathf.Clamp(_pitch, -80f, 80f);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            // ── Move ──────────────────────────────────────────────────────────
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                moveSpeed = Mathf.Clamp(moveSpeed + scroll * 0.01f, 0.1f, 20f);

            float speed = moveSpeed * (kb.leftShiftKey.isPressed ? fastMultiplier : 1f);

            float h  = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ?  1f : 0f)
                     + (kb.aKey.isPressed || kb.leftArrowKey.isPressed  ? -1f : 0f);
            float v  = (kb.wKey.isPressed || kb.upArrowKey.isPressed    ?  1f : 0f)
                     + (kb.sKey.isPressed || kb.downArrowKey.isPressed  ? -1f : 0f);
            float ud = (kb.eKey.isPressed ?  1f : 0f)
                     + (kb.qKey.isPressed ? -1f : 0f);

            Vector3 dir = transform.right * h + transform.forward * v + Vector3.up * ud;
            if (dir.sqrMagnitude > 0.001f)
                transform.position += dir.normalized * speed * Time.deltaTime;
        }
    }
#else
    public class EditorCameraController : MonoBehaviour { }
#endif
}
