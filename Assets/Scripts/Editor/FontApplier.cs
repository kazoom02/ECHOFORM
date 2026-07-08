using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================
// ECHOFORM — FontApplier  (Editor tool)
// Tools ▸ ECHOFORM ▸ Apply Fonts
// Assigns the display font (Chakra Petch) to every TMP text,
// and the mono/terminal font (Share Tech Mono) to system-
// readout texts (matched by GameObject name). Runs across all
// open scenes AND every prefab under Assets, and sets the TMP
// project default so new text uses the display font too.
//
// Setting .font (not the material directly) makes TMP swap the
// font's material for us — no garbled atlas mismatches.
// =====================================================

public static class FontApplier
{
    // Move these if your SDF assets live elsewhere.
    const string DisplayPath = "Assets/TextMesh Pro/Fonts/ChakraPetch-Regular SDF.asset";
    const string MonoPath    = "Assets/TextMesh Pro/Fonts/ShareTechMono-Regular SDF.asset";

    // GameObjects whose name contains any of these get the MONO (terminal) font.
    static readonly string[] MonoKeywords =
        { "install", "copy", "overload", "cpu", "cycle", "cost", "log", "readout", "terminal", "data", "count" };

    [MenuItem("Tools/ECHOFORM/Apply Fonts")]
    public static void ApplyFonts()
    {
        var display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DisplayPath);
        var mono    = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MonoPath);
        if (display == null || mono == null)
        {
            Debug.LogError($"[FontApplier] Missing font asset. Display={display}, Mono={mono}. " +
                           "Fix the paths at the top of FontApplier.cs.");
            return;
        }

        int count = 0;

        // ---- open scenes ----
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
                count += ApplyToTree(root, display, mono);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        // ---- prefabs under Assets (skip the TMP package examples) ----
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("TextMesh Pro")) continue;

            var root = PrefabUtility.LoadPrefabContents(path);
            int c = ApplyToTree(root, display, mono);
            if (c > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); count += c; }
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ---- TMP project default (new text objects) ----
        var settings = TMP_Settings.instance;
        if (settings != null)
        {
            var so = new SerializedObject(settings);
            var prop = so.FindProperty("m_defaultFontAsset");
            if (prop != null) { prop.objectReferenceValue = display; so.ApplyModifiedProperties(); EditorUtility.SetDirty(settings); }
        }

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log($"[FontApplier] Applied fonts to {count} text objects. Display = Chakra Petch, Mono = Share Tech Mono.");
    }

    static int ApplyToTree(GameObject root, TMP_FontAsset display, TMP_FontAsset mono)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            TMP_FontAsset chosen = IsMono(t.gameObject.name) ? mono : display;
            if (t.font != chosen)
            {
                t.font = chosen;                // also swaps to the font's material
                EditorUtility.SetDirty(t);
                n++;
            }
        }
        return n;
    }

    static bool IsMono(string objName)
    {
        string n = objName.ToLowerInvariant();
        foreach (var k in MonoKeywords)
            if (n.Contains(k)) return true;
        return false;
    }
}
