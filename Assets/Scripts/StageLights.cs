using UnityEngine;

// The layer of a stage that holds the lights. It sits behind the figures.
[RequireComponent(typeof(RectTransform))]
public sealed class StageLights : MonoBehaviour {
    [SerializeField] private StageLight lightPrefab;
    [Tooltip("Optional material for this layer's lights, e.g. additive for the audience light map. Empty keeps the default.")]
    [SerializeField] private Material lightMaterial;

    public StageLight CreateLight(bool canBeMoved) {
        RectTransform layer = (RectTransform)transform;
        StageLight stageLight = Instantiate(lightPrefab, layer, false);
        stageLight.Initialize(layer, canBeMoved);

        if (lightMaterial != null)
            stageLight.SetMaterial(lightMaterial);

        return stageLight;
    }
}
