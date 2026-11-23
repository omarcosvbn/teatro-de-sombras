using System;
using UnityEngine;

public enum LightShape {
    Circle,
    Square,
    Star,
    Triangle
}

// How a light looks on the stage: its colour, shape and size (1 = the StageLight prefab's base size).
[Serializable]
public struct LightLook {
    public Color color;
    public LightShape shape;
    public float size;

    public static LightLook Default => new LightLook {
        color = new Color(1f, 0.92f, 0.75f, 1f),
        shape = LightShape.Circle,
        size = 1f
    };
}
