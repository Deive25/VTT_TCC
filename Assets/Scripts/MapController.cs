// ============================================================
// MapController.cs
// Responsável pela coordenação básica de MapInfo.
// Sem deformações globais de Layer. O Zoom é exclusivo da Câmara.
// ============================================================
using UnityEngine;

public class MapController : MonoBehaviour
{
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
    public float CurrentScale => currentScale;

    private void OnEnable()
    {
        MapEvents.OnCenterMapRequested += HandleCenterMap;
        MapEvents.OnResetZoomRequested += HandleResetZoom;
    }

    private void OnDisable()
    {
        MapEvents.OnCenterMapRequested -= HandleCenterMap;
        MapEvents.OnResetZoomRequested -= HandleResetZoom;
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
        CameraController cam = FindAnyObjectByType<CameraController>();
        if (cam != null) cam.FocusOnActiveBoard(); // Câmara assume todo o trabalho
        NotifyMapInfoUpdated();
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