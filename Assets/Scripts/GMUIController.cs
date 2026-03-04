// GMUIController.cs  v7
// ASCII puro. Layout via VerticalLayoutGroup / HorizontalLayoutGroup.
// Paleta via VTTStyles. Widgets via VTTUIBuilder.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GMUIController : MonoBehaviour
{
    // --- Dependencias ---
    private MapController      mapController;
    private CoordinateSystem   coordSystem;
    private CameraController   cameraController;
    private FogOfWarController fogController;
    private DiceRollOverlay    diceOverlay;

    // --- Refs reativos ---
    private Slider   zoomSlider;
    private TMP_Text infoText;
    private TMP_Text mapStatusText;
    private TMP_Text historyText;
    private TMP_Text fogStatusText;
    private TMP_Text brushSizeText;
    private Image    fogPaintImg;
    private Image    fogEraseImg;
    private Button   fogPaintBtn;
    private Button   fogEraseBtn;

    // --- Lifecycle ---

    private void Awake()
    {
        mapController    = FindObjectOfType<MapController>();
        coordSystem      = FindObjectOfType<CoordinateSystem>();
        cameraController = FindObjectOfType<CameraController>();
        fogController    = FindObjectOfType<FogOfWarController>();
        diceOverlay      = FindObjectOfType<DiceRollOverlay>();
    }

    private void Start()
    {
        BuildUI();
        if (diceOverlay   != null) diceOverlay.OnHistoryChanged   += RefreshHistory;
        if (fogController != null) fogController.OnBrushChanged   += RefreshBrushLabel;
    }

    private void OnEnable()  => MapEvents.OnMapInfoUpdated += OnMapInfo;
    private void OnDisable() => MapEvents.OnMapInfoUpdated -= OnMapInfo;

    private void Update()
    {
        SyncZoom();
        SyncMouseCoord();
        SyncFogButtons();
    }

    // =========================================================
    // Construcao
    // =========================================================

    private void BuildUI()
    {
        Canvas cv = FindObjectOfType<Canvas>();
        if (cv == null) { Debug.LogError("[GMUIController] Canvas nao encontrado."); return; }
        BuildRightPanel(cv.transform);
        BuildLeftPanel(cv.transform);
    }

    private void BuildRightPanel(Transform cv)
    {
        RectTransform panel = VTTUIBuilder.Panel("GM_Right", cv,
            anchorRight: true, width: VTTStyles.W_RIGHT, bg: VTTStyles.BG_PANEL);

        VTTUIBuilder.PanelHeader(panel, "TELA DO MESTRE");

        VTTUIBuilder.SectionHeader(panel, "MAPA");
        BuildMapSection(panel);

        VTTUIBuilder.SectionHeader(panel, "CAMERA");
        BuildCameraSection(panel);

        VTTUIBuilder.SectionHeader(panel, "DADOS");
        BuildDiceSection(panel);

        VTTUIBuilder.SectionHeader(panel, "NEVOA DE GUERRA");
        BuildFogSection(panel);

        VTTUIBuilder.SectionHeader(panel, "INFORMACOES");
        BuildInfoSection(panel);
    }

    private void BuildLeftPanel(Transform cv)
    {
        RectTransform panel = VTTUIBuilder.Panel("GM_Left", cv,
            anchorRight: false, width: VTTStyles.W_LEFT, bg: VTTStyles.BG_DARK);

        VTTUIBuilder.PanelHeader(panel, "RECURSOS");

        VTTUIBuilder.SectionHeader(panel, "CAMADAS");
        var s1 = VTTUIBuilder.Section("Sec_Cam", panel, VTTStyles.BG_DARK);
        VTTUIBuilder.Placeholder(s1, "Camadas de terreno\n(em breve)");

        VTTUIBuilder.SectionHeader(panel, "TOKENS");
        var s2 = VTTUIBuilder.Section("Sec_Tok", panel, VTTStyles.BG_DARK);
        VTTUIBuilder.Placeholder(s2, "Tokens de personagens\n(em breve)");

        VTTUIBuilder.SectionHeader(panel, "RASTREAMENTO");
        var s3 = VTTUIBuilder.Section("Sec_Ras", panel, VTTStyles.BG_DARK);
        VTTUIBuilder.Placeholder(s3, "Rastreamento\n(em breve)");
    }

    // =========================================================
    // Secoes de conteudo
    // =========================================================

    private void BuildMapSection(RectTransform panel)
    {
        var sec = VTTUIBuilder.Section("Sec_Mapa", panel, VTTStyles.BG_PANEL);

        mapStatusText = VTTUIBuilder.StatusLabel(sec, "Nenhum mapa carregado");

        var btn = VTTUIBuilder.Btn(sec, "IMPORTAR MAPA",
            VTTStyles.BTN_PRIMARY, VTTStyles.BDR_ACCENT, VTTStyles.TXT_PRIMARY);
        btn.onClick.AddListener(DoImportMap);
    }

    private void BuildCameraSection(RectTransform panel)
    {
        var sec = VTTUIBuilder.Section("Sec_Cam", panel, VTTStyles.BG_PANEL);

        VTTUIBuilder.SectionLabel(sec, "ZOOM");

        float mn  = cameraController?.MinZoom    ?? 0.5f;
        float mx  = cameraController?.MaxZoom    ?? 30f;
        float cur = cameraController?.CurrentZoom ?? 5f;
        zoomSlider = VTTUIBuilder.Slider(sec, mn, mx, cur);
        zoomSlider.onValueChanged.AddListener(v => cameraController?.SetZoom(v));

        var (centerBtn, resetBtn) = VTTUIBuilder.BtnRow(sec,
            "CENTRALIZAR", VTTStyles.BTN_SECOND, VTTStyles.BDR_DEFAULT,
            "RESET ZOOM",  VTTStyles.BTN_SECOND, VTTStyles.BDR_DEFAULT,
            VTTStyles.TXT_PRIMARY, VTTStyles.H_BUTTON_SM);
        centerBtn.onClick.AddListener(DoCenterMap);
        resetBtn.onClick.AddListener(DoResetZoom);
    }

    private void BuildDiceSection(RectTransform panel)
    {
        var sec = VTTUIBuilder.Section("Sec_Dados", panel, VTTStyles.BG_PANEL);

        var openBtn = VTTUIBuilder.Btn(sec, "ABRIR PAINEL DE DADOS",
            VTTStyles.BTN_DICE, VTTStyles.BDR_DICE, VTTStyles.TXT_PRIMARY);
        openBtn.onClick.AddListener(DoOpenDice);

        VTTUIBuilder.SectionLabel(sec, "ULTIMAS ROLAGENS");

        historyText = VTTUIBuilder.InfoBox(sec, VTTStyles.H_HIST_BOX);
        historyText.text        = "Nenhuma rolagem ainda";
        historyText.color       = VTTStyles.TXT_SECOND;
        historyText.lineSpacing = 6f;
    }

    private void BuildFogSection(RectTransform panel)
    {
        var sec = VTTUIBuilder.Section("Sec_Fog", panel, VTTStyles.BG_PANEL);

        var (paintBtn, eraseBtn) = VTTUIBuilder.BtnRow(sec,
            "PINTAR",  VTTStyles.BTN_PAINT, VTTStyles.BDR_PAINT,
            "APAGAR",  VTTStyles.BTN_ERASE, VTTStyles.BDR_ERASE,
            VTTStyles.TXT_PRIMARY);
        fogPaintBtn = paintBtn;
        fogEraseBtn = eraseBtn;
        fogPaintImg = fogPaintBtn.GetComponent<Image>();
        fogEraseImg = fogEraseBtn.GetComponent<Image>();
        fogPaintBtn.onClick.AddListener(DoFogPaint);
        fogEraseBtn.onClick.AddListener(DoFogErase);

        VTTUIBuilder.SectionLabel(sec, "TAMANHO DO PINCEL");

        var (minus, valLabel, plus) = VTTUIBuilder.Counter(sec, "20");
        brushSizeText = valLabel;
        minus.onClick.AddListener(DoDecreaseBrush);
        plus.onClick.AddListener(DoIncreaseBrush);

        var clearBtn = VTTUIBuilder.Btn(sec, "LIMPAR NEVOA",
            VTTStyles.BTN_NEUTRAL, VTTStyles.BDR_DEFAULT, VTTStyles.TXT_SECOND);
        clearBtn.onClick.AddListener(DoFogClear);

        fogStatusText = VTTUIBuilder.StatusLabel(sec, "Ferramenta inativa");
    }

    private void BuildInfoSection(RectTransform panel)
    {
        var sec = VTTUIBuilder.Section("Sec_Info", panel, VTTStyles.BG_PANEL);
        infoText = VTTUIBuilder.InfoBox(sec, VTTStyles.H_INFO_BOX);
        infoText.text = DefaultInfo();
    }

    // =========================================================
    // Sincronizacao
    // =========================================================

    private void SyncZoom()
    {
        if (zoomSlider == null || cameraController == null) return;
        zoomSlider.minValue = cameraController.MinZoom;
        zoomSlider.maxValue = cameraController.MaxZoom;
        zoomSlider.SetValueWithoutNotify(cameraController.CurrentZoom);
    }

    private void SyncMouseCoord()
    {
        if (coordSystem == null || infoText == null) return;
        if (mapController == null || !mapController.IsMapLoaded) return;
        RefreshInfo(new MapInfo
        {
            scale           = mapController.CurrentScale,
            mouseNormalized = coordSystem.GetMouseNormalized(),
            isLoaded        = true
        });
    }

    private void SyncFogButtons()
    {
        if (fogController == null) return;
        bool active  = fogController.IsActive;
        bool isPaint = fogController.CurrentMode == FogOfWarController.FogMode.Paint;

        if (fogStatusText != null)
        {
            if (!active)
                fogStatusText.text = "Ferramenta inativa";
            else if (isPaint)
                fogStatusText.text = "Modo PINTAR ativo";
            else
                fogStatusText.text = "Modo APAGAR ativo";
        }

        SetBtnColor(fogPaintImg, fogPaintBtn,
            active && isPaint  ? VTTStyles.BTN_ACTIVE : VTTStyles.BTN_PAINT,
            active && isPaint  ? VTTStyles.BDR_ACCENT : VTTStyles.BDR_PAINT);

        SetBtnColor(fogEraseImg, fogEraseBtn,
            active && !isPaint ? VTTStyles.BTN_ACTIVE : VTTStyles.BTN_ERASE,
            active && !isPaint ? VTTStyles.BDR_ACCENT : VTTStyles.BDR_ERASE);
    }

    private void OnMapInfo(MapInfo info)
    {
        if (mapStatusText != null)
            mapStatusText.text = info.isLoaded
                ? "Mapa: " + info.widthPx + " x " + info.heightPx + " px"
                : "Nenhum mapa carregado";
        if (infoText != null && info.isLoaded) RefreshInfo(info);
    }

    private void RefreshInfo(MapInfo info)
    {
        if (infoText == null) return;
        string cursor = info.mouseNormalized.HasValue
            ? info.mouseNormalized.Value.x.ToString("F3") + ", " +
              info.mouseNormalized.Value.y.ToString("F3")
            : "fora do mapa";
        float  zoom  = cameraController?.CurrentZoom ?? 0f;
        string fogSt = (fogController != null && fogController.IsActive)
            ? "Ativa" : "Inativa";

        infoText.text =
            "ESCALA  " + info.scale.ToString("F3") + "\n" +
            "ZOOM    " + zoom.ToString("F2") + "\n" +
            "CURSOR  " + cursor + "\n" +
            "NEVOA   " + fogSt;
    }

    private string DefaultInfo() =>
        "ESCALA  --\n" +
        "ZOOM    --\n" +
        "CURSOR  --\n" +
        "NEVOA   Inativa";

    private void RefreshHistory()
    {
        if (historyText == null || diceOverlay == null) return;
        var h = diceOverlay.History;
        if (h.Count == 0)
        {
            historyText.text  = "Nenhuma rolagem ainda";
            historyText.color = VTTStyles.TXT_SECOND;
            return;
        }
        var sb = new System.Text.StringBuilder();
        int n  = Mathf.Min(h.Count, 4);
        for (int i = 0; i < n; i++)
        {
            sb.Append(h[i].descriptor + " -> " + h[i].total);
            if (i < n - 1) sb.Append("\n");
        }
        historyText.text  = sb.ToString();
        historyText.color = VTTStyles.TXT_PRIMARY;
    }

    private void RefreshBrushLabel()
    {
        if (brushSizeText != null && fogController != null)
            brushSizeText.text = fogController.BrushRadius.ToString();
    }

    // =========================================================
    // Handlers
    // =========================================================

    private void DoImportMap()
    {
        if (MapFileLoader.Instance != null) MapFileLoader.Instance.OpenFilePicker();
        else Debug.LogError("[GMUIController] MapFileLoader nao encontrado.");
    }

    private void DoCenterMap() => MapEvents.FireCenterMapRequested();

    private void DoResetZoom()
    {
        MapEvents.FireResetZoomRequested();
        cameraController?.FocusOnActiveBoard();
    }

    private void DoOpenDice()
    {
        if (diceOverlay == null) diceOverlay = FindObjectOfType<DiceRollOverlay>();
        diceOverlay?.OpenPanel();
    }

    private void DoFogPaint()
    {
        ResolveFog();
        if (fogController == null) return;
        bool same = fogController.IsActive &&
                    fogController.CurrentMode == FogOfWarController.FogMode.Paint;
        fogController.SetMode(FogOfWarController.FogMode.Paint);
        fogController.SetActive(!same);
    }

    private void DoFogErase()
    {
        ResolveFog();
        if (fogController == null) return;
        bool same = fogController.IsActive &&
                    fogController.CurrentMode == FogOfWarController.FogMode.Erase;
        fogController.SetMode(FogOfWarController.FogMode.Erase);
        fogController.SetActive(!same);
    }

    private void DoFogClear()
    {
        ResolveFog();
        fogController?.ClearAll();
    }

    private void DoIncreaseBrush()
    {
        ResolveFog();
        fogController?.IncreaseBrush(5);
        RefreshBrushLabel();
    }

    private void DoDecreaseBrush()
    {
        ResolveFog();
        fogController?.DecreaseBrush(5);
        RefreshBrushLabel();
    }

    private void ResolveFog()
    {
        if (fogController == null)
            fogController = FindObjectOfType<FogOfWarController>();
    }

    // =========================================================
    // Utilitarios
    // =========================================================

    private void SetBtnColor(Image img, Button btn, Color bg, Color border)
    {
        if (img == null || btn == null) return;
        img.color = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor      = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.22f);
        cb.pressedColor     = Color.Lerp(bg, Color.black, 0.28f);
        btn.colors = cb;

        Transform wrap = btn.transform.parent;
        if (wrap != null)
        {
            Image wImg = wrap.GetComponent<Image>();
            if (wImg != null) wImg.color = border;
        }
    }
}
