// ============================================================
// FogOfWarController.cs  v5
// Agora gerencia a pintura nas texturas individuais de cada tabuleiro.
// ============================================================
using System.Collections;
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

    public event System.Action OnBrushChanged;
    private Camera _cam;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        _cam = Camera.main;
    }

    // Inicializa uma névoa única para um novo tabuleiro recém-criado
    public void InitFogForBoard(LayerData board)
    {
        int mapW = board.renderer.sprite.texture.width;
        int mapH = board.renderer.sprite.texture.height;
        float asp = (float)mapW / mapH;
        int texW = asp >= 1f ? maxTexResolution : Mathf.Max(1, Mathf.RoundToInt(maxTexResolution * asp));
        int texH = asp >= 1f ? Mathf.Max(1, Mathf.RoundToInt(maxTexResolution / asp)) : maxTexResolution;

        board.fogTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        board.fogTex.filterMode = FilterMode.Bilinear;
        board.fogPixels = new Color32[texW * texH];

        // Inicia transparente (limpo)
        for (int i = 0; i < board.fogPixels.Length; i++)
            board.fogPixels[i] = new Color32(0, 0, 0, 0);

        board.fogTex.SetPixels32(board.fogPixels);
        board.fogTex.Apply();

        GameObject fogGO = new GameObject("FogOverlay");
        fogGO.transform.SetParent(board.gameObject.transform, false);
        fogGO.transform.localPosition = new Vector3(0, 0, -0.1f); // Fica na frente do mapa

        board.fogRenderer = fogGO.AddComponent<SpriteRenderer>();
        board.fogRenderer.sortingOrder = 50;

        float ppu = Mathf.Max(texW, texH) / Mathf.Max(board.renderer.sprite.bounds.size.x, board.renderer.sprite.bounds.size.y);
        board.fogRenderer.sprite = Sprite.Create(board.fogTex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), ppu);
    }

    private void Update()
    {
        if (!IsActive) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButton(0))
        {
            PaintAt(Input.mousePosition);
        }

        // Se a névoa do tabuleiro ativo foi alterada neste frame, aplicamos na placa de video
        if (LayerManager.Instance != null)
        {
            LayerData activeBoard = LayerManager.Instance.GetActiveLayer();
            if (activeBoard != null && activeBoard.fogDirty && activeBoard.fogTex != null)
            {
                activeBoard.fogTex.SetPixels32(activeBoard.fogPixels);
                activeBoard.fogTex.Apply();
                activeBoard.fogDirty = false;
            }
        }
    }

    private void PaintAt(Vector3 screenPos)
    {
        if (LayerManager.Instance == null) return;
        LayerData board = LayerManager.Instance.GetActiveLayer();
        if (board == null || board.fogTex == null || _cam == null) return;

        Vector3 wp = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
        Bounds b = board.renderer.bounds;
        float u = (wp.x - b.min.x) / b.size.x;
        float v = (wp.y - b.min.y) / b.size.y;
        if (u < 0f || u > 1f || v < 0f || v > 1f) return;

        int cx = Mathf.Clamp(Mathf.RoundToInt(u * board.fogTex.width), 0, board.fogTex.width - 1);
        int cy = Mathf.Clamp(Mathf.RoundToInt(v * board.fogTex.height), 0, board.fogTex.height - 1);

        Color32 fill = CurrentMode == FogMode.Paint
            ? new Color32((byte)(fogColor.r * 255), (byte)(fogColor.g * 255), (byte)(fogColor.b * 255), 255)
            : new Color32(0, 0, 0, 0);

        int r = brushRadius, r2 = r * r;
        for (int py = Mathf.Max(0, cy - r); py <= Mathf.Min(board.fogTex.height - 1, cy + r); py++)
            for (int px = Mathf.Max(0, cx - r); px <= Mathf.Min(board.fogTex.width - 1, cx + r); px++)
            {
                int dx = px - cx, dy = py - cy;
                if (dx * dx + dy * dy <= r2)
                    board.fogPixels[py * board.fogTex.width + px] = fill;
            }
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

        for (int i = 0; i < board.fogPixels.Length; i++)
            board.fogPixels[i] = new Color32(0, 0, 0, 0);

        board.fogTex.SetPixels32(board.fogPixels);
        board.fogTex.Apply();
        board.fogDirty = false;
    }
}