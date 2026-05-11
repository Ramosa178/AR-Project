#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Tools > PokemonAR > Fix Everything
/// 1. Switches XR Simulation to the Backyard environment (no more yellow)
/// 2. Fixes camera clear flags
/// 3. Rebuilds the full AR UI with proper transparent panels
public static class FixEverything
{
    [MenuItem("Tools/PokemonAR/Fix Everything")]
    public static void Run()
    {
        FixSimulationEnvironment();
        FixCamera();
        RebuildUI();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[FixEverything] All done. Ctrl+S then Play.");
    }

    // ── 1. Switch XR Simulation environment to Backyard ───────────────────────
    static void FixSimulationEnvironment()
    {
        // Load the Backyard prefab
        var backyardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/UnityXRContent/ARFoundation/SimulationEnvironments/Backyard/Backyard_45ftx40ft.prefab");
        if (backyardPrefab == null)
        {
            Debug.LogWarning("[FixEverything] Backyard prefab not found — skipping env switch.");
            return;
        }

        // Find XRSimulationRuntimeSettings or XRSimulationPreferences and set default env
        // We do it through the SimulationEnvironmentAssetsManager asset
        string mgrPath = "Assets/XR/UserSimulationSettings/SimulationEnvironmentAssetsManager.asset";
        var mgr = AssetDatabase.LoadAssetAtPath<ScriptableObject>(mgrPath);
        if (mgr != null)
        {
            var so = new SerializedObject(mgr);
            // Try to find the default environment property
            var prop = so.FindProperty("m_DefaultEnvironmentPrefab") ??
                       so.FindProperty("defaultEnvironmentPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = backyardPrefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(mgr);
                Debug.Log("[FixEverything] Set default simulation environment to Backyard.");
            }
        }

        // Also try XRSimulationPreferences (Unity 5.x AR Foundation)
        var prefType = System.Type.GetType(
            "UnityEditor.XR.Simulation.XRSimulationPreferences, Unity.XR.ARFoundation.Editor");
        if (prefType != null)
        {
            var inst = prefType.GetProperty("Instance",
                BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
            if (inst != null)
            {
                var envField = prefType.GetField("m_EnvironmentPrefab",
                    BindingFlags.Instance | BindingFlags.NonPublic) ??
                    prefType.GetField("environmentPrefab",
                    BindingFlags.Instance | BindingFlags.Public);
                envField?.SetValue(inst, backyardPrefab);
                Debug.Log("[FixEverything] Set XRSimulationPreferences environment to Backyard.");
            }
        }
    }

    // ── 2. Fix camera ─────────────────────────────────────────────────────────
    static void FixCamera()
    {
        var camGOs = GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var cam in camGOs)
        {
            if (cam.name == "Main Camera" || cam.CompareTag("MainCamera"))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                EditorUtility.SetDirty(cam);
                Debug.Log("[FixEverything] Camera clearFlags = SolidColor black.");
            }
        }
    }

    // ── 3. Rebuild UI ─────────────────────────────────────────────────────────
    static void RebuildUI()
    {
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null) { Debug.LogError("[FixEverything] No Canvas found."); return; }

        // CanvasScaler — phone portrait scale
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Clear all children
        for (int i = canvasGO.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(canvasGO.transform.GetChild(i).gameObject);

        // Colours
        Color clear      = new Color(0, 0, 0, 0);
        Color darkNavy   = new Color(0.04f, 0.04f, 0.14f, 0.96f);
        Color hudPill    = new Color(0, 0, 0, 0.72f);
        Color gold       = new Color(1f, 0.82f, 0.08f, 1f);
        Color red        = new Color(0.88f, 0.08f, 0.08f, 1f);
        Color white      = Color.white;
        Color dimWhite   = new Color(1, 1, 1, 0.7f);
        Color dimBox     = new Color(0, 0, 0, 0.50f);

        // Helpers
        void FullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        GameObject MakeGO(string name, Transform parent, Color imgColor, bool active = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            FullStretch(go.GetComponent<RectTransform>());
            go.GetComponent<Image>().color = imgColor;
            go.SetActive(active);
            return go;
        }

        TextMeshProUGUI MakeTMP(string name, Transform parent, string text, int size, Color col,
            TextAlignmentOptions align, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = col; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        Button MakeBtn(string name, Transform parent, string label, Color bg,
            Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = bg;
            MakeTMP("Lbl", go.transform, label, 56, white, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0,4), new Vector2(0,-4));
            return go.GetComponent<Button>();
        }

        GameObject MakePill(string name, Transform parent,
            Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 oMin, Vector2 oMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
            go.GetComponent<Image>().color = hudPill;
            return go;
        }

        // ── HUD (transparent root, pills float over AR) ───────────────────────
        var hud = new GameObject("HUD", typeof(RectTransform));
        hud.transform.SetParent(canvasGO.transform, false);
        FullStretch(hud.GetComponent<RectTransform>());
        hud.SetActive(false);

        var scorePill = MakePill("ScorePill", hud.transform,
            new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
            new Vector2(28,-175), new Vector2(370,-65));
        var scoreTxt = MakeTMP("Txt", scorePill.transform, "Score: 0", 50, gold,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(14,6), new Vector2(-14,-6));

        var ballPill = MakePill("BallPill", hud.transform,
            new Vector2(1,1), new Vector2(1,1), new Vector2(1,1),
            new Vector2(-370,-175), new Vector2(-28,-65));
        var ballTxt = MakeTMP("Txt", ballPill.transform, "Balls: 0", 50, red,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(14,6), new Vector2(-14,-6));

        // Phase label bottom
        var phasePill = MakePill("PhasePill", hud.transform,
            new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0),
            new Vector2(0,0), new Vector2(0,96));
        MakeTMP("Txt", phasePill.transform, "", 36, dimWhite,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ── MAIN MENU (opaque — no AR yet) ───────────────────────────────────
        var menu = MakeGO("MainMenuPanel", canvasGO.transform, darkNavy, true);

        // decorative stripe
        var stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
        stripe.transform.SetParent(menu.transform, false);
        var srt = stripe.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0, 0.565f); srt.anchorMax = new Vector2(1, 0.572f);
        srt.offsetMin = srt.offsetMax = Vector2.zero;
        stripe.GetComponent<Image>().color = red;

        MakeTMP("Sub",  menu.transform, "POKÉMON",  80, dimWhite, TextAlignmentOptions.Center,
            new Vector2(0.05f,0.70f), new Vector2(0.95f,0.82f), Vector2.zero, Vector2.zero);
        MakeTMP("Main", menu.transform, "AR",      200, gold,     TextAlignmentOptions.Center,
            new Vector2(0.05f,0.52f), new Vector2(0.95f,0.72f), Vector2.zero, Vector2.zero);
        MakeTMP("Tag",  menu.transform,
            "Point your camera at the world\nand catch 'em all!",
            44, dimWhite, TextAlignmentOptions.Center,
            new Vector2(0.08f,0.38f), new Vector2(0.92f,0.52f), Vector2.zero, Vector2.zero);
        var startBtn = MakeBtn("StartBtn", menu.transform, "▶  START AR", red,
            new Vector2(0.12f,0.22f), new Vector2(0.88f,0.35f));
        MakeTMP("Hint", menu.transform,
            "WASD move  ·  Right-drag look  ·  Left-click interact",
            30, new Color(1,1,1,0.32f), TextAlignmentOptions.Center,
            new Vector2(0.04f,0.05f), new Vector2(0.96f,0.13f), Vector2.zero, Vector2.zero);

        // ── PUZZLE (fully transparent — AR camera shows through) ─────────────
        var puzzle = MakeGO("PuzzlePanel", canvasGO.transform, new Color(0.05f, 0.12f, 0.22f, 0.95f), false);

        var pzHeader = MakePill("Header", puzzle.transform,
            new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1),
            new Vector2(0,-195), new Vector2(0,0));
        MakeTMP("Txt", pzHeader.transform, "🔢  PUZZLE PHASE", 62, gold,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(20,0), new Vector2(-20,0));

        var pzBox = new GameObject("HintBox", typeof(RectTransform), typeof(Image));
        pzBox.transform.SetParent(puzzle.transform, false);
        var pzRt = pzBox.GetComponent<RectTransform>();
        pzRt.anchorMin = new Vector2(0.05f,0.28f); pzRt.anchorMax = new Vector2(0.95f,0.68f);
        pzRt.offsetMin = pzRt.offsetMax = Vector2.zero;
        pzBox.GetComponent<Image>().color = dimBox;
        MakeTMP("Txt", pzBox.transform,
            "[ Part 2 ]\nAR number objects will appear here\n\nTap them in the correct order",
            44, dimWhite, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(24,24), new Vector2(-24,-24));

        // ── COLLECTION (transparent) ─────────────────────────────────────────
        var collect = MakeGO("CollectionPanel", canvasGO.transform, new Color(0.05f, 0.18f, 0.08f, 0.95f), false);

        var coHeader = MakePill("Header", collect.transform,
            new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1),
            new Vector2(0,-195), new Vector2(0,0));
        MakeTMP("Txt", coHeader.transform, "⚪  COLLECTION PHASE", 58, gold,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(20,0), new Vector2(-20,0));

        var coBox = new GameObject("HintBox", typeof(RectTransform), typeof(Image));
        coBox.transform.SetParent(collect.transform, false);
        var coRt = coBox.GetComponent<RectTransform>();
        coRt.anchorMin = new Vector2(0.05f,0.28f); coRt.anchorMax = new Vector2(0.95f,0.68f);
        coRt.offsetMin = coRt.offsetMax = Vector2.zero;
        coBox.GetComponent<Image>().color = dimBox;
        MakeTMP("Txt", coBox.transform,
            "[ Part 3 ]\nAR Pokéballs will appear in the world\n\nWalk around to collect them",
            44, dimWhite, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(24,24), new Vector2(-24,-24));

        // ── BATTLE (transparent) ─────────────────────────────────────────────
        var battle = MakeGO("BattlePanel", canvasGO.transform, new Color(0.2f, 0.04f, 0.04f, 0.95f), false);

        var btHeader = MakePill("Header", battle.transform,
            new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1),
            new Vector2(0,-195), new Vector2(0,0));
        MakeTMP("Txt", btHeader.transform, "🐉  BATTLE PHASE", 62, red,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one, new Vector2(20,0), new Vector2(-20,0));

        var btBox = new GameObject("HintBox", typeof(RectTransform), typeof(Image));
        btBox.transform.SetParent(battle.transform, false);
        var btRt = btBox.GetComponent<RectTransform>();
        btRt.anchorMin = new Vector2(0.05f,0.28f); btRt.anchorMax = new Vector2(0.95f,0.68f);
        btRt.offsetMin = btRt.offsetMax = Vector2.zero;
        btBox.GetComponent<Image>().color = dimBox;
        MakeTMP("Txt", btBox.transform,
            "[ Part 4 ]\nTap a flat surface to place the boss\n\nThrow Pokéballs to attack!",
            44, dimWhite, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(24,24), new Vector2(-24,-24));

        // ── GAME END ─────────────────────────────────────────────────────────
        var end = MakeGO("GameEndPanel", canvasGO.transform, darkNavy, false);
        MakeTMP("Trophy", end.transform, "🏆",        150, gold, TextAlignmentOptions.Center,
            new Vector2(0.1f,0.68f), new Vector2(0.9f,0.82f), Vector2.zero, Vector2.zero);
        MakeTMP("Win",    end.transform, "YOU WIN!",   96, gold, TextAlignmentOptions.Center,
            new Vector2(0.1f,0.58f), new Vector2(0.9f,0.70f), Vector2.zero, Vector2.zero);
        var finalScoreTxt = MakeTMP("Score", end.transform, "Final Score: 0", 58, white,
            TextAlignmentOptions.Center,
            new Vector2(0.1f,0.46f), new Vector2(0.9f,0.58f), Vector2.zero, Vector2.zero);
        var restartEnd = MakeBtn("RestartBtn", end.transform, "↩  PLAY AGAIN", red,
            new Vector2(0.15f,0.28f), new Vector2(0.85f,0.41f));

        // ── GAME OVER ────────────────────────────────────────────────────────
        var over = MakeGO("GameOverPanel", canvasGO.transform, new Color(0.10f,0,0,0.97f), false);
        MakeTMP("Skull",  over.transform, "💀",         150, red, TextAlignmentOptions.Center,
            new Vector2(0.1f,0.68f), new Vector2(0.9f,0.82f), Vector2.zero, Vector2.zero);
        MakeTMP("Over",   over.transform, "GAME OVER",   88, red, TextAlignmentOptions.Center,
            new Vector2(0.1f,0.58f), new Vector2(0.9f,0.70f), Vector2.zero, Vector2.zero);
        var overScoreTxt = MakeTMP("Score", over.transform, "Score: 0", 58, white,
            TextAlignmentOptions.Center,
            new Vector2(0.1f,0.46f), new Vector2(0.9f,0.58f), Vector2.zero, Vector2.zero);
        var restartOver = MakeBtn("RestartBtn", over.transform, "↩  TRY AGAIN",
            new Color(0.55f,0,0,1f), new Vector2(0.15f,0.28f), new Vector2(0.85f,0.41f));

        // ── Wire UIStateManager (TMPro version) ───────────────────────────────
        // Remove old UIStateManager and add updated one
        var oldUI = canvasGO.GetComponent<PokemonAR.Core.UIStateManager>();
        if (oldUI != null) Undo.DestroyObjectImmediate(oldUI);

        var ui = Undo.AddComponent<PokemonAR.Core.UIStateManager>(canvasGO);
        var so2 = new SerializedObject(ui);
        void SP(string n, Object v) { var p = so2.FindProperty(n); if (p != null) p.objectReferenceValue = v; }
        SP("mainMenuPanel",        menu);
        SP("puzzlePanel",          puzzle);
        SP("collectionPanel",      collect);
        SP("battlePanel",          battle);
        SP("gameEndPanel",         end);
        SP("gameOverPanel",        over);
        SP("hudRoot",              hud);
        SP("startButton",          startBtn);
        SP("restartButtonGameEnd", restartEnd);
        SP("restartButtonGameOver",restartOver);
        SP("scoreLabelTMP",        scoreTxt);
        SP("pokeballLabelTMP",     ballTxt);
        SP("finalScoreLabelTMP",   finalScoreTxt);
        SP("gameOverScoreLabelTMP",overScoreTxt);
        so2.ApplyModifiedProperties();

        EditorUtility.SetDirty(canvasGO);
        Debug.Log("[FixEverything] UI rebuilt — Main Menu, Puzzle, Collection, Battle, End, Over.");
    }
}
#endif
