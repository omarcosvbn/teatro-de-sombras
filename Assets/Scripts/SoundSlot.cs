using UnityEngine;
using UnityEngine.UI;

// One sound slot in a SoundChannel. Tapping a filled slot plays its sound.
public sealed class SoundSlot : MonoBehaviour {
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Text label;
    [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color filledColor = new Color(0.35f, 0.45f, 0.6f, 1f);
    [SerializeField] private Color playingColor = new Color(0.4f, 0.75f, 0.45f, 1f);

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
        button.interactable = clip != null;

        if (clip == null)
            background.color = emptyColor;
        else
            background.color = playing ? playingColor : filledColor;
    }
}
