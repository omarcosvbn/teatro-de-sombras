using UnityEngine;

// The audience's view of the show on a second display: the stage with every light and figure, and no
// controls. The Light and Shadows windows copy their lights and figures here once they are created.
// Its layout lives in the AudienceDisplay prefab.
public sealed class AudienceDisplay : MonoBehaviour {
    [Tooltip("Display to show the stage on: 0 is the main display, 1 is the second one.")]
    [SerializeField, Min(0)] private int displayIndex = 1;
    [SerializeField] private Camera displayCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private StageLights lights;
    [SerializeField] private FigureMirrorLayer figures;

    // The scene has one audience display; the context menu links new windows to it.
    public static AudienceDisplay Current { get; private set; }

    public StageLights Lights => lights;
    public FigureMirrorLayer Figures => figures;

    private void Awake() {
        Current = this;
        displayCamera.targetDisplay = displayIndex;
        canvas.targetDisplay = displayIndex;

        // In a build, extra displays stay off until they are activated. In the Editor there is only one
        // display: open a Game view set to "Display 2" to see this.
        if (displayIndex > 0 && displayIndex < Display.displays.Length)
            Display.displays[displayIndex].Activate();
    }

    private void OnDestroy() {
        if (Current == this)
            Current = null;
    }
}
