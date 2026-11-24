using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One light in the Light window's list: its number (tap to edit), intensity and on/off switch.
public sealed class LightRow : MonoBehaviour {
    [SerializeField] private Button numberButton;
    [Tooltip("The row's background; it changes colour while the light is being edited.")]
    [SerializeField] private Image highlight;
    [SerializeField] private TMP_Text numberLabel;
    [SerializeField] private Slider intensitySlider;
    [SerializeField] private Toggle onToggle;
    [Tooltip("Shown while the switch is on: the purple fill and the white knob.")]
    [SerializeField] private GameObject switchOn;
    [Tooltip("Shown while the switch is off: the grey knob on the left.")]
    [SerializeField] private GameObject switchOff;
    [SerializeField] private Color normalColor = new Color(0.21f, 0.2f, 0.27f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.24f, 0.5f, 1f);

    private LightWindow window;

    public int Number { get; private set; }
    public LightLook Look { get; private set; }
    // The light on the Light window's stage (can be moved). Its copies (on the Shadows window's stage and
    // the audience display) follow it.
    public StageLight PlacedLight { get; private set; }
    private readonly List<StageLight> mirrorLights = new List<StageLight>();
    public bool IsPlaced => PlacedLight != null;

    public void Initialize(LightWindow owner, int number, LightLook look) {
        window = owner;
        Number = number;
        Look = look;
        name = "Light " + number;
        numberLabel.text = number.ToString();

        numberButton.onClick.AddListener(() => window.Select(this));
        intensitySlider.onValueChanged.AddListener(value => ForEachLight(stageLight => stageLight.SetIntensity(value)));
        onToggle.onValueChanged.AddListener(on => {
            ShowSwitch(on);
            ForEachLight(stageLight => stageLight.SetOn(on));
        });
        ShowSwitch(onToggle.isOn);

        SetSelected(false);
    }

    // Puts the light on every stage the first time, then updates its look on all of them.
    public void Apply(LightLook look, Sprite sprite, StageLights stage, IReadOnlyList<StageLights> mirrorStages) {
        Look = look;

        if (PlacedLight == null) {
            PlacedLight = stage.CreateLight(true);

            foreach (StageLights mirrorStage in mirrorStages) {
                StageLight mirrorLight = mirrorStage.CreateLight(false);
                mirrorLights.Add(mirrorLight);
                PlacedLight.Moved += position => mirrorLight.SetPosition(position);
            }
        }

        ForEachLight(stageLight => {
            stageLight.SetLook(look, sprite);
            stageLight.SetIntensity(intensitySlider.value);
            stageLight.SetOn(onToggle.isOn);
        });
    }

    private void ForEachLight(System.Action<StageLight> action) {
        if (PlacedLight != null)
            action(PlacedLight);

        foreach (StageLight mirrorLight in mirrorLights) {
            if (mirrorLight != null)
                action(mirrorLight);
        }
    }

    // The Toggle only fades its own graphic, not the knob inside it, so the whole on/off look is switched here.
    private void ShowSwitch(bool on) {
        switchOn.SetActive(on);
        switchOff.SetActive(!on);
    }

    public void SetSelected(bool selected) {
        highlight.color = selected ? selectedColor : normalColor;
    }

    public void Remove() {
        ForEachLight(stageLight => stageLight.Remove());
        Destroy(gameObject);
    }
}
