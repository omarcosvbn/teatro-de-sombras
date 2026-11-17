using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Lists the sounds of one category; choosing one hands it back and closes the popup.
public sealed class SoundLibraryPopup : MonoBehaviour {
    [SerializeField] private Text titleLabel;
    [SerializeField] private RectTransform itemContainer;
    [SerializeField] private SoundLibraryItem itemPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<SoundLibraryItem> items = new List<SoundLibraryItem>();
    private bool initialized;

    public void Show(string title, IReadOnlyList<AudioClip> clips, Action<AudioClip> onPicked) {
        Initialize();
        ClearItems();
        titleLabel.text = title;

        foreach (AudioClip clip in clips) {
            if (clip == null)
                continue;

            SoundLibraryItem item = Instantiate(itemPrefab, itemContainer, false);
            item.Configure(clip, () => {
                onPicked(clip);
                Hide();
            });
            items.Add(item);
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide() {
        ClearItems();
        gameObject.SetActive(false);
    }

    private void Initialize() {
        if (initialized)
            return;

        closeButton.onClick.AddListener(Hide);
        initialized = true;
    }

    private void ClearItems() {
        foreach (SoundLibraryItem item in items)
            Destroy(item.gameObject);

        items.Clear();
    }
}
