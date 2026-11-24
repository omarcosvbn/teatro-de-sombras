using UnityEngine;
using UnityEngine.UI;

// One category row of the Sound window (SFX, Music or Voice): level meter, volume, pan and sound slots.
// A channel plays one sound at a time; tapping a slot restarts playback with that slot's sound.
public sealed class SoundChannel : MonoBehaviour {
    [SerializeField] private SoundCategory category;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider panSlider;
    [SerializeField] private Image levelMeterFill;
    [SerializeField] private SoundSlot[] slots = new SoundSlot[0];
    [SerializeField] private Button addButton;
    [SerializeField, Min(0.1f)] private float meterFallSpeed = 1.5f;

    private readonly float[] samples = new float[256];
    private SoundWindow window;
    private SoundSlot playingSlot;
    private float level;

    public SoundCategory Category => category;

    public void Initialize(SoundWindow owner) {
        window = owner;

        foreach (SoundSlot slot in slots)
            slot.Initialize(this);

        volumeSlider.onValueChanged.AddListener(value => audioSource.volume = value);
        panSlider.onValueChanged.AddListener(value => audioSource.panStereo = value);
        audioSource.volume = volumeSlider.value;
        audioSource.panStereo = panSlider.value;

        addButton.onClick.AddListener(() => window.OpenLibrary(this));
        RefreshAddButton();
    }

    // Puts a clip in the first empty slot. Returns false when every slot is full.
    public bool AddClip(AudioClip clip) {
        foreach (SoundSlot slot in slots) {
            if (!slot.IsEmpty)
                continue;

            slot.SetClip(clip);
            RefreshAddButton();
            return true;
        }

        return false;
    }

    public void Play(SoundSlot slot) {
        if (playingSlot != null)
            playingSlot.SetPlaying(false);

        audioSource.Stop();
        audioSource.clip = slot.Clip;
        audioSource.time = 0f;
        audioSource.Play();

        playingSlot = slot;
        playingSlot.SetPlaying(true);
    }

    private void RefreshAddButton() {
        bool hasEmptySlot = false;

        foreach (SoundSlot slot in slots)
            hasEmptySlot |= slot.IsEmpty;

        // The library button sits after the filled slots and disappears once every slot is used.
        addButton.gameObject.SetActive(hasEmptySlot);
    }

    private void Update() {
        if (playingSlot != null && !audioSource.isPlaying) {
            playingSlot.SetPlaying(false);
            playingSlot = null;
        }

        UpdateLevelMeter();
    }

    private void UpdateLevelMeter() {
        float peak = 0f;

        if (audioSource.isPlaying) {
            audioSource.GetOutputData(samples, 0);

            foreach (float sample in samples)
                peak = Mathf.Max(peak, Mathf.Abs(sample));
        }

        level = peak > level ? peak : Mathf.MoveTowards(level, peak, meterFallSpeed * Time.unscaledDeltaTime);
        levelMeterFill.fillAmount = Mathf.Clamp01(level);
    }
}
