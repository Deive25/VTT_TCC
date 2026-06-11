using UnityEngine;

public class LayerData
{
    public string id;
    public string name;
    public GameObject gameObject;
    public SpriteRenderer renderer;
    public Texture2D fogTex;
    public Color32[] fogPixels;
    public SpriteRenderer fogRenderer;
    public bool fogDirty;
}
