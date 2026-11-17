using UnityEngine;

// The shadow manipulator's window: a stage and a catalog of figures and scenarios to drag onto it.
public sealed class ShadowWorkspaceUI : MonoBehaviour {
    [SerializeField] private RectTransform stageArea;
    [SerializeField] private RectTransform catalogArea;
    [SerializeField] private DraggableWorkspaceImage catalogItemPrefab;
    [SerializeField] private Sprite[] figureImages = new Sprite[0];
    [SerializeField] private Sprite[] scenarioImages = new Sprite[0];

    private void Start() {
        BuildCatalog();
    }

    // One CatalogItem prefab instance per sprite in the lists above.
    private void BuildCatalog() {
        Canvas canvas = GetComponentInParent<Canvas>();

        foreach (Sprite image in figureImages)
            AddCatalogItem(image, canvas);

        foreach (Sprite image in scenarioImages)
            AddCatalogItem(image, canvas);
    }

    private void AddCatalogItem(Sprite image, Canvas canvas) {
        DraggableWorkspaceImage item = Instantiate(catalogItemPrefab, catalogArea, false);
        item.Configure(image, stageArea, canvas);
    }
}
