using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;

public sealed class LongPressContextMenu : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler {
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
    private Vector2 pressPosition;
    private bool longPressTriggered;
    private bool mousePress;
    private bool pressActive;
    private bool menuOpen;
    private bool workspaceOpen;
    private GameObject menuObject;
    private GameObject workspaceObject;
    private Coroutine menuTimeoutCoroutine;

    public void OnPointerDown(PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        BeginPress(eventData.position, eventData.pointerId < 0);
    }

    public void OnPointerUp(PointerEventData eventData) {
        EndPress();
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (!longPressTriggered)
            EndPress();
    }

    private void OnMouseDown() {
        BeginPress(Mouse.current.position.ReadValue(), true);
    }

    private void OnMouseUp() {
        EndPress();
    }

    private void OnMouseExit() {
        if (!longPressTriggered)
            EndPress();
    }

    private void BeginPress(Vector2 position, bool isMouse) {
        if (pressActive || longPressTriggered)
            return;

        pressPosition = position;
        mousePress = isMouse;
        longPressTriggered = false;
        pressActive = true;
        holdCoroutine = StartCoroutine(WaitForLongPress());
    }

    private IEnumerator WaitForLongPress() {
        float elapsed = 0f;

        while (elapsed < holdDuration) {
            Vector2 currentPosition = GetCurrentPointerPosition();

            if (Vector2.Distance(pressPosition, currentPosition) > movementTolerance) {
                pressActive = false;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        longPressTriggered = true;
        pressActive = false;
        ShowMenu(mousePress ? GetCurrentPointerPosition() : pressPosition);
    }

    private Vector2 GetCurrentPointerPosition() {
        if (mousePress)
            return Mouse.current.position.ReadValue();

        return Touchscreen.current.primaryTouch.position.ReadValue();
    }

    private void EndPress() {
        if (pressActive) {
            StopCoroutine(holdCoroutine);
            pressActive = false;
        }

        longPressTriggered = false;
    }

    private void ShowMenu(Vector2 screenPosition) {
        Canvas canvas = menuCanvas;

        RectTransform menuRect = Instantiate(menuPrefab, canvas.transform, false);
        menuObject = menuRect.gameObject;
        menuObject.SetActive(true);

        Button[] buttons = menuObject.GetComponentsInChildren<Button>(true);

        buttons[0].onClick.AddListener(() => {
            firstButtonClicked.Invoke();
            if (!workspaceOpen)
                OpenWorkspace();
        });
        buttons[1].onClick.AddListener(() => {
            secondButtonClicked.Invoke();
        });
        buttons[2].onClick.AddListener(() => {
            thirdButtonClicked.Invoke();
        });

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera eventCamera = canvas.worldCamera;

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
        CloseMenu();
    }

    public void CloseMenu() {
        CloseMenuInternal();
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

    private void CloseMenuInternal() {
        EndPress();

        if (menuOpen) {
            StopCoroutine(menuTimeoutCoroutine);
            Destroy(menuObject);
            menuOpen = false;
        }
    }

    private void OnDisable() {
        CloseMenu();
    }
}
