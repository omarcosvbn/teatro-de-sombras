using UnityEngine;

public sealed class ShadowWorkspaceUI : MonoBehaviour {
    [SerializeField] private RectTransform stageArea;
    [SerializeField] private RectTransform catalogArea;
    [SerializeField] private DraggableWorkspaceImage catalogItemPrefab;
    [SerializeField] private Sprite[] figureImages = new Sprite[0];
    [SerializeField] private Sprite[] scenarioImages = new Sprite[0];

    private Canvas canvas;
    private bool workspaceBuilt;

    public void OpenWorkspace() {
        gameObject.SetActive(true);

        if (workspaceBuilt)
            return;

        canvas = GetComponentInParent<Canvas>();
        BuildCatalog();
        workspaceBuilt = true;
    }

    // One CatalogItem prefab instance per sprite in the lists above.
    private void BuildCatalog() {
        foreach (Sprite image in figureImages)
            AddCatalogItem(image);

        foreach (Sprite image in scenarioImages)
            AddCatalogItem(image);
    }

    private void AddCatalogItem(Sprite image) {
        DraggableWorkspaceImage item = Instantiate(catalogItemPrefab, catalogArea, false);
        item.Configure(image, stageArea, canvas);
    }
}
