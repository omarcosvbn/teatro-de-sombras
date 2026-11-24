using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// One entry in the SoundLibraryPopup.
public sealed class SoundLibraryItem : MonoBehaviour {
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    public void Configure(AudioClip clip, UnityAction onPressed) {
        name = clip.name;
        label.text = clip.name;
        button.onClick.AddListener(onPressed);
    }
}
