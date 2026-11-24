using UnityEngine;
using UnityEngine.UI;

// The figure layer of the Light window's stage: display-only copies of the figures placed in the
// Shadows window, so the light technician sees the same scene but can't move them.
[RequireComponent(typeof(RectTransform))]
public sealed class FigureMirrorLayer : MonoBehaviour {
    [SerializeField] private Image mirrorPrefab;

    public Image CreateMirror(Sprite sprite) {
        Image mirror = Instantiate(mirrorPrefab, transform, false);
        mirror.sprite = sprite;
        mirror.raycastTarget = false;
        return mirror;
    }
}
