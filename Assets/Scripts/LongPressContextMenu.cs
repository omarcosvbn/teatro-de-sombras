using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

public sealed class LongPressContextMenu : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler {
    private const int NoPointer = int.MinValue;

    [Header("Long press")]
    [SerializeField, Min(0.1f)] private float holdDuration = 0.6f;
    [SerializeField, Min(0f)] private float movementTolerance = 30f;

    [Header("Menu")]
    [SerializeField] private RectTransform menuPrefab;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField, Min(0f)] private float menuTimeout = 5f;

    [Header("Windows")]
    [SerializeField] private DraggableWindow soundWindowPrefab;
    [SerializeField] private DraggableWindow lightWindowPrefab;
    [SerializeField] private DraggableWindow shadowWindowPrefab;

    [Header("Actions")]
    [SerializeField] private UnityEvent firstButtonClicked = new UnityEvent();
    [SerializeField] private UnityEvent secondButtonClicked = new UnityEvent();
    [SerializeField] private UnityEvent thirdButtonClicked = new UnityEvent();

    private Coroutine holdCoroutine;
    private int activePointerId = NoPointer;
    private Vector2 pressPosition;
    private Vector2 currentPosition;
    private bool longPressTriggered;
    private bool menuOpen;
    private GameObject menuObject;
    private Vector2 menuCanvasPosition;
    private DraggableWindow soundWindow;
    private DraggableWindow lightWindow;
    private DraggableWindow shadowWindow;
    private bool shadowWindowOpened;
    private Coroutine menuTimeoutCoroutine;

    public void OnPointerDown(PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        BeginPress(eventData.pointerId, eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData) {
        if (eventData.pointerId == activePointerId)
            EndPress();
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (eventData.pointerId == activePointerId && !longPressTriggered)
            EndPress();
    }

    // Implemented so the EventSystem keeps sending position updates for the pressing pointer.
    public void OnBeginDrag(PointerEventData eventData) {
        TrackPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData) {
        TrackPosition(eventData);
    }

    private void TrackPosition(PointerEventData eventData) {
        if (eventData.pointerId == activePointerId)
            currentPosition = eventData.position;
    }

    private void BeginPress(int pointerId, Vector2 position) {
        // One long press at a time; other fingers on the table are ignored until this one ends.
        if (activePointerId != NoPointer)
            return;

        activePointerId = pointerId;
        pressPosition = position;
        currentPosition = position;
        longPressTriggered = false;
        holdCoroutine = StartCoroutine(WaitForLongPress());
    }

    private IEnumerator WaitForLongPress() {
        float elapsed = 0f;

        while (elapsed < holdDuration) {
            if (Vector2.Distance(pressPosition, currentPosition) > movementTolerance) {
                holdCoroutine = null;
                EndPress();
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        holdCoroutine = null;
        longPressTriggered = true;
        ShowMenu(currentPosition);
    }

    private void EndPress() {
        if (holdCoroutine != null) {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }

        activePointerId = NoPointer;
        longPressTriggered = false;
    }

    private void ShowMenu(Vector2 screenPosition) {
        // A new long press replaces any menu that is still open.
        DestroyMenu();

        Canvas canvas = menuCanvas;

        RectTransform menuRect = Instantiate(menuPrefab, canvas.transform, false);
        menuObject = menuRect.gameObject;
        menuObject.SetActive(true);

        Button[] buttons = menuObject.GetComponentsInChildren<Button>(true);
        AddButtonAction(buttons, 0, firstButtonClicked);
        AddButtonAction(buttons, 1, secondButtonClicked);
        AddButtonAction(buttons, 2, thirdButtonClicked);

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, eventCamera, out Vector2 localPosition)) {
            menuRect.anchoredPosition = localPosition;
            menuCanvasPosition = localPosition;
        }

        Canvas.ForceUpdateCanvases();
        ClampToCanvas(menuRect, canvasRect);
        FaceOperator(menuRect, canvasRect);
        menuRect.SetAsLastSibling();
        menuOpen = true;
        menuTimeoutCoroutine = StartCoroutine(CloseMenuAfterTimeout());
    }

    private void AddButtonAction(Button[] buttons, int index, UnityEvent action) {
        if (index >= buttons.Length) {
            Debug.LogWarning($"{name}: menu prefab has no button {index + 1}.", this);
            return;
        }

        buttons[index].onClick.AddListener(() => {
            action.Invoke();
            DestroyMenu();
        });
    }

    // Menu buttons toggle their window: they open it, or close it when it is already open.
    public void ToggleShadowWindow() {
        if (shadowWindow != null && shadowWindow.IsOpen)
            shadowWindow.Close();
        else
            OpenShadowWindow();
    }

    public void ToggleLightWindow() {
        if (lightWindow != null && lightWindow.IsOpen)
            lightWindow.Close();
        else
            OpenLightWindow();
    }

    public void ToggleSoundWindow() {
        if (soundWindow != null && soundWindow.IsOpen)
            soundWindow.Close();
        else
            OpenSoundWindow();
    }

    public void OpenSoundWindow() {
        OpenWindow(soundWindowPrefab, ref soundWindow);
    }

    public void OpenLightWindow() {
        bool firstOpen = lightWindow == null;
        OpenWindow(lightWindowPrefab, ref lightWindow);

        if (firstOpen) {
            // The two stages show the same scene: lights are copied to the Shadows stage and figures
            // to the Light stage.
            LightWindow lights = lightWindow.GetComponent<LightWindow>();
            ShadowWorkspaceUI shadows = GetShadowWorkspace();
            lights.Connect(shadows.StageLights);
            shadows.ConnectMirror(lights.FigureMirror);

            // The audience display shows every light too.
            if (AudienceDisplay.Current != null)
                lights.Connect(AudienceDisplay.Current.Lights);
        }
    }

    public void OpenShadowWindow() {
        bool firstOpen = shadowWindow == null || !shadowWindowOpened;
        GetShadowWorkspace();
        shadowWindow.Open();

        if (firstOpen) {
            shadowWindowOpened = true;
            shadowWindow.FaceOperator(menuCanvasPosition);
        }
    }

    // The Shadows window is created once, closed; it opens centred where the menu was. The Light window
    // mirrors its stage, so opening the Light window creates it too. Its figures are always shown on the
    // audience display.
    private ShadowWorkspaceUI GetShadowWorkspace() {
        if (shadowWindow == null) {
            shadowWindow = Instantiate(shadowWindowPrefab, menuCanvas.transform, false);
            shadowWindow.CenterOn(menuCanvasPosition);
            shadowWindow.Close();

            if (AudienceDisplay.Current != null)
                shadowWindow.GetComponent<ShadowWorkspaceUI>().ConnectMirror(AudienceDisplay.Current.Figures);
        }

        return shadowWindow.GetComponent<ShadowWorkspaceUI>();
    }

    // Each window exists once. The first time it opens centred where the menu was, facing the operator who
    // opened the menu; after that it reopens wherever it was left.
    private void OpenWindow(DraggableWindow prefab, ref DraggableWindow instance) {
        bool firstOpen = instance == null;

        if (firstOpen) {
            instance = Instantiate(prefab, menuCanvas.transform, false);
            instance.CenterOn(menuCanvasPosition);
        }

        instance.Open();

        if (firstOpen)
            instance.FaceOperator(menuCanvasPosition);
    }

    private IEnumerator CloseMenuAfterTimeout() {
        yield return new WaitForSecondsRealtime(menuTimeout);
        menuTimeoutCoroutine = null;
        DestroyMenu();
    }

    public void CloseMenu() {
        EndPress();
        DestroyMenu();
    }

    private void DestroyMenu() {
        if (!menuOpen)
            return;

        if (menuTimeoutCoroutine != null) {
            StopCoroutine(menuTimeoutCoroutine);
            menuTimeoutCoroutine = null;
        }

        Destroy(menuObject);
        menuObject = null;
        menuOpen = false;
    }

    private static void ClampToCanvas(RectTransform menuRect, RectTransform canvasRect) {
        Vector2 position = menuRect.anchoredPosition;
        Vector2 menuSize = menuRect.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;

        position.x = Mathf.Clamp(position.x, -canvasSize.x * canvasRect.pivot.x,
            canvasSize.x * (1f - canvasRect.pivot.x) - menuSize.x);
        position.y = Mathf.Clamp(position.y, -canvasSize.y * (1f - canvasRect.pivot.y) + menuSize.y,
            canvasSize.y * (1f - canvasRect.pivot.y));

        menuRect.anchoredPosition = position;
    }

    // Turns the menu to face the operator of the table area the finger pressed in (the same areas the windows use),
    // around its centre, and keeps it on screen.
    private void FaceOperator(RectTransform menuRect, RectTransform canvasRect) {
        TableZones zones = shadowWindowPrefab != null ? shadowWindowPrefab.TableZones : null;

        if (zones == null)
            return;

        Rect rect = menuRect.rect;
        Vector2 center = (Vector2)menuRect.localPosition + rect.center;
        menuRect.localRotation = Quaternion.Euler(0f, 0f, zones.AngleAt(menuCanvasPosition, canvasRect.rect));
        menuRect.localPosition = center - (Vector2)(menuRect.localRotation * rect.center);

        // Its outline on screen after turning, kept inside the canvas.
        Vector2 half = (Vector2)(menuRect.localRotation * rect.size);
        half = new Vector2(Mathf.Abs(half.x), Mathf.Abs(half.y)) * 0.5f;
        Rect bounds = canvasRect.rect;
        Vector2 clamped = new Vector2(
            Mathf.Clamp(center.x, bounds.xMin + half.x, bounds.xMax - half.x),
            Mathf.Clamp(center.y, bounds.yMin + half.y, bounds.yMax - half.y));
        menuRect.localPosition += (Vector3)(clamped - center);
    }

    private void OnDisable() {
        CloseMenu();
    }
}
