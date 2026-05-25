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

    [Header("Modo de Operação")]
    public VTTMode currentMode = VTTMode.Digital_Sync;

    public Camera playerCam;
    private RenderTexture playerViewTex;
    private GameObject floatingWindow;
    private RectTransform windowRT;
    private GameObject viewCont;

    // --- NOVA JANELA DE OPÇÕES ---
    private GameObject optionsWindow;
    private bool isFullscreenPref = false; // Guarda a preferência do Mestre

    private TMP_Text diceText;
    private Coroutine diceRoutine;

    public bool isLinkedToGM = false;
    public bool showDiceRolls = false;

    private Vector2 dragOffset;
    private bool isMinimized = false;
    private bool isMaximized = false;
    private Vector2 savedSize = new Vector2(960f, 580f);
    private Vector2 savedPos = Vector2.zero;

    public int targetDisplayIndex = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Inicia forçando o Modo Janela
        SetFullscreenMode(false);
    }

    private void Start()
    {
        StartCoroutine(DelayedSetup());
    }

    private void Update()
    {
        // Abre o Menu de Opções apertando ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionsWindow != null)
            {
                optionsWindow.SetActive(!optionsWindow.activeSelf);
                if (optionsWindow.activeSelf) optionsWindow.transform.SetAsLastSibling();
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
        playerCam.targetTexture = playerViewTex;

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
        diceText.fontSize = 80;
        diceText.color = Color.clear;
        diceText.alignment = TextAlignmentOptions.Center;
        diceText.fontStyle = FontStyles.Bold;
        diceText.outlineWidth = 0.2f;
        diceText.outlineColor = new Color32(0, 0, 0, 255);

        RectTransform rt = diceText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.15f); rt.anchorMax = new Vector2(0.5f, 0.15f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1200, 200);
        diceText.text = "";

        BuildFloatingWindow();
        BuildOptionsWindow(); // Constrói o menu de opções do Mestre
    }

    // ==========================================
    // SISTEMA DE OPÇÕES E MODO JANELA
    // ==========================================
    private void BuildOptionsWindow()
    {
        GameObject mainCanvasGO = GameObject.Find("MainCanvas");
        Canvas cv = mainCanvasGO != null ? mainCanvasGO.GetComponent<Canvas>() : FindAnyObjectByType<Canvas>();
        if (cv == null) return;

        optionsWindow = new GameObject("OptionsMenuWindow");
        RectTransform winRT = optionsWindow.AddComponent<RectTransform>();
        winRT.SetParent(cv.transform, false);
        winRT.sizeDelta = new Vector2(350f, 400f);
        winRT.anchorMin = new Vector2(0.5f, 0.5f); winRT.anchorMax = new Vector2(0.5f, 0.5f);
        winRT.pivot = new Vector2(0.5f, 0.5f);

        Image bg = optionsWindow.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.12f, 0.98f);
        VTTLayout.AccentBar(winRT, 4f, VTTLayout.C_ACCENT);

        GameObject titleGO = new GameObject("Title");
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.SetParent(winRT, false);
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -30f);
        titleRT.sizeDelta = new Vector2(0, 40f);
        TMP_Text titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "CONFIGURAÇÕES (ESC)";
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.fontSize = 20;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = Color.white;

        // Botões
        Button btnWin = BuildOptionBtn(winRT, "TELA: Modo Janela", new Vector2(0, 90));
        btnWin.onClick.AddListener(() => SetFullscreenMode(false));

        Button btnFull = BuildOptionBtn(winRT, "TELA: Tela Cheia", new Vector2(0, 40));
        btnFull.onClick.AddListener(() => SetFullscreenMode(true));

        Button btnDig = BuildOptionBtn(winRT, "MODO: Digital Sync", new Vector2(0, -30));
        btnDig.onClick.AddListener(() => SetVTTMode(VTTMode.Digital_Sync));

        Button btnPhys = BuildOptionBtn(winRT, "MODO: Mesa Física", new Vector2(0, -80));
        btnPhys.onClick.AddListener(() => SetVTTMode(VTTMode.Physical_Table));

        Button btnClose = BuildOptionBtn(winRT, "FECHAR", new Vector2(0, -150));
        btnClose.onClick.AddListener(() => optionsWindow.SetActive(false));
        btnClose.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f);

        optionsWindow.SetActive(false); // Começa escondido
    }

    private Button BuildOptionBtn(RectTransform parent, string text, Vector2 pos)
    {
        GameObject go = new GameObject("Btn_" + text);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(280f, 40f);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f);

        GameObject txtGo = new GameObject("Text");
        RectTransform txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.SetParent(rt, false);
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;

        TMP_Text t = txtGo.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.color = Color.white;
        t.fontSize = 16f;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;

        return go.AddComponent<Button>();
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
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        }
    }

    public void SetVTTMode(VTTMode newMode)
    {
        currentMode = newMode;
        Debug.Log("VTT Mode alterado para: " + newMode.ToString());
        if (optionsWindow != null) optionsWindow.SetActive(false);
    }

    // ==========================================
    // JANELA DE VISÃO FLUTUANTE
    // ==========================================
    private void BuildFloatingWindow()
    {
        GameObject mainCanvasGO = GameObject.Find("MainCanvas");
        Canvas cv = null;
        if (mainCanvasGO != null) cv = mainCanvasGO.GetComponent<Canvas>();
        if (cv == null) cv = FindAnyObjectByType<Canvas>();
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
        titleTxt.text = "VISÃO DOS JOGADORES (CAPTURA)";
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
            }
        });
        trigger.triggers.Add(entryDrag);

        Button btnClose = BuildWinControlBtn(hdrRT, "X", VTTLayout.C_BTN_CLOSE, 0f);
        btnClose.onClick.AddListener(CloseWindow);

        Button btnMax = BuildWinControlBtn(hdrRT, "□", VTTLayout.C_BTN_SEC, -40f);
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

        floatingWindow.SetActive(false);
    }

    private Button BuildWinControlBtn(RectTransform parent, string text, Color bgColor, float rightOffset)
    {
        GameObject go = new GameObject("WinBtn_" + text);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
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

    public void OpenWindow()
    {
        playerCam.targetTexture = playerViewTex;
        floatingWindow.SetActive(true);
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

    // ==========================================
    // EJETAR ALVO (ENVIO PARA O PROJETOR)
    // ==========================================
    public void SendToMonitor()
    {
        if (Display.displays.Length > 1 && targetDisplayIndex < Display.displays.Length)
        {
            // O uso de parâmetros no Activate() ajuda a Unity a não "sequestrar" todos os monitores
            Display.displays[targetDisplayIndex].Activate(1920, 1080, 60);

            playerCam.targetTexture = null;
            playerCam.targetDisplay = targetDisplayIndex;
            CloseWindow();
            Debug.Log("Sinal enviado para o " + GetCurrentDisplayName());

            // Garante que o monitor do mestre NÃO vá para tela cheia por acidente!
            StartCoroutine(EnforceScreenModeRoutine());
        }
        else
        {
            Debug.LogWarning("O monitor selecionado não está conectado no Windows!");
        }
    }

    private IEnumerator EnforceScreenModeRoutine()
    {
        // Espera alguns frames para a Unity processar a ativação do segundo monitor
        yield return new WaitForSeconds(0.2f);

        // Re-aplica a preferência do Mestre (ex: Modo Janela) na tela principal
        SetFullscreenMode(isFullscreenPref);
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
        diceText.color = new Color(0.9f, 0.8f, 0.2f, 1);
        yield return new WaitForSeconds(5f);

        float t = 1f;
        while (t > 0)
        {
            t -= Time.deltaTime;
            diceText.color = new Color(0.9f, 0.8f, 0.2f, t);
            yield return null;
        }
        diceText.text = "";
    }
}