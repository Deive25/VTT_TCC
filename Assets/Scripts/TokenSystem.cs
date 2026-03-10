// ============================================================
// TokenSystem.cs
// Drag & Drop com limites extremos (0.01 a 10.0).
// Sliders com Matemática Exponencial (Meio exato = 0.31x).
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TokenController : MonoBehaviour
{
    public string charId;
    public bool isPlaced = false;
    public bool revealsFog = true;
    public SpriteRenderer borderRenderer;

    private Vector3 offset;
    private Vector3 dragStartMousePos;

    private int colorIdx = 0;
    private Color[] palette = { Color.white, new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.5f, 0.9f), new Color(0.2f, 0.8f, 0.3f), new Color(0.92f, 0.78f, 0.28f), Color.black };

    public void SetBorderColor(Color color) { if (borderRenderer != null) borderRenderer.color = color; }
    public void CycleBorderColor() { colorIdx = (colorIdx + 1) % palette.Length; SetBorderColor(palette[colorIdx]); }

    public void OnPlacedInMap()
    {
        isPlaced = true;
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null) col.enabled = true;

        GetComponent<SpriteRenderer>().sortingOrder = 41;
        if (borderRenderer != null) borderRenderer.sortingOrder = 40;

        RevealFogIfEnabled();
    }

    public void RevealFogIfEnabled()
    {
        if (isPlaced && revealsFog)
        {
            FogOfWarController fow = FindAnyObjectByType<FogOfWarController>();
            if (fow != null) fow.RevealByToken(transform.position, transform.localScale.x * 2.5f);
        }
    }

    void OnMouseDown()
    {
        dragStartMousePos = Input.mousePosition;
        offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset.z = 0;
    }

    void OnMouseDrag()
    {
        Vector3 newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + offset;
        newPos.z = transform.position.z;

        MapController mc = FindAnyObjectByType<MapController>();
        if (mc != null && mc.IsMapLoaded)
        {
            Bounds b = mc.MapBounds;
            newPos.x = Mathf.Clamp(newPos.x, b.min.x, b.max.x);
            newPos.y = Mathf.Clamp(newPos.y, b.min.y, b.max.y);
        }
        transform.position = newPos;
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
        Vector3 newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        newPos.z = -5f;
        transform.position = newPos;

        // Scroll dinâmico por multiplicação (mais orgânico para escalas)
        if (Input.mouseScrollDelta.y != 0)
        {
            float multiplier = Input.mouseScrollDelta.y > 0 ? 1.15f : 0.85f;
            float s = transform.localScale.x * multiplier;
            s = Mathf.Clamp(s, 0.01f, 10.0f); // Limites extremos
            transform.localScale = new Vector3(s, s, 1f);
        }
        if (Input.GetMouseButtonDown(1)) CycleBorderColor();
    }

    public bool TryPlaceInMap()
    {
        MapController mc = FindAnyObjectByType<MapController>();
        if (mc != null && mc.IsMapLoaded)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Bounds b = mc.MapBounds;
            if (mousePos.x < b.min.x || mousePos.x > b.max.x || mousePos.y < b.min.y || mousePos.y > b.max.y)
            {
                Destroy(gameObject);
                return false;
            }
            transform.position = new Vector3(mousePos.x, mousePos.y, -1f);
            OnPlacedInMap();
            return true;
        }
        Destroy(gameObject);
        return false;
    }
}

public class TokenSystem : MonoBehaviour
{
    public static TokenSystem Instance { get; private set; }

    // O tamanho inicial que o usuário percebeu ser o "ideal"
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
        avatarSR.sortingOrder = 201;

        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(go.transform, false);
        borderGO.transform.localPosition = new Vector3(0, 0, 0.1f);
        borderGO.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
        SpriteRenderer borderSR = borderGO.AddComponent<SpriteRenderer>();
        borderSR.sprite = VTTLayout.GetCircleSprite();
        borderSR.color = record.system == "D&D 5e" ? VTTLayout.C_TEXT_GOLD : new Color(0.85f, 0.25f, 0.25f, 1f);
        borderSR.sortingOrder = 200;

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

        // MÁGICA: Slider Logarítmico. Transforma a barra de 0 a 1 numa curva de 0.01 a 10.0!
        float startT = Mathf.Log(activeToken.transform.localScale.x / 0.01f, 1000f);
        Slider scaleSlider = VTTLayout.MakeSlider(contentRT, y, 24f, 0f, 1f, startT);
        scaleSlider.onValueChanged.AddListener((val) => {
            if (activeToken != null)
            {
                float s = 0.01f * Mathf.Pow(1000f, val);
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

        Color fogColor = activeToken.revealsFog ? VTTLayout.C_BTN_ACTIVE : VTTLayout.C_BTN_SEC;
        string fogText = activeToken.revealsFog ? "EMITE VISÃO: LIGADO" : "FANTASMA (ESCONDIDO)";
        Button btnFog = VTTLayout.BtnFull(contentRT, y, 30f, 0f, fogText, fogColor, VTTLayout.C_BDR_DEFAULT, Color.white, 11f);
        btnFog.onClick.AddListener(() => {
            if (activeToken != null)
            {
                activeToken.revealsFog = !activeToken.revealsFog;
                OpenMiniUI(activeToken);
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