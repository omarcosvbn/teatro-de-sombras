using UnityEngine;

// How the table is split between the three operators: a horizontal line from the left edge to a junction,
// then two diagonals from the junction to the top and bottom edges. That gives a bottom area, a top area
// and a right area; windows and the menu turn to face the operator of the area they are in.
// Points are fractions of the screen: (0, 0) is the bottom-left corner, (1, 1) the top-right.
[CreateAssetMenu(fileName = "TableZones", menuName = "Teatro/Table Zones")]
public sealed class TableZones : ScriptableObject {
    [Tooltip("Where the three lines meet.")]
    [SerializeField] private Vector2 junction = new Vector2(0.67f, 0.5f);
    [Tooltip("Where the upper diagonal reaches the top edge, as a fraction of the screen width.")]
    [SerializeField, Range(0f, 1f)] private float topEdgeX = 0.91f;
    [Tooltip("Where the lower diagonal reaches the bottom edge, as a fraction of the screen width.")]
    [SerializeField, Range(0f, 1f)] private float bottomEdgeX = 0.9f;

    [Header("Rotation of each area (degrees, counter-clockwise)")]
    [Tooltip("Operator at the bottom edge: upright.")]
    [SerializeField] private float bottomAngle = 0f;
    [Tooltip("Operator at the top edge: upside down.")]
    [SerializeField] private float topAngle = 180f;
    [Tooltip("Operator at the right edge: the top of the window points left.")]
    [SerializeField] private float rightAngle = 90f;

    public float AngleAt(Vector2 point) {
        if (point.y >= junction.y) {
            float t = (point.y - junction.y) / Mathf.Max(1f - junction.y, 0.0001f);
            return point.x > Mathf.Lerp(junction.x, topEdgeX, t) ? rightAngle : topAngle;
        }

        float s = (junction.y - point.y) / Mathf.Max(junction.y, 0.0001f);
        return point.x > Mathf.Lerp(junction.x, bottomEdgeX, s) ? rightAngle : bottomAngle;
    }

    // The angle for a point in a RectTransform's local space (e.g. the canvas).
    public float AngleAt(Vector2 localPoint, Rect area) {
        return AngleAt(new Vector2((localPoint.x - area.xMin) / area.width, (localPoint.y - area.yMin) / area.height));
    }
}
