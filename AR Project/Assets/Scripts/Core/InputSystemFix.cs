// InputSystemFix.cs — auto-swaps StandaloneInputModule for InputSystemUIInputModule
// Drop this in any Editor folder or attach to EventSystem. Runs once on Awake.
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PokemonAR.Core
{
    /// <summary>
    /// Fixes the "UnityEngine.Input class" error that appears when the project is set
    /// to use the New Input System but an EventSystem still has StandaloneInputModule.
    /// Safe to keep in builds — it does nothing at runtime if the module is already correct.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class InputSystemFix : MonoBehaviour
    {
        void Awake()
        {
            var es = GetComponent<EventSystem>();
            if (es == null) return;

            var oldModule = GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null)
            {
                Debug.Log("[InputSystemFix] Removing StandaloneInputModule and adding InputSystemUIInputModule.");
                Destroy(oldModule);

                // Add the correct module if not already present
                if (GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                    gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }
    }
}
