// ============================================================
// GMUIController.cs  v4
//
// PAINEL DIREITO — controles do mestre
//   • Mapa: importar
//   • Câmera: zoom slider + centralizar + reset
//   • Ferramentas:
//       – Dados: seletor de tipo + botão rolar + histórico
//       – Névoa: pintar / apagar / tamanho pincel / preencher / limpar
//   • Informações: escala, zoom, cursor, névoa
//
// PAINEL ESQUERDO — base (camadas, tokens placeholder)
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GMUIController : MonoBehaviour
{
    // ─── Dependências ────────────────────────────────────────
    private MapController mapController;
    private CoordinateSystem coordSystem;
    private CameraController cameraController;
    private FogOfWarController fogController;
    private DiceRollOverlay diceOverlay;

    // ─── Elementos reativos ──────────────────────────────────
    private Slider zoomSlider;
    private TMP_Text infoText;
    private TMP_Text mapStatusLabel;

    // Névoa
    private Button fogPaintBtn;
    private Button fogEraseBtn;
    private Slider brushSlider;
    private TMP_Text fogStatusLabel;

    // Dados
    private TMP_Text historyText;

    // ─── Layout ──────────────────────────────────────────────
    private const float W_RIGHT = 280f;
    private const float W_LEFT = 185f;
    private const float PAD = 12f;
    private const float GAP = 6f;
    private const float SEC_GAP = 10f;
    private const float BTN_H = 36f;
    private const float HDR_H = 32f;

    // ─── Paleta ──────────────────────────────────────────────
    private static Color C_BG = Hex("181B22");
    private static Color C_SECTION = Hex("1E2230");
    private static Color C_ACCENT = Hex("3D5A8A");
    private static Color C_ACCENT_LT = Hex("5880B0");
    private static Color C_BTN_PRI = Hex("2E4E7A");
    private static Color C_BTN_SEC = Hex("232838");
    private static Color C_BTN_FOG_P = Hex("1E3048");   // pintar névoa
    private static Color C_BTN_FOG_E = Hex("3A2020");   // apagar névoa
    private static Color C_BTN_FOG_ON = Hex("3D5A8A");   // selecionado
    private static Color C_BTN_DICE = Hex("2E2848");
    private static Color C_BORDER = Hex("35404F");
    private static Color C_BORDER_ACC = Hex("4A6FA5");
    private static Color C_DIVIDER = Hex("252B38");
    private static Color C_TEXT = Hex("D4D9E8");
    private static Color C_SUBTEXT = Hex("606880");
    private static Color C_HDR_TEXT = Hex("96A4BC");
    private static Color C_INFO_BG = Hex("111318");
    private static Color C_LEFT_BG = Hex("151820");
    private static Color C_WARN = Hex("C87840");
    private static Color C_OK = Hex("4A9A5A");

    // ─── Lifecycle ───────────────────────────────────────────

    private void Awake()
    {
        mapController = FindAnyObjectByType<MapController>();
        coordSystem = FindAnyObjectByType<CoordinateSystem>();
        cameraController = FindAnyObjectByType<CameraController>();
        fogController = FindAnyObjectByType<FogOfWarController>();
        diceOverlay = FindAnyObjectByType<DiceRollOverlay>();
    }

    private void Start()
    {
        BuildUI();

        // Ouve histórico de dados
        if (diceOverlay != null)
            diceOverlay.OnHistoryChanged += RefreshHistoryDisplay;
    }

    private void OnEnable() => MapEvents.OnMapInfoUpdated += HandleMapInfoUpdated;
    private void OnDisable() => MapEvents.OnMapInfoUpdated -= HandleMapInfoUpdated;

    private void Update()
    {
        SyncZoomSlider();
        UpdateMouseCoord();
        SyncFogUI();
    }

    // =========================================================
    // Construção principal
    // =========================================================

    private void BuildUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("[GMUIController] Canvas não encontrado."); return; }
        BuildRightPanel(canvas.transform);
        BuildLeftPanel(canvas.transform);
    }

    // ─── PAINEL DIREITO ──────────────────────────────────────

    private void BuildRightPanel(Transform parent)
    {
        RectTransform panel = MakePanel("GM_Right", parent,
            new Vector2(1f, 0f), new Vector2(1f, 1f), W_RIGHT);

        float y = 0f;
        y = BuildPanelHeader(panel, y, "TELA DO MESTRE");

        y = BuildSectionHdr(panel, y, "MAPA");
        y = BuildMapSection(panel, y);

        y = BuildSectionHdr(panel, y, "CÂMERA");
        y = BuildCameraSection(panel, y);

        y = BuildSectionHdr(panel, y, "DADOS");
        y = BuildDiceSection(panel, y);

        y = BuildSectionHdr(panel, y, "NÉVOA DE GUERRA");
        y = BuildFogSection(panel, y);

        y = BuildSectionHdr(panel, y, "INFORMAÇÕES");
        y = BuildInfoSection(panel, y);

        panel.sizeDelta = new Vector2(W_RIGHT, Mathf.Abs(y) + PAD);
    }

    // ─── PAINEL ESQUERDO ─────────────────────────────────────

    private void BuildLeftPanel(Transform parent)
    {
        RectTransform panel = MakePanel("GM_Left", parent,
            new Vector2(0f, 0f), new Vector2(0f, 1f), W_LEFT, C_LEFT_BG,
            new Vector2(0f, 1f));

        float y = 0f;
        y = BuildPanelHeader(panel, y, "RECURSOS");

        y = BuildSectionHdr(panel, y, "CAMADAS");
        y = BuildPlaceholder(panel, y, "Camadas de terreno\n(em breve)");

        y = BuildSectionHdr(panel, y, "TOKENS");
        y = BuildPlaceholder(panel, y, "Tokens de personagens\n(em breve)");

        y = BuildSectionHdr(panel, y, "RASTREAMENTO");
        y = BuildPlaceholder(panel, y, "Rastreamento de\npersonagens (em breve)");

        panel.sizeDelta = new Vector2(W_LEFT, Mathf.Abs(y) + PAD * 2f);
    }

    // =========================================================
    // Seções de conteúdo
    // =========================================================

    private float BuildMapSection(RectTransform p, float y)
    {
        mapStatusLabel = MakeSimpleLabel(p, y, 16f, 9.5f, C_SUBTEXT);
        mapStatusLabel.text = "Nenhum mapa carregado";
        y -= 16f + GAP;

        Button btn = MakeFullBtn(p, y, BTN_H,
            "  ＋  IMPORTAR MAPA", C_BTN_PRI, C_TEXT, 11f, true, C_BORDER_ACC);
        btn.onClick.AddListener(OnImportMap);
        y -= BTN_H + SEC_GAP;
        return y;
    }

    private float BuildCameraSection(RectTransform p, float y)
    {
        MakeSimpleLabel(p, y, 16f, 9.5f, C_SUBTEXT, FontStyles.Bold).text = "ZOOM";
        y -= 16f + 4f;

        float mn = cameraController != null ? cameraController.MinZoom : 0.5f;
        float mx = cameraController != null ? cameraController.MaxZoom : 30f;
        float cur = cameraController != null ? cameraController.CurrentZoom : 5f;
        zoomSlider = MakeSlider(p, y, 30f, mn, mx, cur);
        zoomSlider.onValueChanged.AddListener(v => cameraController?.SetZoom(v));
        y -= 30f + GAP;

        float bw = (W_RIGHT - PAD * 2f - GAP) * 0.5f;
        MakeFixedBtn(p, new Vector2(PAD, y), new Vector2(bw, BTN_H - 4f),
            "⊹ CENTRALIZAR", C_BTN_SEC, C_TEXT, 9f, false, C_BORDER)
            .onClick.AddListener(OnCenterMap);
        MakeFixedBtn(p, new Vector2(PAD + bw + GAP, y), new Vector2(bw, BTN_H - 4f),
            "↺ RESET ZOOM", C_BTN_SEC, C_TEXT, 9f, false, C_BORDER)
            .onClick.AddListener(OnResetZoom);

        y -= (BTN_H - 4f) + SEC_GAP;
        return y;
    }

    // ─── Seção Dados ──────────────────────────────────────────

    private float BuildDiceSection(RectTransform p, float y)
    {
        // Botão abrir painel de dados
        Button openBtn = MakeFullBtn(p, y, BTN_H,
            "⚄  ABRIR DADOS", C_BTN_DICE, C_TEXT, 11f, true, C_BORDER);
        openBtn.onClick.AddListener(OnOpenDice);
        y -= BTN_H + GAP;

        // Histórico
        MakeSimpleLabel(p, y, 14f, 9f, C_SUBTEXT, FontStyles.Bold).text = "HISTÓRICO";
        y -= 14f + 3f;

        float histH = 72f;
        GameObject box = MakeRect("HistBox", p,
            new Vector2(PAD, y), new Vector2(-PAD * 2f, -histH),
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        box.AddComponent<Image>().color = C_INFO_BG;
        AddBorder(box.GetComponent<RectTransform>(), C_BORDER);

        historyText = MakeStretchedText("HistText", box.GetComponent<RectTransform>(),
            new Vector2(8f, -6f), new Vector2(-6f, 6f));
        historyText.fontSize = 9.5f;
        historyText.color = C_SUBTEXT;
        historyText.lineSpacing = 5f;
        historyText.text = "<i>Nenhum lançamento ainda</i>";

        y -= histH + SEC_GAP;
        return y;
    }

    // ─── Seção Névoa de Guerra ────────────────────────────────

    private float BuildFogSection(RectTransform p, float y)
    {
        float bw = (W_RIGHT - PAD * 2f - GAP) * 0.5f;

        // Botões Pintar / Apagar (toggle)
        fogPaintBtn = MakeFixedBtn(p, new Vector2(PAD, y), new Vector2(bw, BTN_H),
            "✏  PINTAR", C_BTN_FOG_P, C_TEXT, 10f, true, C_BORDER);
        fogPaintBtn.onClick.AddListener(OnFogPaint);

        fogEraseBtn = MakeFixedBtn(p, new Vector2(PAD + bw + GAP, y), new Vector2(bw, BTN_H),
            "◻  APAGAR", C_BTN_FOG_E, C_TEXT, 10f, true, C_BORDER);
        fogEraseBtn.onClick.AddListener(OnFogErase);

        y -= BTN_H + GAP;

        // Tamanho do pincel
        MakeSimpleLabel(p, y, 14f, 9.5f, C_SUBTEXT).text = "Tamanho do pincel";
        y -= 14f + 3f;

        Slider brush = MakeSlider(p, y, 26f, 5f, 80f, 20f);
        brush.onValueChanged.AddListener(v =>
        {
            if (fogController != null)
                fogController.SetBrushRadius(Mathf.RoundToInt(v));
        });
        y -= 26f + GAP;

        // Preencher / Limpar tudo
        MakeFixedBtn(p, new Vector2(PAD, y), new Vector2(bw, BTN_H - 6f),
            "▩  PREENCHER", C_BTN_SEC, C_WARN, 9f, false, C_BORDER)
            .onClick.AddListener(OnFogFill);

        MakeFixedBtn(p, new Vector2(PAD + bw + GAP, y), new Vector2(bw, BTN_H - 6f),
            "□  LIMPAR TUDO", C_BTN_SEC, C_TEXT, 9f, false, C_BORDER)
            .onClick.AddListener(OnFogClear);

        y -= (BTN_H - 6f) + GAP;

        // Status
        fogStatusLabel = MakeSimpleLabel(p, y, 14f, 9f, C_SUBTEXT);
        fogStatusLabel.text = "Ferramenta inativa";
        y -= 14f + SEC_GAP;

        return y;
    }

    // ─── Seção Informações ────────────────────────────────────

    private float BuildInfoSection(RectTransform p, float y)
    {
        float h = 104f;
        GameObject box = MakeRect("InfoBox", p,
            new Vector2(PAD, y), new Vector2(-PAD * 2f, -h),
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        box.AddComponent<Image>().color = C_INFO_BG;
        MakeAccentBar(box.GetComponent<RectTransform>(), 2f, C_ACCENT);
        AddBorder(box.GetComponent<RectTransform>(), C_BORDER);

        infoText = MakeStretchedText("InfoText", box.GetComponent<RectTransform>(),
            new Vector2(PAD + 2f, -7f), new Vector2(-6f, 7f));
        infoText.fontSize = 10f;
        infoText.color = C_TEXT;
        infoText.lineSpacing = 8f;
        infoText.text = GetDefaultInfoText();

        y -= h + PAD;
        return y;
    }

    private float BuildPlaceholder(RectTransform p, float y, string msg)
    {
        float h = 60f;
        GameObject box = MakeRect("PH", p,
            new Vector2(PAD, y), new Vector2(-PAD * 2f, -h),
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        box.AddComponent<Image>().color = new Color(0.07f, 0.08f, 0.10f, 0.5f);
        AddBorder(box.GetComponent<RectTransform>(), C_DIVIDER);

        TMP_Text t = MakeStretchedText("PHText", box.GetComponent<RectTransform>(),
            new Vector2(8f, -6f), new Vector2(-8f, 6f));
        t.fontSize = 9.5f;
        t.color = C_SUBTEXT;
        t.lineSpacing = 4f;
        t.text = msg;

        y -= h + SEC_GAP;
        return y;
    }

    // =========================================================
    // Elementos comuns de layout
    // =========================================================

    private float BuildPanelHeader(RectTransform p, float y, string title)
    {
        float h = 46f;
        GameObject hdr = MakeRect("PanelHdr", p,
            new Vector2(0f, y), new Vector2(0f, -h),
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        hdr.AddComponent<Image>().color = Hex("253048");
        MakeAccentBar(hdr.GetComponent<RectTransform>(), 3f, C_ACCENT_LT);

        TMP_Text t = MakeStretchedText("HdrTitle", hdr.GetComponent<RectTransform>(),
            new Vector2(PAD + 6f, -4f), new Vector2(-PAD, 4f));
        t.text = title;
        t.fontSize = 12f;
        t.fontStyle = FontStyles.Bold;
        t.color = Hex("E8EEF8");
        t.alignment = TextAlignmentOptions.MidlineLeft;

        return y - h - 2f;
    }

    private float BuildSectionHdr(RectTransform p, float y, string label)
    {
        y -= SEC_GAP * 0.3f;

        GameObject hdr = MakeRect("SecHdr_" + label, p,
            new Vector2(0f, y), new Vector2(0f, -HDR_H),
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        hdr.AddComponent<Image>().color = C_SECTION;
        MakeAccentBar(hdr.GetComponent<RectTransform>(), 3f, C_ACCENT);

        TMP_Text lbl = MakeStretchedText("SecLabel", hdr.GetComponent<RectTransform>(),
            new Vector2(PAD + 6f, 0f), new Vector2(-PAD, 0f));
        lbl.text = label;
        lbl.fontSize = 9.5f;
        lbl.fontStyle = FontStyles.Bold;
        lbl.color = C_HDR_TEXT;
        lbl.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject div = MakeRect("SecDiv", p,
            new Vector2(0f, y - HDR_H), new Vector2(0f, -1f),
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        div.AddComponent<Image>().color = C_ACCENT;

        return y - HDR_H - 1f - GAP;
    }

    // =========================================================
    // Fábrica de widgets
    // =========================================================

    private RectTransform MakePanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, float width,
        Color? bg = null, Vector2? pivot = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot ?? new Vector2(1f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, 0f);
        obj.AddComponent<Image>().color = bg ?? C_BG;
        obj.AddComponent<GraphicRaycaster>();
        return rt;
    }

    private GameObject MakeRect(string name, RectTransform parent,
        Vector2 pos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax,
        Vector2? pivot = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot ?? new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return obj;
    }

    private TMP_Text MakeSimpleLabel(RectTransform p, float y, float height,
        float fontSize, Color color, FontStyles style = FontStyles.Normal)
    {
        GameObject obj = new GameObject("Lbl");
        obj.transform.SetParent(p, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(PAD, y);
        rt.sizeDelta = new Vector2(-PAD * 2f, -height);
        TMP_Text t = obj.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.color = color;
        t.fontStyle = style;
        return t;
    }

    private TMP_Text MakeStretchedText(string name, RectTransform parent,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return obj.AddComponent<TextMeshProUGUI>();
    }

    // Botão full-width com borda
    private Button MakeFullBtn(RectTransform p, float y, float height,
        string label, Color bg, Color textColor, float fontSize,
        bool bold, Color borderColor)
    {
        GameObject wrap = MakeRect("BtnW", p,
            new Vector2(PAD, y), new Vector2(-PAD * 2f, -height),
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        AddBorder(wrap.GetComponent<RectTransform>(), borderColor);
        return MakeBtnInside(wrap.GetComponent<RectTransform>(), label, bg, textColor, fontSize, bold);
    }

    // Botão posição e tamanho fixos com borda
    private Button MakeFixedBtn(RectTransform p, Vector2 pos, Vector2 size,
        string label, Color bg, Color textColor, float fontSize,
        bool bold, Color borderColor)
    {
        GameObject wrap = new GameObject("BtnW");
        wrap.transform.SetParent(p, false);
        RectTransform wrt = wrap.AddComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0f, 1f);
        wrt.anchorMax = new Vector2(0f, 1f);
        wrt.pivot = new Vector2(0f, 1f);
        wrt.anchoredPosition = pos;
        wrt.sizeDelta = new Vector2(size.x, -size.y);
        AddBorder(wrt, borderColor);
        return MakeBtnInside(wrt, label, bg, textColor, fontSize, bold);
    }

    private Button MakeBtnInside(RectTransform parent, string label,
        Color bg, Color textColor, float fontSize, bool bold)
    {
        GameObject obj = new GameObject("Btn");
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = new Vector2(-2f, -2f);
        rt.anchoredPosition = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image img = obj.AddComponent<Image>();
        img.color = bg;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
        cb.pressedColor = Color.Lerp(bg, Color.black, 0.2f);
        cb.selectedColor = bg;
        btn.colors = cb;

        GameObject to = new GameObject("Lbl");
        to.transform.SetParent(obj.transform, false);
        RectTransform trt = to.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;

        TMP_Text t = to.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = fontSize;
        t.color = textColor;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;

        return btn;
    }

    private Slider MakeSlider(RectTransform p, float y, float height,
        float min, float max, float val)
    {
        GameObject wrap = MakeRect("SliderW", p,
            new Vector2(PAD, y), new Vector2(-PAD * 2f, -height),
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        AddBorder(wrap.GetComponent<RectTransform>(), C_BORDER);

        GameObject obj = new GameObject("Slider");
        obj.transform.SetParent(wrap.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = new Vector2(-4f, -4f);
        rt.anchoredPosition = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        Slider s = obj.AddComponent<Slider>();
        s.minValue = min;
        s.maxValue = max;
        s.value = val;
        s.direction = Slider.Direction.LeftToRight;

        // Track
        GameObject track = new GameObject("Track");
        track.transform.SetParent(obj.transform, false);
        RectTransform trt = track.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.3f);
        trt.anchorMax = new Vector2(1f, 0.7f);
        trt.sizeDelta = Vector2.zero;
        track.AddComponent<Image>().color = Hex("111420");

        // Fill area
        GameObject fa = new GameObject("FillArea");
        fa.transform.SetParent(obj.transform, false);
        RectTransform fart = fa.AddComponent<RectTransform>();
        fart.anchorMin = new Vector2(0f, 0.3f);
        fart.anchorMax = new Vector2(1f, 0.7f);
        fart.sizeDelta = new Vector2(-10f, 0f);
        fart.anchoredPosition = new Vector2(5f, 0f);
        fart.pivot = new Vector2(0.5f, 0.5f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fa.transform, false);
        RectTransform fillrt = fill.AddComponent<RectTransform>();
        fillrt.anchorMin = Vector2.zero;
        fillrt.anchorMax = new Vector2(0f, 1f);
        fillrt.sizeDelta = Vector2.zero;
        fillrt.pivot = new Vector2(0f, 0.5f);
        fillrt.anchoredPosition = Vector2.zero;
        fill.AddComponent<Image>().color = C_ACCENT;

        // Handle area
        GameObject ha = new GameObject("HandleArea");
        ha.transform.SetParent(obj.transform, false);
        RectTransform hart = ha.AddComponent<RectTransform>();
        hart.anchorMin = Vector2.zero;
        hart.anchorMax = Vector2.one;
        hart.sizeDelta = new Vector2(-10f, 0f);
        hart.anchoredPosition = Vector2.zero;
        hart.pivot = new Vector2(0.5f, 0.5f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(ha.transform, false);
        RectTransform hrt = handle.AddComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(14f, 14f);
        hrt.pivot = new Vector2(0.5f, 0.5f);
        Image himg = handle.AddComponent<Image>();
        himg.color = Hex("B8C8E0");

        s.fillRect = fillrt;
        s.handleRect = hrt;
        s.targetGraphic = himg;

        return s;
    }

    private void AddBorder(RectTransform parent, Color color)
    {
        GameObject b = new GameObject("Border");
        b.transform.SetParent(parent, false);
        RectTransform rt = b.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        Image img = b.AddComponent<Image>();
        img.color = color;
        Outline o = b.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(1f, -1f);
        img.color = Color.clear;
    }

    private void MakeAccentBar(RectTransform parent, float width, Color color)
    {
        GameObject bar = new GameObject("Accent");
        bar.transform.SetParent(parent, false);
        RectTransform rt = bar.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(width, 0f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        bar.AddComponent<Image>().color = color;
    }

    // =========================================================
    // Sincronização e dados
    // =========================================================

    private void SyncZoomSlider()
    {
        if (zoomSlider == null || cameraController == null) return;
        zoomSlider.minValue = cameraController.MinZoom;
        zoomSlider.maxValue = cameraController.MaxZoom;
        zoomSlider.SetValueWithoutNotify(cameraController.CurrentZoom);
    }

    private void SyncFogUI()
    {
        if (fogController == null || fogStatusLabel == null) return;

        bool active = fogController.IsActive;
        bool isPaint = fogController.CurrentMode == FogOfWarController.FogMode.Paint;

        fogStatusLabel.text = active
            ? (isPaint ? "<color=#4A90D0>✏ Modo pintura ativo — clique no mapa</color>"
                       : "<color=#D06050>◻ Modo apagar ativo — clique no mapa</color>")
            : "Ferramenta inativa";

        // Visual dos botões de modo
        UpdateBtnColor(fogPaintBtn, active && isPaint ? C_BTN_FOG_ON : C_BTN_FOG_P);
        UpdateBtnColor(fogEraseBtn, active && !isPaint ? C_BTN_FOG_ON : C_BTN_FOG_E);
    }

    private void UpdateBtnColor(Button btn, Color color)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img == null) return;
        img.color = color;
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        btn.colors = cb;
    }

    private void HandleMapInfoUpdated(MapInfo info)
    {
        if (mapStatusLabel != null)
            mapStatusLabel.text = info.isLoaded
                ? $"<color=#4A9A5A>●</color>  {info.widthPx}×{info.heightPx} px"
                : "Nenhum mapa carregado";

        if (infoText != null && info.isLoaded)
            RefreshInfoDisplay(info);
    }

    private void UpdateMouseCoord()
    {
        if (coordSystem == null || infoText == null) return;
        if (mapController == null || !mapController.IsMapLoaded) return;

        RefreshInfoDisplay(new MapInfo
        {
            scale = mapController.CurrentScale,
            mouseNormalized = coordSystem.GetMouseNormalized(),
            isLoaded = true
        });
    }

    private void RefreshInfoDisplay(MapInfo info)
    {
        if (infoText == null) return;
        string cursor = info.mouseNormalized.HasValue
            ? $"{info.mouseNormalized.Value.x:F3} , {info.mouseNormalized.Value.y:F3}"
            : "— fora do mapa";

        float zoom = cameraController != null ? cameraController.CurrentZoom : 0f;
        bool fog = fogController != null && fogController.IsActive;

        string fogStatus = (fogController != null && fogController.IsActive)
            ? "<color=#4A90D0>Pintura ativa</color>"
            : "<color=#404858>Inativa</color>";

        infoText.text =
            $"<color=#3A5070>ESCALA</color>   {info.scale:F3}\n" +
            $"<color=#3A5070>ZOOM</color>     {zoom:F2}\n" +
            $"<color=#3A5070>CURSOR</color>   {cursor}\n" +
            $"<color=#3A5070>NÉVOA</color>    {fogStatus}";
    }

    private string GetDefaultInfoText() =>
        "<color=#3A5070>ESCALA</color>   —\n" +
        "<color=#3A5070>ZOOM</color>     —\n" +
        "<color=#3A5070>CURSOR</color>   —\n" +
        "<color=#3A5070>NÉVOA</color>    <color=#404858>Inativa</color>";

    private void RefreshHistoryDisplay()
    {
        if (historyText == null || diceOverlay == null) return;

        List<DiceRollOverlay.DiceResult> h = diceOverlay.History;
        if (h.Count == 0)
        {
            historyText.text = "<i>Nenhum lançamento ainda</i>";
            return;
        }

        // Mostra os últimos 6 resultados em 2 colunas
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int show = Mathf.Min(h.Count, 6);
        for (int i = 0; i < show; i++)
        {
            var r = h[i];
            string col = r.IsCrit ? "#E8C84A"
                       : r.IsFail ? "#C84040"
                                  : "#8899BB";
            sb.Append($"<color={col}>D{r.sides}→{r.value}</color>");
            if (i < show - 1) sb.Append(i % 3 == 2 ? "\n" : "  ");
        }
        historyText.text = sb.ToString();
    }

    // =========================================================
    // Handlers de botões
    // =========================================================

    private void OnImportMap()
    {
        if (MapFileLoader.Instance != null) MapFileLoader.Instance.OpenFilePicker();
        else Debug.LogError("[GMUIController] MapFileLoader.Instance é nulo.");
    }

    private void OnCenterMap() => MapEvents.FireCenterMapRequested();

    private void OnResetZoom()
    {
        MapEvents.FireResetZoomRequested();
        cameraController?.FocusOnActiveBoard();
    }

    private void OnOpenDice()
    {
        if (diceOverlay == null) diceOverlay = FindAnyObjectByType<DiceRollOverlay>();
        diceOverlay?.ShowPanel();
    }

    // Névoa — ativa modo Pintar
    private void OnFogPaint()
    {
        if (fogController == null) fogController = FindAnyObjectByType<FogOfWarController>();
        if (fogController == null) { Debug.LogWarning("[GMUIController] FogOfWarController não encontrado."); return; }

        bool alreadyActive = fogController.IsActive &&
                             fogController.CurrentMode == FogOfWarController.FogMode.Paint;
        fogController.SetMode(FogOfWarController.FogMode.Paint);
        fogController.SetActive(!alreadyActive); // toggle
    }

    // Névoa — ativa modo Apagar
    private void OnFogErase()
    {
        if (fogController == null) fogController = FindAnyObjectByType<FogOfWarController>();
        if (fogController == null) return;

        bool alreadyActive = fogController.IsActive &&
                             fogController.CurrentMode == FogOfWarController.FogMode.Erase;
        fogController.SetMode(FogOfWarController.FogMode.Erase);
        fogController.SetActive(!alreadyActive); // toggle
    }

    private void OnFogFill()
    {
        if (fogController == null) fogController = FindAnyObjectByType<FogOfWarController>();
        fogController?.FillAll();
    }

    private void OnFogClear()
    {
        if (fogController == null) fogController = FindAnyObjectByType<FogOfWarController>();
        fogController?.ClearAll();
    }

    // ─── Utilitário ──────────────────────────────────────────

    private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    private static Color Hex(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color c)) return c;
        return Color.magenta;
    }
}