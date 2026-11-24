using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A figure that has been dropped on the stage. Unlocked figures can be dragged around the stage;
// a tap opens the FigureMenu to lock/unlock or delete the figure.
[RequireComponent(typeof(RectTransform))]
public sealed class PlacedFigure : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private const int NoPointer = int.MinValue;

    [SerializeField] private Image image;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Outline lockOutline;
    [SerializeField] private FigureMenu figureMenuPrefab;
    [SerializeField, Min(0f)] private float menuTimeout = 4f;
    [SerializeField, Min(0f)] private float menuGap = 8f;

    private RectTransform stageArea;
    private RectTransform rectTransform;
    private bool isLocked;
    private int dragPointerId = NoPointer;
    private Vector2 dragOffset;
    private FigureMenu menu;
    // Copies on other stages (the Light window's, the audience display's), by layer.
    private readonly Dictionary<FigureMirrorLayer, Image> mirrors = new Dictionary<FigureMirrorLayer, Image>();
    private Coroutine menuTimeoutCoroutine;

    public bool IsLocked => isLocked;

    private void Awake() {
        rectTransform = (RectTransform)transform;
        lockOutline.enabled = false;
    }

    // Called while the figure is being carried from the catalog: it shows the sprite and lets
    // raycasts pass through so the drop target underneath can be detected.
    public void PrepareForCarry(Sprite sprite) {
        image.sprite = sprite;
        name = sprite.name;
        canvasGroup.blocksRaycasts = false;
    }

    // Called once the figure has been dropped on the stage.
    public void PlaceOnStage(RectTransform configuredStageArea) {
        stageArea = configuredStageArea;
        rectTransform.SetParent(stageArea, true);
        rectTransform.localScale = Vector3.one;
        canvasGroup.blocksRaycasts = true;
        MoveTo(rectTransform.localPosition);
    }

    // Shows a copy of this figure on another stage and keeps it in step.
    public void AttachMirror(FigureMirrorLayer mirrorLayer) {
        if (mirrorLayer == null || mirrors.ContainsKey(mirrorLayer))
            return;

        mirrors.Add(mirrorLayer, mirrorLayer.CreateMirror(image.sprite));
        SyncMirror();
    }

    private void SyncMirror() {
        foreach (Image mirror in mirrors.Values) {
            if (mirror == null)
                continue;

            RectTransform mirrorRect = mirror.rectTransform;
            mirrorRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.rect.width);
            mirrorRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.rect.height);
            mirrorRect.localPosition = rectTransform.localPosition;
        }
    }

    // ---------- Tap ----------

    public void OnPointerClick(PointerEventData eventData) {
        // A press that turned into a drag is not a tap.
        if (eventData.dragging || stageArea == null)
            return;

        if (menu != null)
            CloseMenu();
        else
            OpenMenu();
    }

    // ---------- Drag ----------

    public void OnBeginDrag(PointerEventData eventData) {
        // Locked figures stay put, and only one finger moves a figure at a time.
        if (stageArea == null || isLocked || dragPointerId != NoPointer)
            return;

        if (!TryGetStagePoint(eventData, out Vector2 point))
            return;

        dragPointerId = eventData.pointerId;
        dragOffset = (Vector2)rectTransform.localPosition - point;
        CloseMenu();
    }

    public void OnDrag(PointerEventData eventData) {
        if (eventData.pointerId != dragPointerId)
            return;

        if (TryGetStagePoint(eventData, out Vector2 point))
            MoveTo(point + dragOffset);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (eventData.pointerId == dragPointerId)
            dragPointerId = NoPointer;
    }

    private bool TryGetStagePoint(PointerEventData eventData, out Vector2 point) {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            stageArea, eventData.position, eventData.pressEventCamera, out point);
    }

    // Moves the figure to a point in stage space, kept fully inside the stage.
    private void MoveTo(Vector2 localPoint) {
        Rect stageRect = stageArea.rect;
        Vector2 size = rectTransform.rect.size;
        Vector2 pivot = rectTransform.pivot;

        localPoint.x = Mathf.Clamp(localPoint.x, stageRect.xMin + size.x * pivot.x, stageRect.xMax - size.x * (1f - pivot.x));
        localPoint.y = Mathf.Clamp(localPoint.y, stageRect.yMin + size.y * pivot.y, stageRect.yMax - size.y * (1f - pivot.y));
        rectTransform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
        SyncMirror();
    }

    // ---------- Lock / delete ----------

    public void SetLocked(bool locked) {
        isLocked = locked;

        if (locked)
            dragPointerId = NoPointer;

        lockOutline.enabled = locked;
    }

    public void Delete() {
        CloseMenu();
        Destroy(gameObject);
    }

    // ---------- Menu ----------

    private void OpenMenu() {
        CloseOtherMenusOnStage();

        menu = Instantiate(figureMenuPrefab, stageArea, false);
        menu.Bind(isLocked,
            () => {
                SetLocked(!isLocked);
                CloseMenu();
            },
            Delete);

        RectTransform menuRect = (RectTransform)menu.transform;
        menuRect.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);
        PositionMenu(menuRect);

        menuTimeoutCoroutine = StartCoroutine(CloseMenuAfterTimeout());
    }

    private void CloseOtherMenusOnStage() {
        foreach (PlacedFigure figure in stageArea.GetComponentsInChildren<PlacedFigure>()) {
            if (figure != this)
                figure.CloseMenu();
        }
    }

    // Shows the menu above the figure, or below it when there is no room above, kept inside the stage.
    private void PositionMenu(RectTransform menuRect) {
        Rect stageRect = stageArea.rect;
        Vector2 figureSize = rectTransform.rect.size;
        Vector2 figurePivot = rectTransform.pivot;
        Vector2 figurePosition = rectTransform.localPosition;
        Vector2 menuSize = menuRect.rect.size;

        float figureTop = figurePosition.y + figureSize.y * (1f - figurePivot.y);
        float figureBottom = figurePosition.y - figureSize.y * figurePivot.y;
        float figureCenterX = figurePosition.x + figureSize.x * (0.5f - figurePivot.x);

        float y;
        if (figureTop + menuGap + menuSize.y <= stageRect.yMax) {
            menuRect.pivot = new Vector2(0.5f, 0f);
            y = figureTop + menuGap;
        } else {
            menuRect.pivot = new Vector2(0.5f, 1f);
            y = Mathf.Max(figureBottom - menuGap, stageRect.yMin + menuSize.y);
        }

        float halfWidth = menuSize.x * 0.5f;
        float x = Mathf.Clamp(figureCenterX, stageRect.xMin + halfWidth, stageRect.xMax - halfWidth);
        menuRect.localPosition = new Vector3(x, y, 0f);
    }

    private IEnumerator CloseMenuAfterTimeout() {
        yield return new WaitForSecondsRealtime(menuTimeout);
        menuTimeoutCoroutine = null;
        CloseMenu();
    }

    public void CloseMenu() {
        if (menuTimeoutCoroutine != null) {
            StopCoroutine(menuTimeoutCoroutine);
            menuTimeoutCoroutine = null;
        }

        if (menu != null) {
            Destroy(menu.gameObject);
            menu = null;
        }
    }

    private void OnDisable() {
        CloseMenu();
    }

    private void OnDestroy() {
        foreach (Image mirror in mirrors.Values) {
            if (mirror != null)
                Destroy(mirror.gameObject);
        }
    }
}
