using System;
using UnityEngine;

[Serializable]
public struct MapInfo
{
    public int widthPx;
    public int heightPx;
    public float scale;
    public Vector2? mouseNormalized;
    public bool isLoaded;
}
