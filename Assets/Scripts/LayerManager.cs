using System.Collections.Generic;
using UnityEngine;

public class LayerData
{
    public string id;
    public string name;
    public bool isVisible = true;
    public GameObject gameObject;
    public SpriteRenderer renderer;
}

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance { get; private set; }

    private List<LayerData> _layers = new List<LayerData>();
    public IReadOnlyList<LayerData> Layers => _layers;

    public string ActiveLayerId { get; private set; }
    private int layerCounter = 1;

    private void Awake()
    {
        Instance = this;
    }

    public void AddLayer(Texture2D texture)
    {
        string newId = System.Guid.NewGuid().ToString();
        string newName = "Camada " + layerCounter++;

        GameObject go = new GameObject("Layer_" + newName);
        go.transform.SetParent(this.transform);
        go.transform.position = Vector3.zero;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);

        LayerData newLayer = new LayerData
        {
            id = newId,
            name = newName,
            gameObject = go,
            renderer = sr
        };

        // Adiciona no topo
        _layers.Insert(0, newLayer);
        UpdateSortingOrders();

        SetActiveLayer(newId);
        MapEvents.FireLayersChanged();
    }

    public void RemoveLayer(string id)
    {
        LayerData layer = _layers.Find(l => l.id == id);
        if (layer != null)
        {
            Destroy(layer.gameObject);
            _layers.Remove(layer);
            if (ActiveLayerId == id) ActiveLayerId = null;
            UpdateSortingOrders();
            MapEvents.FireLayersChanged();
        }
    }

    public void ToggleVisibility(string id)
    {
        LayerData layer = _layers.Find(l => l.id == id);
        if (layer != null)
        {
            layer.isVisible = !layer.isVisible;
            layer.renderer.enabled = layer.isVisible;
            MapEvents.FireLayersChanged();
        }
    }

    public void MoveLayerUp(string id)
    {
        int index = _layers.FindIndex(l => l.id == id);
        if (index > 0)
        {
            LayerData temp = _layers[index];
            _layers[index] = _layers[index - 1];
            _layers[index - 1] = temp;
            UpdateSortingOrders();
            MapEvents.FireLayersChanged();
        }
    }

    public void MoveLayerDown(string id)
    {
        int index = _layers.FindIndex(l => l.id == id);
        if (index >= 0 && index < _layers.Count - 1)
        {
            LayerData temp = _layers[index];
            _layers[index] = _layers[index + 1];
            _layers[index + 1] = temp;
            UpdateSortingOrders();
            MapEvents.FireLayersChanged();
        }
    }

    public void SetActiveLayer(string id)
    {
        ActiveLayerId = id;
        MapEvents.FireActiveLayerChanged(id);
        MapEvents.FireLayersChanged(); // Atualiza UI
    }

    private void UpdateSortingOrders()
    {
        // A lista é renderizada de baixo para cima. 
        // Index 0 é o topo da lista na UI (maior Sorting Order).
        int order = _layers.Count + 10; // +10 para ficar acima do MapBase que costuma ser 0
        for (int i = 0; i < _layers.Count; i++)
        {
            _layers[i].renderer.sortingOrder = order;
            order--;
        }
    }
}