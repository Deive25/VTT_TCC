// ============================================================
// GMUIController.cs
// Constrói e gerencia o painel lateral do Mestre.
//
// Construção 100% por código: sem prefabs extras.
// Comunica com os demais sistemas APENAS via MapEvents.
//
// Painel contém:
//   [IMPORTAR MAPA] — abre o file picker
//   Slider de escala
//   [CENTRALIZAR]   — centraliza mapa na câmera
//   [RESET ZOOM]    — reseta zoom para fit-to-screen
//   Informações: Dimensão | Escala | Coord. Mouse
//   Campo de caminho manual (fallback para todas as plataformas)
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro — veja a nota de setup

/// <summary>
/// Cria o painel da UI do Mestre via código.
/// Requer: Canvas + CanvasScaler + GraphicRaycaster no GameObject pai.
/// </summary>
public class GMUIController : MonoBehaviour
{
    // --------------------------------------------------------
    // Dependências
    // --------------------------------------------------------
    private MapController    mapController;
    private CoordinateSystem coordSystem;
    private CameraController cameraController;

    // --------------------------------------------------------
    // Elementos de UI
    // --------------------------------------------------------
    private Slider      scaleSlider;
    private TMP_Text    infoText;
    private TMP_InputField pathInputField;

    // --------------------------------------------------------
    // Constantes visuais
    // --------------------------------------------------------
    private const float PANEL_WIDTH    = 260f;
    private const float BUTTON_HEIGHT  = 40f;
    private const float PADDING        = 12f;
    private const float ELEMENT_GAP    = 8f;

    private readonly Color PANEL_BG_COLOR  = new Color(0.08f, 0.09f, 0.12f, 0.92f);
    private readonly Color BUTTON_COLOR    = new Color(0.20f, 0.45f, 0.80f, 1f);
    private readonly Color BUTTON_HOVER    = new Color(0.28f, 0.55f, 0.90f, 1f);
    private readonly Color SLIDER_COLOR    = new Color(0.28f, 0.55f, 0.90f, 1f);
    private readonly Color TEXT_COLOR      = new Color(0.85f, 0.88f, 0.95f, 1f);
    private readonly Color SUBTEXT_COLOR   = new Color(0.60f, 0.65f, 0.75f, 1f);
    private readonly Color SECTION_COLOR   = new Color(0.15f, 0.17f, 0.22f, 1f);
    private readonly Color DANGER_COLOR    = new Color(0.80f, 0.32f, 0.25f, 1f);

    // --------------------------------------------------------
    // Unity Lifecycle
    // --------------------------------------------------------
    private void Awake()
    {
        mapController    = FindFirstObjectByType<MapController>();
        coordSystem      = FindFirstObjectByType<CoordinateSystem>();
        cameraController = FindFirstObjectByType<CameraController>();
    }

    private void Start()
    {
        BuildUI();
    }

    private void OnEnable()
    {
        MapEvents.OnMapInfoUpdated += HandleMapInfoUpdated;
    }

    private void OnDisable()
    {
        MapEvents.OnMapInfoUpdated -= HandleMapInfoUpdated;
    }

    private void Update()
    {
        UpdateMouseCoord();
    }

    // --------------------------------------------------------
    // Construção da UI
    // --------------------------------------------------------

    private void BuildUI()
    {
        // Garante que há um Canvas na cena
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[GMUIController] Nenhum Canvas encontrado na cena!");
            return;
        }

        // --- Painel principal ---
        RectTransform panel = CreatePanel(canvas.transform);

        // --- Título ---
        float yOffset = -PADDING;
        CreateLabel(panel, "VTT — Tela do Mestre", new Vector2(PADDING, yOffset),
                    new Vector2(PANEL_WIDTH - PADDING * 2, 28f), 14f, TEXT_COLOR, FontStyles.Bold);
        yOffset -= 28f + ELEMENT_GAP;

        CreateDivider(panel, yOffset);
        yOffset -= 2f + ELEMENT_GAP * 2;

        // --- Seção: Mapa ---
        CreateSectionHeader(panel, "MAPA", yOffset);
        yOffset -= 22f + ELEMENT_GAP;

        Button importBtn = CreateButton(panel, "  ＋  Importar Mapa", yOffset, BUTTON_COLOR);
        importBtn.onClick.AddListener(OnImportMapClicked);
        yOffset -= BUTTON_HEIGHT + ELEMENT_GAP;

        // --- Campo de caminho manual ---
        CreateLabel(panel, "Caminho manual (fallback):", new Vector2(PADDING, yOffset),
                    new Vector2(PANEL_WIDTH - PADDING * 2, 18f), 10f, SUBTEXT_COLOR);
        yOffset -= 18f + 4f;

