// ============================================================
// DashboardOverlay.cs
// Ecrã inteiro de Gerenciamento de Personagens (Inspirado na ref.)
// Layout Responsivo: O Grid adapta-se ao tamanho da janela.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashboardOverlay : MonoBehaviour
{
    public static DashboardOverlay Instance { get; private set; }

    private GameObject _mainScreen;
    private RectTransform _gridRT;

    // --- Paleta de Cores Inspirada na Referência ---
    private static readonly Color C_BG_SCREEN = new Color(0.92f, 0.93f, 0.95f, 1f); // Fundo cinza muito claro
    private static readonly Color C_HEADER = new Color(0.12f, 0.14f, 0.18f, 1f); // Cabeçalho escuro para contraste
    private static readonly Color C_CARD_BG = new Color(1.00f, 1.00f, 1.00f, 1f); // Cartões brancos
    private static readonly Color C_TEXT_DARK = new Color(0.15f, 0.15f, 0.15f, 1f); // Texto principal escuro
    private static readonly Color C_TEXT_DIM = new Color(0.40f, 0.45f, 0.50f, 1f); // Texto secundário (Level)
    private static readonly Color C_ACCENT = new Color(0.90f, 0.15f, 0.40f, 1f); // Rosa/Magenta de destaque
    private static readonly Color C_AVATAR_BG = new Color(0.85f, 0.86f, 0.88f, 1f); // Fundo circular do avatar

    private void Awake()
    {
        Instance = this;
        BuildFullScreenUI();
        _mainScreen.SetActive(false);
    }

    private void BuildFullScreenUI()
    {
        Canvas cv = GetComponent<Canvas>();
        if (cv == null) cv = FindAnyObjectByType<Canvas>();

        // 1. Painel Base (Ocupa 100% do ecrã)
        _mainScreen = VTTLayout.New("DashboardScreen", cv.transform);
        RectTransform screenRT = _mainScreen.AddComponent<RectTransform>();
        screenRT.anchorMin = Vector2.zero; screenRT.anchorMax = Vector2.one;
        screenRT.sizeDelta = Vector2.zero;

        Image bgImg = _mainScreen.AddComponent<Image>();
        bgImg.color = C_BG_SCREEN;
        bgImg.raycastTarget = true; // Bloqueia cliques para o mapa atrás

        // 2. Cabeçalho (Header Escuro no Topo)
        float headerH = 70f;
        GameObject header = VTTLayout.New("Header", screenRT);
        RectTransform headerRT = header.AddComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1); headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0, headerH);

        Image headerImg = header.AddComponent<Image>();
        headerImg.color = C_HEADER;

        // Título no Cabeçalho
        TMP_Text title = MakeText(headerRT, "PAINEL DA CAMPANHA", 22f, Color.white, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        title.rectTransform.anchorMin = new Vector2(0, 0); title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.offsetMin = new Vector2(40f, 0); // Padding esquerdo

        // Botão "VOLTAR AO MAPA" no Cabeçalho (Direita)
        Button btnClose = MakeButton(headerRT, "VOLTAR AO MAPA", C_ACCENT, Color.white, 14f, true);
        RectTransform closeRT = btnClose.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 0.5f); closeRT.anchorMax = new Vector2(1, 0.5f);
        closeRT.pivot = new Vector2(1, 0.5f);
        closeRT.anchoredPosition = new Vector2(-40f, 0f);
        closeRT.sizeDelta = new Vector2(180f, 40f);
        btnClose.onClick.AddListener(ClosePanel);

        // 3. Área de Scroll Principal (Ocupa o resto do ecrã)
        RectTransform contentRT;
        ScrollRect scroll = VTTLayout.MakeScrollView("MainScroll", screenRT, 0, -headerH, 0, 0, out contentRT);
        RectTransform scrollRT = scroll.GetComponent<RectTransform>();
        // Esticar o scrollview
        scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMax = new Vector2(0, -headerH);
        scrollRT.offsetMin = Vector2.zero;

        // Adiciona um Layout Vertical ao Content para empilhar seções
        VerticalLayoutGroup vLayout = contentRT.gameObject.AddComponent<VerticalLayoutGroup>();
        vLayout.padding = new RectOffset(40, 40, 40, 40); // Margens grandes e arejadas
        vLayout.spacing = 30f;
        vLayout.childControlHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentRT.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- SEÇÃO 1: TÍTULO DOS PERSONAGENS ---
        GameObject sec1Title = VTTLayout.New("Sec1Title", contentRT);
        sec1Title.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 30f);
        MakeText(sec1Title.GetComponent<RectTransform>(), "PERSONAGENS E FICHAS", 16f, C_ACCENT, FontStyles.Bold, TextAlignmentOptions.BottomLeft)
            .rectTransform.sizeDelta = new Vector2(0, 30f);

        // --- SEÇÃO 2: GRID DE PERSONAGENS RESPONSIVO ---
        GameObject gridObj = VTTLayout.New("GridCharacters", contentRT);
        _gridRT = gridObj.AddComponent<RectTransform>();

        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(320f, 130f); // Tamanho ideal de cada cartão
        grid.spacing = new Vector2(25f, 25f);    // Espaçamento entre cartões
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        // --- POPULAR O GRID DE EXEMPLO ---
        BuildCharacterCard(_gridRT, "Kai", "Elfo • Nível 4", "27 / 31", "9m / 30ft");
        BuildCharacterCard(_gridRT, "Demi", "Anão • Nível 3", "40 / 47", "5m / 16ft");
        BuildCharacterCard(_gridRT, "Queiroz", "Humano • Nível 4", "36 / 50", "7m / 22ft");
        BuildCharacterCard(_gridRT, "Olívia", "Meio-Elfo • Nível 5", "34 / 42", "9m / 30ft");
        BuildCharacterCard(_gridRT, "Henrique", "Monge • Nível 4", "27 / 28", "10m / 32ft");
        BuildAddCard(_gridRT);

        // --- SEÇÃO 3: OUTRAS FUNCIONALIDADES ---
        GameObject sec2Title = VTTLayout.New("Sec2Title", contentRT);
        sec2Title.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 30f);
        MakeText(sec2Title.GetComponent<RectTransform>(), "HABILIDADES E ATRIBUTOS (EM BREVE)", 16f, C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.BottomLeft)
            .rectTransform.sizeDelta = new Vector2(0, 30f);
    }

    // --- CONSTRUTOR DO CARTÃO DE PERSONAGEM ---
    private void BuildCharacterCard(RectTransform parent, string name, string subText, string hp, string mov)
    {
        GameObject card = VTTLayout.New("Card_" + name, parent);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        Image bg = card.AddComponent<Image>();
        bg.color = C_CARD_BG;

        // Borda ou Sombra subtil (Simulada com Outline)
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.05f);
        outline.effectDistance = new Vector2(2, -2);

        // Avatar Circular (Simulado com uma imagem com máscara, aqui usaremos apenas uma cor base para simular)
        GameObject avatar = VTTLayout.New("Avatar", cardRT);
        RectTransform avRT = avatar.AddComponent<RectTransform>();
        avRT.anchorMin = new Vector2(0, 0.5f); avRT.anchorMax = new Vector2(0, 0.5f);
        avRT.pivot = new Vector2(0, 0.5f);
        avRT.anchoredPosition = new Vector2(20f, 0f);
        avRT.sizeDelta = new Vector2(90f, 90f);
        Image avImg = avatar.AddComponent<Image>();
        avImg.color = C_AVATAR_BG;

        // Container de Texto à direita do Avatar
        float textX = 130f;

        // Nome
        TMP_Text nameTxt = MakeText(cardRT, name, 20f, C_TEXT_DARK, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
        nameTxt.rectTransform.anchorMin = new Vector2(0, 1); nameTxt.rectTransform.anchorMax = new Vector2(1, 1);
        nameTxt.rectTransform.pivot = new Vector2(0, 1);
        nameTxt.rectTransform.anchoredPosition = new Vector2(textX, -20f);
        nameTxt.rectTransform.sizeDelta = new Vector2(-140f, 30f);

        // Classe / Nível
        TMP_Text lvlTxt = MakeText(cardRT, subText, 13f, C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        lvlTxt.rectTransform.anchorMin = new Vector2(0, 1); lvlTxt.rectTransform.anchorMax = new Vector2(1, 1);
        lvlTxt.rectTransform.pivot = new Vector2(0, 1);
        lvlTxt.rectTransform.anchoredPosition = new Vector2(textX, -50f);
        lvlTxt.rectTransform.sizeDelta = new Vector2(-140f, 20f);

        // Estatísticas (HP e MOV em Rosa)
        TMP_Text statsTxt = MakeText(cardRT, $"HP: <color=#E62565>{hp}</color>   MOV: <color=#E62565>{mov}</color>", 13f, C_TEXT_DARK, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        statsTxt.rectTransform.anchorMin = new Vector2(0, 1); statsTxt.rectTransform.anchorMax = new Vector2(1, 1);
        statsTxt.rectTransform.pivot = new Vector2(0, 1);
        statsTxt.rectTransform.anchoredPosition = new Vector2(textX, -85f);
        statsTxt.rectTransform.sizeDelta = new Vector2(-140f, 20f);

        // Botão Lápis/Editar (Canto Superior Direito)
        Button btnEdit = MakeButton(cardRT, "✎", Color.clear, C_TEXT_DIM, 16f, false);
        RectTransform editRT = btnEdit.GetComponent<RectTransform>();
        editRT.anchorMin = new Vector2(1, 1); editRT.anchorMax = new Vector2(1, 1);
        editRT.pivot = new Vector2(1, 1);
        editRT.anchoredPosition = new Vector2(-10f, -10f);
        editRT.sizeDelta = new Vector2(30f, 30f);

        // Botão Eliminar (Canto Inferior Direito)
        Button btnDel = MakeButton(cardRT, "X", Color.clear, C_ACCENT, 16f, true);
        RectTransform delRT = btnDel.GetComponent<RectTransform>();
        delRT.anchorMin = new Vector2(1, 0); delRT.anchorMax = new Vector2(1, 0);
        delRT.pivot = new Vector2(1, 0);
        delRT.anchoredPosition = new Vector2(-10f, 10f);
        delRT.sizeDelta = new Vector2(30f, 30f);
    }

    // --- CONSTRUTOR DO CARTÃO DE ADICIONAR (+) ---
    private void BuildAddCard(RectTransform parent)
    {
        GameObject card = VTTLayout.New("Card_Add", parent);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.88f, 0.89f, 0.91f, 1f); // Fundo ligeiramente mais escuro que a tela para destacar o botão

        Button btn = card.AddComponent<Button>();
        btn.targetGraphic = bg;
        // Permite clicar na carta toda para adicionar um personagem

        // Círculo central do +
        GameObject circle = VTTLayout.New("CircleBtn", cardRT);
        RectTransform cRT = circle.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.5f, 0.5f); cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot = new Vector2(0.5f, 0.5f);
        cRT.sizeDelta = new Vector2(60f, 60f);
        Image cImg = circle.AddComponent<Image>();
        cImg.color = C_CARD_BG;

        TMP_Text plusTxt = MakeText(cRT, "+", 36f, C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.Center);
        plusTxt.rectTransform.anchorMin = Vector2.zero; plusTxt.rectTransform.anchorMax = Vector2.one;
        plusTxt.rectTransform.sizeDelta = Vector2.zero;
    }

    // --- MÉTODOS UTILITÁRIOS ---

    private TMP_Text MakeText(RectTransform parent, string text, float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        GameObject go = VTTLayout.New("Text", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        // Ancoragem padrão de preenchimento completo (quem chama ajusta os offsets)
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = align;
        t.raycastTarget = false;
        t.richText = true; // Permite colorir partes específicas do texto (como o HP)
        return t;
    }

    private Button MakeButton(RectTransform parent, string label, Color bg, Color textColor, float fontSize, bool bold)
    {
        GameObject go = VTTLayout.New("Btn", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        Image img = go.AddComponent<Image>();
        img.color = bg;
        img.raycastTarget = true;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        TMP_Text t = MakeText(rt, label, fontSize, textColor, bold ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.Center);
        return btn;
    }

    public void OpenPanel()
    {
        _mainScreen.SetActive(true);
        _mainScreen.transform.SetAsLastSibling(); // Garante que sobrepõe tudo (UI lateral, mapa, etc)

        // Força a atualização do layout do Grid para evitar bugs visuais no primeiro frame
        Canvas.ForceUpdateCanvases();
    }

    public void ClosePanel()
    {
        _mainScreen.SetActive(false);
    }
}