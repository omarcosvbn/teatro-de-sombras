using UnityEngine;
using UnityEngine.EventSystems;

public enum WindowCorner {
    BottomLeft,
    BottomRight,
    TopLeft,
    TopRight
}

// Put on a window's corner: one finger drags that corner to scale the whole window up or down.
// The opposite corner stays where it is. Corners are the window's own, so this works however the window
// is turned.
public sealed class WindowResizeHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private const int NoPointer = int.MinValue;
    private const int FitSteps = 12;

    [SerializeField] private DraggableWindow window;
    [SerializeField] private WindowCorner corner;

    private int dragPointerId = NoPointer;
    private Vector2 startPointerPosition;
    private float startScale;
    // The dragged corner and the opposite (still) corner: in the window's own space, and on screen at the start.
    private Vector2 movingCornerLocal;
    private Vector2 stillCornerLocal;
    private Vector2 movingCornerStart;
    private Vector2 stillCornerStart;

    public void OnPointerDown(PointerEventData eventData) {
        window.BringToFront();
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (window.IsLocked || dragPointerId != NoPointer)
            return;

        if (!window.TryGetParentPoint(eventData.position, eventData.pressEventCamera, out startPointerPosition))
            return;

        Rect rect = window.LocalRect;
        bool right = corner == WindowCorner.BottomRight || corner == WindowCorner.TopRight;
        bool top = corner == WindowCorner.TopLeft || corner == WindowCorner.TopRight;
        movingCornerLocal = new Vector2(right ? rect.xMax : rect.xMin, top ? rect.yMax : rect.yMin);
        stillCornerLocal = new Vector2(right ? rect.xMin : rect.xMax, top ? rect.yMin : rect.yMax);

        dragPointerId = eventData.pointerId;
        startScale = window.Scale;
        movingCornerStart = window.LocalToParent(movingCornerLocal);
        stillCornerStart = window.LocalToParent(stillCornerLocal);
    }

    public void OnDrag(PointerEventData eventData) {
        if (eventData.pointerId != dragPointerId)
            return;

        if (!window.TryGetParentPoint(eventData.position, eventData.pressEventCamera, out Vector2 point))
            return;

        // How far the dragged corner has moved along the window's diagonal decides the new scale.
        Vector2 diagonal = movingCornerStart - stillCornerStart;
        Vector2 movingCorner = movingCornerStart + (point - startPointerPosition);
        float factor = Vector2.Dot(movingCorner - stillCornerStart, diagonal) / diagonal.sqrMagnitude;
        float scale = Mathf.Clamp(startScale * factor, window.MinScale, window.MaxScale);

        window.SetScale(scale, stillCornerLocal, stillCornerStart);

        if (window.FitsInParent())
            return;

        // Too big for the screen: find the largest scale between the start and the wanted one that fits.
        float fits = startScale;
        float tooBig = scale;

        for (int i = 0; i < FitSteps; i++) {
            float middle = (fits + tooBig) * 0.5f;
            window.SetScale(middle, stillCornerLocal, stillCornerStart);

            if (window.FitsInParent())
                fits = middle;
            else
                tooBig = middle;
        }

        window.SetScale(fits, stillCornerLocal, stillCornerStart);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (eventData.pointerId == dragPointerId)
            dragPointerId = NoPointer;
    }
}
