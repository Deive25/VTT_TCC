// ============================================================
// FogOfWarController.cs
// Sistema Avançado: Visão customizada individual por Token (Formato e Raio)
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FogOfWarController : MonoBehaviour
{
    public enum FogMode { Paint, Erase }
    public enum FillMode { SolidColor, CustomTexture }

    [Header("Configurações Visuais")]
    public Color fogColor = new Color(0.04f, 0.05f, 0.08f, 1f);
    public FillMode currentFillMode = FillMode.SolidColor;
    public Texture2D customFogTexture;
    public float textureTiling = 1f;

    [Header("Visão Dinâmica")]
    public float exploredOpacity = 0.65f;
    // Removido: public float tokenVisionRadius (Agora pertence a cada Token)

    [Header("Pincel")]
    [SerializeField] private int brushRadius = 20;
    private int minBrushRadius = 3;
    private int maxBrushRadius = 80;

    [Header("Resolução")]
    [SerializeField] private int maxTexResolution = 512;

    public static FogOfWarController Instance { get; private set; }

    public bool IsActive { get; private set; } = false;
    public FogMode CurrentMode { get; private set; } = FogMode.Paint;
    public int BrushRadius => brushRadius;
    public bool isXRayActive = false;

    public event System.Action OnBrushChanged;

    private Camera _cam;
    private Material fogMaterial;

    private GameObject cursorRoot;
    private LineRenderer brushCursorInner;
    private LineRenderer brushCursorOuter;

    private Dictionary<TokenController, Vector3> lastRevealedPos = new Dictionary<TokenController, Vector3>();
    private Vector4[] tokenVisionArray = new Vector4[64];
    private Vector4[] tokenDirArray = new Vector4[64];
    private Dictionary<LayerData, SpriteRenderer> playerFogRenderers = new Dictionary<LayerData, SpriteRenderer>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        _cam = Camera.main;

        Shader fogShader = Shader.Find("VTT/FogOfWar");
        if (fogShader != null) fogMaterial = new Material(fogShader);

        CreateBrushCursor();
    }

    private void CreateBrushCursor()
    {
        cursorRoot = new GameObject("FogBrushCursor");
        cursorRoot.transform.SetParent(this.transform);

        GameObject outerGO = new GameObject("CursorOuter");
        outerGO.transform.SetParent(cursorRoot.transform, false);
        brushCursorOuter = outerGO.AddComponent<LineRenderer>();
        ConfigureLineRenderer(brushCursorOuter, new Color(0f, 0f, 0f, 0.85f), 100);

        GameObject innerGO = new GameObject("CursorInner");
        innerGO.transform.SetParent(cursorRoot.transform, false);
        brushCursorInner = innerGO.AddComponent<LineRenderer>();
        ConfigureLineRenderer(brushCursorInner, Color.white, 101);

        cursorRoot.SetActive(false);
    }

    private void ConfigureLineRenderer(LineRenderer lr, Color c, int sortOrder)
    {
        lr.positionCount = 64; lr.loop = true; lr.useWorldSpace = false;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = c; lr.endColor = c; lr.sortingOrder = sortOrder;
        lr.numCapVertices = 4; lr.numCornerVertices = 4;
    }

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

        for (int i = 0; i < board.fogPixels.Length; i++)
            board.fogPixels[i] = new Color32(255, 255, 255, 0);

        board.fogTex.SetPixels32(board.fogPixels);
        board.fogTex.Apply(false);

        float ppu = Mathf.Max(texW, texH) / Mathf.Max(board.renderer.sprite.bounds.size.x, board.renderer.sprite.bounds.size.y);
        Sprite sharedFogSprite = Sprite.Create(board.fogTex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), ppu);

        board.fogRenderer = CreateFogOverlay(board, "GMFogOverlay", 1, 50, sharedFogSprite);
        SpriteRenderer pFogSR = CreateFogOverlay(board, "PlayerFogOverlay", 4, 50, sharedFogSprite);
        playerFogRenderers[board] = pFogSR;

        if (_cam != null) _cam.cullingMask &= ~(1 << 4);
        if (PlayerDisplaySystem.Instance != null && PlayerDisplaySystem.Instance.playerCam != null)
            PlayerDisplaySystem.Instance.playerCam.cullingMask &= ~(1 << 1);

        UpdateVisuals();
    }

    private SpriteRenderer CreateFogOverlay(LayerData board, string name, int layer, int order, Sprite sprite)
    {
        GameObject go = new GameObject(name); go.layer = layer;
        go.transform.SetParent(board.gameObject.transform, false);
        go.transform.localPosition = new Vector3(0, 0, -0.1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.sortingOrder = order;
        if (fogMaterial != null) sr.material = fogMaterial;
        return sr;
    }

    private void Update()
    {
        if (LayerManager.Instance != null)
        {
            LayerData activeBoard = LayerManager.Instance.GetActiveLayer();
            if (activeBoard != null && activeBoard.fogDirty && activeBoard.fogTex != null)
            {
                activeBoard.fogTex.SetPixels32(activeBoard.fogPixels);
                activeBoard.fogTex.Apply(false);
                activeBoard.fogDirty = false;
            }
        }

        UpdateTokenVision();
        UpdateBrushCursor();

        if (!IsActive) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (Input.GetMouseButton(0)) PaintAt(Input.mousePosition);
    }

    private void UpdateTokenVision()
    {
        TokenController[] tokens = FindObjectsOfType<TokenController>();
        int count = 0;

        List<TokenController> keys = new List<TokenController>(lastRevealedPos.Keys);
        foreach (var k in keys) if (k == null) lastRevealedPos.Remove(k);

        foreach (var t in tokens)
        {
            // O TOKEN AGORA CONTROLA A SUA PRÓPRIA VISÃO
            if (t != null && t.gameObject.activeInHierarchy && t.revealsFog && t.isPlaced)
            {

                // Substitua o bloco de código dentro do "if (count < 64)" no UpdateTokenVision por:
                if (count < 64)
                {
                    tokenVisionArray[count] = new Vector4(t.transform.position.x, t.transform.position.y, t.visionRadius, (float)t.visionShape);

                    // Lógica do Cone: Pega a direção (Frente do sprite) e o cosseno da metade do ângulo
                    Vector2 dir = t.transform.up; // Se o seu token "olha" para a direita, use t.transform.right
                    float cosAngle = Mathf.Cos(t.visionAngle * 0.5f * Mathf.Deg2Rad);
                    tokenDirArray[count] = new Vector4(dir.x, dir.y, cosAngle, 0f);

                    count++;
                }

                bool needsReveal = false;
                if (!lastRevealedPos.ContainsKey(t)) needsReveal = true;
                else if (Vector3.Distance(lastRevealedPos[t], t.transform.position) > 0.2f) needsReveal = true;

                if (needsReveal)
                {
                    RevealByToken(t.transform.position, t.visionRadius, t.visionShape, t.transform.up, t.visionAngle);
                    lastRevealedPos[t] = t.transform.position;
                }
            }
        }

        if (fogMaterial != null)
        {
            fogMaterial.SetInt("_TokenCount", count);
            if (count > 0)
            {
                fogMaterial.SetVectorArray("_TokenPositions", tokenVisionArray);
                fogMaterial.SetVectorArray("_TokenDirections", tokenDirArray); // NOVO
            }
        }
    }

    private void UpdateBrushCursor()
    {
        if (!IsActive || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            if (cursorRoot.activeSelf) cursorRoot.SetActive(false); return;
        }

        LayerData board = LayerManager.Instance?.GetActiveLayer();
        if (board == null || board.fogTex == null || _cam == null)
        {
            if (cursorRoot.activeSelf) cursorRoot.SetActive(false); return;
        }

        if (!cursorRoot.activeSelf) cursorRoot.SetActive(true);

        Vector3 wp = _cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -_cam.transform.position.z));
        wp.z = -0.2f;
        cursorRoot.transform.position = wp;

        float sw = _cam.orthographicSize * 0.003f;
        brushCursorInner.startWidth = sw; brushCursorInner.endWidth = sw;
        brushCursorOuter.startWidth = sw * 2.5f; brushCursorOuter.endWidth = sw * 2.5f;

        float ppu = board.fogTex.width / board.renderer.bounds.size.x;
        float worldRadius = brushRadius / ppu;

        float angle = 0f;
        for (int i = 0; i < 64; i++)
        {
            Vector3 p = new Vector3(Mathf.Cos(Mathf.Deg2Rad * angle) * worldRadius, Mathf.Sin(Mathf.Deg2Rad * angle) * worldRadius, 0);
            brushCursorInner.SetPosition(i, p); brushCursorOuter.SetPosition(i, p);
            angle += (360f / 64f);
        }

        Color ic = CurrentMode == FogMode.Paint ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 0.4f, 0.4f, 0.9f);
        brushCursorInner.startColor = ic; brushCursorInner.endColor = ic;
    }

    public void UpdateVisuals()
    {
        if (fogMaterial == null) return;
        fogMaterial.SetColor("_Color", fogColor);
        fogMaterial.SetFloat("_UseTexture", currentFillMode == FillMode.CustomTexture ? 1f : 0f);
        fogMaterial.SetFloat("_Tiling", textureTiling);
        fogMaterial.SetFloat("_ExploredOpacity", exploredOpacity);
        if (customFogTexture != null) fogMaterial.SetTexture("_FogTex", customFogTexture);

        float targetAlpha = isXRayActive ? 0.35f : 1f;
        LayerData activeBoard = LayerManager.Instance?.GetActiveLayer();
        if (activeBoard != null && activeBoard.fogRenderer != null)
            if (Mathf.Abs(activeBoard.fogRenderer.color.a - targetAlpha) > 0.01f)
                activeBoard.fogRenderer.color = new Color(1, 1, 1, targetAlpha);
    }

    public void SetColor(Color c) { fogColor = c; UpdateVisuals(); }
    public void SetOpacity(float alpha) { fogColor.a = alpha; UpdateVisuals(); }
    public void SetExploredOpacity(float alpha) { exploredOpacity = alpha; UpdateVisuals(); }
    public void SetMode(FillMode mode) { currentFillMode = mode; UpdateVisuals(); }
    public void SetTiling(float t) { textureTiling = t; UpdateVisuals(); }
    public void LoadTexture(Texture2D tex) { customFogTexture = tex; currentFillMode = FillMode.CustomTexture; UpdateVisuals(); }
    public void ToggleXRay(bool active) { isXRayActive = active; UpdateVisuals(); }

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

        byte alphaValue = CurrentMode == FogMode.Paint ? (byte)255 : (byte)0;
        Color32 pixel = new Color32(255, 255, 255, alphaValue);

        int r = brushRadius, r2 = r * r;
        for (int py = Mathf.Max(0, cy - r); py <= Mathf.Min(board.fogTex.height - 1, cy + r); py++)
            for (int px = Mathf.Max(0, cx - r); px <= Mathf.Min(board.fogTex.width - 1, cx + r); px++)
                if ((px - cx) * (px - cx) + (py - cy) * (py - cy) <= r2)
                    board.fogPixels[py * board.fogTex.width + px] = pixel;

        board.fogDirty = true;
    }

    // --- AGORA RECEBE O FORMATO DA VISÃO DO TOKEN ---
    public void RevealByToken(Vector3 worldPos, float radiusInWorldUnits, VisionShape shape, Vector2 direction, float visionAngle)
    {
        if (LayerManager.Instance == null) return;
        LayerData board = LayerManager.Instance.GetActiveLayer();
        if (board == null || board.fogTex == null) return;

        Bounds b = board.renderer.bounds;
        float u = (worldPos.x - b.min.x) / b.size.x; float v = (worldPos.y - b.min.y) / b.size.y;
        if (u < -0.5f || u > 1.5f || v < -0.5f || v > 1.5f) return;

        int cx = Mathf.Clamp(Mathf.RoundToInt(u * board.fogTex.width), 0, board.fogTex.width - 1);
        int cy = Mathf.Clamp(Mathf.RoundToInt(v * board.fogTex.height), 0, board.fogTex.height - 1);

        Color32 exploredColor = new Color32(255, 255, 255, 128);

        float pixelsPerUnit = board.fogTex.width / b.size.x;
        int r = Mathf.RoundToInt(radiusInWorldUnits * pixelsPerUnit);
        int r2 = r * r;

        float cosAngleThreshold = Mathf.Cos(visionAngle * 0.5f * Mathf.Deg2Rad);

        for (int py = Mathf.Max(0, cy - r); py <= Mathf.Min(board.fogTex.height - 1, cy + r); py++)
        {
            for (int px = Mathf.Max(0, cx - r); px <= Mathf.Min(board.fogTex.width - 1, cx + r); px++)
            {
                bool inVision = false;
                float dx = px - cx;
                float dy = py - cy;
                float sqrDist = dx * dx + dy * dy;

                if (shape == VisionShape.Circle)
                {
                    if (sqrDist <= r2) inVision = true;
                }
                else if (shape == VisionShape.Square)
                {
                    if (Mathf.Abs(dx) <= r && Mathf.Abs(dy) <= r) inVision = true;
                }
                else if (shape == VisionShape.Cone) 
                {
                    if (sqrDist <= r2 && sqrDist > 0)
                    {
                        Vector2 pixelDir = new Vector2(dx, dy).normalized;
                        if (Vector2.Dot(pixelDir, direction) >= cosAngleThreshold)
                            inVision = true;
                    }
                    else if (sqrDist == 0) inVision = true; // O centro da visão sempre é visível
                }

                if (inVision)
                {
                    int idx = py * board.fogTex.width + px;
                    if (board.fogPixels[idx].a > 200)
                    {
                        board.fogPixels[idx] = new Color32(255, 255, 255, 128); // exploredColor
                        board.fogDirty = true;
                    }
                }
            }
        }
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
        Color32 clearColor = new Color32(255, 255, 255, 0);
        for (int i = 0; i < board.fogPixels.Length; i++) board.fogPixels[i] = clearColor;
        board.fogTex.SetPixels32(board.fogPixels); board.fogTex.Apply(false); board.fogDirty = false;
    }

    public void FillAll()
    {
        if (LayerManager.Instance == null) return;
        LayerData board = LayerManager.Instance.GetActiveLayer();
        if (board == null || board.fogPixels == null) return;
        Color32 fillCol = new Color32(255, 255, 255, 255);
        for (int i = 0; i < board.fogPixels.Length; i++) board.fogPixels[i] = fillCol;
        board.fogTex.SetPixels32(board.fogPixels); board.fogTex.Apply(false); board.fogDirty = false;
    }
}