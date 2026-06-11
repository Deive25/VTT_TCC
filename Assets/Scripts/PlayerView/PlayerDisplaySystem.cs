using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public enum VTTMode
{
    Digital_Sync,
    Physical_Table
}

public class PlayerDisplaySystem : MonoBehaviour
{
    public static PlayerDisplaySystem Instance { get; private set; }
    [Header("Modo de Operacao")]
    public VTTMode currentMode = VTTMode.Digital_Sync;

    public Camera playerCam;
    private RenderTexture playerViewTex;
    private GameObject floatingWindow;
    private RectTransform windowRT;
    private GameObject viewCont;

    private GameObject optionsWindow;
    private RectTransform optionsWindowRT;
    private bool isFullscreenPref = false;
    private TMP_Text telaBtnText;
    private TMP_Text modoBtnText;

    private TMP_Text diceText;
    private Coroutine diceRoutine;

    public bool isLinkedToGM = false;
    public bool showDiceRolls = false;

    private Vector2 dragOffset;
    private Vector2 resizeStartMouse;
    private Vector2 resizeStartSize;
    private bool isMinimized = false;
    private bool isMaximized = false;
    private Vector2 savedSize = new Vector2(960f, 580f);
    private Vector2 savedPos = Vector2.zero;

    public int targetDisplayIndex = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        SetFullscreenMode(false);
    }

    private void Start()
    {
        StartCoroutine(DelayedSetup());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionsWindow != null)
            {
                optionsWindow.SetActive(!optionsWindow.activeSelf);
                if (optionsWindow.activeSelf)
                {
                    FitOptionsWindowToScreen();
                    optionsWindow.transform.SetAsLastSibling();
                    UpdateOptionsUI();
                }
            }
        }
    }

    private IEnumerator DelayedSetup()
    {
        yield return new WaitForEndOfFrame();
        SetupSystem();
    }

    private void SetupSystem()
    {
        playerViewTex = new RenderTexture(1920, 1080, 24);
        playerViewTex.filterMode = FilterMode.Bilinear;

        GameObject camGO = new GameObject("PlayerCamera");
        playerCam = camGO.AddComponent<Camera>();

        playerCam.CopyFrom(Camera.main);

        playerCam.rect = new Rect(0f, 0f, 1f, 1f);

        playerCam.targetTexture = playerViewTex;
        playerCam.depth = Camera.main.depth - 5;

        playerCam.clearFlags = CameraClearFlags.SolidColor;
        playerCam.backgroundColor = Color.black;

        playerCam.cullingMask &= ~(1 << 1);
        playerCam.cullingMask &= ~(1 << 5);
        Camera.main.cullingMask &= ~(1 << 4);

        GameObject canvasGO = new GameObject("PlayerCanvas");
        canvasGO.layer = 0;
        Canvas playerCanvas = canvasGO.AddComponent<Canvas>();
        playerCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        playerCanvas.worldCamera = playerCam;
        playerCanvas.planeDistance = 1f;

        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);

        GameObject textGO = new GameObject("DiceText");
        textGO.layer = 0;
        textGO.transform.SetParent(canvasGO.transform, false);
        diceText = textGO.AddComponent<TextMeshProUGUI>();
        diceText.fontSize = 92;
        diceText.color = Color.clear;
        diceText.alignment = TextAlignmentOptions.Center;
        diceText.fontStyle = FontStyles.Bold;
        diceText.outlineWidth = 0.28f;
        diceText.outlineColor = new Color32(0, 0, 0, 255);

        RectTransform rt = diceText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.15f); rt.anchorMax = new Vector2(0.5f, 0.15f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1200, 200);
        diceText.text = "";

        BuildFloatingWindow();
        BuildOptionsWindow();
    }

    private void BuildOptionsWindow()
    {
        GameObject mainCanvasGO = GameObject.Find("MainCanvas");
        Canvas cv = mainCanvasGO != null ? mainCanvasGO.GetComponent<Canvas>() : FindAnyObjectByType<Canvas>();
        if (cv == null) return;

        optionsWindow = new GameObject("OptionsMenuWindow");
        RectTransform winRT = optionsWindow.AddComponent<RectTransform>();
        optionsWindowRT = winRT;
        winRT.SetParent(cv.transform, false);
        winRT.sizeDelta = new Vector2(340f, 220f);
        winRT.anchorMin = new Vector2(0.5f, 0.5f); winRT.anchorMax = new Vector2(0.5f, 0.5f);
        winRT.pivot = new Vector2(0.5f, 0.5f);

        Image bg = optionsWindow.AddComponent<Image>();
        bg.color = new Color(0.11f, 0.12f, 0.15f, 0.98f);
        VTTLayout.AccentBar(winRT, 4f, VTTLayout.C_ACCENT);

        GameObject titleGO = new GameObject("Title");
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.SetParent(winRT, false);
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -20f);
        titleRT.sizeDelta = new Vector2(0, 30f);
        TMP_Text titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "PAINEL DE CONFIGURACOES";
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.fontSize = 16;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = Color.white;

        Button btnTela = BuildOptionBtn(winRT, "TELA: ...", new Vector2(0, 35f), out telaBtnText);
        btnTela.onClick.AddListener(() => {
            SetFullscreenMode(!isFullscreenPref);
        });

        Button btnModo = BuildOptionBtn(winRT, "MODO: ...", new Vector2(0, -25f), out modoBtnText);
        btnModo.onClick.AddListener(() => {
            VTTMode proximoModo = (currentMode == VTTMode.Digital_Sync) ? VTTMode.Physical_Table : VTTMode.Digital_Sync;
            string msg = proximoModo == VTTMode.Physical_Table ? "Entrar no modo fisico vai limpar rastreamento/calibracao anteriores e exigir nova calibracao." : "Voltar ao modo digital preserva os tokens no mapa, mas desativa o vinculo fisico atual.";
            UIConfirmDialog.Show("Trocar modo do VTT", msg, () => SetVTTMode(proximoModo));
        });

       GameObject footerGO = new GameObject("FooterHelp");
        RectTransform footerRT = footerGO.AddComponent<RectTransform>();
        footerRT.SetParent(winRT, false);
        footerRT.anchorMin = new Vector2(0, 0); footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 15f);
        footerRT.sizeDelta = new Vector2(0, 20f);
        TMP_Text footerTxt = footerGO.AddComponent<TextMeshProUGUI>();
        footerTxt.text = "Pressione ESC para fechar este menu";
        footerTxt.alignment = TextAlignmentOptions.Center;
        footerTxt.fontSize = 11;
        footerTxt.color = new Color(0.6f, 0.6f, 0.6f);

        UpdateOptionsUI();
        FitOptionsWindowToScreen();
        optionsWindow.SetActive(false);
    }


    private void FitOptionsWindowToScreen()
    {
        if (optionsWindowRT == null) return;
        Canvas canvas = optionsWindowRT.GetComponentInParent<Canvas>();
        RectTransform canvasRT = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        float w = canvasRT != null && canvasRT.rect.width > 0f ? canvasRT.rect.width : Screen.width;
        float h = canvasRT != null && canvasRT.rect.height > 0f ? canvasRT.rect.height : Screen.height;
        float scale = Mathf.Min(1f, (w - 48f) / 340f, (h - 48f) / 220f);
        optionsWindowRT.localScale = Vector3.one * Mathf.Clamp(scale, 0.76f, 1f);
        optionsWindowRT.anchoredPosition = Vector2.zero;
    }
    private Button BuildOptionBtn(RectTransform parent, string defaultText, Vector2 pos, out TMP_Text textComponent)
    {
        GameObject go = new GameObject("Btn_Setting");
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(290f, 42f);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.21f, 0.26f);

        GameObject txtGo = new GameObject("Text");
        RectTransform txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.SetParent(rt, false);
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;

        textComponent = txtGo.AddComponent<TextMeshProUGUI>();
        textComponent.text = defaultText;
        textComponent.color = Color.white;
        textComponent.fontSize = 13f;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontStyle = FontStyles.Bold;

        go.AddComponent<ButtonFeedback>();

        return go.AddComponent<Button>();
    }

    private void UpdateOptionsUI()
    {
        if (telaBtnText != null)
        {
            telaBtnText.text = isFullscreenPref ? "TELA: TELA CHEIA (MAXIMIZADO)" : "TELA: MODO JANELA (PADRAO)";
        }

        if (modoBtnText != null)
        {
            modoBtnText.text = (currentMode == VTTMode.Digital_Sync) ? "MODO DO SISTEMA: DIGITAL SYNC" : "MODO DO SISTEMA: MESA FISICA (KINECT)";
        }
    }

    public void SetFullscreenMode(bool fullscreen)
    {
        isFullscreenPref = fullscreen;
        if (fullscreen)
        {
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            if (Screen.width > 1920 || Screen.height > 1080 || Screen.width < 900 || Screen.height < 600)
                Screen.SetResolution(1280, 720, false);
        }
        UpdateOptionsUI();
    }

    public void RestoreMainWindowMode()
    {
        isFullscreenPref = false;
        Screen.fullScreenMode = FullScreenMode.Windowed;
        if (Screen.width > 1920 || Screen.height > 1080 || Screen.width < 900 || Screen.height < 600)
            Screen.SetResolution(1280, 720, false);
        UpdateOptionsUI();
    }

    public void SetVTTMode(VTTMode newMode)
    {
        if (currentMode == newMode)
        {
            UpdateOptionsUI();
            return;
        }

        VTTMode previousMode = currentMode;
        currentMode = newMode;

        if (newMode == VTTMode.Physical_Table)
        {
            if (KinectManager.Instance != null) KinectManager.Instance.PrepareForPhysicalMode();
            isLinkedToGM = false;
            FocusFullMap();
        }
        else if (previousMode == VTTMode.Physical_Table)
        {
            if (KinectManager.Instance != null) KinectManager.Instance.ReleasePhysicalTrackingPreserveTokens();
        }

        Debug.Log("VTT Mode alterado de forma centralizada para: " + newMode.ToString());
        UpdateOptionsUI();
    }

   private void BuildFloatingWindow()
    {
        GameObject mainCanvasGO = GameObject.Find("MainCanvas");
        Canvas cv = mainCanvasGO != null ? mainCanvasGO.GetComponent<Canvas>() : FindAnyObjectByType<Canvas>();
        if (cv == null) return;

        floatingWindow = new GameObject("PlayerViewWindow");
        windowRT = floatingWindow.AddComponent<RectTransform>();
        windowRT.SetParent(cv.transform, false);
        windowRT.sizeDelta = savedSize;
        windowRT.anchorMin = new Vector2(0.5f, 0.5f); windowRT.anchorMax = new Vector2(0.5f, 0.5f);
        windowRT.pivot = new Vector2(0.5f, 0.5f);
        windowRT.anchoredPosition = savedPos;

        Image bg = floatingWindow.AddComponent<Image>();
        bg.color = Color.black;
        VTTLayout.AccentBar(windowRT, 4f, VTTLayout.C_ACCENT);

        GameObject header = new GameObject("Header");
        RectTransform hdrRT = header.AddComponent<RectTransform>();
        hdrRT.SetParent(windowRT, false);
        hdrRT.anchorMin = new Vector2(0, 1); hdrRT.anchorMax = new Vector2(1, 1);
        hdrRT.pivot = new Vector2(0.5f, 1);
        hdrRT.anchoredPosition = Vector2.zero;
        hdrRT.sizeDelta = new Vector2(0, 40f);
        Image hdrImg = header.AddComponent<Image>();
        hdrImg.color = VTTLayout.C_HDR_BG;
        hdrImg.raycastTarget = true;

        TMP_Text titleTxt = VTTLayout.LabelFixed(hdrRT, 15f, 0, 400f, 40f, 14f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        titleTxt.text = "VISAO DOS JOGADORES (CAPTURA)";
        RectTransform titleRT = titleTxt.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.5f); titleRT.anchorMax = new Vector2(0, 0.5f);
        titleRT.pivot = new Vector2(0, 0.5f);
        titleRT.anchoredPosition = new Vector2(15f, 0f);

        EventTrigger trigger = header.AddComponent<EventTrigger>();
        EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryDown.callback.AddListener((data) => {
            PointerEventData ped = (PointerEventData)data;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(windowRT, ped.position, ped.pressEventCamera, out dragOffset);
            floatingWindow.transform.SetAsLastSibling();
        });
        trigger.triggers.Add(entryDown);

        EventTrigger.Entry entryDrag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        entryDrag.callback.AddListener((data) => {
            if (isMaximized) return;
            PointerEventData ped = (PointerEventData)data;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(windowRT.parent as RectTransform, ped.position, ped.pressEventCamera, out Vector2 localMousePos))
            {
                windowRT.localPosition = localMousePos - dragOffset;
                FitFloatingWindowToCanvas();
            }
        });
        trigger.triggers.Add(entryDrag);

        Button btnClose = BuildWinControlBtn(hdrRT, "X", VTTLayout.C_BTN_CLOSE, 0f);
        btnClose.onClick.AddListener(CloseWindow);

        Button btnMax = BuildWinControlBtn(hdrRT, "MAX", VTTLayout.C_BTN_SEC, -40f);
        btnMax.onClick.AddListener(ToggleMaximize);

        Button btnMin = BuildWinControlBtn(hdrRT, "_", VTTLayout.C_BTN_SEC, -80f);
        btnMin.onClick.AddListener(ToggleMinimize);

        viewCont = new GameObject("ViewContainer");
        RectTransform contRT = viewCont.AddComponent<RectTransform>();
        contRT.SetParent(windowRT, false);
        contRT.anchorMin = new Vector2(0, 0); contRT.anchorMax = new Vector2(1, 1);
        contRT.offsetMin = new Vector2(4f, 4f); contRT.offsetMax = new Vector2(-4f, -40f);

        GameObject videoGO = new GameObject("VideoImage");
        RectTransform videoRT = videoGO.AddComponent<RectTransform>();
        videoRT.SetParent(contRT, false);
        videoRT.anchorMin = Vector2.zero; videoRT.anchorMax = Vector2.one;
        videoRT.offsetMin = Vector2.zero; videoRT.offsetMax = Vector2.zero;

        RawImage rawImg = videoGO.AddComponent<RawImage>();
        rawImg.texture = playerViewTex;

        AspectRatioFitter arf = videoGO.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = 1920f / 1080f;

        BuildResizeHandle(windowRT);

        floatingWindow.SetActive(false);
    }


    private void BuildResizeHandle(RectTransform parent)
    {
        GameObject handle = new GameObject("ResizeHandle");
        RectTransform handleRT = handle.AddComponent<RectTransform>();
        handleRT.SetParent(parent, false);
        handleRT.anchorMin = new Vector2(1f, 0f);
        handleRT.anchorMax = new Vector2(1f, 0f);
        handleRT.pivot = new Vector2(1f, 0f);
        handleRT.anchoredPosition = new Vector2(-5f, 5f);
        handleRT.sizeDelta = new Vector2(26f, 26f);

        Image img = handle.AddComponent<Image>();
        img.color = new Color(0.82f, 0.72f, 0.48f, 0.45f);
        img.raycastTarget = true;

        EventTrigger trigger = handle.AddComponent<EventTrigger>();
        EventTrigger.Entry down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener((data) => {
            PointerEventData ped = (PointerEventData)data;
            resizeStartMouse = ped.position;
            resizeStartSize = windowRT.sizeDelta;
            floatingWindow.transform.SetAsLastSibling();
        });
        trigger.triggers.Add(down);

        EventTrigger.Entry drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener((data) => {
            if (isMaximized || isMinimized) return;
            PointerEventData ped = (PointerEventData)data;
            Vector2 delta = ped.position - resizeStartMouse;
            float newW = Mathf.Clamp(resizeStartSize.x + delta.x, 520f, 1600f);
            float newH = Mathf.Clamp(resizeStartSize.y - delta.y, 340f, 1000f);
            windowRT.sizeDelta = new Vector2(newW, newH);
            savedSize = windowRT.sizeDelta;
            FitFloatingWindowToCanvas();
        });
        trigger.triggers.Add(drag);
    }
    private Button BuildWinControlBtn(RectTransform parent, string text, Color bgColor, float rightOffset)
    {
        GameObject go = new GameObject("WinBtn_" + text);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(rightOffset, 0f);
        rt.sizeDelta = new Vector2(40f, 40f);

        Image img = go.AddComponent<Image>();
        img.color = bgColor;

        GameObject txtGo = new GameObject("Text");
        RectTransform txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.SetParent(rt, false);
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;

        TMP_Text t = txtGo.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.color = Color.white;
        t.fontSize = 14f;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;

        return go.AddComponent<Button>();
    }

    private void ToggleMinimize()
    {
        if (isMaximized) ToggleMaximize();
        isMinimized = !isMinimized;

        if (isMinimized)
        {
            savedSize = windowRT.sizeDelta;
            savedPos = windowRT.anchoredPosition;
            viewCont.SetActive(false);
            windowRT.sizeDelta = new Vector2(savedSize.x, 40f);
        }
        else
        {
            viewCont.SetActive(true);
            windowRT.sizeDelta = savedSize;
            windowRT.anchoredPosition = savedPos;
        }
    }

    private void ToggleMaximize()
    {
        if (isMinimized) ToggleMinimize();
        isMaximized = !isMaximized;

        RectTransform canvasRT = floatingWindow.GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        if (isMaximized)
        {
            savedSize = windowRT.sizeDelta;
            savedPos = windowRT.anchoredPosition;
            windowRT.sizeDelta = canvasRT.rect.size;
            windowRT.anchoredPosition = Vector2.zero;
        }
        else
        {
            windowRT.sizeDelta = savedSize;
            windowRT.anchoredPosition = savedPos;
        }
    }


    private void FitFloatingWindowToCanvas()
    {
        if (floatingWindow == null || windowRT == null) return;
        Canvas canvas = floatingWindow.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRT = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRT.rect.size;
        float maxW = Mathf.Max(520f, canvasSize.x - 48f);
        float maxH = Mathf.Max(340f, canvasSize.y - 48f);
        windowRT.sizeDelta = new Vector2(Mathf.Clamp(windowRT.sizeDelta.x, 520f, maxW), Mathf.Clamp(windowRT.sizeDelta.y, 340f, maxH));

        float halfW = windowRT.sizeDelta.x * 0.5f;
        float halfH = windowRT.sizeDelta.y * 0.5f;
        float minX = -canvasSize.x * 0.5f + halfW + 12f;
        float maxX = canvasSize.x * 0.5f - halfW - 12f;
        float minY = -canvasSize.y * 0.5f + halfH + 12f;
        float maxY = canvasSize.y * 0.5f - halfH - 12f;

        if (maxX >= minX && maxY >= minY)
            windowRT.anchoredPosition = new Vector2(Mathf.Clamp(windowRT.anchoredPosition.x, minX, maxX), Mathf.Clamp(windowRT.anchoredPosition.y, minY, maxY));
        else
            windowRT.anchoredPosition = Vector2.zero;
    }
    public void OpenWindow()
    {
        playerCam.targetDisplay = 0;
        playerCam.targetTexture = playerViewTex;
        RestoreMainWindowMode();
        floatingWindow.SetActive(true);
        FitFloatingWindowToCanvas();
        floatingWindow.transform.SetAsLastSibling();

        if (!isLinkedToGM || currentMode == VTTMode.Physical_Table) FocusFullMap();
    }

    public void CloseWindow() => floatingWindow.SetActive(false);

    public string GetCurrentDisplayName()
    {
        if (Display.displays.Length <= 1) return "SEM MONITORES";
        return "MONITOR " + (targetDisplayIndex + 1);
    }

    public void CycleTargetDisplay()
    {
        if (Display.displays.Length <= 1) return;
        targetDisplayIndex++;
        if (targetDisplayIndex >= Display.displays.Length) targetDisplayIndex = 1;
    }

    public void SendToMonitor()
    {
        if (Display.displays.Length > 1 && targetDisplayIndex < Display.displays.Length)
        {
            RestoreMainWindowMode();
            Display.displays[targetDisplayIndex].Activate(1920, 1080, 60);

            playerCam.targetTexture = null;
            playerCam.targetDisplay = targetDisplayIndex;
            CloseWindow();
            Debug.Log("Sinal enviado para o " + GetCurrentDisplayName());

            StartCoroutine(EnforceScreenModeRoutine());
        }
        else
        {
            Debug.LogWarning("[PlayerDisplaySystem] Monitor selecionado nao esta conectado no Windows.");
        }
    }

    private IEnumerator EnforceScreenModeRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        // Mantém a janela principal do mestre fora de fullscreen ao ejetar para outro monitor.
        RestoreMainWindowMode();
    }

    private void LateUpdate()
    {
        if (currentMode == VTTMode.Digital_Sync)
        {
            if (isLinkedToGM) SyncAndClampCamera();
        }
        else if (currentMode == VTTMode.Physical_Table)
        {
            FocusFullMap();
        }
    }

    private void SyncAndClampCamera()
    {
        MapController mc = FindAnyObjectByType<MapController>();
        if (mc == null || !mc.IsMapLoaded) return;

        Camera gmCam = Camera.main;
        playerCam.orthographicSize = gmCam.orthographicSize;

        Vector3 newPos = gmCam.transform.position;
        Bounds b = mc.MapBounds;

        float camHeight = playerCam.orthographicSize;
        float camWidth = camHeight * playerCam.aspect;

        float minX = b.min.x + camWidth; float maxX = b.max.x - camWidth;
        float minY = b.min.y + camHeight; float maxY = b.max.y - camHeight;

        if (maxX < minX) newPos.x = b.center.x; else newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        if (maxY < minY) newPos.y = b.center.y; else newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

        newPos.z = -10f;
        playerCam.transform.position = newPos;
    }

    public void FocusFullMap()
    {
        isLinkedToGM = false;
        MapController mc = FindAnyObjectByType<MapController>();
        if (mc == null || !mc.IsMapLoaded) return;

        Bounds b = mc.MapBounds;
        float screenRatio = playerCam.aspect;
        float targetRatio = b.size.x / b.size.y;

        if (screenRatio >= targetRatio) playerCam.orthographicSize = b.size.y / 2f;
        else playerCam.orthographicSize = b.size.y / 2f * (targetRatio / screenRatio);

        playerCam.transform.position = new Vector3(b.center.x, b.center.y, -10f);
    }

    public void ShowRoll(string rollText)
    {
        if (!showDiceRolls) return;
        if (diceRoutine != null) StopCoroutine(diceRoutine);
        diceRoutine = StartCoroutine(AnimateDiceText(rollText));
    }

    private IEnumerator AnimateDiceText(string text)
    {
        diceText.text = text;
        diceText.color = new Color(1.0f, 0.78f, 0.30f, 1);
        yield return new WaitForSeconds(5f);

        float t = 1f;
        while (t > 0)
        {
            t -= Time.deltaTime;
            diceText.color = new Color(1.0f, 0.78f, 0.30f, t);
            yield return null;
        }
        diceText.text = "";
    }
}
