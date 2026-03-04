// ============================================================
// FogOfWarController.cs  v4
// CORREÇÃO: EventSystem check para não pintar ao clicar na UI.
// ClearAll é direto e imediato, sem dependência de estado.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class FogOfWarController : MonoBehaviour
{
    public enum FogMode { Paint, Erase }

    [Header("Pincel")]
    [SerializeField] private int brushRadius    = 20;
    [SerializeField] private int minBrushRadius = 3;
    [SerializeField] private int maxBrushRadius = 80;

    [Header("Visual")]
    [SerializeField] private Color fogColor         = new Color(0.04f, 0.05f, 0.08f, 1f);
    [SerializeField] private int   maxTexResolution = 512;

    public static FogOfWarController Instance { get; private set; }

    public bool    IsActive    { get; private set; } = false;
    public FogMode CurrentMode { get; private set; } = FogMode.Paint;
    public int     BrushRadius => brushRadius;
    public int     MinBrush    => minBrushRadius;
    public int     MaxBrush    => maxBrushRadius;

    public event System.Action OnBrushChanged;

    private Texture2D      _fogTex;
    private SpriteRenderer _fogRenderer;
    private Color32[]      _pixels;
    private bool           _dirty;
    private Camera         _cam;
    private SpriteRenderer _mapRenderer;

    // ─── Lifecycle ───────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        _cam     = Camera.main;
    }

    private void OnEnable()  => MapEvents.OnMapLoaded += OnMapLoaded;
    private void OnDisable() => MapEvents.OnMapLoaded -= OnMapLoaded;

    private void Update()
    {
        if (!IsActive) return;

        // Não pinta se o cursor está sobre um elemento de UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButton(0))
        {
            PaintAt(Input.mousePosition);
            if (_dirty) FlushTexture();
        }
    }

    // ─── Setup ───────────────────────────────────────────────

    private void OnMapLoaded(Texture2D _) => StartCoroutine(SetupNextFrame());

    private IEnumerator SetupNextFrame()
    {
        yield return null;
        MapController mc = FindObjectOfType<MapController>();
        if (mc == null) yield break;
        _mapRenderer = mc.GetComponent<SpriteRenderer>();
        if (_mapRenderer == null || _mapRenderer.sprite == null) yield break;

        int mapW  = _mapRenderer.sprite.texture.width;
        int mapH  = _mapRenderer.sprite.texture.height;
        float asp = (float)mapW / mapH;
        int texW  = asp >= 1f ? maxTexResolution : Mathf.Max(1, Mathf.RoundToInt(maxTexResolution * asp));
        int texH  = asp >= 1f ? Mathf.Max(1, Mathf.RoundToInt(maxTexResolution / asp)) : maxTexResolution;

        BuildTexture(texW, texH);
        BuildRenderer(mc.transform);
    }

    private void BuildTexture(int w, int h)
    {
        if (_fogTex != null) Destroy(_fogTex);
        _fogTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        _fogTex.filterMode = FilterMode.Bilinear;
        _pixels = new Color32[w * h];
        ClearBuffer();
        _fogTex.SetPixels32(_pixels);
        _fogTex.Apply();
    }

    private void BuildRenderer(Transform mapTransform)
    {
        if (_fogRenderer == null)
        {
            GameObject go = new GameObject("FogOfWar_Overlay");
            go.transform.SetParent(mapTransform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one;
            _fogRenderer = go.AddComponent<SpriteRenderer>();
            _fogRenderer.sortingOrder = 10;
        }

        float ppu = Mathf.Max(_fogTex.width, _fogTex.height) /
                    Mathf.Max(_mapRenderer.sprite.bounds.size.x,
                              _mapRenderer.sprite.bounds.size.y);
        _fogRenderer.sprite = Sprite.Create(_fogTex,
            new Rect(0, 0, _fogTex.width, _fogTex.height),
            new Vector2(0.5f, 0.5f), ppu);
        _fogRenderer.color = Color.white;
    }

    // ─── Pintura ──────────────────────────────────────────────

    private void PaintAt(Vector3 screenPos)
    {
        if (_fogTex == null || _mapRenderer == null) return;
        if (_cam == null) _cam = Camera.main;

        Vector3 wp = _cam.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
        Bounds b = _mapRenderer.bounds;
        float u  = (wp.x - b.min.x) / b.size.x;
        float v  = (wp.y - b.min.y) / b.size.y;
        if (u < 0f || u > 1f || v < 0f || v > 1f) return;

        int cx = Mathf.Clamp(Mathf.RoundToInt(u * _fogTex.width),  0, _fogTex.width  - 1);
        int cy = Mathf.Clamp(Mathf.RoundToInt(v * _fogTex.height), 0, _fogTex.height - 1);

        Color32 fill = CurrentMode == FogMode.Paint
            ? new Color32((byte)(fogColor.r*255),(byte)(fogColor.g*255),(byte)(fogColor.b*255),255)
            : new Color32(0,0,0,0);

        int r = brushRadius, r2 = r * r;
        for (int py = Mathf.Max(0,cy-r); py <= Mathf.Min(_fogTex.height-1,cy+r); py++)
        for (int px = Mathf.Max(0,cx-r); px <= Mathf.Min(_fogTex.width -1,cx+r); px++)
        {
            int dx = px-cx, dy = py-cy;
            if (dx*dx + dy*dy <= r2)
                _pixels[py * _fogTex.width + px] = fill;
        }
        _dirty = true;
    }

    private void FlushTexture()
    {
        if (_fogTex == null || !_dirty) return;
        _fogTex.SetPixels32(_pixels);
        _fogTex.Apply();
        _dirty = false;
    }

    private void ClearBuffer()
    {
        for (int i = 0; i < _pixels.Length; i++)
            _pixels[i] = new Color32(0,0,0,0);
    }

    // ─── API Pública ──────────────────────────────────────────

    public void SetActive(bool active)       { IsActive    = active;  }
    public void SetMode(FogMode mode)        { CurrentMode = mode;    }
    public void IncreaseBrush(int delta = 5) => SetBrushRadius(brushRadius + delta);
    public void DecreaseBrush(int delta = 5) => SetBrushRadius(brushRadius - delta);

    public void SetBrushRadius(int r)
    {
        brushRadius = Mathf.Clamp(r, minBrushRadius, maxBrushRadius);
        OnBrushChanged?.Invoke();
    }

    /// <summary>Remove toda a névoa imediatamente. Sem animação, sem estado.</summary>
    public void ClearAll()
    {
        if (_pixels == null || _fogTex == null) return;
        ClearBuffer();
        _fogTex.SetPixels32(_pixels);
        _fogTex.Apply();
        _dirty = false;
        Debug.Log("[FogOfWarController] ClearAll executado.");
    }
}
