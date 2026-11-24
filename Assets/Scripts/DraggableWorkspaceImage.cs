using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A quick-access slot in the Shadows window. Dragging a filled slot carries a copy of the PlacedFigure
// prefab; dropping that copy on the stage places it.
public sealed class DraggableWorkspaceImage : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    [SerializeField] private Image image;
    [SerializeField] private PlacedFigure placedFigurePrefab;

    private ShadowWorkspaceUI owner;
    private RectTransform stageArea;
    private Canvas canvas;
    private PlacedFigure draggedFigure;
    private Sprite sprite;

    public Sprite Sprite => sprite;

    public void Initialize(ShadowWorkspaceUI workspace, RectTransform configuredStageArea, Canvas configuredCanvas) {
        owner = workspace;
        stageArea = configuredStageArea;
        canvas = configuredCanvas;
    }

    public void SetSprite(Sprite newSprite) {
        sprite = newSprite;
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (sprite == null || draggedFigure != null)
            return;

        draggedFigure = Instantiate(placedFigurePrefab, canvas.transform, false);
        draggedFigure.PrepareForCarry(sprite);

        // Match the stage's size on screen while carrying, in case the window has been scaled.
        float scale = stageArea.lossyScale.x / canvas.transform.lossyScale.x;
        draggedFigure.transform.localScale = new Vector3(scale, scale, 1f);
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

        if (droppedOnStage) {
            draggedFigure.PlaceOnStage(stageArea);
            owner.OnFigurePlaced(draggedFigure);
        } else
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
