using UnityEngine;
using UnityEngine.UI;

// The sound technician's window: one SoundChannel per category, plus the library panel beside it.
public sealed class SoundWindow : MonoBehaviour {
    [SerializeField] private SoundLibrary library;
    [Tooltip("Optional: the current design has no master slider.")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private SoundChannel[] channels = new SoundChannel[0];
    [SerializeField] private SoundLibraryPopup libraryPopup;

    private SoundChannel libraryChannel;

    private void Awake() {
        if (masterVolumeSlider != null) {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            SetMasterVolume(masterVolumeSlider.value);
        }

        foreach (SoundChannel channel in channels)
            channel.Initialize(this);

        libraryPopup.Hide();
    }

    // Opens the library for a channel; pressing the same channel's library button again closes it.
    public void OpenLibrary(SoundChannel channel) {
        if (libraryPopup.IsOpen && libraryChannel == channel) {
            libraryPopup.Hide();
            return;
        }

        libraryChannel = channel;
        libraryPopup.Show(SoundLibrary.DisplayName(channel.Category), library.GetClips(channel.Category),
            clip => channel.AddClip(clip));
    }

    private static void SetMasterVolume(float volume) {
        AudioListener.volume = volume;
    }
}
