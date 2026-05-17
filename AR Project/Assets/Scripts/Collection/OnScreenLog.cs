using UnityEngine;
using System.Collections.Generic;

namespace PokemonAR.Collection
{
    public class OnScreenLog : MonoBehaviour
    {
        private List<string> _logs = new List<string>();
        private GUIStyle _style;

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            _logs.Add(message);
            if (_logs.Count > 15)
                _logs.RemoveAt(0);
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.fontSize = 28;
                _style.normal.textColor = Color.yellow;
            }

            float y = 10;
            foreach (var log in _logs)
            {
                GUI.Label(new Rect(10, y, Screen.width - 20, 40), log, _style);
                y += 40;
            }
        }
    }
}