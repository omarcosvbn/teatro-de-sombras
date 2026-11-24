using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A figure (or scenery image) that has been dropped on the stage. Unlocked figures can be dragged with one
// finger and resized with two (pinch), or with the mouse wheel; a tap opens the FigureMenu to lock/unlock
// or delete the figure. A figure may go past the stage's edges (the stage clips it); one left completely
// outside when the fingers lift is deleted.
[RequireComponent(typeof(RectTransform))]
public sealed class PlacedFigure : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler {
    private const int NoPointer = int.MinValue;

    [SerializeField] private Image image;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Outline lockOutline;
    [SerializeField] private FigureMenu figureMenuPrefab;
    [SerializeField, Min(0f)] private float menuTimeout = 4f;
    [SerializeField, Min(0f)] private float menuGap = 8f;

    [Header("Resize")]
    [Tooltip("Smallest size, in stage units, of the figure's shorter side.")]
    [SerializeField, Min(1f)] private float minSize = 24f;
    [Tooltip("Largest size, in stage units, of the figure's longer side (the stage is 450 wide).")]
    [SerializeField, Min(1f)] private float maxSize = 900f;
    [Tooltip("How much one mouse-wheel step resizes the figure (for testing without a touch screen).")]
    [SerializeField, Min(1.01f)] private float wheelStep = 1.1f;

    private RectTransform stageArea;
    private RectTransform rectTransform;
    private bool isLocked;
    private int dragPointerId = NoPointer;
    private Vector2 dragOffset;
    private FigureMenu menu;
    // Copies on other stages (the Light window's, the audience display's), by layer.
    private readonly Dictionary<FigureMirrorLayer, Image> mirrors = new Dictionary<FigureMirrorLayer, Image>();
    private Coroutine menuTimeoutCoroutine;

    // Fingers currently pressing this figure, by pointer ID, with their screen positions.
    private readonly Dictionary<int, Vector2> pointers = new Dictionary<int, Vector2>();
    private Camera pointerCamera;
    private bool pinching;
    private int pinchPointerA;
    private int pinchPointerB;
    private float pinchStartDistance;
    private Vector2 pinchStartSize;
    private Vector2 pinchStartMidpoint;
    private Vector2 pinchStartPosition;
    // A pinch ends with fingers lifting, which must not count as a tap.
    private bool ignoreNextTap;

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

    // ---------- Fingers ----------

    public void OnPointerDown(PointerEventData eventData) {
        if (stageArea == null)
            return;

        if (pointers.Count == 0)
            ignoreNextTap = false;

        pointers[eventData.pointerId] = eventData.position;
        pointerCamera = eventData.pressEventCamera;

        if (!isLocked && !pinching && pointers.Count >= 2)
            StartPinch();
    }

    public void OnPointerUp(PointerEventData eventData) {
        pointers.Remove(eventData.pointerId);

        if (!pinching || (eventData.pointerId != pinchPointerA && eventData.pointerId != pinchPointerB))
            return;

        pinching = false;

        if (pointers.Count == 0) {
            DeleteIfOffStage();
            return;
        }

        // The finger still down carries on moving the figure.
        int remaining = eventData.pointerId == pinchPointerA ? pinchPointerB : pinchPointerA;

        if (pointers.TryGetValue(remaining, out Vector2 screenPoint) && TryGetStagePoint(screenPoint, out Vector2 point)) {
            dragPointerId = remaining;
            dragOffset = (Vector2)rectTransform.localPosition - point;
        }
    }

    // ---------- Tap ----------

    public void OnPointerClick(PointerEventData eventData) {
        // A press that turned into a drag or a pinch is not a tap.
        if (eventData.dragging || stageArea == null || ignoreNextTap)
            return;

        if (menu != null)
            CloseMenu();
        else
            OpenMenu();
    }

    // ---------- Drag ----------

    public void OnBeginDrag(PointerEventData eventData) {
        // Locked figures stay put, and only one finger moves a figure at a time (two fingers pinch).
        if (stageArea == null || isLocked || pinching || dragPointerId != NoPointer)
            return;

        if (!TryGetStagePoint(eventData.position, out Vector2 point))
            return;

        dragPointerId = eventData.pointerId;
        dragOffset = (Vector2)rectTransform.localPosition - point;
        CloseMenu();
    }

