using UnityEngine;
using UnityEngine.EventSystems;

// Put on a window's drag bars: one finger drags the whole window. When it is let go, the window turns to
// face the operator of the table area it was dropped in.
public sealed class WindowDragHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private const int NoPointer = int.MinValue;

    [SerializeField] private DraggableWindow window;

    private int dragPointerId = NoPointer;
    private Vector2 startPointerPosition;
    private Vector2 startWindowPosition;

    public void OnPointerDown(PointerEventData eventData) {
        window.BringToFront();
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (window.IsLocked || dragPointerId != NoPointer)
            return;

        if (!window.TryGetParentPoint(eventData.position, eventData.pressEventCamera, out startPointerPosition))
            return;

        dragPointerId = eventData.pointerId;
        startWindowPosition = window.GetParentSpaceRect().position;
    }

    public void OnDrag(PointerEventData eventData) {
        if (eventData.pointerId != dragPointerId)
            return;

        if (window.TryGetParentPoint(eventData.position, eventData.pressEventCamera, out Vector2 point))
            window.MoveTo(startWindowPosition + point - startPointerPosition);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (eventData.pointerId != dragPointerId)
            return;

        dragPointerId = NoPointer;

        if (window.TryGetParentPoint(eventData.position, eventData.pressEventCamera, out Vector2 point))
            window.FaceOperator(point);
        else
            window.FaceOperator();
    }
}
