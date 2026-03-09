// ============================================================
// LayerManager.cs (Atuando como Board Manager)
// ============================================================
using System.Collections;
using System.Collections.Generic;
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

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<LayerManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("LayerManager_Auto");
                    _instance = go.AddComponent<LayerManager>();
                }
            }
            return _instance;
        }
    }
    private static LayerManager _instance;

    private List<LayerData> _layers = new List<LayerData>();
    public IReadOnlyList<LayerData> Layers => _layers;

    public string ActiveLayerId { get; private set; }
    private int layerCounter = 1;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);
    }

    public void AddLayer(Texture2D texture)
    {
        string newId = System.Guid.NewGuid().ToString();
        string newName = "Tabuleiro " + layerCounter++;

        GameObject go = new GameObject("Board_" + newName);

        MapController mapCtrl = FindAnyObjectByType<MapController>();
        if (mapCtrl != null) go.transform.SetParent(mapCtrl.transform, false);
        else go.transform.SetParent(this.transform, false);

        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one; // Trava a escala nativa do Sprite

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);

        LayerData newLayer = new LayerData { id = newId, name = newName, gameObject = go, renderer = sr };

        if (FogOfWarController.Instance != null)
        {
            FogOfWarController.Instance.InitFogForBoard(newLayer);
        }

        _layers.Insert(0, newLayer);
        MapEvents.FireMapLoaded(texture);
        SetActiveLayer(newId);
    }

    public void RemoveLayer(string id)
    {
        LayerData layer = _layers.Find(l => l.id == id);
        if (layer != null)
        {
            if (layer.fogTex != null) Destroy(layer.fogTex);
            Destroy(layer.gameObject);

            _layers.Remove(layer);

            if (ActiveLayerId == id)
            {
                ActiveLayerId = null;
                if (_layers.Count > 0) SetActiveLayer(_layers[0].id);
                else
                {
                    DefaultBoardRenderer defaultBoard = FindAnyObjectByType<DefaultBoardRenderer>();
                    if (defaultBoard != null) defaultBoard.SetVisible(true);
                }
            }
            MapEvents.FireLayersChanged();

            MapController mapCtrl = FindAnyObjectByType<MapController>();
            if (mapCtrl != null) mapCtrl.NotifyMapInfoUpdated();
        }
    }

    public void RenameLayer(string id, string newName)
    {
        LayerData layer = _layers.Find(l => l.id == id);
        if (layer != null && !string.IsNullOrWhiteSpace(newName)) layer.name = newName;
    }

    public void MoveLayerUp(string id)
    {
        int index = _layers.FindIndex(l => l.id == id);
        if (index > 0)
        {
            var temp = _layers[index]; _layers[index] = _layers[index - 1]; _layers[index - 1] = temp;
            MapEvents.FireLayersChanged();
        }
    }

    public void MoveLayerDown(string id)
    {
        int index = _layers.FindIndex(l => l.id == id);
        if (index >= 0 && index < _layers.Count - 1)
        {
            var temp = _layers[index]; _layers[index] = _layers[index + 1]; _layers[index + 1] = temp;
            MapEvents.FireLayersChanged();
        }
    }

    public void SetActiveLayer(string id)
    {
        ActiveLayerId = id;
        foreach (var l in _layers) l.gameObject.SetActive(l.id == id);

        MapEvents.FireActiveLayerChanged(id);
        MapEvents.FireLayersChanged();

        MapController mc = FindAnyObjectByType<MapController>();
        if (mc != null) mc.NotifyMapInfoUpdated();

        // Aguarda 1 frame para garantir que o objeto ativou e os tamanhos estão prontos
        StartCoroutine(FocusCameraNextFrame());
    }

    private IEnumerator FocusCameraNextFrame()
    {
        yield return null;
        CameraController camCtrl = FindAnyObjectByType<CameraController>();
        if (camCtrl != null) camCtrl.FocusOnActiveBoard();
    }

    public LayerData GetActiveLayer()
    {
        return _layers.Find(l => l.id == ActiveLayerId);
    }
}