using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One sound slot in a SoundChannel. Empty slots are hidden; tapping a filled slot plays its sound.
public sealed class SoundSlot : MonoBehaviour {
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [Tooltip("Shown while this slot's sound is playing.")]
    [SerializeField] private GameObject playingIndicator;

    private SoundChannel channel;
    private AudioClip clip;

    public AudioClip Clip => clip;
    public bool IsEmpty => clip == null;

    public void Initialize(SoundChannel owner) {
        channel = owner;
        button.onClick.AddListener(OnPressed);
        Refresh(false);
    }

    public void SetClip(AudioClip newClip) {
        clip = newClip;
        Refresh(false);
    }

    public void SetPlaying(bool playing) {
        Refresh(playing);
    }

    private void OnPressed() {
        if (clip != null)
            channel.Play(this);
    }

    private void Refresh(bool playing) {
        label.text = clip != null ? clip.name : string.Empty;
        playingIndicator.SetActive(playing);
        gameObject.SetActive(clip != null);
    }
}
