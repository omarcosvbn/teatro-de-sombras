using UnityEngine;
using UnityEngine.UI;

// The tab on top of a window's frame. Lock stops the window being moved or scaled; delete closes it,
// and the context menu reopens it with everything still in it. Its layout lives in each window's prefab.
public sealed class WindowControls : MonoBehaviour {
    [SerializeField] private DraggableWindow window;
    [SerializeField] private Button lockButton;
    [SerializeField] private Image lockIcon;
    [SerializeField] private Button deleteButton;
    [Tooltip("Shown on the lock button while the window is unlocked.")]
    [SerializeField] private Sprite lockSprite;
    [Tooltip("Shown on the lock button while the window is locked.")]
    [SerializeField] private Sprite unlockSprite;
    [SerializeField] private Color unlockedColor = new Color(0.93f, 0.93f, 0.97f, 1f);
    [Tooltip("The lock icon turns this colour while the window is locked, so the state shows at a glance.")]
    [SerializeField] private Color lockedColor = new Color(0.71f, 0.94f, 0.35f, 1f);

    private void Awake() {
        lockButton.onClick.AddListener(ToggleLock);
        deleteButton.onClick.AddListener(window.Close);
        ShowLockState();
    }

    private void ToggleLock() {
        window.SetLocked(!window.IsLocked);
        ShowLockState();
    }

    private void ShowLockState() {
        lockIcon.sprite = window.IsLocked ? unlockSprite : lockSprite;
        lockIcon.color = window.IsLocked ? lockedColor : unlockedColor;
    }
}
