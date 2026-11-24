using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Fills the figure and scenario lists of ShadowWorkspaceUI (in the Workspace prefab) with every sprite found in
// Assets/Figures/Figures and Assets/Figures/Scenarios. It runs by itself whenever files in Assets/Figures change,
// and can also be run from the menu: Teatro > Sync Figures.
public sealed class FigureLibrarySync : AssetPostprocessor {
    private const string FiguresFolder = "Assets/Figures/Figures";
    private const string ScenariosFolder = "Assets/Figures/Scenarios";
    private const string WorkspacePrefab = "Assets/Prefabs/Workspace.prefab";

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom) {
        if (Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom))
            EditorApplication.delayCall += Sync;
    }

    private static bool Touches(string[] paths) {
        return paths.Any(path => path.StartsWith("Assets/Figures/"));
    }

    [MenuItem("Teatro/Sync Figures")]
    public static void Sync() {
        Sprite[] figures = LoadSprites(FiguresFolder);
        Sprite[] scenarios = LoadSprites(ScenariosFolder);

        GameObject root = PrefabUtility.LoadPrefabContents(WorkspacePrefab);

        try {
            ShadowWorkspaceUI workspace = root.GetComponentInChildren<ShadowWorkspaceUI>(true);

            if (workspace == null) {
                Debug.LogWarning("FigureLibrarySync: no ShadowWorkspaceUI found in " + WorkspacePrefab);
                return;
            }

            SerializedObject serialized = new SerializedObject(workspace);
            bool changed = Assign(serialized.FindProperty("figureImages"), figures);
            changed |= Assign(serialized.FindProperty("scenarioImages"), scenarios);

            if (!changed)
                return;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, WorkspacePrefab);
            Debug.Log("FigureLibrarySync: " + figures.Length + " figures and " + scenarios.Length + " scenarios.");
        } finally {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Sprite[] LoadSprites(string folder) {
        List<Sprite> sprites = new List<Sprite>();

        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { folder })) {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path)) {
                if (asset is Sprite sprite)
                    sprites.Add(sprite);
            }
        }

        return sprites.OrderBy(sprite => sprite.name, new NaturalComparer()).ToArray();
    }

    // Returns true when the list was different and has been replaced.
    private static bool Assign(SerializedProperty list, Sprite[] sprites) {
        bool same = list.arraySize == sprites.Length;

        for (int i = 0; same && i < sprites.Length; i++)
            same = list.GetArrayElementAtIndex(i).objectReferenceValue == sprites[i];

        if (same)
            return false;

        list.arraySize = sprites.Length;

        for (int i = 0; i < sprites.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];

        return true;
    }

    // "Frame 2" comes before "Frame 10".
    private sealed class NaturalComparer : IComparer<string> {
        public int Compare(string a, string b) {
            return EditorUtility.NaturalCompare(a, b);
        }
    }
}
