using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A catalog entry. Dragging it carries a copy of the PlacedFigure prefab; dropping that copy on the stage places it.
public sealed class DraggableWorkspaceImage : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    [SerializeField] private Image image;
    [SerializeField] private PlacedFigure placedFigurePrefab;

    private RectTransform stageArea;
    private Canvas canvas;
    private PlacedFigure draggedFigure;

    public void Configure(Sprite sprite, RectTransform configuredStageArea, Canvas configuredCanvas) {
        image.sprite = sprite;
        name = sprite.name;
        stageArea = configuredStageArea;
        canvas = configuredCanvas;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        draggedFigure = Instantiate(placedFigurePrefab, canvas.transform, false);
        draggedFigure.PrepareForCarry(image.sprite);
        MoveDraggedFigure(eventData);
    }

    public void OnDrag(PointerEventData eventData) {
        if (draggedFigure != null)
            MoveDraggedFigure(eventData);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (draggedFigure == null)
            return;

        bool droppedOnStage = RectTransformUtility.RectangleContainsScreenPoint(
            stageArea, eventData.position, eventData.pressEventCamera);

        if (droppedOnStage)
            draggedFigure.PlaceOnStage(stageArea);
        else
            Destroy(draggedFigure.gameObject);

        draggedFigure = null;
    }

    private void MoveDraggedFigure(PointerEventData eventData) {
        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPosition);

        ((RectTransform)draggedFigure.transform).anchoredPosition = localPosition;
    }
}
