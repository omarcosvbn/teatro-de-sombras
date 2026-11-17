using UnityEngine;
using UnityEngine.UI;

// The sound technician's window: master volume plus one SoundChannel per category.
public sealed class SoundWindow : MonoBehaviour {
    [SerializeField] private SoundLibrary library;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private SoundChannel[] channels = new SoundChannel[0];
    [SerializeField] private SoundLibraryPopup libraryPopup;

    private void Awake() {
        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        SetMasterVolume(masterVolumeSlider.value);

        foreach (SoundChannel channel in channels)
            channel.Initialize(this);

        libraryPopup.Hide();
    }

    public void OpenLibrary(SoundChannel channel) {
        libraryPopup.Show(SoundLibrary.DisplayName(channel.Category), library.GetClips(channel.Category),
            clip => channel.AddClip(clip));
    }

    private static void SetMasterVolume(float volume) {
        AudioListener.volume = volume;
    }
}
