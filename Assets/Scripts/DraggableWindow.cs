using UnityEngine;

// A window on the table. Its WindowDragHandles move it and its WindowResizeHandles (corners) scale it
// up or down; it always stays inside its parent (the canvas). A locked window can't be moved or scaled.
[RequireComponent(typeof(RectTransform))]
public sealed class DraggableWindow : MonoBehaviour {
    [Tooltip("Smallest and largest scale the corner handles can resize the window to.")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.6f, 2f);
    [Tooltip("Used to hide the window without disabling it, so things like audio keep running while it is closed.")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("How far something sticks out above the window (the lock/delete tab), so it stays on screen too.")]
    [SerializeField] private float topOverhang;

    private RectTransform rectTransform;

    public float MinScale => scaleRange.x;
    public float MaxScale => scaleRange.y;
    public float Scale => CachedRect.localScale.x;
    public Vector2 UnscaledSize => CachedRect.rect.size;
    public bool IsOpen => gameObject.activeSelf && (canvasGroup == null || canvasGroup.alpha > 0f);
    public Rect ParentBounds => ParentRect.rect;
    public float TopOverhang => topOverhang;
    public bool IsLocked { get; private set; }

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

    // Locking only stops the handles; the window's contents still work.
    public void SetLocked(bool locked) {
        IsLocked = locked;
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
        Vector2 size = GetParentSpaceRect().size;
        Rect bounds = ParentBounds;
        bottomLeft.x = Mathf.Clamp(bottomLeft.x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - size.x));
        float top = size.y + topOverhang * Scale;
        bottomLeft.y = Mathf.Clamp(bottomLeft.y, bounds.yMin, Mathf.Max(bounds.yMin, bounds.yMax - top));
        Place(bottomLeft);
    }

    // Scales the window and puts its bottom-left corner at the given point (used by the resize handles).
    public void SetScale(float scale, Vector2 bottomLeft) {
        CachedRect.localScale = new Vector3(scale, scale, 1f);
        Place(bottomLeft);
    }

    public void ClampInsideParent() {
        MoveTo(GetParentSpaceRect().position);
    }

    public bool TryGetParentPoint(Vector2 screenPosition, Camera eventCamera, out Vector2 parentPoint) {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(ParentRect, screenPosition, eventCamera, out parentPoint);
    }

    // The window's rectangle in its parent's local space, including its scale.
    public Rect GetParentSpaceRect() {
        Vector2 size = CachedRect.rect.size * Scale;
        Vector2 position = CachedRect.localPosition;
        return new Rect(position - Vector2.Scale(size, CachedRect.pivot), size);
    }

    private void Place(Vector2 bottomLeft) {
        Vector2 size = CachedRect.rect.size * Scale;
        Vector2 position = bottomLeft + Vector2.Scale(size, CachedRect.pivot);
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
