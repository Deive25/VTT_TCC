// ============================================================
// TokenSystem.cs
// Integração completa da Visão Individual por Token (Formato e Raio).
// Controles de visão adicionados à Mini UI do Token e atalhos de rato.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Enumeração para os formatos de visão
public enum VisionShape { Circle = 0, Square = 1, Cone = 2 }

public class TokenController : MonoBehaviour
{
    public string charId;
    public bool isPlaced = false;
    public SpriteRenderer borderRenderer;

    [Header("Kinect Tracking")]
    public int kinectTrackingId = -1; // -1 = Digital. Se tiver número, é físico.
    private Vector3 _targetKinectPos;

    [Header("Sistema de Visão")]
    public bool revealsFog = true;
    public VisionShape visionShape = VisionShape.Circle;
    public float visionRadius = 3f;

    private Vector3 offset;
    private Vector3 dragStartMousePos;
    private int colorIdx = 0;
    private Color[] palette = { Color.white, new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.5f, 0.9f), new Color(0.2f, 0.8f, 0.3f), new Color(0.92f, 0.78f, 0.28f), Color.black };

    // --- OTIMIZAÇÃO: Cache de Referências ---
    private MapController _mapController;
    private FogOfWarController _fogController;
    private Camera _mainCam;
    private SpriteRenderer _spriteRenderer;

    private void Start()
    {
        _mapController = FindAnyObjectByType<MapController>();
        _fogController = FindAnyObjectByType<FogOfWarController>();
        _mainCam = Camera.main;
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetBorderColor(Color color) { if (borderRenderer != null) borderRenderer.color = color; }
    public void CycleBorderColor() { colorIdx = (colorIdx + 1) % palette.Length; SetBorderColor(palette[colorIdx]); }

    public void OnPlacedInMap()
    {
        isPlaced = true;
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null) col.enabled = true;

        // Ao soltar no mapa, volta para a camada abaixo da névoa
        if (_spriteRenderer != null) _spriteRenderer.sortingOrder = 41;
        if (borderRenderer != null) borderRenderer.sortingOrder = 40;

        RevealFogIfEnabled();
    }

    public void RevealFogIfEnabled()
    {
        // Garante que só limpa a névoa se já estiver fixado no tabuleiro
        if (isPlaced && revealsFog && _fogController != null)
        {
            // Agora envia o raio individual e o formato de visão para a GPU/Textura
            _fogController.RevealByToken(transform.position, visionRadius, visionShape);
        }
    }

    public void SetLostState(bool isLost) {
        float alpha = isLost ? 0.35f : 1f;
        if (_spriteRenderer != null)
        {
            Color c = _spriteRenderer.color;
            c.a = alpha;
            _spriteRenderer.color = c;
        }
        if (borderRenderer != null)
        {
            Color c = borderRenderer.color;
            c.a = alpha;
            borderRenderer.color = c;
        }
    }
    public void UpdatePositionFromKinect(Vector3 worldPos)
    {
        _targetKinectPos = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, _targetKinectPos, Time.deltaTime * 15f);

        RevealFogIfEnabled();
    }

    private void OnMouseOver()
    {
        // Se ainda não estiver fixado, os controlos são geridos pelo FollowMouse
        if (!isPlaced) return;

        // Botão Direito: Troca entre Círculo e Quadrado rapidamente
        if (Input.GetMouseButtonDown(1))
        {
            visionShape = visionShape == VisionShape.Circle ? VisionShape.Square : VisionShape.Circle;
            RevealFogIfEnabled(); // Atualiza na hora
        }

        // Scroll do Rato: Aumenta ou diminui o raio de visão rapidamente
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            visionRadius = Mathf.Clamp(visionRadius + Mathf.Sign(scroll) * 0.5f, 1f, 20f);
            RevealFogIfEnabled(); // Atualiza na hora
        }
    }

    void OnMouseDown()
    {
        if (kinectTrackingId != -1 && PlayerDisplaySystem.Instance != null && PlayerDisplaySystem.Instance.currentMode == VTTMode.Physical_Table)
            return;

        dragStartMousePos = Input.mousePosition;
        offset = transform.position - _mainCam.ScreenToWorldPoint(Input.mousePosition);
        offset.z = 0;
    }

    void OnMouseDrag()
    {
        if (!isPlaced) return;

        if (kinectTrackingId != -1 && PlayerDisplaySystem.Instance != null && PlayerDisplaySystem.Instance.currentMode == VTTMode.Physical_Table)
            return;

        Vector3 newPos = _mainCam.ScreenToWorldPoint(Input.mousePosition) + offset;
        newPos.z = transform.position.z;

        if (_mapController != null && _mapController.IsMapLoaded)
        {
            Bounds b = _mapController.MapBounds;
            newPos.x = Mathf.Clamp(newPos.x, b.min.x, b.max.x);
            newPos.y = Mathf.Clamp(newPos.y, b.min.y, b.max.y);
        }
        transform.position = newPos;

        // Limpa a névoa ativamente em tempo real enquanto o token passa por cima
        RevealFogIfEnabled();
    }

    void OnMouseUpAsButton()
    {
        if (Vector3.Distance(dragStartMousePos, Input.mousePosition) < 5f)
        {
            TokenSystem.Instance.OpenMiniUI(this);
        }
    }

    public void FollowMouse()
    {
        Vector3 newPos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
        newPos.z = -5f;
        transform.position = newPos;

        // Controle orgânico de escala via Scroll ANTES de fixar
        if (Input.mouseScrollDelta.y != 0)
        {
            float multiplier = Input.mouseScrollDelta.y > 0 ? 1.15f : 0.85f;
            float s = transform.localScale.x * multiplier;
            s = Mathf.Clamp(s, 0.01f, 20.0f); // Limites expandidos
            transform.localScale = new Vector3(s, s, 1f);
        }

        // Ajuste de borda antes de colocar no mapa
        if (Input.GetMouseButtonDown(1)) CycleBorderColor();
    }

    public bool TryPlaceInMap()
    {
        if (_mapController != null && _mapController.IsMapLoaded)
        {
            Vector3 mousePos = _mainCam.ScreenToWorldPoint(Input.mousePosition);
            Bounds b = _mapController.MapBounds;

            if (mousePos.x < b.min.x || mousePos.x > b.max.x || mousePos.y < b.min.y || mousePos.y > b.max.y)
            {
                Destroy(gameObject);
                return false;
            }
            transform.position = new Vector3(mousePos.x, mousePos.y, -1f);

            if (PlayerDisplaySystem.Instance != null && PlayerDisplaySystem.Instance.currentMode == VTTMode.Physical_Table)
            {
                if (KinectManager.Instance != null)
                {
                    KinectManager.Instance.StartBinding(this);
                }
                else
                {
                    Debug.LogError("KinectManager não encontrado na Cena!");
                    OnPlacedInMap();
                }
            }
            else
            {
                OnPlacedInMap();
            }
            return true;
        }
        Destroy(gameObject);
        return false;
    }
}

