using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DraggableWorkspaceImage : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private Sprite image;
    private RectTransform stageArea;
    private Canvas canvas;
    private float itemSize;
    private RectTransform draggedItem;
    private CanvasGroup draggedGroup;

    public void Configure(Sprite configuredImage, RectTransform configuredStageArea, Canvas configuredCanvas, float configuredItemSize) {
        image = configuredImage;
        stageArea = configuredStageArea;
        canvas = configuredCanvas;
        itemSize = configuredItemSize;
    }

    public void OnBeginDrag(PointerEventData eventData) {
        draggedItem = CreateDraggedItem();
        draggedGroup = draggedItem.gameObject.AddComponent<CanvasGroup>();
        draggedGroup.blocksRaycasts = false;
        MoveDraggedItem(eventData);
    }

    public void OnDrag(PointerEventData eventData) {
        MoveDraggedItem(eventData);
    }

    public void OnEndDrag(PointerEventData eventData) {
        bool droppedOnStage = RectTransformUtility.RectangleContainsScreenPoint(
            stageArea, eventData.position, eventData.pressEventCamera);

        if (droppedOnStage) {
            draggedItem.SetParent(stageArea, true);
            draggedGroup.blocksRaycasts = true;
            ClampToStage();
        } else {
            Destroy(draggedItem.gameObject);
        }

    }

    private RectTransform CreateDraggedItem() {
        GameObject itemObject = new GameObject(image.name, typeof(RectTransform), typeof(Image));
        itemObject.transform.SetParent(canvas.transform, false);

        Image itemImage = itemObject.GetComponent<Image>();
        itemImage.sprite = image;
        itemImage.preserveAspect = true;
        itemImage.raycastTarget = true;

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(itemSize, itemSize);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        return itemRect;
    }

    private void MoveDraggedItem(PointerEventData eventData) {
        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPosition);

        draggedItem.anchoredPosition = localPosition;
    }

    private void ClampToStage() {
        Vector2 position = draggedItem.anchoredPosition;
        Vector2 itemHalfSize = draggedItem.rect.size * 0.5f;
        Vector2 stageHalfSize = stageArea.rect.size * 0.5f;

        position.x = Mathf.Clamp(position.x, -stageHalfSize.x + itemHalfSize.x, stageHalfSize.x - itemHalfSize.x);
        position.y = Mathf.Clamp(position.y, -stageHalfSize.y + itemHalfSize.y, stageHalfSize.y - itemHalfSize.y);
        draggedItem.anchoredPosition = position;
    }
}
