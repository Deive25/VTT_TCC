using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GMUIController : MonoBehaviour
{
    private MapController mapController;
    private CoordinateSystem coordSystem;
    private CameraController cameraController;
    private FogOfWarController fogController;
    private DiceRollOverlay diceOverlay;
    private DashboardOverlay dashboardOverlay;

    private Slider zoomSlider;
    private TMP_Text infoText;
    private TMP_Text historyText;
    private TMP_Text fogStatusText;
    private TMP_Text brushSizeText;
    private Button fogPaintBtn;
    private Button fogEraseBtn;
    private RectTransform layersContainer;

    // NOVO: Evita conflito constante do valor do slider de zoom
    private float _lastCamZoom = -1f;

    private const float W_RIGHT = 276f;
    private const float W_LEFT = 240f;
    private const float PAD = VTTLayout.PAD;
    private const float GAP = VTTLayout.GAP;
    private const float SGAP = VTTLayout.SGAP;
    private const float BH = VTTLayout.BTN_H;
    private const float HH = VTTLayout.HDR_H;
    private const float PHH = VTTLayout.PHDR_H;

    private void Awake()
    {
        mapController = FindAnyObjectByType<MapController>();
        coordSystem = FindAnyObjectByType<CoordinateSystem>();
        cameraController = FindAnyObjectByType<CameraController>();
        fogController = FindAnyObjectByType<FogOfWarController>();
        diceOverlay = FindAnyObjectByType<DiceRollOverlay>();
        dashboardOverlay = FindAnyObjectByType<DashboardOverlay>();
    }

    private void Start()
    {
        BuildUI();
        if (diceOverlay != null) diceOverlay.OnHistoryChanged += RefreshHistory;
        if (fogController != null) fogController.OnBrushChanged += RefreshBrushLabel;
        RefreshLayersList();
    }

    private void OnEnable()
    {
        MapEvents.OnMapInfoUpdated += OnMapInfo;
        MapEvents.OnLayersChanged += RefreshLayersList;
    }

    private void OnDisable()
    {
        MapEvents.OnMapInfoUpdated -= OnMapInfo;
        MapEvents.OnLayersChanged -= RefreshLayersList;
    }

    private void Update()
    {
        SyncZoom();
        SyncMouseCoord();
        SyncFogState();
    }

    private void BuildUI()
    {
        Canvas cv = FindAnyObjectByType<Canvas>();
        if (cv == null) return;
        BuildRightPanel(cv.transform);
        BuildLeftPanel(cv.transform);
    }

    private void BuildRightPanel(Transform cvTransform)
    {
        RectTransform p = VTTLayout.Panel("GM_Right", cvTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), W_RIGHT);

        float y = 0f;
        y = DrawPanelHeader(p, y, "TELA DO MESTRE");
        y = DrawSecHeader(p, y, "CAMERA");
        y = DrawCameraSection(p, y);
        y = DrawSecHeader(p, y, "DADOS");
        y = DrawDiceSection(p, y);
        y = DrawSecHeader(p, y, "NEVOA DE GUERRA");
        y = DrawFogSection(p, y);
        y = DrawSecHeader(p, y, "INFORMACOES");
        y = DrawInfoSection(p, y);

        p.sizeDelta = new Vector2(W_RIGHT, Mathf.Abs(y) + PAD);
    }

    private void BuildLeftPanel(Transform cvTransform)
    {
        RectTransform p = VTTLayout.Panel("GM_Left", cvTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), W_LEFT, VTTLayout.C_LEFT_BG);

        float y = 0f;
        y = DrawPanelHeader(p, y, "RECURSOS");

        y = DrawSecHeader(p, y, "GERENCIAMENTO");
        VTTLayout.BtnFull(p, y, BH, -PAD * 2f, "ABRIR DASHBOARD", VTTLayout.C_BTN_PRI, VTTLayout.C_BDR_ACC, VTTLayout.C_TEXT, VTTLayout.F_BTN).onClick.AddListener(DoOpenDashboard);
        y -= BH + SGAP;

        y = DrawSecHeader(p, y, "TABULEIROS");
        VTTLayout.BtnFull(p, y, BH, -PAD * 2f, "+ NOVO TABULEIRO", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, VTTLayout.F_BTN, false).onClick.AddListener(DoAddLayer);
        y -= BH + GAP;

        float scrollHeight = 250f;
        VTTLayout.MakeScrollView("BoardsScroll", p, 0, y, W_LEFT, scrollHeight, out layersContainer);
        y -= scrollHeight + SGAP;

        p.sizeDelta = new Vector2(W_LEFT, Mathf.Abs(y) + PAD * 2f);
    }

    private void DoOpenDashboard()
    {
        if (dashboardOverlay == null) dashboardOverlay = FindAnyObjectByType<DashboardOverlay>();
        if (dashboardOverlay != null) dashboardOverlay.OpenPanel();
    }

    private void DoAddLayer()
    {
        if (MapFileLoader.Instance != null)
        {
            MapFileLoader.Instance.OpenFilePicker((tex) => LayerManager.Instance.AddLayer(tex));
        }
    }

    private void RefreshLayersList()
    {
        if (layersContainer == null || LayerManager.Instance == null) return;
        foreach (Transform child in layersContainer) Destroy(child.gameObject);

        float ly = 0f;
        var layers = LayerManager.Instance.Layers;

        if (layers.Count == 0)
        {
            TMP_Text emptyTxt = VTTLayout.LabelFixed(layersContainer, PAD, ly, W_LEFT - (PAD * 2), 40f, VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM);
            emptyTxt.text = "Nenhum tabuleiro criado.";
            emptyTxt.alignment = TextAlignmentOptions.Center;
            layersContainer.sizeDelta = new Vector2(0f, 40f);
            return;
        }

        foreach (var layer in layers)
        {
            bool isActive = (layer.id == LayerManager.Instance.ActiveLayerId);
            Color bgColor = isActive ? VTTLayout.C_SEC_BG : VTTLayout.C_CONTENT_BG;
            float itemH = 38f;

            RectTransform itemRT = VTTLayout.Box("LayerItem_" + layer.id, layersContainer, PAD, ly, -PAD * 2f, itemH, bgColor);
            if (isActive) VTTLayout.AccentBar(itemRT, 4f, VTTLayout.C_ACCENT_LT);

            Button selectBtn = VTTLayout.BtnFixed(itemRT, 0f, 0f, W_LEFT - 110f, itemH, "", Color.clear, Color.clear, Color.white, 10f);
            selectBtn.onClick.AddListener(() => { if (!isActive) LayerManager.Instance.SetActiveLayer(layer.id); });

            float textX = isActive ? 14f : 8f;
            float textW = W_LEFT - PAD * 2f - 90f;

            TMP_InputField nameInput = VTTLayout.InputFieldFixed(itemRT, textX, 0f, textW, itemH, VTTLayout.F_PANEL, isActive ? VTTLayout.C_TEXT_PANEL : VTTLayout.C_TEXT_DIM, isActive ? FontStyles.Bold : FontStyles.Normal, layer.name);
            string currentId = layer.id;

            nameInput.onEndEdit.AddListener((newName) => { LayerManager.Instance.RenameLayer(currentId, newName); });
            nameInput.onSelect.AddListener((text) => { if (!isActive) LayerManager.Instance.SetActiveLayer(currentId); });

            float btnSz = 24f;
            float btnY = -(itemH - btnSz) * 0.5f;

            VTTLayout.BtnFixed(itemRT, W_LEFT - PAD * 2f - btnSz - 4f, btnY, btnSz, btnSz, "X", VTTLayout.C_BTN_CLOSE, VTTLayout.C_BDR_CLOSE, VTTLayout.C_TEXT, 11f, true).onClick.AddListener(() => LayerManager.Instance.RemoveLayer(layer.id));
            VTTLayout.BtnFixed(itemRT, W_LEFT - PAD * 2f - (btnSz * 2) - 8f, btnY, btnSz, btnSz, "v", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT_DIM, 12f).onClick.AddListener(() => LayerManager.Instance.MoveLayerDown(layer.id));
            VTTLayout.BtnFixed(itemRT, W_LEFT - PAD * 2f - (btnSz * 3) - 10f, btnY, btnSz, btnSz, "^", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT_DIM, 12f).onClick.AddListener(() => LayerManager.Instance.MoveLayerUp(layer.id));

            ly -= itemH + 6f;
        }
        layersContainer.sizeDelta = new Vector2(0f, Mathf.Abs(ly));
    }

    private float DrawCameraSection(RectTransform p, float y)
    {
        TMP_Text zoomLbl = VTTLayout.Label(p, y, 16f, VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM, FontStyles.Bold);
        zoomLbl.text = "ZOOM";
        y -= 16f + 4f;
        float mn = (cameraController != null) ? cameraController.MinZoom : 0.5f;
        float mx = (cameraController != null) ? cameraController.MaxZoom : 30f;
        float cur = (cameraController != null) ? cameraController.CurrentZoom : 5f;

        zoomSlider = VTTLayout.MakeSlider(p, y, 28f, mn, mx, cur);
        zoomSlider.onValueChanged.AddListener(OnZoomChanged);
        y -= 28f + GAP;

        float hw = (W_RIGHT - PAD * 2f - GAP) * 0.5f;
        VTTLayout.BtnFixed(p, PAD, y, hw, BH - 4f, "CENTRALIZAR", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, VTTLayout.F_BTN).onClick.AddListener(DoCenterMap);
        VTTLayout.BtnFixed(p, PAD + hw + GAP, y, hw, BH - 4f, "RESET ZOOM", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, VTTLayout.F_BTN).onClick.AddListener(DoResetZoom);
        return y - (BH - 4f) - SGAP;
    }

    // A mágica que resolve a barra de zoom a tremer está aqui:
    private void SyncZoom()
    {
        if (zoomSlider == null || cameraController == null) return;
        float camZoom = cameraController.CurrentZoom;

        // Só diz ao slider para se mexer caso o sistema (scroll do rato) tenha alterado a câmara
        if (Mathf.Abs(camZoom - _lastCamZoom) > 0.001f)
        {
            zoomSlider.SetValueWithoutNotify(camZoom);
            _lastCamZoom = camZoom;
        }
    }

    private void OnZoomChanged(float v)
    {
        if (cameraController != null)
        {
            cameraController.SetZoom(v);
            _lastCamZoom = v; // Regista que foi o utilizador da UI a mudar o zoom
        }
    }

    private float DrawDiceSection(RectTransform p, float y)
    {
        VTTLayout.BtnFull(p, y, BH, -PAD * 2f, "ABRIR DADOS", VTTLayout.C_BTN_DICE, VTTLayout.C_BDR_DICE, VTTLayout.C_TEXT, VTTLayout.F_BTN).onClick.AddListener(DoOpenDice);
        y -= BH + GAP;
        TMP_Text histLbl = VTTLayout.Label(p, y, 14f, VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM, FontStyles.Bold);
        histLbl.text = "ULTIMAS ROLAGENS";
        y -= 14f + 3f;
        float boxH = 66f;
        RectTransform boxRT = VTTLayout.Box("HistBox", p, 0f, y, 0f, boxH, VTTLayout.C_CONTENT_BG);
        VTTLayout.AccentBar(boxRT, 2f, VTTLayout.C_ACCENT);
        historyText = VTTLayout.LabelStretch("HistText", boxRT, new Vector2(PAD, 5f), new Vector2(-4f, -5f), VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM, align: TextAlignmentOptions.TopLeft);
        historyText.lineSpacing = 4f;
        historyText.text = "Nenhuma rolagem ainda";
        return y - boxH - SGAP;
    }

    private float DrawFogSection(RectTransform p, float y)
    {
        float hw = (W_RIGHT - PAD * 2f - GAP) * 0.5f;
        fogPaintBtn = VTTLayout.BtnFixed(p, PAD, y, hw, BH, "PINTAR", VTTLayout.C_BTN_PAINT, VTTLayout.C_BDR_PAINT, VTTLayout.C_TEXT, VTTLayout.F_BTN);
        fogPaintBtn.onClick.AddListener(DoFogPaint);
        fogEraseBtn = VTTLayout.BtnFixed(p, PAD + hw + GAP, y, hw, BH, "APAGAR", VTTLayout.C_BTN_ERASE, VTTLayout.C_BDR_ERASE, VTTLayout.C_TEXT, VTTLayout.F_BTN);
        fogEraseBtn.onClick.AddListener(DoFogErase);
        y -= BH + GAP;
        TMP_Text brushLbl = VTTLayout.Label(p, y, 14f, VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM);
        brushLbl.text = "Tamanho do pincel";
        y -= 14f + 3f;
        float smW = 30f; float cntW = W_RIGHT - PAD * 2f - smW * 2f - GAP * 2f; float bh2 = BH - 6f;
        VTTLayout.BtnFixed(p, PAD, y, smW, bh2, "-", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 14f).onClick.AddListener(DoDecreaseBrush);
        brushSizeText = VTTLayout.LabelFixed(p, PAD + smW + GAP, y, cntW, bh2, VTTLayout.F_LABEL, VTTLayout.C_TEXT, FontStyles.Bold);
        brushSizeText.text = "20";
        VTTLayout.BtnFixed(p, PAD + smW + GAP + cntW + GAP, y, smW, bh2, "+", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 14f).onClick.AddListener(DoIncreaseBrush);
        y -= bh2 + GAP;
        VTTLayout.BtnFull(p, y, BH - 4f, -PAD * 2f, "LIMPAR NEVOA", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT_WARN, VTTLayout.F_BTN, bold: false).onClick.AddListener(DoFogClear);
        y -= (BH - 4f) + GAP;
        fogStatusText = VTTLayout.Label(p, y, 14f, VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM);
        fogStatusText.text = "Ferramenta inativa";
        return y - 14f - SGAP;
    }

    private float DrawInfoSection(RectTransform p, float y)
    {
        float boxH = 100f;
        RectTransform boxRT = VTTLayout.Box("InfoBox", p, 0f, y, 0f, boxH, VTTLayout.C_CONTENT_BG);
        VTTLayout.AccentBar(boxRT, 2f, VTTLayout.C_ACCENT);
        infoText = VTTLayout.LabelStretch("InfoText", boxRT, new Vector2(PAD + 2f, 7f), new Vector2(-5f, -7f), VTTLayout.F_LABEL, VTTLayout.C_TEXT, align: TextAlignmentOptions.TopLeft);
        infoText.lineSpacing = 9f;
        infoText.text = DefaultInfo();
        return y - boxH - PAD;
    }

    private float DrawPlaceholder(RectTransform p, float y, string msg)
    {
        float boxH = 52f;
        RectTransform boxRT = VTTLayout.Box("PH", p, 0f, y, 0f, boxH, VTTLayout.RGB(0.07f, 0.08f, 0.10f, 0.5f));
        VTTLayout.LabelStretch("PHText", boxRT, new Vector2(PAD, 5f), new Vector2(-PAD, -5f), VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM, align: TextAlignmentOptions.TopLeft).text = msg;
        return y - boxH - SGAP;
    }

    private float DrawPanelHeader(RectTransform p, float y, string title)
    {
        RectTransform hdrRT = VTTLayout.Box("PHdr", p, 0f, y, 0f, PHH, VTTLayout.C_HDR_BG, new Vector2(0f, 1f), new Vector2(1f, 1f));
        VTTLayout.AccentBar(hdrRT, 3f, VTTLayout.C_ACCENT_LT);
        VTTLayout.LabelStretch("PTitle", hdrRT, new Vector2(PAD + 5f, 3f), new Vector2(-PAD, -3f), VTTLayout.F_PANEL, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = title;
        return y - PHH - 2f;
    }

    private float DrawSecHeader(RectTransform p, float y, string label)
    {
        y -= SGAP * 0.4f;
        RectTransform hdrRT = VTTLayout.Box("SHdr_" + label, p, 0f, y, 0f, HH, VTTLayout.C_SEC_BG, new Vector2(0f, 1f), new Vector2(1f, 1f));
        VTTLayout.AccentBar(hdrRT, 3f, VTTLayout.C_ACCENT);
        VTTLayout.LabelStretch("SLabel", hdrRT, new Vector2(PAD + 5f, 0f), new Vector2(-PAD, 0f), VTTLayout.F_SEC, VTTLayout.C_TEXT_HDR, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = label;
        VTTLayout.Box("Div", p, 0f, y - HH, 0f, 1f, VTTLayout.C_ACCENT, new Vector2(0f, 1f), new Vector2(1f, 1f));
        return y - HH - 1f - GAP;
    }

    private void SyncMouseCoord() { if (coordSystem != null && infoText != null && mapController != null && mapController.IsMapLoaded) RefreshInfo(new MapInfo { scale = mapController.CurrentScale, mouseNormalized = coordSystem.GetMouseNormalized(), isLoaded = true }); }
    private void SyncFogState() { if (fogController == null || fogStatusText == null) return; bool active = fogController.IsActive; bool isPaint = fogController.CurrentMode == FogOfWarController.FogMode.Paint; if (!active) { fogStatusText.text = "Ferramenta inativa"; fogStatusText.color = VTTLayout.C_TEXT_DIM; } else if (isPaint) { fogStatusText.text = "Modo pintura ativo"; fogStatusText.color = VTTLayout.RGB(0.30f, 0.60f, 0.90f); } else { fogStatusText.text = "Modo apagar ativo"; fogStatusText.color = VTTLayout.RGB(0.85f, 0.40f, 0.35f); } VTTLayout.SetBtnColor(fogPaintBtn, (active && isPaint) ? VTTLayout.C_BTN_ACTIVE : VTTLayout.C_BTN_PAINT); VTTLayout.SetBtnColor(fogEraseBtn, (active && !isPaint) ? VTTLayout.C_BTN_ACTIVE : VTTLayout.C_BTN_ERASE); }
    private void OnMapInfo(MapInfo info) { if (infoText != null && info.isLoaded) RefreshInfo(info); }
    private void RefreshInfo(MapInfo info) { if (infoText == null) return; string cursor = info.mouseNormalized.HasValue ? info.mouseNormalized.Value.x.ToString("F3") + "  " + info.mouseNormalized.Value.y.ToString("F3") : "fora do mapa"; float zoom = (cameraController != null) ? cameraController.CurrentZoom : 0f; string fogSt = (fogController != null && fogController.IsActive) ? "Ativa" : "Inativa"; infoText.text = "Acesso    OK" + "\nZoom      " + zoom.ToString("F2") + "\nCursor    " + cursor + "\nNevoa     " + fogSt; }
    private string DefaultInfo() { return "Acesso    --\nZoom      --\nCursor    --\nNevoa     Inativa"; }
    private void RefreshHistory() { if (historyText == null || diceOverlay == null) return; var h = diceOverlay.History; if (h.Count == 0) { historyText.text = "Nenhuma rolagem ainda"; return; } System.Text.StringBuilder sb = new System.Text.StringBuilder(); int n = Mathf.Min(h.Count, 4); for (int i = 0; i < n; i++) { sb.Append(h[i].descriptor + "  =>  " + h[i].total); if (i < n - 1) sb.Append("\n"); } historyText.text = sb.ToString(); }
    private void RefreshBrushLabel() { if (brushSizeText != null && fogController != null) brushSizeText.text = fogController.BrushRadius.ToString(); }
    private void DoCenterMap() { MapEvents.FireCenterMapRequested(); }
    private void DoResetZoom() { MapEvents.FireResetZoomRequested(); if (cameraController != null) cameraController.FocusOnActiveBoard(); }
    private void DoOpenDice() { if (diceOverlay == null) diceOverlay = FindAnyObjectByType<DiceRollOverlay>(); if (diceOverlay != null) diceOverlay.OpenPanel(); }
    private void DoFogPaint() { if (fogController == null) fogController = FindAnyObjectByType<FogOfWarController>(); if (fogController == null) return; bool sameMode = fogController.IsActive && fogController.CurrentMode == FogOfWarController.FogMode.Paint; fogController.SetMode(FogOfWarController.FogMode.Paint); fogController.SetActive(!sameMode); }
    private void DoFogErase() { if (fogController == null) fogController = FindAnyObjectByType<FogOfWarController>(); if (fogController == null) return; bool sameMode = fogController.IsActive && fogController.CurrentMode == FogOfWarController.FogMode.Erase; fogController.SetMode(FogOfWarController.FogMode.Erase); fogController.SetActive(!sameMode); }
    private void DoFogClear() { if (fogController == null) fogController = FindAnyObjectByType<FogOfWarController>(); if (fogController != null) fogController.ClearAll(); }
    private void DoIncreaseBrush() { if (fogController == null) fogController = FindAnyObjectByType<FogOfWarController>(); if (fogController != null) fogController.IncreaseBrush(5); RefreshBrushLabel(); }
    private void DoDecreaseBrush() { if (fogController == null) fogController = FindAnyObjectByType<FogOfWarController>(); if (fogController != null) fogController.DecreaseBrush(5); RefreshBrushLabel(); }
}