    public void OnDrag(PointerEventData eventData) {
        if (pointers.ContainsKey(eventData.pointerId))
            pointers[eventData.pointerId] = eventData.position;

        if (pinching) {
            if (eventData.pointerId == pinchPointerA || eventData.pointerId == pinchPointerB)
                UpdatePinch();

            return;
        }

        if (eventData.pointerId != dragPointerId)
            return;

        if (TryGetStagePoint(eventData.position, out Vector2 point))
            MoveTo(point + dragOffset);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (eventData.pointerId != dragPointerId)
            return;

        dragPointerId = NoPointer;
        DeleteIfOffStage();
    }

    private bool TryGetStagePoint(Vector2 screenPoint, out Vector2 point) {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(stageArea, screenPoint, pointerCamera, out point);
    }

    // ---------- Resize ----------

    // Two fingers on the figure: it grows or shrinks with the distance between them and follows their midpoint.
    private void StartPinch() {
        // The first two fingers pinch; any further ones are ignored.
        bool foundFirst = false;

        foreach (int pointerId in pointers.Keys) {
            if (!foundFirst) {
                pinchPointerA = pointerId;
                foundFirst = true;
            } else {
                pinchPointerB = pointerId;
                break;
            }
        }

        if (!TryGetStagePoint(pointers[pinchPointerA], out Vector2 a) || !TryGetStagePoint(pointers[pinchPointerB], out Vector2 b))
            return;

        pinching = true;
        ignoreNextTap = true;
        dragPointerId = NoPointer;
        CloseMenu();

        pinchStartDistance = Mathf.Max(Vector2.Distance(a, b), 1f);
        pinchStartSize = rectTransform.rect.size;
        pinchStartMidpoint = (a + b) * 0.5f;
        pinchStartPosition = rectTransform.localPosition;
    }

    private void UpdatePinch() {
        if (!TryGetStagePoint(pointers[pinchPointerA], out Vector2 a) || !TryGetStagePoint(pointers[pinchPointerB], out Vector2 b))
            return;

        float scale = ClampScale(pinchStartSize, Vector2.Distance(a, b) / pinchStartDistance);
        Vector2 midpoint = (a + b) * 0.5f;

        SetSize(pinchStartSize * scale);
        MoveTo(midpoint + (pinchStartPosition - pinchStartMidpoint) * scale);
    }

    // The mouse wheel resizes around the cursor, for testing without a touch screen.
    public void OnScroll(PointerEventData eventData) {
        if (stageArea == null || isLocked || pinching || Mathf.Approximately(eventData.scrollDelta.y, 0f))
            return;

        pointerCamera = eventData.enterEventCamera;

        if (!TryGetStagePoint(eventData.position, out Vector2 cursor))
            return;

        Vector2 size = rectTransform.rect.size;
        float scale = ClampScale(size, Mathf.Pow(wheelStep, Mathf.Sign(eventData.scrollDelta.y)));

        SetSize(size * scale);
        MoveTo(cursor + ((Vector2)rectTransform.localPosition - cursor) * scale);
        CloseMenu();
        DeleteIfOffStage();
    }

    // Keeps the figure's shape, its shorter side at least minSize and its longer side at most maxSize.
    private float ClampScale(Vector2 baseSize, float scale) {
        float smallest = minSize / Mathf.Max(Mathf.Min(baseSize.x, baseSize.y), 0.01f);
        float largest = maxSize / Mathf.Max(Mathf.Max(baseSize.x, baseSize.y), 0.01f);
        return Mathf.Clamp(scale, Mathf.Min(smallest, largest), largest);
    }

    private void SetSize(Vector2 size) {
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
    }

    // Moves the figure to a point in stage space. It may go past the stage's edges.
    private void MoveTo(Vector2 localPoint) {
        rectTransform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
        SyncMirror();
    }

    // Called when the fingers lift: a figure with no part left on the stage is deleted.
    private void DeleteIfOffStage() {
        Vector2 size = rectTransform.rect.size;
        Vector2 bottomLeft = (Vector2)rectTransform.localPosition - Vector2.Scale(size, rectTransform.pivot);

        if (!new Rect(bottomLeft, size).Overlaps(stageArea.rect))
            Delete();
    }

    // ---------- Lock / delete ----------

    public void SetLocked(bool locked) {
        isLocked = locked;

        if (locked) {
            dragPointerId = NoPointer;
            pinching = false;
        }

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
