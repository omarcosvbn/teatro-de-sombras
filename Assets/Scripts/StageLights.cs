using UnityEngine;

// The layer of the Shadows window's stage that holds the lights. It sits behind the figures.
[RequireComponent(typeof(RectTransform))]
public sealed class StageLights : MonoBehaviour {
    [SerializeField] private StageLight lightPrefab;

    public StageLight CreateLight() {
        RectTransform layer = (RectTransform)transform;
        StageLight stageLight = Instantiate(lightPrefab, layer, false);
        stageLight.Initialize(layer);
        return stageLight;
    }
}
