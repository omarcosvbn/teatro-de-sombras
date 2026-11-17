using UnityEngine;

// A window on the table. Its WindowDragHandle (title bar) moves it and its WindowResizeHandles (corners)
// resize it; it always stays inside its parent (the canvas).
[RequireComponent(typeof(RectTransform))]
public sealed class DraggableWindow : MonoBehaviour {
    [SerializeField] private Vector2 minSize = new Vector2(320f, 240f);
    [Tooltip("Used to hide the window without disabling it, so things like audio keep running while it is closed.")]
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform rectTransform;

    public Vector2 MinSize => minSize;
    public bool IsOpen => canvasGroup == null ? gameObject.activeSelf : canvasGroup.alpha > 0f;
    public Rect ParentBounds => ParentRect.rect;

    private RectTransform CachedRect {
        get {
            if (rectTransform == null)
                rectTransform = (RectTransform)transform;
            return rectTransform;
        }
    }

    private RectTransform ParentRect => (RectTransform)CachedRect.parent;

    public void Open() {
        gameObject.SetActive(true);
        SetVisible(true);
        BringToFront();
        ClampInsideParent();
    }

    public void Close() {
        if (canvasGroup != null)
            SetVisible(false);
        else
            gameObject.SetActive(false);
    }

    public void BringToFront() {
        CachedRect.SetAsLastSibling();
    }

    // Centres the window on a point in its parent's space, kept inside the parent.
    public void CenterOn(Vector2 parentPoint) {
        Rect rect = GetParentSpaceRect();
        MoveTo(parentPoint - rect.size * 0.5f);
    }

    // Moves the window's bottom-left corner to a point in its parent's space, kept inside the parent.
    public void MoveTo(Vector2 bottomLeft) {
        Rect rect = GetParentSpaceRect();
        Rect bounds = ParentBounds;
        rect.x = Mathf.Clamp(bottomLeft.x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - rect.width));
        rect.y = Mathf.Clamp(bottomLeft.y, bounds.yMin, Mathf.Max(bounds.yMin, bounds.yMax - rect.height));
        SetParentSpaceRect(rect);
    }

    public void ClampInsideParent() {
        MoveTo(GetParentSpaceRect().position);
    }

    public bool TryGetParentPoint(Vector2 screenPosition, Camera eventCamera, out Vector2 parentPoint) {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(ParentRect, screenPosition, eventCamera, out parentPoint);
    }

    // The window's rectangle in its parent's local space.
    public Rect GetParentSpaceRect() {
        Vector2 size = CachedRect.rect.size;
        Vector2 pivot = CachedRect.pivot;
        Vector2 position = CachedRect.localPosition;
        return new Rect(position - Vector2.Scale(size, pivot), size);
    }

    public void SetParentSpaceRect(Rect rect) {
        CachedRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect.width);
        CachedRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.height);
        Vector2 position = rect.position + Vector2.Scale(rect.size, CachedRect.pivot);
        CachedRect.localPosition = new Vector3(position.x, position.y, 0f);
    }

    private void SetVisible(bool visible) {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
