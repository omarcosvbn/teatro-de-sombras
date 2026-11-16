using UnityEngine;
using UnityEngine.UI;

public sealed class ShadowWorkspaceUI : MonoBehaviour {
    [SerializeField] private Sprite[] figureImages = new Sprite[0];
    [SerializeField] private Sprite[] scenarioImages = new Sprite[0];
    [SerializeField, Min(40f)] private float catalogItemSize = 96f;

    private RectTransform stageArea;
    private RectTransform catalogArea;
    private Canvas canvas;
    private bool workspaceBuilt;

    private void Awake() {
        canvas = GetComponentInParent<Canvas>();
    }

    public void OpenWorkspace() {
        gameObject.SetActive(true);

        if (workspaceBuilt)
            return;

        canvas = GetComponentInParent<Canvas>();
        BuildWorkspace();
        workspaceBuilt = true;
    }

    private void BuildWorkspace() {
        stageArea = transform.Find("Stage").GetComponent<RectTransform>();
        catalogArea = transform.Find("Catalog").GetComponent<RectTransform>();

        foreach (Sprite image in figureImages)
            CreateCatalogItem(image);

        foreach (Sprite image in scenarioImages)
            CreateCatalogItem(image);
    }

    private void CreateCatalogItem(Sprite image) {
        GameObject itemObject = new GameObject(image.name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        itemObject.transform.SetParent(catalogArea, false);

        Image itemImage = itemObject.GetComponent<Image>();
        itemImage.sprite = image;
        itemImage.preserveAspect = true;
        itemImage.raycastTarget = true;

        LayoutElement itemLayout = itemObject.GetComponent<LayoutElement>();
        itemLayout.preferredHeight = catalogItemSize;
        itemLayout.preferredWidth = catalogItemSize;

        DraggableWorkspaceImage draggable = itemObject.AddComponent<DraggableWorkspaceImage>();
        draggable.Configure(image, stageArea, canvas, catalogItemSize);
    }
}
