using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Builds the TextMeshPro font assets used by the UI from the static Science Gothic fonts.
// Run it from Tools > Teatro > Create TMP Fonts. Fonts that already have an asset are skipped.
public static class CreateTmpFonts {
    private const string Characters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "ÀÁÂÃÇÉÊÍÓÔÕÚàáâãçéêíóôõú·–—…‘’“”";

    [MenuItem("Tools/Teatro/Create TMP Fonts")]
    public static void CreateAll() {
        Create("Assets/Fonts/Static/ScienceGothic-Title.ttf", "Assets/Fonts/Static/ScienceGothic-Title SDF.asset");
        Create("Assets/Fonts/Static/ScienceGothic-Body.ttf", "Assets/Fonts/Static/ScienceGothic-Body SDF.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("Created the Science Gothic TMP font assets.");
    }

    private static void Create(string fontPath, string assetPath) {
        // Recreating an existing asset would give it a new ID and break every label that uses it.
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null) {
            Debug.Log("Already exists, left as is: " + assetPath);
            return;
        }

        Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);

        if (font == null) {
            Debug.LogError("Font not found: " + fontPath);
            return;
        }

        // Sampled at 90 pt with 9 px padding, so letters stay sharp when a window is scaled up.
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        fontAsset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        fontAsset.TryAddCharacters(Characters, out _);

        AssetDatabase.CreateAsset(fontAsset, assetPath);

        fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);

        fontAsset.material.name = fontAsset.name + " Material";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        EditorUtility.SetDirty(fontAsset);
    }
}
