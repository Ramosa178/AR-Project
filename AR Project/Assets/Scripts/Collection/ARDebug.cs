using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace PokemonAR.Collection
{
    public class ARDebug : MonoBehaviour
    {
        private ARTrackedImageManager _manager;

        private void Awake()
        {
            _manager = FindObjectOfType<ARTrackedImageManager>();
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 40;
            style.normal.textColor = Color.red;

            string info = $"AR State: {ARSession.state}\n" +
                         $"Manager enabled: {(_manager != null ? _manager.enabled.ToString() : "N/A")}\n" +
                         $"Tracked images: {(_manager != null ? _manager.trackables.count.ToString() : "N/A")}\n";

            if (_manager != null)
            {
                foreach (var image in _manager.trackables)
                {
                    info += $"Image: {image.referenceImage.name}\n";
                    info += $"State: {image.trackingState}\n";
                    info += $"Pos: {image.transform.position}\n";
                    info += $"Children: {image.transform.childCount}\n";
                }
            }

            GUI.Label(new Rect(10, 10, Screen.width, 600), info, style);
        }
    }
}