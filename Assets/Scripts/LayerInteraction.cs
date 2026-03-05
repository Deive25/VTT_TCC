using UnityEngine;
using UnityEngine.EventSystems;

public class LayerInteraction : MonoBehaviour
{
    [SerializeField] private float resizeSpeed = 0.5f;
    private Camera cam;
    private MapController mapController;

    private bool isDragging = false;
    private Vector3 dragOffset;

    private void Start()
    {
        cam = Camera.main;
        mapController = FindAnyObjectByType<MapController>();
    }

    private void Update()
    {
        if (LayerManager.Instance == null || string.IsNullOrEmpty(LayerManager.Instance.ActiveLayerId)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        LayerData activeLayer = GetActiveLayer();
        if (activeLayer == null || !activeLayer.isVisible) return;

        HandleDrag(activeLayer);
        HandleResize(activeLayer);
        ClampToBounds(activeLayer);
    }

    private void HandleDrag(LayerData layer)
    {
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        if (Input.GetMouseButtonDown(0))
        {
            // Verifica se clicou dentro da bounding box do sprite
            if (layer.renderer.bounds.Contains(mouseWorldPos))
            {
                isDragging = true;
                dragOffset = layer.gameObject.transform.position - mouseWorldPos;
            }
        }

        if (Input.GetMouseButtonUp(0)) isDragging = false;

        if (isDragging && Input.GetMouseButton(0))
        {
            layer.gameObject.transform.position = mouseWorldPos + dragOffset;
        }
    }

    private void HandleResize(LayerData layer)
    {
        // Segurar Shift + Scroll = Redimensionar camada em vez de Zoom da Câmera
        if (Input.GetKey(KeyCode.LeftShift))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector3 scale = layer.gameObject.transform.localScale;
                scale += Vector3.one * scroll * resizeSpeed;

                // Impede que fique negativa ou 0
                scale.x = Mathf.Max(0.1f, scale.x);
                scale.y = Mathf.Max(0.1f, scale.y);

                layer.gameObject.transform.localScale = scale;
            }
        }
    }

    private void ClampToBounds(LayerData layer)
    {
        if (mapController == null || !mapController.IsMapLoaded) return;

        Bounds boardBounds = mapController.MapBounds;
        Bounds layerBounds = layer.renderer.bounds;

        Vector3 pos = layer.gameObject.transform.position;

        // Se a camada for menor que o tabuleiro, trava dentro.
        // Se for maior, trava pelas bordas.
        float minX = boardBounds.min.x + layerBounds.extents.x;
        float maxX = boardBounds.max.x - layerBounds.extents.x;
        float minY = boardBounds.min.y + layerBounds.extents.y;
        float maxY = boardBounds.max.y - layerBounds.extents.y;

        if (minX <= maxX) pos.x = Mathf.Clamp(pos.x, minX, maxX);
        if (minY <= maxY) pos.y = Mathf.Clamp(pos.y, minY, maxY);

        layer.gameObject.transform.position = pos;
    }

    private LayerData GetActiveLayer()
    {
        foreach (var l in LayerManager.Instance.Layers)
            if (l.id == LayerManager.Instance.ActiveLayerId) return l;
        return null;
    }
}