using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The light technician's window: its own view of the stage (lights can be moved, figures are only
// shown), a list of lights and an editor. Lights are copied to the Shadows window's stage and the
// audience display (Connect).
public sealed class LightWindow : MonoBehaviour {
    [Tooltip("The light layer of this window's own stage.")]
    [SerializeField] private StageLights stage;
    [Tooltip("Shows the figures placed in the Shadows window.")]
    [SerializeField] private FigureMirrorLayer figureMirror;
    [SerializeField] private LightRow rowPrefab;
    [SerializeField] private RectTransform rowContainer;
    [SerializeField] private ScrollRect listScroll;
    [SerializeField] private Button addButton;
    [SerializeField] private LightEditor editor;
    [Tooltip("Glow sprite for each LightShape, in order: Circle, Square, Star, Triangle.")]
    [SerializeField] private Sprite[] shapeSprites = new Sprite[0];

    private readonly List<LightRow> rows = new List<LightRow>();
    private readonly List<StageLights> mirrorStages = new List<StageLights>();
    private LightRow selectedRow;

    private void Awake() {
        addButton.onClick.AddListener(AddLight);
        editor.Initialize(this);
    }

    public FigureMirrorLayer FigureMirror => figureMirror;

    // Another stage's light layer (the Shadows window's, the audience display's) that shows a copy of every light.
    public void Connect(StageLights mirrorStage) {
        if (mirrorStage != null && !mirrorStages.Contains(mirrorStage))
            mirrorStages.Add(mirrorStage);
    }

    public Sprite GetShapeSprite(LightShape shape) {
        int index = (int)shape;
        return index < shapeSprites.Length ? shapeSprites[index] : null;
    }

    // Adds a light and opens it in the editor straight away; it reaches the stages on Apply.
    // It takes the lowest number not in use, so a deleted light's number is given out again.
    private void AddLight() {
        int number = LowestFreeNumber();
        int index = rows.FindIndex(other => other.Number > number);

        if (index < 0)
            index = rows.Count;

        LightRow row = Instantiate(rowPrefab, rowContainer, false);
        row.Initialize(this, number, LightLook.Default);
        // Keep the list in number order.
        row.transform.SetSiblingIndex(index);
        rows.Insert(index, row);
        Select(row);

        // Scroll so the new row is visible.
        Canvas.ForceUpdateCanvases();
        listScroll.verticalNormalizedPosition = rows.Count > 1 ? 1f - (float)index / (rows.Count - 1) : 1f;
    }

    private int LowestFreeNumber() {
        int number = 1;

        // The rows are kept in number order, so the first gap is the lowest free number.
        foreach (LightRow row in rows) {
            if (row.Number != number)
                break;

            number++;
        }

        return number;
    }

    public void Select(LightRow row) {
        if (selectedRow != null)
            selectedRow.SetSelected(false);

        selectedRow = row;
        selectedRow.SetSelected(true);
        editor.Open(row);
    }

    public void ApplyEdit(LightRow row, LightLook look) {
        row.Apply(look, GetShapeSprite(look.shape), stage, mirrorStages);
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
