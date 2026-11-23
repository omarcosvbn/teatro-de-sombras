using UnityEngine;
using UnityEngine.UI;

// The lower part of the Light window: a preview of the light being edited, colour (swatches + hue),
// shape and size, and Apply / Cancel / Delete. Changes only reach the stage on Apply.
public sealed class LightEditor : MonoBehaviour {
    [SerializeField] private GameObject hint;
    [SerializeField] private GameObject content;
    [SerializeField] private Text titleLabel;
    [SerializeField] private Image previewImage;
    [Tooltip("Each swatch's Image colour is the colour it picks.")]
    [SerializeField] private Button[] swatchButtons = new Button[0];
    [SerializeField] private Slider hueSlider;
    [Tooltip("In LightShape order: Circle, Square, Star, Triangle.")]
    [SerializeField] private Button[] shapeButtons = new Button[0];
    [SerializeField] private Slider sizeSlider;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Color shapeNormalColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color shapeSelectedColor = new Color(0.95f, 0.75f, 0.25f, 1f);
    [SerializeField, Range(0f, 1f)] private float hueSaturation = 0.85f;

    private LightWindow window;
    private LightRow row;
    private LightLook draft;

    public void Initialize(LightWindow owner) {
        window = owner;

        foreach (Button swatch in swatchButtons) {
            Color swatchColor = swatch.image.color;
            swatch.onClick.AddListener(() => SetColor(swatchColor, true));
        }

        hueSlider.onValueChanged.AddListener(hue => SetColor(Color.HSVToRGB(hue, hueSaturation, 1f), false));

        for (int i = 0; i < shapeButtons.Length; i++) {
            LightShape shape = (LightShape)i;
            shapeButtons[i].onClick.AddListener(() => SetShape(shape));
        }

        sizeSlider.onValueChanged.AddListener(size => {
            draft.size = size;
            RefreshPreview();
        });

        applyButton.onClick.AddListener(() => window.ApplyEdit(row, draft));
        cancelButton.onClick.AddListener(() => window.CancelEdit(row));
        deleteButton.onClick.AddListener(() => window.DeleteLight(row));

        Close();
    }

    public void Open(LightRow editRow) {
        row = editRow;
        draft = row.Look;
        titleLabel.text = "Light " + row.Number;
        sizeSlider.SetValueWithoutNotify(draft.size);
        SyncHueSlider();

        hint.SetActive(false);
        content.SetActive(true);
        RefreshPreview();
    }

    public void Close() {
        row = null;
        content.SetActive(false);
        hint.SetActive(true);
    }

    private void SetColor(Color color, bool updateHueSlider) {
        color.a = 1f;
        draft.color = color;

        if (updateHueSlider)
            SyncHueSlider();

        RefreshPreview();
    }

    private void SetShape(LightShape shape) {
        draft.shape = shape;
        RefreshPreview();
    }

    private void SyncHueSlider() {
        Color.RGBToHSV(draft.color, out float hue, out _, out _);
        hueSlider.SetValueWithoutNotify(hue);
    }

    private void RefreshPreview() {
        previewImage.sprite = window.GetShapeSprite(draft.shape);
        previewImage.color = draft.color;

        float scale = sizeSlider.maxValue > 0f ? draft.size / sizeSlider.maxValue : 1f;
        previewImage.rectTransform.localScale = Vector3.one * Mathf.Clamp(scale, 0.2f, 1f);

        for (int i = 0; i < shapeButtons.Length; i++)
            shapeButtons[i].image.color = i == (int)draft.shape ? shapeSelectedColor : shapeNormalColor;
    }
}
