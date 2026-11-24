using UnityEngine;

// The layer of a stage that holds the lights. It sits behind the figures.
[RequireComponent(typeof(RectTransform))]
public sealed class StageLights : MonoBehaviour {
    [SerializeField] private StageLight lightPrefab;

    public StageLight CreateLight(bool canBeMoved) {
        RectTransform layer = (RectTransform)transform;
        StageLight stageLight = Instantiate(lightPrefab, layer, false);
        stageLight.Initialize(layer, canBeMoved);
        return stageLight;
    }
}
