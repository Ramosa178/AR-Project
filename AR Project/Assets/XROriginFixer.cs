using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;

[ExecuteInEditMode]
public class XROriginFixer : MonoBehaviour
{
void OnEnable()
    {
        var xrOrigin = FindFirstObjectByType<XROrigin>();

        if (xrOrigin == null)
        {
            Debug.LogError("[XROriginFixer] XROrigin component not found!");
            return;
        }

        if (xrOrigin.Camera == null)
        {
            Debug.LogWarning("[XROriginFixer] XROrigin.Camera is null. Searching for MainCamera...");
            var mainCameraGO = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCameraGO != null)
            {
                var originType = typeof(XROrigin);
                var cameraField = originType.GetField(
                    "m_Camera",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (cameraField != null)
                {
                    cameraField.SetValue(xrOrigin, mainCameraGO.GetComponent<Camera>());
                    Debug.Log("[XROriginFixer] Camera set to: " + mainCameraGO.name);
                }
            }
            else
            {
                Debug.LogError("[XROriginFixer] No MainCamera found!");
            }
        }
        else
        {
            Debug.Log("[XROriginFixer] XROrigin.Camera ok: " + xrOrigin.Camera.name);
        }

        // ARSession.state is static - must use type name, not instance
        Debug.Log("[XROriginFixer] ARSession.state = " + ARSession.state);
    }
}