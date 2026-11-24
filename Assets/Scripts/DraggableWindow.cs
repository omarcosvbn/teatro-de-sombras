using UnityEngine;

// A window on the table. Its WindowDragHandles move it and its WindowResizeHandles (corners) scale it
// up or down; it always stays inside its parent (the canvas). A locked window can't be moved or scaled.
// When it is dropped (and when it opens) it turns to face the operator of the table area it is in.
// Windows are only ever turned by multiples of 90°, so their outline stays axis-aligned on screen.
[RequireComponent(typeof(RectTransform))]
public sealed class DraggableWindow : MonoBehaviour {
    [Tooltip("Smallest and largest scale the corner handles can resize the window to.")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.6f, 2f);
    [Tooltip("Used to hide the window without disabling it, so things like audio keep running while it is closed.")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("How far something sticks out above the window (the lock/delete tab), so it stays on screen too.")]
    [SerializeField] private float topOverhang;
    [Tooltip("Which operator each part of the table belongs to. Empty keeps the window upright.")]
    [SerializeField] private TableZones tableZones;

    private RectTransform rectTransform;

    public float MinScale => scaleRange.x;
    public float MaxScale => scaleRange.y;
    public float Scale => CachedRect.localScale.x;
    public bool IsOpen => gameObject.activeSelf && (canvasGroup == null || canvasGroup.alpha > 0f);
    public Rect ParentBounds => ParentRect.rect;
    public bool IsLocked { get; private set; }
    public TableZones TableZones => tableZones;

    // The window's own rectangle, unscaled, relative to its pivot.
    public Rect LocalRect => CachedRect.rect;

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
        FaceOperator();
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
        Vector2 offset = parentPoint - LocalToParent(LocalRect.center);
        CachedRect.localPosition += (Vector3)offset;
        ClampInsideParent();
    }

    // Moves the window so the bottom-left of its outline on screen (see GetParentSpaceRect) is at a point
    // in its parent's space, kept inside the parent.
    public void MoveTo(Vector2 outlineMin) {
        Rect outline = GetParentSpaceRect();
        Rect bounds = ParentBounds;
        outlineMin.x = Mathf.Clamp(outlineMin.x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - outline.width));
        outlineMin.y = Mathf.Clamp(outlineMin.y, bounds.yMin, Mathf.Max(bounds.yMin, bounds.yMax - outline.height));
        CachedRect.localPosition += (Vector3)(outlineMin - outline.min);
    }

    public void ClampInsideParent() {
        MoveTo(GetParentSpaceRect().min);
    }

    // Scales the window so that a point of it (in LocalRect space) ends up at a point in its parent's
    // space. The resize handles use it to keep the opposite corner still.
    public void SetScale(float scale, Vector2 localPoint, Vector2 parentPoint) {
        CachedRect.localScale = new Vector3(scale, scale, 1f);
        CachedRect.localPosition += (Vector3)(parentPoint - LocalToParent(localPoint));
    }

    // Whether the window's outline (tab included) is fully inside its parent.
    public bool FitsInParent() {
        const float tolerance = 0.5f;
        Rect outline = GetParentSpaceRect();
        Rect bounds = ParentBounds;
        return outline.xMin >= bounds.xMin - tolerance && outline.xMax <= bounds.xMax + tolerance
            && outline.yMin >= bounds.yMin - tolerance && outline.yMax <= bounds.yMax + tolerance;
    }

    // Turns the window to face the operator of the table area its centre is in, keeping its centre still.
    public void FaceOperator() {
        FaceOperator(LocalToParent(LocalRect.center));
    }

    // Turns the window to face the operator of the table area a point (in its parent's space) is in, e.g.
    // where the finger let go of it, keeping its centre still. A wide window pushed against an edge can
    // have its centre in a different area from the finger, so the finger decides.
    public void FaceOperator(Vector2 parentPoint) {
        if (tableZones != null) {
            Vector2 center = LocalToParent(LocalRect.center);
            float angle = tableZones.AngleAt(parentPoint, ParentBounds);

            if (!Mathf.Approximately(Mathf.DeltaAngle(CachedRect.localEulerAngles.z, angle), 0f)) {
                CachedRect.localRotation = Quaternion.Euler(0f, 0f, angle);
                CachedRect.localPosition += (Vector3)(center - LocalToParent(LocalRect.center));
            }
        }

        ClampInsideParent();
    }

    public bool TryGetParentPoint(Vector2 screenPosition, Camera eventCamera, out Vector2 parentPoint) {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(ParentRect, screenPosition, eventCamera, out parentPoint);
    }

    // A point of the window (in LocalRect space) in its parent's space, with the window's scale and rotation.
    public Vector2 LocalToParent(Vector2 localPoint) {
        return (Vector2)CachedRect.localPosition + (Vector2)(CachedRect.localRotation * (localPoint * Scale));
    }

    // The window's outline in its parent's space, with its scale and rotation and including the tab
    // above it: the smallest upright rectangle around it.
    public Rect GetParentSpaceRect() {
        Rect local = LocalRect;
        local.yMax += topOverhang;

        Vector2 a = LocalToParent(new Vector2(local.xMin, local.yMin));
        Vector2 b = LocalToParent(new Vector2(local.xMax, local.yMin));
        Vector2 c = LocalToParent(new Vector2(local.xMin, local.yMax));
        Vector2 d = LocalToParent(new Vector2(local.xMax, local.yMax));

        Vector2 min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
        Vector2 max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void SetVisible(bool visible) {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
