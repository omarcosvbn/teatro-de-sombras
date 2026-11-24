using UnityEngine;
using UnityEngine.UI;

// The audience's view of the show on a second display: the stage with every light and figure, and no
// controls. Only what the lights reach is visible; the rest is black. The Light and Shadows windows copy
// their lights and figures here once they are created. Its layout lives in the AudienceDisplay prefab.
//
// How the darkness works: LightmapCamera draws the lights (added together, on black) into a texture, and
// LightMask multiplies the stage by that texture, so unlit parts turn black and lit parts take the
// lights' colour.
public sealed class AudienceDisplay : MonoBehaviour {
    [Tooltip("Display to show the stage on: 0 is the main display, 1 is the second one.")]
    [SerializeField, Min(0)] private int displayIndex = 1;
    [SerializeField] private Camera displayCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private StageLights lights;
    [SerializeField] private FigureMirrorLayer figures;

    [Header("Light map")]
    [SerializeField] private Camera lightmapCamera;
    [SerializeField] private RawImage lightMask;
    [Tooltip("Resolution of the light map. Keep the stage's 450:272 shape; the glows are soft, so it can be low.")]
    [SerializeField] private Vector2Int lightmapSize = new Vector2Int(900, 544);

    private RenderTexture lightmap;

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

        // URP needs a camera's output texture to have a depth buffer, even though the lights don't use it.
        lightmap = new RenderTexture(lightmapSize.x, lightmapSize.y, 24) { name = "Audience Light Map" };
        lightmapCamera.targetTexture = lightmap;
        lightMask.texture = lightmap;
    }

    private void OnDestroy() {
        if (Current == this)
            Current = null;

        if (lightmap != null) {
            lightmapCamera.targetTexture = null;
            lightmap.Release();
            Destroy(lightmap);
        }
    }
}
