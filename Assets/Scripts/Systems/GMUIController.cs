using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

public class GMUIController : MonoBehaviour
{
    private MapController mapController;
    private CoordinateSystem coordSystem;
    private CameraController cameraController;
    private FogOfWarController fogController;
    private DiceRollOverlay diceOverlay;

    private Slider zoomSlider;
    private TMP_Text infoText;
    private TMP_Text historyText;
    private TMP_Text fogStatusText;
    private TMP_Text brushSizeText;
    private Button fogPaintBtn;
    private Button fogEraseBtn;

    private RectTransform layersContainer;
    private RectTransform tokensContainer;

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
    }

    private void Start()
    {
        BuildUI();
        RefreshLayersList();
        RefreshTokensList();

        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnCharactersUpdated -= RefreshTokensList;
            CharacterManager.Instance.OnCharactersUpdated += RefreshTokensList;
        }
    }

    private void OnEnable()
    {
        MapEvents.OnMapInfoUpdated += OnMapInfo;
        MapEvents.OnLayersChanged += RefreshLayersList;
        if (diceOverlay != null) diceOverlay.OnHistoryChanged += RefreshHistory;
        if (fogController != null) fogController.OnBrushChanged += RefreshBrushLabel;

        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.OnCharactersUpdated -= RefreshTokensList;
            CharacterManager.Instance.OnCharactersUpdated += RefreshTokensList;
        }
    }

    private void OnDisable()
    {
        MapEvents.OnMapInfoUpdated -= OnMapInfo;
        MapEvents.OnLayersChanged -= RefreshLayersList;
        if (diceOverlay != null) diceOverlay.OnHistoryChanged -= RefreshHistory;
        if (fogController != null) fogController.OnBrushChanged -= RefreshBrushLabel;
        if (CharacterManager.Instance != null) CharacterManager.Instance.OnCharactersUpdated -= RefreshTokensList;
    }

    private void Update()
    {
        SyncZoom(); SyncMouseCoord(); SyncFogState();

        // Código atualizado para o New Input System
        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame)
        {
            ToggleWindowMode();
        }
    }

    public void ToggleWindowMode()
    {
        if (Screen.fullScreenMode == FullScreenMode.Windowed)
        {
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        }
    }

    private void BuildUI()
    {
        Canvas cv = GetComponent<Canvas>();
        if (cv == null)
        {
            Canvas[] allCanvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvas)
            {
                if (c.gameObject.name != "PlayerCanvas")
                {
                    cv = c;
                    break;
                }
            }
        }

        if (cv == null)
        {
            Debug.LogError("[GMUIController] Canvas principal não encontrado!");
            return;
        }

        BuildRightPanel(cv.transform);
        BuildLeftPanel(cv.transform);
    }

    private void BuildRightPanel(Transform cvTransform)
    {
        RectTransform baseRT = VTTLayout.Panel("GM_Right_Base", cvTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f), W_RIGHT);
        baseRT.sizeDelta = new Vector2(W_RIGHT, 0);

        ScrollRect scroll = VTTLayout.MakeScrollView("RightScroll", baseRT, 0, 0, W_RIGHT, 0, out RectTransform p);
        RectTransform scrollRT = scroll.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = Vector2.one;
        scrollRT.sizeDelta = Vector2.zero;
        scrollRT.anchoredPosition = Vector2.zero;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        float y = 0f;
        y = DrawPanelHeader(p, y, "TELA DO MESTRE");
        y = DrawSecHeader(p, y, "TELA DOS JOGADORES"); y = DrawPlayerScreenSection(p, y);
        y = DrawSecHeader(p, y, "CAMERA DO MESTRE"); y = DrawCameraSection(p, y);
        y = DrawSecHeader(p, y, "DADOS"); y = DrawDiceSection(p, y);
        y = DrawSecHeader(p, y, "NEVOA DE GUERRA"); y = DrawFogSection(p, y);
        y = DrawSecHeader(p, y, "INFORMACOES"); y = DrawInfoSection(p, y);

        p.sizeDelta = new Vector2(0f, Mathf.Abs(y) + PAD);
    }

    private void BuildLeftPanel(Transform cvTransform)
    {
        RectTransform p = VTTLayout.Panel("GM_Left", cvTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), W_LEFT, VTTLayout.C_LEFT_BG);
        p.sizeDelta = new Vector2(W_LEFT, 0);

        float y = 0f;
        y = DrawPanelHeader(p, y, "RECURSOS");

        y = DrawSecHeader(p, y, "GERENCIAMENTO");

        VTTLayout.BtnFull(p, y, BH, -PAD * 2f, "ABRIR DASHBOARD", VTTLayout.C_BTN_PRI, VTTLayout.C_BDR_ACC, VTTLayout.C_TEXT, VTTLayout.F_BTN).onClick.AddListener(() => {
            DashboardOverlay d = FindAnyObjectByType<DashboardOverlay>(); if (d != null) d.OpenPanel();
        });
        y -= BH + GAP; 

        VTTLayout.BtnFull(p, y, BH, -PAD * 2f, "TELA CHEIA / JANELA", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, VTTLayout.F_BTN, false).onClick.AddListener(() => {
            ToggleWindowMode();
        });
        y -= BH + SGAP;

        y = DrawSecHeader(p, y, "TABULEIROS");
        VTTLayout.BtnFull(p, y, BH, -PAD * 2f, "+ NOVO TABULEIRO", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, VTTLayout.F_BTN, false).onClick.AddListener(() => {
            if (MapFileLoader.Instance != null) MapFileLoader.Instance.OpenFilePicker((tex) => LayerManager.Instance.AddLayer(tex));
        });
        y -= BH + GAP;

        ScrollRect boardScroll = VTTLayout.MakeScrollView("BoardsScroll", p, 0, y, W_LEFT, 150f, out layersContainer);
        boardScroll.movementType = ScrollRect.MovementType.Clamped;
        y -= 150f + SGAP;

        y = DrawSecHeader(p, y, "TOKENS DA CAMPANHA");

        VTTLayout.LabelFixed(p, PAD, y + 2f, 120f, 20f, 10f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = "TAMANHO PADRAO";
        TMP_Text valText = VTTLayout.LabelFixed(p, W_LEFT - PAD - 60f, y + 2f, 60f, 20f, 10f, VTTLayout.C_ACCENT, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        valText.text = TokenSystem.GlobalTokenScale.ToString("F2") + "x";
        y -= 18f;

        float currentT = Mathf.Log(TokenSystem.GlobalTokenScale / 0.01f, 1000f);
        Slider globalScaleSlider = VTTLayout.MakeSlider(p, y, 20f, 0f, 1f, currentT);

        globalScaleSlider.onValueChanged.AddListener((val) => {
            float s = 0.01f * Mathf.Pow(1000f, val);
            TokenSystem.GlobalTokenScale = s;
            valText.text = s.ToString("F2") + "x";
        });
        y -= 26f + SGAP;

        ScrollRect tokScroll = VTTLayout.MakeScrollView("TokensScroll", p, 0, 0, 0, 0, out tokensContainer);
        RectTransform tokScrollRT = tokScroll.GetComponent<RectTransform>();
        tokScrollRT.anchorMin = new Vector2(0, 0);
        tokScrollRT.anchorMax = new Vector2(1, 1);
        tokScrollRT.pivot = new Vector2(0, 1);
        tokScrollRT.offsetMax = new Vector2(0, y);
        tokScrollRT.offsetMin = new Vector2(0, PAD);
        tokScroll.movementType = ScrollRect.MovementType.Clamped;
    }

    private float DrawPlayerScreenSection(RectTransform p, float y)
    {
        VTTLayout.BtnFull(p, y, BH, -PAD * 2f, "ABRIR JANELA FLUTUANTE", VTTLayout.C_BTN_PRI, VTTLayout.C_BDR_ACC, VTTLayout.C_TEXT, 11f).onClick.AddListener(() => {
            if (PlayerDisplaySystem.Instance != null) PlayerDisplaySystem.Instance.OpenWindow();
        });
        y -= BH + GAP;

        Button modeBtn = VTTLayout.BtnFull(p, y, BH, -PAD * 2f, "MODO ATUAL: DIGITAL", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 10f, bold: true);
        modeBtn.onClick.AddListener(() => {
            if (PlayerDisplaySystem.Instance != null)
            {
                bool isPhysical = PlayerDisplaySystem.Instance.currentMode == VTTMode.Physical_Table;
                PlayerDisplaySystem.Instance.currentMode = isPhysical ? VTTMode.Digital_Sync : VTTMode.Physical_Table;

                bool isNowPhysical = PlayerDisplaySystem.Instance.currentMode == VTTMode.Physical_Table;
                modeBtn.GetComponentInChildren<TMP_Text>().text = isNowPhysical ? "MODO ATUAL: MESA FÍSICA (KINECT)" : "MODO ATUAL: DIGITAL";
                VTTLayout.SetBtnColor(modeBtn, isNowPhysical ? VTTLayout.C_BTN_ACTIVE : VTTLayout.C_BTN_SEC);
            }
        });

        if (PlayerDisplaySystem.Instance != null)
        {
            bool isPhysicalInit = PlayerDisplaySystem.Instance.currentMode == VTTMode.Physical_Table;
            modeBtn.GetComponentInChildren<TMP_Text>().text = isPhysicalInit ? "MODO ATUAL: MESA FÍSICA (KINECT)" : "MODO ATUAL: DIGITAL";
            VTTLayout.SetBtnColor(modeBtn, isPhysicalInit ? VTTLayout.C_BTN_ACTIVE : VTTLayout.C_BTN_SEC);
        }
        y -= BH + GAP;

        float hw = (W_RIGHT - PAD * 2f - GAP) * 0.5f;

        string initialMon = PlayerDisplaySystem.Instance != null ? PlayerDisplaySystem.Instance.GetCurrentDisplayName() : "MONITOR 2";
        Button cycleBtn = VTTLayout.BtnFixed(p, PAD, y, hw, BH, "ALVO:\n" + initialMon, VTTLayout.C_SEC_BG, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT_DIM, 9f, true);
        TMP_Text monitorText = cycleBtn.GetComponentInChildren<TMP_Text>();
        cycleBtn.onClick.AddListener(() => {
            if (PlayerDisplaySystem.Instance != null)
            {
                PlayerDisplaySystem.Instance.CycleTargetDisplay();
                monitorText.text = "ALVO:\n" + PlayerDisplaySystem.Instance.GetCurrentDisplayName();
            }
        });

        VTTLayout.BtnFixed(p, PAD + hw + GAP, y, hw, BH, "EJETAR PARA\nO ALVO", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 10f).onClick.AddListener(() => {
            if (PlayerDisplaySystem.Instance != null) PlayerDisplaySystem.Instance.SendToMonitor();
        });
        y -= BH + GAP;

        Button linkBtn = VTTLayout.BtnFixed(p, PAD, y, hw, BH - 4f, "VINCULAR CÂMERAS", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 9f);
        linkBtn.onClick.AddListener(() => {
            if (PlayerDisplaySystem.Instance != null)
            {
                PlayerDisplaySystem.Instance.isLinkedToGM = !PlayerDisplaySystem.Instance.isLinkedToGM;
                VTTLayout.SetBtnColor(linkBtn, PlayerDisplaySystem.Instance.isLinkedToGM ? VTTLayout.C_BTN_ACTIVE : VTTLayout.C_BTN_SEC);
            }
        });

        VTTLayout.BtnFixed(p, PAD + hw + GAP, y, hw, BH - 4f, "FOCAR MAPA TODO", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 9f).onClick.AddListener(() => {
            if (PlayerDisplaySystem.Instance != null)
            {
                PlayerDisplaySystem.Instance.FocusFullMap();
                VTTLayout.SetBtnColor(linkBtn, VTTLayout.C_BTN_SEC);
            }
        });
        y -= (BH - 4f) + GAP;

        Button diceBtn = VTTLayout.BtnFull(p, y, BH - 4f, -PAD * 2f, "ROLAGENS NA TELA DOS JOGADORES: OFF", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 10f, bold: false);
        diceBtn.onClick.AddListener(() => {
            if (PlayerDisplaySystem.Instance != null)
            {
                PlayerDisplaySystem.Instance.showDiceRolls = !PlayerDisplaySystem.Instance.showDiceRolls;
                diceBtn.GetComponentInChildren<TMP_Text>().text = PlayerDisplaySystem.Instance.showDiceRolls ? "ROLAGENS NA TELA DOS JOGADORES: ON" : "ROLAGENS NA TELA DOS JOGADORES: OFF";
                VTTLayout.SetBtnColor(diceBtn, PlayerDisplaySystem.Instance.showDiceRolls ? VTTLayout.C_BTN_ACTIVE : VTTLayout.C_BTN_SEC);
            }
        });

        return y - (BH - 4f) - SGAP;
    }

    private void RefreshTokensList()
    {
        if (tokensContainer == null || CharacterManager.Instance == null) return;
        foreach (Transform child in tokensContainer) Destroy(child.gameObject);

        float ly = -PAD;
        var records = CharacterManager.Instance.Database.records;

        if (records.Count == 0)
        {
            TMP_Text emptyTxt = VTTLayout.LabelFixed(tokensContainer, PAD, ly, W_LEFT - (PAD * 2), 40f, VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM);
            emptyTxt.text = "Crie um personagem no Dashboard.";
            emptyTxt.alignment = TextAlignmentOptions.Center;
            tokensContainer.sizeDelta = new Vector2(0f, 60f);
            return;
        }

        foreach (var rec in records)
        {
            float itemH = 46f;
            RectTransform item = VTTLayout.Box("TokItem_" + rec.id, tokensContainer, PAD, ly, -PAD * 2f, itemH, VTTLayout.C_SEC_BG);
            VTTLayout.AccentBar(item, 3f, rec.system == "D&D 5e" ? VTTLayout.C_TEXT_GOLD : new Color(0.85f, 0.25f, 0.25f, 1f));
            item.GetComponent<Image>().raycastTarget = true;

            Texture2D tex = CharacterManager.Instance.LoadAvatar(rec.avatarFileName);
            Image avImg = VTTLayout.MakeMaskedAvatar(item, new Vector2(10f, 0f), new Vector2(34f, 34f), VTTLayout.C_CONTENT_BG);
            if (tex != null)
            {
                avImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                avImg.color = Color.white;
            }

            VTTLayout.LabelStretch("NameLabel", item, new Vector2(55f, 0f), new Vector2(0f, 0f), 12f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = rec.name;

            EventTrigger trigger = item.gameObject.AddComponent<EventTrigger>();
            TokenController spawnedToken = null;
            string tid = rec.id;

            EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) => {
                spawnedToken = TokenSystem.SpawnToken(tid);
                if (spawnedToken != null) spawnedToken.FollowMouse();
            });
            trigger.triggers.Add(entryDown);

            EventTrigger.Entry entryDrag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            entryDrag.callback.AddListener((data) => {
                if (spawnedToken != null) spawnedToken.FollowMouse();
            });
            trigger.triggers.Add(entryDrag);

            EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) => {
                if (spawnedToken != null)
                {
                    spawnedToken.TryPlaceInMap();
                    spawnedToken = null;
                }
            });
            trigger.triggers.Add(entryUp);

            ly -= (itemH + 6f);
        }
        tokensContainer.sizeDelta = new Vector2(0f, Mathf.Abs(ly) + PAD);
    }

    private void RefreshLayersList()
    {
        if (layersContainer == null || LayerManager.Instance == null) return;
        foreach (Transform child in layersContainer) Destroy(child.gameObject);

        float ly = 0f;
        var layers = LayerManager.Instance.Layers;
        if (layers.Count == 0) return;

        foreach (var layer in layers)
        {
            bool isActive = (layer.id == LayerManager.Instance.ActiveLayerId);
            Color bgColor = isActive ? VTTLayout.C_SEC_BG : VTTLayout.C_CONTENT_BG;
            float itemH = 38f;

            RectTransform itemRT = VTTLayout.Box("LayerItem_" + layer.id, layersContainer, PAD, ly, -PAD * 2f, itemH, bgColor);
            if (isActive) VTTLayout.AccentBar(itemRT, 4f, VTTLayout.C_ACCENT_LT);

            Button selectBtn = VTTLayout.BtnFixed(itemRT, 0f, 0f, W_LEFT - 110f, itemH, "", Color.clear, Color.clear, Color.white, 10f);
            selectBtn.onClick.AddListener(() => { if (!isActive) LayerManager.Instance.SetActiveLayer(layer.id); });

            float textX = isActive ? 14f : 8f; float textW = W_LEFT - PAD * 2f - 90f;

            TMP_InputField nameInput = VTTLayout.InputFieldFixed(itemRT, textX, 0f, textW, itemH, VTTLayout.F_PANEL, isActive ? VTTLayout.C_TEXT_PANEL : VTTLayout.C_TEXT_DIM, isActive ? FontStyles.Bold : FontStyles.Normal, layer.name);
            string currentId = layer.id;

            nameInput.onEndEdit.AddListener((newName) => { LayerManager.Instance.RenameLayer(currentId, newName); });
            nameInput.onSelect.AddListener((text) => { if (!isActive) LayerManager.Instance.SetActiveLayer(currentId); });

            float btnSz = 24f; float btnY = -(itemH - btnSz) * 0.5f;
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
        zoomLbl.text = "ZOOM"; y -= 20f;
        float mn = (cameraController != null) ? cameraController.MinZoom : 0.5f;
        float mx = (cameraController != null) ? cameraController.MaxZoom : 30f;
        float cur = (cameraController != null) ? cameraController.CurrentZoom : 5f;

        zoomSlider = VTTLayout.MakeSlider(p, y, 28f, mn, mx, cur);
        zoomSlider.onValueChanged.AddListener((v) => { if (cameraController != null) { cameraController.SetZoom(v); _lastCamZoom = v; } });
        y -= 28f + GAP;

        float hw = (W_RIGHT - PAD * 2f - GAP) * 0.5f;
        VTTLayout.BtnFixed(p, PAD, y, hw, BH - 4f, "CENTRALIZAR", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, VTTLayout.F_BTN).onClick.AddListener(() => MapEvents.FireCenterMapRequested());
        VTTLayout.BtnFixed(p, PAD + hw + GAP, y, hw, BH - 4f, "RESET ZOOM", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, VTTLayout.F_BTN).onClick.AddListener(() => { MapEvents.FireResetZoomRequested(); if (cameraController != null) cameraController.FocusOnActiveBoard(); });
        return y - (BH - 4f) - SGAP;
    }

    private float DrawDiceSection(RectTransform p, float y)
    {
        VTTLayout.BtnFull(p, y, BH, -PAD * 2f, "ABRIR DADOS", VTTLayout.C_BTN_DICE, VTTLayout.C_BDR_DICE, VTTLayout.C_TEXT, VTTLayout.F_BTN).onClick.AddListener(() => { if (diceOverlay != null) diceOverlay.OpenPanel(); });
        y -= BH + GAP;
        VTTLayout.Label(p, y, 14f, VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM, FontStyles.Bold).text = "ULTIMAS ROLAGENS";
        y -= 17f;
        RectTransform boxRT = VTTLayout.Box("HistBox", p, 0f, y, 0f, 66f, VTTLayout.C_CONTENT_BG);
        VTTLayout.AccentBar(boxRT, 2f, VTTLayout.C_ACCENT);
        historyText = VTTLayout.LabelStretch("HistText", boxRT, new Vector2(PAD, 5f), new Vector2(-4f, -5f), VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM, align: TextAlignmentOptions.TopLeft);
        historyText.lineSpacing = 4f; historyText.text = "Nenhuma rolagem ainda";
        return y - 66f - SGAP;
    }

    private float DrawFogSection(RectTransform p, float y)
    {
        Button xrayBtn = VTTLayout.BtnFull(p, y, BH - 4f, -PAD * 2f, "VISÃO RAIO-X: DESLIGADA", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, Color.white, VTTLayout.F_BTN, bold: false);
        xrayBtn.onClick.AddListener(() => {
            if (fogController != null)
            {
                bool isXRay = !fogController.isXRayActive;
                fogController.ToggleXRay(isXRay);
                xrayBtn.GetComponentInChildren<TMP_Text>().text = isXRay ? "VISÃO RAIO-X: LIGADA" : "VISÃO RAIO-X: DESLIGADA";
                VTTLayout.SetBtnColor(xrayBtn, isXRay ? VTTLayout.C_BTN_ACTIVE : VTTLayout.C_BTN_SEC);
            }
        });
        y -= (BH - 4f) + GAP;

        float hw = (W_RIGHT - PAD * 2f - GAP) * 0.5f;
        fogPaintBtn = VTTLayout.BtnFixed(p, PAD, y, hw, BH, "PINTAR", VTTLayout.C_BTN_PAINT, VTTLayout.C_BDR_PAINT, VTTLayout.C_TEXT, VTTLayout.F_BTN);
        fogPaintBtn.onClick.AddListener(() => { if (fogController != null) { fogController.SetMode(FogOfWarController.FogMode.Paint); fogController.SetActive(!(fogController.IsActive && fogController.CurrentMode == FogOfWarController.FogMode.Paint)); } });
        fogEraseBtn = VTTLayout.BtnFixed(p, PAD + hw + GAP, y, hw, BH, "APAGAR", VTTLayout.C_BTN_ERASE, VTTLayout.C_BDR_ERASE, VTTLayout.C_TEXT, VTTLayout.F_BTN);
        fogEraseBtn.onClick.AddListener(() => { if (fogController != null) { fogController.SetMode(FogOfWarController.FogMode.Erase); fogController.SetActive(!(fogController.IsActive && fogController.CurrentMode == FogOfWarController.FogMode.Erase)); } });
        y -= BH + GAP;
        VTTLayout.Label(p, y, 14f, VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM).text = "Tamanho do pincel";
        y -= 17f;
        float smW = 30f; float cntW = W_RIGHT - PAD * 2f - smW * 2f - GAP * 2f; float bh2 = BH - 6f;
        VTTLayout.BtnFixed(p, PAD, y, smW, bh2, "-", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 14f).onClick.AddListener(() => { if (fogController != null) fogController.DecreaseBrush(5); RefreshBrushLabel(); });
        brushSizeText = VTTLayout.LabelFixed(p, PAD + smW + GAP, y, cntW, bh2, VTTLayout.F_LABEL, VTTLayout.C_TEXT, FontStyles.Bold);
        brushSizeText.text = "20";
        VTTLayout.BtnFixed(p, PAD + smW + GAP + cntW + GAP, y, smW, bh2, "+", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 14f).onClick.AddListener(() => { if (fogController != null) fogController.IncreaseBrush(5); RefreshBrushLabel(); });
        y -= bh2 + GAP;
        VTTLayout.BtnFull(p, y, BH - 4f, -PAD * 2f, "LIMPAR NEVOA", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT_WARN, VTTLayout.F_BTN, bold: false).onClick.AddListener(() => { if (fogController != null) fogController.ClearAll(); });
        y -= (BH - 4f) + GAP;
        fogStatusText = VTTLayout.Label(p, y, 14f, VTTLayout.F_SMALL, VTTLayout.C_TEXT_DIM);
        fogStatusText.text = "Ferramenta inativa";
        return y - 14f - SGAP;
    }

    private float DrawInfoSection(RectTransform p, float y)
    {
        RectTransform boxRT = VTTLayout.Box("InfoBox", p, 0f, y, 0f, 100f, VTTLayout.C_CONTENT_BG);
        VTTLayout.AccentBar(boxRT, 2f, VTTLayout.C_ACCENT);
        infoText = VTTLayout.LabelStretch("InfoText", boxRT, new Vector2(PAD + 2f, 7f), new Vector2(-5f, -7f), VTTLayout.F_LABEL, VTTLayout.C_TEXT, align: TextAlignmentOptions.TopLeft);
        infoText.lineSpacing = 9f;
        infoText.text = "Acesso    --\nZoom      --\nCursor    --\nNevoa     Inativa";
        return y - 100f - PAD;
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

    private void SyncZoom() { if (zoomSlider != null && cameraController != null) { float camZoom = cameraController.CurrentZoom; if (Mathf.Abs(camZoom - _lastCamZoom) > 0.001f) { zoomSlider.SetValueWithoutNotify(camZoom); _lastCamZoom = camZoom; } } }
    private void SyncMouseCoord() { if (coordSystem != null && infoText != null && mapController != null && mapController.IsMapLoaded) OnMapInfo(new MapInfo { scale = mapController.CurrentScale, mouseNormalized = coordSystem.GetMouseNormalized(), isLoaded = true }); }
    private void SyncFogState() { if (fogController == null || fogStatusText == null) return; bool active = fogController.IsActive; bool isPaint = fogController.CurrentMode == FogOfWarController.FogMode.Paint; if (!active) { fogStatusText.text = "Ferramenta inativa"; fogStatusText.color = VTTLayout.C_TEXT_DIM; } else if (isPaint) { fogStatusText.text = "Modo pintura ativo"; fogStatusText.color = VTTLayout.RGB(0.30f, 0.60f, 0.90f); } else { fogStatusText.text = "Modo apagar ativo"; fogStatusText.color = VTTLayout.RGB(0.85f, 0.40f, 0.35f); } VTTLayout.SetBtnColor(fogPaintBtn, (active && isPaint) ? VTTLayout.C_BTN_ACTIVE : VTTLayout.C_BTN_PAINT); VTTLayout.SetBtnColor(fogEraseBtn, (active && !isPaint) ? VTTLayout.C_BTN_ACTIVE : VTTLayout.C_BTN_ERASE); }
    private void OnMapInfo(MapInfo info) { if (infoText == null || !info.isLoaded) return; string cursor = info.mouseNormalized.HasValue ? info.mouseNormalized.Value.x.ToString("F3") + "  " + info.mouseNormalized.Value.y.ToString("F3") : "fora do mapa"; float zoom = (cameraController != null) ? cameraController.CurrentZoom : 0f; string fogSt = (fogController != null && fogController.IsActive) ? "Ativa" : "Inativa"; infoText.text = "Acesso    OK\nZoom      " + zoom.ToString("F2") + "\nCursor    " + cursor + "\nNevoa     " + fogSt; }

    private void RefreshHistory()
    {
        if (historyText == null || diceOverlay == null) return;
        var h = diceOverlay.History;
        if (h.Count == 0) { historyText.text = "Nenhuma rolagem ainda"; return; }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int n = Mathf.Min(h.Count, 4);
        for (int i = 0; i < n; i++)
        {
            sb.Append(h[i].descriptor + "  =>  " + h[i].total);
            if (i < n - 1) sb.Append("\n");
        }
        historyText.text = sb.ToString();

        if (PlayerDisplaySystem.Instance != null && PlayerDisplaySystem.Instance.showDiceRolls)
        {
            PlayerDisplaySystem.Instance.ShowRoll(h[0].descriptor + "\n=> " + h[0].total + " <=");
        }
    }

    private void RefreshBrushLabel() { if (brushSizeText != null && fogController != null) brushSizeText.text = fogController.BrushRadius.ToString(); }
}