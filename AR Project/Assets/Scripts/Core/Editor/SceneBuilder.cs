
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using PokemonAR.Core;

namespace PokemonAR.Core.Editor
{
    public static class SceneBuilder
    {
        [MenuItem("PokemonAR/Rebuild Full Main Scene")]
        public static void BuildMainScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);

            // Wipe everything
            foreach (var go in scene.GetRootGameObjects())
                Object.DestroyImmediate(go);

            // ── EventSystem ───────────────────────────────────────────────
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();

            // ── GameManager ───────────────────────────────────────────────
            var gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();

            // ── AR Session ────────────────────────────────────────────────
            var arSessionGO = new GameObject("AR Session");
            arSessionGO.AddComponent<ARSession>();
            arSessionGO.AddComponent<ARInputManager>();

            // ── XR Origin (AR camera rig) ─────────────────────────────────
            var xrOriginGO  = new GameObject("XR Origin");
            var xrOrigin    = xrOriginGO.AddComponent<XROrigin>();
            xrOriginGO.AddComponent<ARPlaneManager>();
            xrOriginGO.AddComponent<ARRaycastManager>();

            var camOffsetGO = new GameObject("Camera Offset");
            camOffsetGO.transform.SetParent(xrOriginGO.transform, false);

            var arCamGO = new GameObject("Main Camera");
            arCamGO.tag = "MainCamera";
            arCamGO.transform.SetParent(camOffsetGO.transform, false);
            var cam = arCamGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.2f);
            cam.nearClipPlane   = 0.1f;
            cam.farClipPlane    = 20f;
            arCamGO.AddComponent<AudioListener>();
            arCamGO.AddComponent<ARCameraManager>();
            arCamGO.AddComponent<ARCameraBackground>();

            xrOrigin.Camera = cam;
            xrOrigin.CameraFloorOffsetObject = camOffsetGO;
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            // ── XRI actions + InputActionManager + TrackedPoseDriver (required by XROrigin + XR sim) ──
            var xriActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/XR/XRI Default Input Actions.inputactions");
            if (xriActions == null)
            {
                Debug.LogWarning("[SceneBuilder] Missing Assets/XR/XRI Default Input Actions.inputactions. " +
                    "Copy from Package: XR Interaction Toolkit → Samples → Starter Assets.");
            }
            else
            {
                var iam = xrOriginGO.AddComponent<InputActionManager>();
                var soIam = new SerializedObject(iam);
                SerializedProperty listProp = soIam.FindProperty("m_ActionAssets");
                listProp.ClearArray();
                listProp.InsertArrayElementAtIndex(0);
                listProp.GetArrayElementAtIndex(0).objectReferenceValue = xriActions;
                soIam.ApplyModifiedProperties();

                var headMap = xriActions.FindActionMap("XRI Head");
                var tpd = arCamGO.AddComponent<TrackedPoseDriver>();
                var soTpd = new SerializedObject(tpd);
                WireInputActionProp(soTpd, "m_PositionInput",       headMap.FindAction("Position"));
                WireInputActionProp(soTpd, "m_RotationInput",       headMap.FindAction("Rotation"));
                WireInputActionProp(soTpd, "m_TrackingStateInput", headMap.FindAction("Tracking State"));
                soTpd.FindProperty("m_IgnoreTrackingState").boolValue = true;
                soTpd.ApplyModifiedProperties();
            }

            var uiMod = esGO.GetComponent<InputSystemUIInputModule>();
            if (uiMod != null)
            {
                var soUi = new SerializedObject(uiMod);
                soUi.FindProperty("m_XRTrackingOrigin").objectReferenceValue = xrOriginGO.transform;
                soUi.ApplyModifiedProperties();
            }

            // ── XR Device Simulator ───────────────────────────────────────
            var simGO = new GameObject("XR Device Simulator");
            simGO.AddComponent<XRDeviceSimulator>();

            // ── Canvas ────────────────────────────────────────────────────
            var cvGO    = new GameObject("Canvas");
            var cv      = cvGO.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler  = cvGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            cvGO.AddComponent<GraphicRaycaster>();
            var cvt = cvGO.transform;

            // ── HUD ───────────────────────────────────────────────────────
            var hud = MakePanel("HUD", cvt, new Color(0,0,0,0.5f), false);
            Anchors(hud, new Vector2(0,0.88f), Vector2.one);
            var scoreLabel    = MakeText("ScoreLabel",    hud.transform, "Score: 0",     34, Color.white);
            var pokeballLabel = MakeText("PokeballLabel", hud.transform, "Pokeballs: 0", 34, Color.white);
            Anchors(scoreLabel,    Vector2.zero, new Vector2(0.5f,1));
            Anchors(pokeballLabel, new Vector2(0.5f,0), Vector2.one);

            // ── MainMenuPanel ─────────────────────────────────────────────
            var mm      = MakePanel("MainMenuPanel", cvt, new Color(0.06f,0.06f,0.18f), true);
            var mmTitle = MakeText("TitleText", mm.transform, "Pokemon AR", 60, new Color(1f,0.85f,0f));
            Anchors(mmTitle, new Vector2(0.05f,0.62f), new Vector2(0.95f,0.80f));
            var mmSub   = MakeText("SubText",   mm.transform, "AR Pokemon Game", 28, new Color(0.7f,0.7f,1f));
            Anchors(mmSub,   new Vector2(0.1f,0.52f),  new Vector2(0.9f,0.62f));
            var startBtn = MakeButton("StartButton", mm.transform, "START GAME", new Color(0.15f,0.55f,0.15f));
            Anchors(startBtn, new Vector2(0.2f,0.36f), new Vector2(0.8f,0.48f));
            var mmHint  = MakeText("HintText",  mm.transform, "Press 1 on keyboard to start", 20, new Color(0.5f,0.5f,0.5f));
            Anchors(mmHint, new Vector2(0.05f,0.08f), new Vector2(0.95f,0.16f));

            // ── PuzzlePanel ───────────────────────────────────────────────
            var puz     = MakePanel("PuzzlePanel",      cvt, new Color(0.05f,0.12f,0.22f), false);
            Anchors(MakeText("Title",   puz.transform, "PUZZLE PHASE",             52, new Color(0.4f,0.8f,1f)),   new Vector2(0.05f,0.65f), new Vector2(0.95f,0.80f));
            Anchors(MakeText("SubText", puz.transform, "Part 2 implements this",   30, new Color(0.6f,0.6f,0.9f)), new Vector2(0.05f,0.52f), new Vector2(0.95f,0.63f));
            Anchors(MakeText("HintText",puz.transform, "Press 2 to complete",      20, new Color(0.5f,0.5f,0.5f)), new Vector2(0.05f,0.06f), new Vector2(0.95f,0.15f));

            // ── CollectionPanel ───────────────────────────────────────────
            var col     = MakePanel("CollectionPanel",  cvt, new Color(0.05f,0.18f,0.08f), false);
            Anchors(MakeText("Title",   col.transform, "COLLECTION PHASE",          48, new Color(0.4f,1f,0.5f)),   new Vector2(0.05f,0.65f), new Vector2(0.95f,0.80f));
            Anchors(MakeText("SubText", col.transform, "Part 3 implements this",    30, new Color(0.6f,0.8f,0.6f)), new Vector2(0.05f,0.52f), new Vector2(0.95f,0.63f));
            Anchors(MakeText("HintText",col.transform, "Press 3 (+5 balls) to complete", 20, new Color(0.5f,0.5f,0.5f)), new Vector2(0.05f,0.06f), new Vector2(0.95f,0.15f));

            // ── BattlePanel ───────────────────────────────────────────────
            var bat     = MakePanel("BattlePanel",      cvt, new Color(0.2f,0.04f,0.04f), false);
            Anchors(MakeText("Title",   bat.transform, "BATTLE PHASE",              52, new Color(1f,0.4f,0.4f)),   new Vector2(0.05f,0.65f), new Vector2(0.95f,0.80f));
            Anchors(MakeText("SubText", bat.transform, "Part 4 implements this",    30, new Color(0.8f,0.6f,0.6f)), new Vector2(0.05f,0.52f), new Vector2(0.95f,0.63f));
            Anchors(MakeText("HintText",bat.transform, "Press 4 to win  |  0 to lose", 20, new Color(0.5f,0.5f,0.5f)), new Vector2(0.05f,0.06f), new Vector2(0.95f,0.15f));

            // ── GameEndPanel ──────────────────────────────────────────────
            var end     = MakePanel("GameEndPanel",     cvt, new Color(0.04f,0.18f,0.04f), false);
            Anchors(MakeText("WinText",        end.transform, "YOU WIN!",       60, new Color(1f,0.9f,0.2f)), new Vector2(0.05f,0.65f), new Vector2(0.95f,0.82f));
            var finalScore  = MakeText("FinalScoreLabel", end.transform, "Final Score: 0", 36, Color.white);
            Anchors(finalScore, new Vector2(0.1f,0.50f), new Vector2(0.9f,0.62f));
            var restartEnd  = MakeButton("RestartButton", end.transform, "PLAY AGAIN", new Color(0.15f,0.35f,0.75f));
            Anchors(restartEnd, new Vector2(0.2f,0.30f), new Vector2(0.8f,0.42f));

            // ── GameOverPanel ─────────────────────────────────────────────
            var over    = MakePanel("GameOverPanel",    cvt, new Color(0.18f,0.02f,0.02f), false);
            Anchors(MakeText("GameOverText",  over.transform, "GAME OVER",      60, new Color(1f,0.3f,0.3f)), new Vector2(0.05f,0.65f), new Vector2(0.95f,0.82f));
            var overScore   = MakeText("GameOverScore", over.transform, "Final Score: 0", 36, Color.white);
            Anchors(overScore, new Vector2(0.1f,0.50f), new Vector2(0.9f,0.62f));
            var restartOver = MakeButton("RestartButton", over.transform, "TRY AGAIN", new Color(0.65f,0.1f,0.1f));
            Anchors(restartOver, new Vector2(0.2f,0.30f), new Vector2(0.8f,0.42f));

            // ── Wire UIStateManager ───────────────────────────────────────
            var uism = cvGO.AddComponent<UIStateManager>();
            var so   = new SerializedObject(uism);
            so.FindProperty("mainMenuPanel").objectReferenceValue         = mm;
            so.FindProperty("puzzlePanel").objectReferenceValue           = puz;
            so.FindProperty("collectionPanel").objectReferenceValue       = col;
            so.FindProperty("battlePanel").objectReferenceValue           = bat;
            so.FindProperty("gameEndPanel").objectReferenceValue          = end;
            so.FindProperty("gameOverPanel").objectReferenceValue         = over;
            so.FindProperty("hudRoot").objectReferenceValue               = hud;
            so.FindProperty("scoreLabel").objectReferenceValue            = scoreLabel.GetComponent<Text>();
            so.FindProperty("pokeballLabel").objectReferenceValue         = pokeballLabel.GetComponent<Text>();
            so.FindProperty("startButton").objectReferenceValue           = startBtn.GetComponent<Button>();
            so.FindProperty("restartButtonGameEnd").objectReferenceValue  = restartEnd.GetComponent<Button>();
            so.FindProperty("restartButtonGameOver").objectReferenceValue = restartOver.GetComponent<Button>();
            so.FindProperty("finalScoreLabel").objectReferenceValue       = finalScore.GetComponent<Text>();
            so.FindProperty("gameOverScoreLabel").objectReferenceValue    = overScore.GetComponent<Text>();
            so.ApplyModifiedProperties();

            // ── Save ──────────────────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneBuilder] Full scene rebuilt and saved.");
            EditorUtility.DisplayDialog("Done!", "Scene rebuilt!\n\nNext: Edit → Project Settings → XR Plug-in Management\n- Windows, Mac, Linux: enable XR Simulation loader\n\nThen press Play. Debug flow: keys 1–4, 0, R (Editor only).", "OK");
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        static GameObject MakePanel(string n, Transform p, Color c, bool active)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            go.AddComponent<Image>().color = new Color(c.r, c.g, c.b, 1f);
            Stretch(go.GetComponent<RectTransform>());
            go.SetActive(active);
            return go;
        }

        static Font _font;
        static Font GetFont()
        {
            if (_font != null) return _font;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }

        static GameObject MakeText(string n, Transform p, string txt, int size, Color c)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var t = go.AddComponent<Text>();
            t.text = txt; t.font = GetFont(); t.fontSize = size;
            t.color = c; t.alignment = TextAnchor.MiddleCenter;
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        static GameObject MakeButton(string n, Transform p, string label, Color bg)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            go.AddComponent<Image>().color = bg;
            go.AddComponent<Button>();
            Stretch(go.GetComponent<RectTransform>());
            MakeText("Text", go.transform, label, 32, Color.white);
            return go;
        }

        static void Stretch(RectTransform rt)
        { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

        static void Anchors(GameObject go, Vector2 min, Vector2 max)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void WireInputActionProp(SerializedObject so, string propertyName, InputAction action)
        {
            var prop = so.FindProperty(propertyName);
            prop.FindPropertyRelative("m_UseReference").boolValue = true;
            prop.FindPropertyRelative("m_Reference").objectReferenceValue = InputActionReference.Create(action);
        }
    }
}
#endif
