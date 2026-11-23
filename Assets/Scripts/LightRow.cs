using UnityEngine;
using UnityEngine.UI;

// One light in the Light window's list: its number (tap to edit), intensity and on/off switch.
public sealed class LightRow : MonoBehaviour {
    [SerializeField] private Button numberButton;
    [SerializeField] private Image numberBackground;
    [SerializeField] private Text numberLabel;
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private Toggle onToggle;
    [SerializeField] private Color normalColor = new Color(0.35f, 0.35f, 0.38f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.95f, 0.75f, 0.25f, 1f);

    private LightWindow window;

    public int Number { get; private set; }
    public LightLook Look { get; private set; }
    public StageLight PlacedLight { get; private set; }
    public bool IsPlaced => PlacedLight != null;

    public void Initialize(LightWindow owner, int number, LightLook look) {
        window = owner;
        Number = number;
        Look = look;
        name = "Light " + number;
        numberLabel.text = number.ToString();

        numberButton.onClick.AddListener(() => window.Select(this));
        intensitySlider.onValueChanged.AddListener(value => {
            if (PlacedLight != null)
                PlacedLight.SetIntensity(value);
        });
        onToggle.onValueChanged.AddListener(on => {
            if (PlacedLight != null)
                PlacedLight.SetOn(on);
        });

        SetSelected(false);
    }

    // Puts the light on the stage the first time, then updates its look.
    public void Apply(LightLook look, Sprite sprite, StageLights stage) {
        Look = look;

        if (PlacedLight == null)
            PlacedLight = stage.CreateLight();

        PlacedLight.SetLook(look, sprite);
        PlacedLight.SetIntensity(intensitySlider.value);
        PlacedLight.SetOn(onToggle.isOn);
    }

    public void SetSelected(bool selected) {
        numberBackground.color = selected ? selectedColor : normalColor;
    }

    public void Remove() {
        if (PlacedLight != null)
            PlacedLight.Remove();

        Destroy(gameObject);
    }
}
