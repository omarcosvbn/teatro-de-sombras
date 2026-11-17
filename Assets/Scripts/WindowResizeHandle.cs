using UnityEngine;
using UnityEngine.EventSystems;

public enum WindowCorner {
    BottomLeft,
    BottomRight,
    TopLeft,
    TopRight
}

// Put on a window's corner: one finger drags that corner to resize the window.
// The opposite corner stays where it is.
public sealed class WindowResizeHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private const int NoPointer = int.MinValue;

    [SerializeField] private DraggableWindow window;
    [SerializeField] private WindowCorner corner;

    private int dragPointerId = NoPointer;
    private Vector2 startPointerPosition;
    private Rect startRect;

    public void OnPointerDown(PointerEventData eventData) {
        window.BringToFront();
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (dragPointerId != NoPointer)
            return;

        if (!window.TryGetParentPoint(eventData.position, eventData.pressEventCamera, out startPointerPosition))
            return;

        dragPointerId = eventData.pointerId;
        startRect = window.GetParentSpaceRect();
    }

    public void OnDrag(PointerEventData eventData) {
        if (eventData.pointerId != dragPointerId)
            return;

        if (!window.TryGetParentPoint(eventData.position, eventData.pressEventCamera, out Vector2 point))
            return;

        Vector2 delta = point - startPointerPosition;
        Rect bounds = window.ParentBounds;
        Vector2 minSize = window.MinSize;

        float xMin = startRect.xMin;
        float xMax = startRect.xMax;
        float yMin = startRect.yMin;
        float yMax = startRect.yMax;

        bool movesRightEdge = corner == WindowCorner.BottomRight || corner == WindowCorner.TopRight;
        bool movesTopEdge = corner == WindowCorner.TopLeft || corner == WindowCorner.TopRight;

        if (movesRightEdge)
            xMax = Mathf.Clamp(startRect.xMax + delta.x, xMin + minSize.x, bounds.xMax);
        else
            xMin = Mathf.Clamp(startRect.xMin + delta.x, bounds.xMin, xMax - minSize.x);

        if (movesTopEdge)
            yMax = Mathf.Clamp(startRect.yMax + delta.y, yMin + minSize.y, bounds.yMax);
        else
            yMin = Mathf.Clamp(startRect.yMin + delta.y, bounds.yMin, yMax - minSize.y);

        window.SetParentSpaceRect(Rect.MinMaxRect(xMin, yMin, xMax, yMax));
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (eventData.pointerId == dragPointerId)
            dragPointerId = NoPointer;
    }
}
