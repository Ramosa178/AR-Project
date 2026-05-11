#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public static class XRSimulatorFix
{
    [MenuItem("Tools/PokemonAR/Fix XR Simulator")]
    public static void Run()
    {
        var sim = Object.FindFirstObjectByType<XRDeviceSimulator>();
        if (sim == null) { Debug.LogError("[Fix] No XRDeviceSimulator in scene."); return; }

        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/XR/XRI Default Input Actions.inputactions");
        if (asset == null)
        {
            foreach (string g in AssetDatabase.FindAssets("t:InputActionAsset"))
            {
                asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(g));
                if (asset != null) break;
            }
        }
        if (asset == null) { Debug.LogError("[Fix] No InputActionAsset in project."); return; }
        Debug.Log("[Fix] Asset: " + AssetDatabase.GetAssetPath(asset));

        int n = 0;
        for (var t = sim.GetType(); t != null; t = t.BaseType)
        {
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (f.FieldType == typeof(InputActionAsset) && f.FieldType == typeof(InputActionAsset))
                { f.SetValue(sim, asset); n++; Debug.Log("[Fix] set field " + f.Name); }
            }
        }

        var so = new SerializedObject(sim);
        foreach (string name in new[]{ "m_DeviceSimulatorActionAsset","m_ControllerActionAsset",
            "m_HandActionAsset","deviceSimulatorActionAsset","controllerActionAsset","handActionAsset" })
        {
            var p = so.FindProperty(name);
            if (p != null)
            { p.objectReferenceValue = asset; n++; Debug.Log("[Fix] set prop " + name); }
        }
        so.ApplyModifiedProperties();

        var xro = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xro != null)
        {
            var xso = new SerializedObject(xro);
            var cy = xso.FindProperty("m_CameraYOffset");
            if (cy != null && cy.floatValue != 0f) { cy.floatValue = 0f; xso.ApplyModifiedProperties(); EditorUtility.SetDirty(xro); Debug.Log("[Fix] Zeroed CameraYOffset."); }
        }

        EditorUtility.SetDirty(sim);
        EditorSceneManager.MarkSceneDirty(sim.gameObject.scene);
        Debug.Log("[Fix] Done. Assigned " + n + " slots. Ctrl+S to save, then Play.");
    }
}
#endif
