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
    [SerializeField] private RectTransform workspacePrefab;
    [SerializeField, Min(0f)] private float menuTimeout = 5f;

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
    private bool workspaceOpen;
    private GameObject menuObject;
    private GameObject workspaceObject;
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
        }

        Canvas.ForceUpdateCanvases();
        ClampToCanvas(menuRect, canvasRect);
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

    public void OpenWorkspace() {
        if (workspaceOpen) {
            workspaceObject.SetActive(true);
            return;
        }

        RectTransform workspaceRect = Instantiate(workspacePrefab, menuCanvas.transform, false);
        workspaceObject = workspaceRect.gameObject;
        workspaceObject.SetActive(true);
        workspaceObject.GetComponent<ShadowWorkspaceUI>().OpenWorkspace();
        workspaceOpen = true;
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

    private void OnDisable() {
        CloseMenu();
    }
}
