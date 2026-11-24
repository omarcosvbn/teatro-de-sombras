using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A touch colour picker: drag around the ring to choose the hue, and inside the square to choose
// saturation (left to right) and brightness (bottom to top).
[RequireComponent(typeof(RectTransform))]
public sealed class ColorWheel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler {
    private const int NoPointer = int.MinValue;

    private enum Target {
        None,
        Hue,
        SaturationValue
    }

    [Tooltip("Inner edge of the hue ring, as a fraction of the wheel's radius (must match the HueRing sprite).")]
    [SerializeField, Range(0f, 1f)] private float ringInnerRadius = 0.78f;
    [SerializeField] private RectTransform saturationValueArea;
    [Tooltip("Tinted with the pure hue; the white and black gradients sit on top of it.")]
    [SerializeField] private Image saturationValueBase;
    [SerializeField] private RectTransform hueMarker;
    [SerializeField] private RectTransform saturationValueMarker;

    private RectTransform rectTransform;
    private float hue;
    private float saturation = 1f;
    private float value = 1f;
    private Target target;
    private int pointerId = NoPointer;

    public event Action<Color> ColorChanged;

    public Color Color => Color.HSVToRGB(hue, saturation, value);

    private void Awake() {
        rectTransform = (RectTransform)transform;
        Refresh();
    }

    // Shows a colour without raising ColorChanged.
    public void SetColor(Color color) {
        Color.RGBToHSV(color, out hue, out saturation, out value);
        Refresh();
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (pointerId != NoPointer)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 point))
            return;

        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
        float distance = (point - rectTransform.rect.center).magnitude / radius;

        if (distance >= ringInnerRadius - 0.05f && distance <= 1.05f)
            target = Target.Hue;
        else if (saturationValueArea.rect.Contains(saturationValueArea.InverseTransformPoint(rectTransform.TransformPoint(point))))
            target = Target.SaturationValue;
        else
            return;

        pointerId = eventData.pointerId;
        Apply(eventData);
    }

    public void OnDrag(PointerEventData eventData) {
        if (eventData.pointerId == pointerId)
            Apply(eventData);
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (eventData.pointerId == pointerId) {
            pointerId = NoPointer;
            target = Target.None;
        }
    }

    private void Apply(PointerEventData eventData) {
        if (target == Target.Hue) {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 point))
                return;

            Vector2 direction = point - rectTransform.rect.center;
            hue = Mathf.Repeat(Mathf.Atan2(direction.y, direction.x) / (2f * Mathf.PI), 1f);
        } else if (target == Target.SaturationValue) {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(saturationValueArea, eventData.position, eventData.pressEventCamera, out Vector2 point))
                return;

            Rect area = saturationValueArea.rect;
            saturation = Mathf.Clamp01(Mathf.InverseLerp(area.xMin, area.xMax, point.x));
            value = Mathf.Clamp01(Mathf.InverseLerp(area.yMin, area.yMax, point.y));
        }

        Refresh();
        ColorChanged?.Invoke(Color);
    }

    private void Refresh() {
        if (rectTransform == null)
            rectTransform = (RectTransform)transform;

        saturationValueBase.color = Color.HSVToRGB(hue, 1f, 1f);

        float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
        float ringMiddle = radius * (1f + ringInnerRadius) * 0.5f;
        float angle = hue * 2f * Mathf.PI;
        hueMarker.localPosition = rectTransform.rect.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringMiddle;

        Rect area = saturationValueArea.rect;
        saturationValueMarker.localPosition = new Vector2(
            Mathf.Lerp(area.xMin, area.xMax, saturation),
            Mathf.Lerp(area.yMin, area.yMax, value));
    }
}