        pathInputField = CreateInputField(panel, "Ex: C:/Users/.../mapa.png", yOffset, 38f);
        yOffset -= 38f + ELEMENT_GAP;

        Button loadPathBtn = CreateButton(panel, "Carregar do Caminho", yOffset, SECTION_COLOR);
        loadPathBtn.onClick.AddListener(OnLoadFromPathClicked);
        yOffset -= BUTTON_HEIGHT + ELEMENT_GAP * 2;

        // --- Seção: Câmera ---
        CreateDivider(panel, yOffset);
        yOffset -= 2f + ELEMENT_GAP * 2;

        CreateSectionHeader(panel, "CÂMERA", yOffset);
        yOffset -= 22f + ELEMENT_GAP;

        // Escala do mapa
        CreateLabel(panel, "Escala do Mapa:", new Vector2(PADDING, yOffset),
                    new Vector2(PANEL_WIDTH - PADDING * 2, 18f), 11f, TEXT_COLOR);
        yOffset -= 18f + 4f;

        scaleSlider = CreateSlider(panel, yOffset,
            mapController != null ? mapController.MinScale : 0.1f,
            mapController != null ? mapController.MaxScale : 5f,
            1f);
        scaleSlider.onValueChanged.AddListener(OnScaleChanged);
        yOffset -= 28f + ELEMENT_GAP;

        Button centerBtn = CreateButton(panel, "⊹ Centralizar Mapa", yOffset, SECTION_COLOR);
        centerBtn.onClick.AddListener(OnCenterMapClicked);
        yOffset -= BUTTON_HEIGHT + ELEMENT_GAP;

        Button resetBtn = CreateButton(panel, "↺ Reset Zoom", yOffset, SECTION_COLOR);
        resetBtn.onClick.AddListener(OnResetZoomClicked);
        yOffset -= BUTTON_HEIGHT + ELEMENT_GAP * 2;

        // --- Seção: Informações ---
        CreateDivider(panel, yOffset);
        yOffset -= 2f + ELEMENT_GAP * 2;

        CreateSectionHeader(panel, "INFORMAÇÕES", yOffset);
        yOffset -= 22f + ELEMENT_GAP;

        // Info box
        float infoHeight = 90f;
        infoText = CreateInfoBox(panel, yOffset, infoHeight);
        yOffset -= infoHeight + ELEMENT_GAP;

