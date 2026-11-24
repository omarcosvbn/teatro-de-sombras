using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Lists the sounds of one category; choosing one hands it back and closes the panel.
public sealed class SoundLibraryPopup : MonoBehaviour {
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private RectTransform itemContainer;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private LibraryPickItem itemPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<LibraryPickItem> items = new List<LibraryPickItem>();
    private bool initialized;

    public bool IsOpen => gameObject.activeSelf;

    public void Show(string title, IReadOnlyList<AudioClip> clips, Action<AudioClip> onPicked) {
        Initialize();
        ClearItems();

        if (titleLabel != null)
            titleLabel.text = title;

        foreach (AudioClip clip in clips) {
            if (clip == null)
                continue;

            LibraryPickItem item = Instantiate(itemPrefab, itemContainer, false);
            item.Configure(clip.name, null, () => {
                onPicked(clip);
                Hide();
            });
            items.Add(item);
        }

        gameObject.SetActive(true);

        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    public void Hide() {
        ClearItems();
        gameObject.SetActive(false);
    }

    private void Initialize() {
        if (initialized)
            return;

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        initialized = true;
    }

    private void ClearItems() {
        foreach (LibraryPickItem item in items)
            Destroy(item.gameObject);

        items.Clear();
    }
}
