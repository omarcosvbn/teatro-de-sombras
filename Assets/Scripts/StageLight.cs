using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One light shown on a stage: a soft glow shape. On the Light window's stage it can be dragged with one
// finger; on the Shadows window's stage it is a display-only copy that follows it.
[RequireComponent(typeof(RectTransform))]
public sealed class StageLight : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private const int NoPointer = int.MinValue;

    [SerializeField] private Image image;
    [SerializeField, Min(1f)] private float baseSize = 160f;

    private bool interactive = true;
    private RectTransform layer;
    private RectTransform rectTransform;
    private LightLook look = LightLook.Default;
    private float intensity = 1f;
    private bool isOn = true;
    private int dragPointerId = NoPointer;
    private Vector2 dragOffset;

    // Raised with the light's new local position whenever it is dragged.
    public event Action<Vector2> Moved;

    public void Initialize(RectTransform lightLayer, bool canBeMoved) {
        layer = lightLayer;
        rectTransform = (RectTransform)transform;
        interactive = canBeMoved;
        image.raycastTarget = canBeMoved;
    }

    // Places the light without raising Moved (used by the display-only copy).
    public void SetPosition(Vector2 localPoint) {
        rectTransform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
    }

    public void SetLook(LightLook newLook, Sprite sprite) {
        look = newLook;
        image.sprite = sprite;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, baseSize * look.size);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseSize * look.size);
        Refresh();
    }

    public void SetMaterial(Material material) {
        image.material = material;
    }

    public void SetIntensity(float value) {
        intensity = value;
        Refresh();
    }

    public void SetOn(bool on) {
        isOn = on;
        Refresh();
    }

    public void Remove() {
        Destroy(gameObject);
    }

    private void Refresh() {
        Color color = look.color;
        color.a = intensity;
        image.color = color;
        image.enabled = isOn;
    }

    // ---------- Drag ----------

    public void OnBeginDrag(PointerEventData eventData) {
        if (!interactive || dragPointerId != NoPointer || !TryGetLayerPoint(eventData, out Vector2 point))
            return;

        dragPointerId = eventData.pointerId;
        dragOffset = (Vector2)rectTransform.localPosition - point;
    }

    public void OnDrag(PointerEventData eventData) {
        if (eventData.pointerId != dragPointerId)
            return;

        if (TryGetLayerPoint(eventData, out Vector2 point))
            MoveTo(point + dragOffset);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (eventData.pointerId == dragPointerId)
            dragPointerId = NoPointer;
    }

    private bool TryGetLayerPoint(PointerEventData eventData, out Vector2 point) {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, eventData.position, eventData.pressEventCamera, out point);
    }

    // Keeps the light's centre on the stage; its glow may spill past the edge, where the layer clips it.
    private void MoveTo(Vector2 localPoint) {
        Rect bounds = layer.rect;
        localPoint.x = Mathf.Clamp(localPoint.x, bounds.xMin, bounds.xMax);
        localPoint.y = Mathf.Clamp(localPoint.y, bounds.yMin, bounds.yMax);
        rectTransform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
        Moved?.Invoke(localPoint);
    }
}