public class TokenSystem : MonoBehaviour
{
    public static TokenSystem Instance { get; private set; }

    public static float GlobalTokenScale = 0.31f;

    private GameObject _panel;
    private RectTransform contentRT;
    private TokenController activeToken;
    private TMP_Text nameLbl;

    private void Awake()
    {
        Instance = this;
        BuildMiniUI();
    }

    public static TokenController SpawnToken(string charId)
    {
        LayerData layer = LayerManager.Instance.GetActiveLayer();
        if (layer == null) { Debug.LogWarning("Abra um tabuleiro antes!"); return null; }

        var record = CharacterManager.Instance.GetCharacter(charId);
        if (record == null) return null;

        GameObject go = new GameObject("Token_" + record.name);
        go.transform.SetParent(layer.gameObject.transform, false);
        go.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, -5f);

        go.transform.localScale = new Vector3(GlobalTokenScale, GlobalTokenScale, 1f);

        SpriteRenderer avatarSR = go.AddComponent<SpriteRenderer>();
        Texture2D tex = CharacterManager.Instance.LoadAvatar(record.avatarFileName);
        avatarSR.sprite = VTTLayout.CreateCircularWorldSprite(tex);

        // --- CORREÇÃO: Passa por cima da névoa na primeira arrastada ---
        avatarSR.sortingOrder = 501;

        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(go.transform, false);
        borderGO.transform.localPosition = new Vector3(0, 0, 0.1f);
        borderGO.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
        SpriteRenderer borderSR = borderGO.AddComponent<SpriteRenderer>();
        borderSR.sprite = VTTLayout.GetCircleSprite();
        borderSR.color = record.system == "D&D 5e" ? VTTLayout.C_TEXT_GOLD : new Color(0.85f, 0.25f, 0.25f, 1f);
        borderSR.sortingOrder = 500;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.enabled = false;

