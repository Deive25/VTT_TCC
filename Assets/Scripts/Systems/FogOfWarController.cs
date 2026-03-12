// ============================================================
// FogOfWarController.cs
// Suporta a "Visão Raio-X" do Mestre através de renders duplicados
// em Camadas da Unity. O Mestre vê a camada 1, os Jogadores a 4.
// ============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FogOfWarController : MonoBehaviour
{
    public enum FogMode { Paint, Erase }

    [Header("Pincel")]
    [SerializeField] private int brushRadius = 20;
    [SerializeField] private int minBrushRadius = 3;
    [SerializeField] private int maxBrushRadius = 80;

    [Header("Visual")]
    [SerializeField] private Color fogColor = new Color(0.04f, 0.05f, 0.08f, 1f);
    [SerializeField] private int maxTexResolution = 512;

    public static FogOfWarController Instance { get; private set; }

    public bool IsActive { get; private set; } = false;
    public FogMode CurrentMode { get; private set; } = FogMode.Paint;
    public int BrushRadius => brushRadius;

    public bool isXRayActive = false; // Novo Estado do Raio-X

    public event System.Action OnBrushChanged;
    private Camera _cam;

    // Guarda as referências da névoa escura dos jogadores
    private Dictionary<LayerData, SpriteRenderer> playerFogRenderers = new Dictionary<LayerData, SpriteRenderer>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        _cam = Camera.main;
    }

    public void InitFogForBoard(LayerData board)
    {
        int mapW = board.renderer.sprite.texture.width; int mapH = board.renderer.sprite.texture.height;
        float asp = (float)mapW / mapH;
        int texW = asp >= 1f ? maxTexResolution : Mathf.Max(1, Mathf.RoundToInt(maxTexResolution * asp));
        int texH = asp >= 1f ? Mathf.Max(1, Mathf.RoundToInt(maxTexResolution / asp)) : maxTexResolution;

        board.fogTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        board.fogTex.filterMode = FilterMode.Bilinear;
        board.fogPixels = new Color32[texW * texH];
        for (int i = 0; i < board.fogPixels.Length; i++) board.fogPixels[i] = new Color32(0, 0, 0, 0);

        board.fogTex.SetPixels32(board.fogPixels); board.fogTex.Apply();
        float ppu = Mathf.Max(texW, texH) / Mathf.Max(board.renderer.sprite.bounds.size.x, board.renderer.sprite.bounds.size.y);
        Sprite sharedFogSprite = Sprite.Create(board.fogTex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), ppu);

        // 1. NÉVOA DO MESTRE (Layer 1 - TransparentFX)
        GameObject gmFogGO = new GameObject("GMFogOverlay");
        gmFogGO.layer = 1;
        gmFogGO.transform.SetParent(board.gameObject.transform, false);
        gmFogGO.transform.localPosition = new Vector3(0, 0, -0.1f);
        board.fogRenderer = gmFogGO.AddComponent<SpriteRenderer>();
        board.fogRenderer.sortingOrder = 50;
        board.fogRenderer.sprite = sharedFogSprite;

        // 2. NÉVOA DOS JOGADORES (Layer 4 - Water)
        GameObject pFogGO = new GameObject("PlayerFogOverlay");
        pFogGO.layer = 4;
        pFogGO.transform.SetParent(board.gameObject.transform, false);
        pFogGO.transform.localPosition = new Vector3(0, 0, -0.1f);
        SpriteRenderer pFogSR = pFogGO.AddComponent<SpriteRenderer>();
        pFogSR.sortingOrder = 50;
        pFogSR.sprite = sharedFogSprite; // Usam o mesmo ficheiro de imagem!

        playerFogRenderers[board] = pFogSR;
    }

    private void Update()
    {
        if (LayerManager.Instance != null)
        {
            LayerData activeBoard = LayerManager.Instance.GetActiveLayer();
            if (activeBoard != null && activeBoard.fogRenderer != null)
            {
                // Controla a transparência da visão do Mestre
                float targetAlpha = isXRayActive ? 0.35f : 1f;
                if (Mathf.Abs(activeBoard.fogRenderer.color.a - targetAlpha) > 0.01f)
                    activeBoard.fogRenderer.color = new Color(1, 1, 1, targetAlpha);

                // Aplica mudanças na placa gráfica
                if (activeBoard.fogDirty && activeBoard.fogTex != null)
                {
                    activeBoard.fogTex.SetPixels32(activeBoard.fogPixels);
                    activeBoard.fogTex.Apply();
                    activeBoard.fogDirty = false;
                }
            }
        }

        if (!IsActive) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (Input.GetMouseButton(0)) PaintAt(Input.mousePosition);
    }

    public void ToggleXRay(bool active) { isXRayActive = active; }

    private void PaintAt(Vector3 screenPos)
    {
        if (LayerManager.Instance == null) return;
        LayerData board = LayerManager.Instance.GetActiveLayer();
        if (board == null || board.fogTex == null || _cam == null) return;

        Vector3 wp = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
        Bounds b = board.renderer.bounds;
        float u = (wp.x - b.min.x) / b.size.x; float v = (wp.y - b.min.y) / b.size.y;
        if (u < 0f || u > 1f || v < 0f || v > 1f) return;

        int cx = Mathf.Clamp(Mathf.RoundToInt(u * board.fogTex.width), 0, board.fogTex.width - 1);
        int cy = Mathf.Clamp(Mathf.RoundToInt(v * board.fogTex.height), 0, board.fogTex.height - 1);

        Color32 fill = CurrentMode == FogMode.Paint ? new Color32((byte)(fogColor.r * 255), (byte)(fogColor.g * 255), (byte)(fogColor.b * 255), 255) : new Color32(0, 0, 0, 0);

        int r = brushRadius, r2 = r * r;
        for (int py = Mathf.Max(0, cy - r); py <= Mathf.Min(board.fogTex.height - 1, cy + r); py++)
            for (int px = Mathf.Max(0, cx - r); px <= Mathf.Min(board.fogTex.width - 1, cx + r); px++)
                if ((px - cx) * (px - cx) + (py - cy) * (py - cy) <= r2)
                    board.fogPixels[py * board.fogTex.width + px] = fill;

        board.fogDirty = true;
    }

    public void RevealByToken(Vector3 worldPos, float radiusInWorldUnits)
    {
        if (LayerManager.Instance == null) return;
        LayerData board = LayerManager.Instance.GetActiveLayer();
        if (board == null || board.fogTex == null) return;

        Bounds b = board.renderer.bounds;
        float u = (worldPos.x - b.min.x) / b.size.x; float v = (worldPos.y - b.min.y) / b.size.y;
        if (u < -0.5f || u > 1.5f || v < -0.5f || v > 1.5f) return;

        int cx = Mathf.Clamp(Mathf.RoundToInt(u * board.fogTex.width), 0, board.fogTex.width - 1);
        int cy = Mathf.Clamp(Mathf.RoundToInt(v * board.fogTex.height), 0, board.fogTex.height - 1);
        Color32 clearColor = new Color32(0, 0, 0, 0);

        float pixelsPerUnit = board.fogTex.width / b.size.x;
        int r = Mathf.RoundToInt(radiusInWorldUnits * pixelsPerUnit);
        int r2 = r * r;

        for (int py = Mathf.Max(0, cy - r); py <= Mathf.Min(board.fogTex.height - 1, cy + r); py++)
            for (int px = Mathf.Max(0, cx - r); px <= Mathf.Min(board.fogTex.width - 1, cx + r); px++)
                if ((px - cx) * (px - cx) + (py - cy) * (py - cy) <= r2)
                    board.fogPixels[py * board.fogTex.width + px] = clearColor;
        board.fogDirty = true;
    }

    public void SetActive(bool active) { IsActive = active; }
    public void SetMode(FogMode mode) { CurrentMode = mode; }
    public void IncreaseBrush(int delta = 5) { brushRadius = Mathf.Clamp(brushRadius + delta, minBrushRadius, maxBrushRadius); OnBrushChanged?.Invoke(); }
    public void DecreaseBrush(int delta = 5) { brushRadius = Mathf.Clamp(brushRadius - delta, minBrushRadius, maxBrushRadius); OnBrushChanged?.Invoke(); }

    public void ClearAll()
    {
        if (LayerManager.Instance == null) return;
        LayerData board = LayerManager.Instance.GetActiveLayer();
        if (board == null || board.fogPixels == null) return;
        for (int i = 0; i < board.fogPixels.Length; i++) board.fogPixels[i] = new Color32(0, 0, 0, 0);
        board.fogTex.SetPixels32(board.fogPixels); board.fogTex.Apply(); board.fogDirty = false;
    }
}