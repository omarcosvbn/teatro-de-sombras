using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Edits one light: the Preview block in the Lights panel (preview, edit, apply, cancel, delete) and the
// COR (colour) and FORMAS (shape and size) panels. Changes only reach the stage on Apply.
public sealed class LightEditor : MonoBehaviour {
    [SerializeField] private GameObject hint;
    [SerializeField] private GameObject previewContent;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private Image previewImage;
    [Tooltip("The COR and FORMAS panels; shown while a light is being edited, and toggled by the edit button.")]
    [SerializeField] private GameObject[] editPanels = new GameObject[0];
    [SerializeField] private Button editButton;
    [SerializeField] private ColorWheel colorWheel;
    [Tooltip("In LightShape order: Circle, Square, Star, Triangle.")]
    [SerializeField] private Button[] shapeButtons = new Button[0];
    [SerializeField] private Slider sizeSlider;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Color shapeNormalColor = new Color(0.45f, 0.45f, 0.52f, 1f);
    [SerializeField] private Color shapeSelectedColor = new Color(0.71f, 0.94f, 0.35f, 1f);

    private LightWindow window;
    private LightRow row;
    private LightLook draft;

    public void Initialize(LightWindow owner) {
        window = owner;

        colorWheel.ColorChanged += color => {
            color.a = 1f;
            draft.color = color;
            RefreshPreview();
        };

        for (int i = 0; i < shapeButtons.Length; i++) {
            LightShape shape = (LightShape)i;
            shapeButtons[i].onClick.AddListener(() => {
                draft.shape = shape;
                RefreshPreview();
            });
        }

        sizeSlider.onValueChanged.AddListener(size => {
            draft.size = size;
            RefreshPreview();
        });

        editButton.onClick.AddListener(() => SetEditPanelsVisible(!editPanels[0].activeSelf));
        applyButton.onClick.AddListener(() => window.ApplyEdit(row, draft));
        cancelButton.onClick.AddListener(() => window.CancelEdit(row));
        deleteButton.onClick.AddListener(() => window.DeleteLight(row));

        Close();
    }

    public void Open(LightRow editRow) {
        row = editRow;
        draft = row.Look;

        if (titleLabel != null)
            titleLabel.text = "Preview · " + row.Number;

        sizeSlider.SetValueWithoutNotify(draft.size);
        colorWheel.SetColor(draft.color);

        hint.SetActive(false);
        previewContent.SetActive(true);
        SetEditPanelsVisible(true);
        RefreshPreview();
    }

    public void Close() {
        row = null;
        previewContent.SetActive(false);
        hint.SetActive(true);
        SetEditPanelsVisible(false);
    }

    private void SetEditPanelsVisible(bool visible) {
        foreach (GameObject panel in editPanels)
            panel.SetActive(visible);
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