        TokenController tc = go.AddComponent<TokenController>();
        tc.charId = charId;
        tc.borderRenderer = borderSR;
        return tc;
    }

    private void BuildMiniUI()
    {
        Canvas cv = FindAnyObjectByType<Canvas>();
        _panel = VTTLayout.New("TokenMiniUI", cv.transform);
        RectTransform rt = _panel.AddComponent<RectTransform>();

        rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-20f, 20f);
        Image bg = _panel.AddComponent<Image>(); bg.color = VTTLayout.C_HDR_BG;
        VTTLayout.AccentBar(rt, 4f, VTTLayout.C_ACCENT);

        Button btnClose = VTTLayout.BtnFixed(rt, 240f, -5f, 30f, 30f, "X", VTTLayout.C_BTN_CLOSE, VTTLayout.C_BDR_CLOSE, Color.white, 12f, true);
        btnClose.onClick.AddListener(ClosePanel);

        nameLbl = VTTLayout.LabelFixed(rt, 15f, -10f, 220f, 20f, 14f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        contentRT = VTTLayout.New("Content", rt).AddComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero; contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(15f, 15f); contentRT.offsetMax = new Vector2(-15f, -40f);

        _panel.SetActive(false);
    }

    public void OpenMiniUI(TokenController token)
    {
        activeToken = token;
        var record = CharacterManager.Instance.GetCharacter(token.charId);
        if (record == null) return;

        nameLbl.text = record.name;
        foreach (Transform child in contentRT) Destroy(child.gameObject);

        float y = 0f;
        if (record.system == "D&D 5e")
        {
            BuildStatRow("HP", "dnd_hp_curr", "dnd_hp_max", record, VTTLayout.C_TEXT_GOLD, ref y);
        }
        else
        {
            BuildStatRow("PV", "ord_pv_curr", "ord_pv_max", record, new Color(0.85f, 0.2f, 0.3f), ref y);
            BuildStatRow("PE", "ord_pe_curr", "ord_pe_max", record, new Color(0.2f, 0.5f, 0.85f), ref y);
            BuildStatRow("SAN", "ord_san_curr", "ord_san_max", record, new Color(0.6f, 0.2f, 0.85f), ref y);
        }

        y -= 5f;
        VTTLayout.Box("Div", contentRT, 0, y, 250f, 1f, VTTLayout.C_BDR_DEFAULT);
        y -= 15f;

        VTTLayout.LabelFixed(contentRT, 0, y, 150f, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = "TAMANHO DO TOKEN";
        TMP_Text scaleTxt = VTTLayout.LabelFixed(contentRT, 150f, y, 100f, 20f, 11f, VTTLayout.C_ACCENT, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        scaleTxt.text = activeToken.transform.localScale.x.ToString("F2") + "x";
        y -= 25f;

        float currentScale = activeToken.transform.localScale.x;
        float startT = currentScale <= 0.31f
            ? Mathf.InverseLerp(0.01f, 0.31f, currentScale) * 0.5f
            : 0.5f + Mathf.InverseLerp(0.31f, 20.0f, currentScale) * 0.5f;

        Slider scaleSlider = VTTLayout.MakeSlider(contentRT, y, 24f, 0f, 1f, startT);
        scaleSlider.onValueChanged.AddListener((val) => {
            if (activeToken != null)
            {
                float s = val <= 0.5f
                    ? Mathf.Lerp(0.01f, 0.31f, val * 2f)
                    : Mathf.Lerp(0.31f, 20.0f, (val - 0.5f) * 2f);

                activeToken.transform.localScale = new Vector3(s, s, 1f);
                scaleTxt.text = s.ToString("F2") + "x";
                activeToken.RevealFogIfEnabled();
            }
        });
        y -= 35f;

        VTTLayout.LabelFixed(contentRT, 0, y, 250f, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = "COR DA BORDA";
        y -= 25f;
        Color[] palette = { Color.white, new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.5f, 0.9f), new Color(0.2f, 0.8f, 0.3f), new Color(0.92f, 0.78f, 0.28f), Color.black };
        float bx = 0;
        foreach (Color c in palette)
        {
            Button cb = VTTLayout.BtnFixed(contentRT, bx, y, 32f, 32f, "", c, Color.white, c, 10f);
            cb.onClick.AddListener(() => { if (activeToken != null) activeToken.SetBorderColor(c); });
            bx += 38f;
        }
        y -= 45f;

        y -= 5f;
        VTTLayout.Box("Div2", contentRT, 0, y, 250f, 1f, VTTLayout.C_BDR_DEFAULT);
        y -= 15f;

        // --- NOVOS CONTROLOS DE VISÃO NA MINI UI ---
        VTTLayout.LabelFixed(contentRT, 0, y, 150f, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = "RAIO DE VISÃO";
        TMP_Text radTxt = VTTLayout.LabelFixed(contentRT, 150f, y, 100f, 20f, 11f, VTTLayout.C_ACCENT, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        radTxt.text = activeToken.visionRadius.ToString("F1");
        y -= 25f;

        Slider radSlider = VTTLayout.MakeSlider(contentRT, y, 24f, 1f, 20f, activeToken.visionRadius);
        radSlider.onValueChanged.AddListener((val) => {
            if (activeToken != null)
            {
                activeToken.visionRadius = val;
                radTxt.text = val.ToString("F1");
                activeToken.RevealFogIfEnabled();
            }
        });
        y -= 35f;

        Button btnShape = VTTLayout.BtnFull(contentRT, y, 30f, 0f, "FORMATO: " + (activeToken.visionShape == VisionShape.Circle ? "CÍRCULO" : "QUADRADO"), VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, Color.white, 11f);
        btnShape.onClick.AddListener(() => {
            if (activeToken != null)
            {
                activeToken.visionShape = activeToken.visionShape == VisionShape.Circle ? VisionShape.Square : VisionShape.Circle;
                btnShape.GetComponentInChildren<TMP_Text>().text = "FORMATO: " + (activeToken.visionShape == VisionShape.Circle ? "CÍRCULO" : "QUADRADO");
                activeToken.RevealFogIfEnabled();
            }
        });
        y -= 40f;

        Color fogColor = activeToken.revealsFog ? VTTLayout.C_BTN_ACTIVE : VTTLayout.C_BTN_SEC;
        string fogText = activeToken.revealsFog ? "EMITE VISÃO: LIGADO" : "FANTASMA (ESCONDIDO)";
        Button btnFog = VTTLayout.BtnFull(contentRT, y, 30f, 0f, fogText, fogColor, VTTLayout.C_BDR_DEFAULT, Color.white, 11f);
        btnFog.onClick.AddListener(() => {
            if (activeToken != null)
            {
                activeToken.revealsFog = !activeToken.revealsFog;
                OpenMiniUI(activeToken); // Atualiza a cor e o texto do botão instantaneamente
                activeToken.RevealFogIfEnabled();
            }
        });
        y -= 40f;

        Button btnDel = VTTLayout.BtnFull(contentRT, y, 34f, 0f, "REMOVER DO MAPA", VTTLayout.C_BTN_CLOSE, VTTLayout.C_BDR_CLOSE, Color.white, 12f);
        btnDel.onClick.AddListener(() => {
            if (activeToken != null) Destroy(activeToken.gameObject);
            ClosePanel();
        });
        y -= 44f;

        _panel.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, Mathf.Abs(y) + 60f);
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
    }

    private void BuildStatRow(string label, string currKey, string maxKey, CharacterRecord record, Color color, ref float y)
    {
        VTTLayout.LabelFixed(contentRT, 0, y, 60f, 30f, 12f, color, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = label;
        string cVal = record.fields.Find(f => f.key == currKey)?.value ?? "0";
        string mVal = record.fields.Find(f => f.key == maxKey)?.value ?? "0";

        TMP_InputField cIn = VTTLayout.InputFieldFixed(contentRT, 65f, y, 60f, 30f, 14f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, cVal);
        cIn.textComponent.alignment = TextAlignmentOptions.Center;

        VTTLayout.LabelFixed(contentRT, 130f, y, 15f, 30f, 14f, VTTLayout.C_TEXT_DIM, FontStyles.Bold).text = "/";

        TMP_InputField mIn = VTTLayout.InputFieldFixed(contentRT, 150f, y, 60f, 30f, 14f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, mVal);
        mIn.textComponent.alignment = TextAlignmentOptions.Center;

        cIn.onEndEdit.AddListener((val) => CharacterManager.Instance.UpdateCharacterField(activeToken.charId, currKey, val));
        mIn.onEndEdit.AddListener((val) => CharacterManager.Instance.UpdateCharacterField(activeToken.charId, maxKey, val));

        y -= 40f;
    }

    public void ClosePanel() => _panel.SetActive(false);
}