        // Ajusta altura do painel
        float totalHeight = Mathf.Abs(yOffset) + PADDING;
        panel.sizeDelta = new Vector2(PANEL_WIDTH, totalHeight);
    }

    // --------------------------------------------------------
    // Fábrica de Elementos UI
    // --------------------------------------------------------

    private RectTransform CreatePanel(Transform canvasTransform)
    {
        GameObject panelObj = new GameObject("GM_Panel");
        panelObj.transform.SetParent(canvasTransform, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        // Ancora no lado direito, esticado verticalmente
        rt.anchorMin  = new Vector2(1f, 0f);
        rt.anchorMax  = new Vector2(1f, 1f);
        rt.pivot      = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(0f, 0f);
        rt.sizeDelta  = new Vector2(PANEL_WIDTH, 0f);

        // Fundo semitransparente
        Image img    = panelObj.AddComponent<Image>();
        img.color    = PANEL_BG_COLOR;

        // Previne que cliques passem para o mundo
        panelObj.AddComponent<GraphicRaycaster>();

        return rt;
    }

    private void CreateLabel(Transform parent, string text, Vector2 anchoredPos,
                              Vector2 size, float fontSize, Color color,
                              FontStyles fontStyle = FontStyles.Normal)
    {
        GameObject obj = new GameObject("Label_" + text.Substring(0, Mathf.Min(10, text.Length)));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;

        TMP_Text tmp       = obj.AddComponent<TextMeshProUGUI>();
        tmp.text           = text;
        tmp.fontSize       = fontSize;
        tmp.color          = color;
        tmp.fontStyle      = fontStyle;
        tmp.overflowMode   = TextOverflowModes.Ellipsis;
    }

    private void CreateSectionHeader(Transform parent, string text, float yOffset)
    {
        CreateLabel(parent, text,
            new Vector2(PADDING, yOffset),
            new Vector2(PANEL_WIDTH - PADDING * 2, 20f),
            10f, SUBTEXT_COLOR, FontStyles.Bold);
    }

    private void CreateDivider(Transform parent, float yOffset)
    {
        GameObject obj = new GameObject("Divider");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yOffset);
        rt.sizeDelta        = new Vector2(-PADDING * 2, 1f);

        Image img  = obj.AddComponent<Image>();
        img.color  = new Color(0.25f, 0.28f, 0.35f, 1f);
    }

    private Button CreateButton(Transform parent, string label, float yOffset, Color bgColor)
    {
        GameObject obj = new GameObject("Button_" + label);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yOffset);
        rt.sizeDelta        = new Vector2(-PADDING * 2, BUTTON_HEIGHT);

        Image img  = obj.AddComponent<Image>();
        img.color  = bgColor;

        Button btn  = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.15f);
        cb.pressedColor     = Color.Lerp(bgColor, Color.black, 0.2f);
        btn.colors          = cb;

        // Label do botão
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);

        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        TMP_Text tmp       = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text           = label;
        tmp.fontSize       = 12f;
        tmp.color          = TEXT_COLOR;
        tmp.alignment      = TextAlignmentOptions.Center;
        tmp.fontStyle      = FontStyles.Bold;

        return btn;
    }

    private Slider CreateSlider(Transform parent, float yOffset, float min, float max, float value)
    {
        GameObject obj = new GameObject("Slider_Scale");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yOffset);
        rt.sizeDelta        = new Vector2(-PADDING * 2, 20f);

        Slider slider      = obj.AddComponent<Slider>();
        slider.minValue    = min;
        slider.maxValue    = max;
        slider.value       = value;
        slider.wholeNumbers = false;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(obj.transform, false);
        RectTransform bgRT = bgObj.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0.25f);
        bgRT.anchorMax = new Vector2(1f, 0.75f);
        bgRT.sizeDelta = Vector2.zero;
        Image bgImg    = bgObj.AddComponent<Image>();
        bgImg.color    = new Color(0.18f, 0.20f, 0.26f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(obj.transform, false);
        RectTransform faRT  = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin      = new Vector2(0f, 0.25f);
        faRT.anchorMax      = new Vector2(1f, 0.75f);
        faRT.sizeDelta      = new Vector2(-10f, 0f);

        GameObject fill     = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRT   = fill.AddComponent<RectTransform>();
        fRT.sizeDelta       = Vector2.zero;
        Image fillImg       = fill.AddComponent<Image>();
        fillImg.color       = SLIDER_COLOR;

        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(obj.transform, false);
        RectTransform haRT = handleArea.AddComponent<RectTransform>();
        haRT.anchorMin     = Vector2.zero;
        haRT.anchorMax     = Vector2.one;
        haRT.sizeDelta     = new Vector2(-10f, 0f);

        GameObject handle    = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hRT    = handle.AddComponent<RectTransform>();
        hRT.sizeDelta        = new Vector2(16f, 16f);
        Image handleImg      = handle.AddComponent<Image>();
        handleImg.color      = Color.white;

        slider.fillRect      = fRT;
        slider.handleRect    = hRT;
        slider.targetGraphic = handleImg;

        return slider;
    }

    private TMP_InputField CreateInputField(Transform parent, string placeholder, float yOffset, float height)
    {
        GameObject obj = new GameObject("InputField");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yOffset);
        rt.sizeDelta        = new Vector2(-PADDING * 2, height);

        Image bg   = obj.AddComponent<Image>();
        bg.color   = new Color(0.14f, 0.16f, 0.20f, 1f);

        TMP_InputField inputField = obj.AddComponent<TMP_InputField>();

        // Text area
        GameObject textAreaObj = new GameObject("Text Area");
        textAreaObj.transform.SetParent(obj.transform, false);
        RectTransform taRT = textAreaObj.AddComponent<RectTransform>();
        taRT.anchorMin     = Vector2.zero;
        taRT.anchorMax     = Vector2.one;
        taRT.sizeDelta     = new Vector2(-8f, -4f);
        textAreaObj.AddComponent<RectMask2D>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textAreaObj.transform, false);
        RectTransform tRT  = textObj.AddComponent<RectTransform>();
        tRT.anchorMin      = Vector2.zero;
        tRT.anchorMax      = Vector2.one;
        tRT.sizeDelta      = Vector2.zero;
        TMP_Text tmp       = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize       = 10f;
        tmp.color          = TEXT_COLOR;

        GameObject phObj   = new GameObject("Placeholder");
        phObj.transform.SetParent(textAreaObj.transform, false);
        RectTransform phRT = phObj.AddComponent<RectTransform>();
        phRT.anchorMin     = Vector2.zero;
        phRT.anchorMax     = Vector2.one;
        phRT.sizeDelta     = Vector2.zero;
        TMP_Text phTmp     = phObj.AddComponent<TextMeshProUGUI>();
        phTmp.text         = placeholder;
        phTmp.fontSize     = 10f;
        phTmp.color        = SUBTEXT_COLOR;
        phTmp.fontStyle    = FontStyles.Italic;

        inputField.textComponent = tmp;
        inputField.placeholder   = phTmp;
        inputField.textViewport  = taRT;

        return inputField;
    }

    private TMP_Text CreateInfoBox(Transform parent, float yOffset, float height)
    {
        GameObject boxObj = new GameObject("InfoBox");
        boxObj.transform.SetParent(parent, false);

        RectTransform rt = boxObj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yOffset);
        rt.sizeDelta        = new Vector2(-PADDING * 2, height);

        Image img  = boxObj.AddComponent<Image>();
        img.color  = SECTION_COLOR;

        GameObject textObj = new GameObject("InfoText");
        textObj.transform.SetParent(boxObj.transform, false);
        RectTransform tRT  = textObj.AddComponent<RectTransform>();
        tRT.anchorMin      = Vector2.zero;
        tRT.anchorMax      = Vector2.one;
        tRT.sizeDelta      = new Vector2(-10f, -8f);
        tRT.anchoredPosition = Vector2.zero;

        TMP_Text tmp       = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize       = 10.5f;
        tmp.color          = TEXT_COLOR;
        tmp.text           = GetDefaultInfoText();
        tmp.lineSpacing    = 4f;

        return tmp;
    }

    // --------------------------------------------------------
    // Atualização de Dados
    // --------------------------------------------------------

    private void HandleMapInfoUpdated(MapInfo info)
    {
        if (scaleSlider != null)
        {
            scaleSlider.SetValueWithoutNotify(info.scale);
        }

        if (infoText != null && info.isLoaded)
        {
            UpdateInfoText(info);
        }
    }

    private void UpdateMouseCoord()
    {
        if (coordSystem == null || infoText == null) return;

        Vector2? normalized = coordSystem.GetMouseNormalized();

        if (mapController == null || !mapController.IsMapLoaded) return;

        MapInfo info = new MapInfo
        {
            widthPx   = mapController != null ? 0 : 0,
            heightPx  = 0,
            scale     = mapController != null ? mapController.CurrentScale : 1f,
            mouseNormalized = normalized,
            isLoaded  = mapController != null && mapController.IsMapLoaded
        };

        if (info.isLoaded)
            UpdateInfoText(info);
    }

    private void UpdateInfoText(MapInfo info)
    {
        string mouseStr = info.mouseNormalized.HasValue
            ? $"({info.mouseNormalized.Value.x:F3}, {info.mouseNormalized.Value.y:F3})"
            : "—  fora do mapa";

        if (infoText == null) return;

        infoText.text =
            $"<color=#8899BB>Dimensão:</color> {info.widthPx} × {info.heightPx} px\n" +
            $"<color=#8899BB>Escala atual:</color> {info.scale:F3}\n" +
            $"<color=#8899BB>Mouse (norm):</color> {mouseStr}";
    }

    private string GetDefaultInfoText()
    {
        return "<color=#8899BB>Dimensão:</color>  —\n" +
               "<color=#8899BB>Escala atual:</color>  —\n" +
               "<color=#8899BB>Mouse (norm):</color>  —";
    }

    // --------------------------------------------------------
    // Handlers de Botões
    // --------------------------------------------------------

    private void OnImportMapClicked()
    {
        if (MapFileLoader.Instance != null)
            MapFileLoader.Instance.OpenFilePicker();
        else
            Debug.LogError("[GMUIController] MapFileLoader.Instance é nulo.");
    }

    private void OnLoadFromPathClicked()
    {
        if (pathInputField == null || MapFileLoader.Instance == null) return;

        string path = pathInputField.text.Trim();
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[GMUIController] Caminho está vazio.");
            return;
        }

        MapFileLoader.Instance.LoadFromPath(path);
    }

    private void OnScaleChanged(float value)
    {
        MapEvents.FireScaleChangeRequested(value);
    }

    private void OnCenterMapClicked()
    {
        MapEvents.FireCenterMapRequested();
    }

    private void OnResetZoomClicked()
    {
        // Notifica o MapController de resetar escala
        MapEvents.FireResetZoomRequested();

        // E pede à câmera para enquadrar o tabuleiro
        if (cameraController != null)
            cameraController.FocusOnActiveBoard();
    }
}
