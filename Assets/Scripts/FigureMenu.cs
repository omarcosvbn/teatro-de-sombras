using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// The small menu shown above a placed figure. Its layout lives in the FigureMenu prefab.
public sealed class FigureMenu : MonoBehaviour {
    [SerializeField] private Button lockButton;
    [SerializeField] private Text lockLabel;
    [SerializeField] private Button deleteButton;
    [SerializeField] private string lockText = "Lock";
    [SerializeField] private string unlockText = "Unlock";

    public void Bind(bool figureLocked, UnityAction onToggleLock, UnityAction onDelete) {
        lockLabel.text = figureLocked ? unlockText : lockText;

        lockButton.onClick.RemoveAllListeners();
        lockButton.onClick.AddListener(onToggleLock);

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(onDelete);
    }
}
