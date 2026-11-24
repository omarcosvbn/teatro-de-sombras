using UnityEngine;
using UnityEngine.UI;

// The shadow manipulator's window: the stage, quick-access slots for figures and scenery, and a library
// panel to put more of them into those slots.
public sealed class ShadowWorkspaceUI : MonoBehaviour {
    private enum Category {
        Figures,
        Scenery
    }

    [SerializeField] private RectTransform stageArea;
    [SerializeField] private StageLights stageLights;

    [Header("Quick access")]
    [SerializeField] private DraggableWorkspaceImage[] figureSlots = new DraggableWorkspaceImage[0];
    [SerializeField] private DraggableWorkspaceImage[] scenerySlots = new DraggableWorkspaceImage[0];
    [SerializeField] private Button figureLibraryButton;
    [SerializeField] private Button sceneryLibraryButton;

    [Header("Library")]
    [SerializeField] private GameObject libraryPanel;
    [SerializeField] private RectTransform libraryContent;
    [SerializeField] private LibraryPickItem libraryItemPrefab;
    [SerializeField] private Sprite[] figureImages = new Sprite[0];
    [SerializeField] private Sprite[] scenarioImages = new Sprite[0];

    private FigureMirrorLayer figureMirror;
    private bool libraryOpen;
    private Category libraryCategory;

    public StageLights StageLights => stageLights;

    // The Light window's figure layer: every figure on this stage is copied there.
    public void ConnectMirror(FigureMirrorLayer mirrorLayer) {
        figureMirror = mirrorLayer;

        foreach (PlacedFigure figure in stageArea.GetComponentsInChildren<PlacedFigure>())
            figure.AttachMirror(figureMirror);
    }

    public void OnFigurePlaced(PlacedFigure figure) {
        figure.AttachMirror(figureMirror);
    }

    private void Start() {
        Canvas canvas = GetComponentInParent<Canvas>();
        SetUpSlots(figureSlots, figureImages, canvas);
        SetUpSlots(scenerySlots, scenarioImages, canvas);

        figureLibraryButton.onClick.AddListener(() => ToggleLibrary(Category.Figures));
        sceneryLibraryButton.onClick.AddListener(() => ToggleLibrary(Category.Scenery));
        CloseLibrary();
    }

    // The quick-access slots start with the first images of each library.
    private void SetUpSlots(DraggableWorkspaceImage[] slots, Sprite[] images, Canvas canvas) {
        for (int i = 0; i < slots.Length; i++) {
            slots[i].Initialize(this, stageArea, canvas);
            slots[i].SetSprite(i < images.Length ? images[i] : null);
        }
    }

    private void ToggleLibrary(Category category) {
        if (libraryOpen && libraryCategory == category) {
            CloseLibrary();
            return;
        }

        ClearLibrary();
        libraryCategory = category;

        foreach (Sprite image in category == Category.Figures ? figureImages : scenarioImages) {
            if (image == null)
                continue;

            LibraryPickItem item = Instantiate(libraryItemPrefab, libraryContent, false);
            item.Configure(image.name, image, () => AddToQuickAccess(category, image));
        }

        libraryPanel.SetActive(true);
        libraryOpen = true;
    }

    private void CloseLibrary() {
        ClearLibrary();
        libraryPanel.SetActive(false);
        libraryOpen = false;
    }

    private void ClearLibrary() {
        for (int i = libraryContent.childCount - 1; i >= 0; i--)
            Destroy(libraryContent.GetChild(i).gameObject);
    }

    // Puts a library image in the first free quick-access slot; when all are full, the last slot is replaced.
    private void AddToQuickAccess(Category category, Sprite image) {
        DraggableWorkspaceImage[] slots = category == Category.Figures ? figureSlots : scenerySlots;

        if (slots.Length == 0)
            return;

        foreach (DraggableWorkspaceImage slot in slots) {
            if (slot.Sprite == image)
                return;
        }

        foreach (DraggableWorkspaceImage slot in slots) {
            if (slot.Sprite != null)
                continue;

            slot.SetSprite(image);
            return;
        }

        slots[slots.Length - 1].SetSprite(image);
    }
}
