using UnityEngine;
using UnityEngine.UI;

namespace PokemonAR.Puzzle
{
    /// <summary>
    /// Runtime canvas HUD so hint/progress/score are always visible on device.
    /// </summary>
    public class PuzzleRuntimeHUD : MonoBehaviour
    {
        private PuzzleManager _puzzleManager;
        private Text _titleText;
        private Text _hintText;
        private Text _progressText;
        private Text _orderText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<PuzzleRuntimeHUD>() != null) return;
            if (FindObjectOfType<PuzzleManager>() == null) return;

            var hud = new GameObject("PuzzleRuntimeHUD");
            hud.AddComponent<PuzzleRuntimeHUD>();
        }

        private void Awake()
        {
            _puzzleManager = FindObjectOfType<PuzzleManager>();
            BuildCanvas();
            Debug.Log("[PuzzleRuntimeHUD] Runtime HUD canvas created.");
        }

        private void Update()
        {
            if (_puzzleManager == null || _progressText == null) return;

            _titleText.text = "Evolution Puzzle";
            _hintText.text = "Hint: " + _puzzleManager.HintMessage;
            _progressText.text = $"Progress: {_puzzleManager.CurrentIndex}/{_puzzleManager.SequenceLength}    |    Score: {_puzzleManager.Score}    |    Wrong: {_puzzleManager.WrongAttempts}";
           
        }

        private void OnGUI()
        {
            if (_puzzleManager == null) return;
            GUI.color = Color.white;
            GUI.Label(new Rect(20f, 20f, 1400f, 40f), "Evolution Puzzle HUD Active");
            GUI.Label(new Rect(20f, 52f, 1600f, 40f), "Hint: " + _puzzleManager.HintMessage);
        }

        private void BuildCanvas()
        {
            GameObject canvasGo = new GameObject("PuzzleHUDCanvas");
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.6f);
            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            panelRt.anchoredPosition = new Vector2(16f, -16f);
            panelRt.sizeDelta = new Vector2(900f, 220f);

            _titleText = CreateText(panelGo.transform, new Vector2(16f, -14f), new Vector2(860f, 36f), 30, FontStyle.Bold);
            _hintText = CreateText(panelGo.transform, new Vector2(16f, -56f), new Vector2(860f, 66f), 24, FontStyle.Normal);
            _progressText = CreateText(panelGo.transform, new Vector2(16f, -126f), new Vector2(860f, 36f), 24, FontStyle.Normal);
            _orderText = CreateText(panelGo.transform, new Vector2(16f, -166f), new Vector2(860f, 36f), 24, FontStyle.Normal);
        }

        private static Text CreateText(Transform parent, Vector2 anchoredPos, Vector2 size, int fontSize, FontStyle style)
        {
            GameObject go = new GameObject("Text");
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            return text;
        }
    }
}
