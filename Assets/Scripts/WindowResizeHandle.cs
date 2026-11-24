using UnityEngine;
using UnityEngine.EventSystems;

public enum WindowCorner {
    BottomLeft,
    BottomRight,
    TopLeft,
    TopRight
}

// Put on a window's corner: one finger drags that corner to scale the whole window up or down.
// The opposite corner stays where it is.
public sealed class WindowResizeHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private const int NoPointer = int.MinValue;

    [SerializeField] private DraggableWindow window;
    [SerializeField] private WindowCorner corner;

    private int dragPointerId = NoPointer;
    private Vector2 startPointerPosition;
    private Rect startRect;
    private float startScale;

    public void OnPointerDown(PointerEventData eventData) {
        window.BringToFront();
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (window.IsLocked || dragPointerId != NoPointer)
            return;

        if (!window.TryGetParentPoint(eventData.position, eventData.pressEventCamera, out startPointerPosition))
            return;

        dragPointerId = eventData.pointerId;
        startRect = window.GetParentSpaceRect();
        startScale = window.Scale;
    }

    public void OnDrag(PointerEventData eventData) {
        if (eventData.pointerId != dragPointerId)
            return;

        if (!window.TryGetParentPoint(eventData.position, eventData.pressEventCamera, out Vector2 point))
            return;

        bool movesRightEdge = corner == WindowCorner.BottomRight || corner == WindowCorner.TopRight;
        bool movesTopEdge = corner == WindowCorner.TopLeft || corner == WindowCorner.TopRight;
        Vector2 delta = point - startPointerPosition;

        float widthFactor = (startRect.width + (movesRightEdge ? delta.x : -delta.x)) / startRect.width;
        float heightFactor = (startRect.height + (movesTopEdge ? delta.y : -delta.y)) / startRect.height;
        float scale = Mathf.Clamp(startScale * (widthFactor + heightFactor) * 0.5f, window.MinScale, window.MaxScale);

        // Don't let the moving edges leave the screen.
        Rect bounds = window.ParentBounds;
        Vector2 unscaled = window.UnscaledSize;
        float roomX = movesRightEdge ? bounds.xMax - startRect.xMin : startRect.xMax - bounds.xMin;
        float roomY = movesTopEdge ? bounds.yMax - startRect.yMin : startRect.yMax - bounds.yMin;
        // When the top edge moves, the tab above it needs room too.
        float unscaledHeight = movesTopEdge ? unscaled.y + window.TopOverhang : unscaled.y;
        scale = Mathf.Min(scale, roomX / unscaled.x, roomY / unscaledHeight);

        Vector2 size = unscaled * scale;
        float x = movesRightEdge ? startRect.xMin : startRect.xMax - size.x;
        float y = movesTopEdge ? startRect.yMin : startRect.yMax - size.y;
        window.SetScale(scale, new Vector2(x, y));
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (eventData.pointerId == dragPointerId)
            dragPointerId = NoPointer;
    }
}
