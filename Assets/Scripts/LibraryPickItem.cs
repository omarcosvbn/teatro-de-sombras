using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// One entry in a library panel (sounds or figures): a slot showing a picture and/or a name.
public sealed class LibraryPickItem : MonoBehaviour {
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;

    public void Configure(string itemName, Sprite picture, UnityAction onPressed) {
        name = itemName;

        if (label != null)
            label.text = picture == null ? itemName : string.Empty;

        if (icon != null) {
            icon.sprite = picture;
            icon.enabled = picture != null;
        }

        button.onClick.AddListener(onPressed);
    }
}
