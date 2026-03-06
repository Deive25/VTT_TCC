using UnityEngine;

public class MapController : MonoBehaviour
{
    [Header("Configurações de Escala")]
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 5f;

    private float currentScale = 1f;

    public SpriteRenderer ActiveRenderer
    {
        get
        {
            if (LayerManager.Instance != null)
            {
                var active = LayerManager.Instance.GetActiveLayer();
                if (active != null) return active.renderer;
            }
            return null;
        }
    }

    public Bounds MapBounds => ActiveRenderer != null ? ActiveRenderer.bounds : new Bounds(Vector3.zero, new Vector3(24f, 14f, 1f));
    public bool IsMapLoaded => ActiveRenderer != null;
    public float MinScale => minScale;
    public float MaxScale => maxScale;
    public float CurrentScale => currentScale;

    private void OnEnable()
    {
        MapEvents.OnScaleChangeRequested += HandleScaleChange;
        MapEvents.OnCenterMapRequested += HandleCenterMap;
        MapEvents.OnResetZoomRequested += HandleResetZoom;
    }

    private void OnDisable()
    {
        MapEvents.OnScaleChangeRequested -= HandleScaleChange;
        MapEvents.OnCenterMapRequested -= HandleCenterMap;
        MapEvents.OnResetZoomRequested -= HandleResetZoom;
    }

    private void HandleScaleChange(float newScale)
    {
        currentScale = Mathf.Clamp(newScale, minScale, maxScale);
        transform.localScale = new Vector3(currentScale, currentScale, 1f);
        NotifyMapInfoUpdated();
    }

    private void HandleCenterMap()
    {
        Camera cam = Camera.main;
        if (cam != null) transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        else transform.position = Vector3.zero;
    }

    private void HandleResetZoom()
    {
        transform.position = Vector3.zero;
        FitMapToScreen();
        NotifyMapInfoUpdated();
    }

    public void FitMapToScreen()
    {
        if (!IsMapLoaded) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        float screenHeight = cam.orthographicSize * 2f;
        float screenWidth = screenHeight * cam.aspect;

        Bounds bounds = MapBounds;
        float spriteHeight = bounds.size.y / currentScale;
        float spriteWidth = bounds.size.x / currentScale;

        if (spriteWidth == 0 || spriteHeight == 0) return;

        float scaleX = screenWidth / spriteWidth;
        float scaleY = screenHeight / spriteHeight;

        currentScale = Mathf.Min(scaleX, scaleY) * 0.95f;
        currentScale = Mathf.Clamp(currentScale, minScale, maxScale);

        transform.localScale = new Vector3(currentScale, currentScale, 1f);
    }

    public void NotifyMapInfoUpdated()
    {
        int w = 0, h = 0;
        if (IsMapLoaded)
        {
            w = ActiveRenderer.sprite.texture.width;
            h = ActiveRenderer.sprite.texture.height;
        }

        MapEvents.FireMapInfoUpdated(new MapInfo { widthPx = w, heightPx = h, scale = currentScale, isLoaded = IsMapLoaded });
    }
}