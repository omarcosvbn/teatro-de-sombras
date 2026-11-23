using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The light technician's window: a list of lights and an editor. Confirmed lights appear on the
// Shadows window's stage (connected with Connect).
public sealed class LightWindow : MonoBehaviour {
    [SerializeField] private LightRow rowPrefab;
    [SerializeField] private RectTransform rowContainer;
    [SerializeField] private ScrollRect listScroll;
    [SerializeField] private Button addButton;
    [SerializeField] private LightEditor editor;
    [Tooltip("Glow sprite for each LightShape, in order: Circle, Square, Star, Triangle.")]
    [SerializeField] private Sprite[] shapeSprites = new Sprite[0];

    private readonly List<LightRow> rows = new List<LightRow>();
    private StageLights stage;
    private LightRow selectedRow;
    private int nextNumber = 1;

    private void Awake() {
        addButton.onClick.AddListener(AddLight);
        editor.Initialize(this);
    }

    public void Connect(StageLights stageLights) {
        stage = stageLights;
    }

    public Sprite GetShapeSprite(LightShape shape) {
        int index = (int)shape;
        return index < shapeSprites.Length ? shapeSprites[index] : null;
    }

    // Adds a light and opens it in the editor straight away; it reaches the stage on Apply.
    private void AddLight() {
        LightRow row = Instantiate(rowPrefab, rowContainer, false);
        row.Initialize(this, nextNumber++, LightLook.Default);
        rows.Add(row);
        Select(row);

        Canvas.ForceUpdateCanvases();
        listScroll.verticalNormalizedPosition = 0f;
    }

    public void Select(LightRow row) {
        if (selectedRow != null)
            selectedRow.SetSelected(false);

        selectedRow = row;
        selectedRow.SetSelected(true);
        editor.Open(row);
    }

    public void ApplyEdit(LightRow row, LightLook look) {
        if (stage == null) {
            Debug.LogWarning($"{name}: no stage connected, so the light can't be shown.", this);
            return;
        }

        row.Apply(look, GetShapeSprite(look.shape), stage);
        Deselect();
    }

    // Cancelling a light that was never applied removes it again.
    public void CancelEdit(LightRow row) {
        Deselect();

        if (!row.IsPlaced)
            RemoveRow(row);
    }

    public void DeleteLight(LightRow row) {
        Deselect();
        RemoveRow(row);
    }

    private void Deselect() {
        if (selectedRow != null)
            selectedRow.SetSelected(false);

        selectedRow = null;
        editor.Close();
    }

    private void RemoveRow(LightRow row) {
        rows.Remove(row);
        row.Remove();
    }
}
