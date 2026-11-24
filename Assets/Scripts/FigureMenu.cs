using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// The small menu shown above a placed figure. Its layout lives in the FigureMenu prefab.
public sealed class FigureMenu : MonoBehaviour {
    [SerializeField] private Button lockButton;
    [SerializeField] private Image lockIcon;
    [SerializeField] private Button deleteButton;
    [Tooltip("Shown on the lock button while the figure is unlocked.")]
    [SerializeField] private Sprite lockSprite;
    [Tooltip("Shown on the lock button while the figure is locked.")]
    [SerializeField] private Sprite unlockSprite;

    public void Bind(bool figureLocked, UnityAction onToggleLock, UnityAction onDelete) {
        lockIcon.sprite = figureLocked ? unlockSprite : lockSprite;

        lockButton.onClick.RemoveAllListeners();
        lockButton.onClick.AddListener(onToggleLock);

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(onDelete);
    }
}
