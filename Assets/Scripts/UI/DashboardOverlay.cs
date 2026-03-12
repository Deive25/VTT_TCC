// ============================================================
// DashboardOverlay.cs
// Ecrã inteiro de Gerenciamento de Personagens.
// Agora recebe strings de status formatadas dinamicamente.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashboardOverlay : MonoBehaviour
{
    public static DashboardOverlay Instance { get; private set; }

    private GameObject _mainScreen;
    private RectTransform _gridRT;
    private GridLayoutGroup _gridLayout;
    private RectTransform _contentRT;

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

        _mainScreen = VTTLayout.New("DashboardScreen", cv.transform);
        RectTransform screenRT = _mainScreen.AddComponent<RectTransform>();
        screenRT.anchorMin = Vector2.zero; screenRT.anchorMax = Vector2.one;
        screenRT.sizeDelta = Vector2.zero;
        Image bgImg = _mainScreen.AddComponent<Image>();
        bgImg.color = VTTLayout.C_BG;
        bgImg.raycastTarget = true;

        float headerH = 60f;
        GameObject header = VTTLayout.New("Header", screenRT);
        RectTransform headerRT = header.AddComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1); headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0, headerH);

        Image headerImg = header.AddComponent<Image>();
        headerImg.color = VTTLayout.C_HDR_BG;
        VTTLayout.AccentBar(headerRT, 4f, VTTLayout.C_ACCENT);

        TMP_Text title = MakeText(headerRT, "PAINEL DA CAMPANHA", 22f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        title.rectTransform.anchorMin = new Vector2(0, 0); title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.offsetMin = new Vector2(40f, 0);

        Button btnClose = MakeButton(headerRT, "VOLTAR AO MAPA", VTTLayout.C_BTN_CLOSE, VTTLayout.C_TEXT, 14f, true);
        RectTransform closeRT = btnClose.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 0.5f); closeRT.anchorMax = new Vector2(1, 0.5f);
        closeRT.pivot = new Vector2(1, 0.5f);
        closeRT.anchoredPosition = new Vector2(-40f, 0f);
        closeRT.sizeDelta = new Vector2(180f, 36f);
        btnClose.onClick.AddListener(ClosePanel);

        ScrollRect scroll = VTTLayout.MakeScrollView("MainScroll", screenRT, 0, -headerH, 0, 0, out _contentRT);
        RectTransform scrollRT = scroll.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMax = new Vector2(0, -headerH);
        scrollRT.offsetMin = Vector2.zero;

        VerticalLayoutGroup vLayout = _contentRT.gameObject.AddComponent<VerticalLayoutGroup>();
        vLayout.padding = new RectOffset(40, 40, 40, 40);
        vLayout.spacing = 30f;
        vLayout.childControlHeight = true;
        vLayout.childControlWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childForceExpandWidth = true;

        ContentSizeFitter fitter = _contentRT.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject sec1Title = VTTLayout.New("Sec1Title", _contentRT);
        LayoutElement le1 = sec1Title.AddComponent<LayoutElement>();
        le1.minHeight = 30f;
        RectTransform sec1RT = sec1Title.GetComponent<RectTransform>();

        GameObject bar1 = VTTLayout.New("Bar", sec1RT);
        RectTransform bar1RT = bar1.AddComponent<RectTransform>();
        bar1RT.anchorMin = new Vector2(0, 0.5f); bar1RT.anchorMax = new Vector2(0, 0.5f);
        bar1RT.pivot = new Vector2(0, 0.5f);
        bar1RT.anchoredPosition = Vector2.zero;
        bar1RT.sizeDelta = new Vector2(4f, 18f);
        VTTLayout.Deco(bar1, VTTLayout.C_ACCENT);

        TMP_Text t1 = MakeText(sec1RT, "PERSONAGENS E FICHAS", 15f, VTTLayout.C_TEXT_HDR, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        t1.rectTransform.offsetMin = new Vector2(12f, 0);

        GameObject gridObj = VTTLayout.New("GridCharacters", _contentRT);
        _gridRT = gridObj.AddComponent<RectTransform>();

        _gridLayout = gridObj.AddComponent<GridLayoutGroup>();
        // ALARGADO: Cartão passou de 320f para 380f de largura para caber os status de Ordem Paranormal.
        _gridLayout.cellSize = new Vector2(380f, 130f);
        _gridLayout.spacing = new Vector2(25f, 25f);
        _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        _gridLayout.childAlignment = TextAnchor.UpperLeft;

        gridObj.AddComponent<LayoutElement>();

        BuildAddCard(_gridRT);

        GameObject sec2Title = VTTLayout.New("Sec2Title", _contentRT);
        LayoutElement le2 = sec2Title.AddComponent<LayoutElement>();
        le2.minHeight = 30f;
        RectTransform sec2RT = sec2Title.GetComponent<RectTransform>();

        GameObject bar2 = VTTLayout.New("Bar", sec2RT);
        RectTransform bar2RT = bar2.AddComponent<RectTransform>();
        bar2RT.anchorMin = new Vector2(0, 0.5f); bar2RT.anchorMax = new Vector2(0, 0.5f);
        bar2RT.pivot = new Vector2(0, 0.5f);
        bar2RT.anchoredPosition = Vector2.zero;
        bar2RT.sizeDelta = new Vector2(4f, 18f);
        VTTLayout.Deco(bar2, VTTLayout.C_ACCENT);

        TMP_Text t2 = MakeText(sec2RT, "HABILIDADES E ATRIBUTOS (EM BREVE)", 15f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        t2.rectTransform.offsetMin = new Vector2(12f, 0);
    }


    // --- NOVA ASSINATURA: statsFormatado em vez de HP/MOV ---
    public void AddCharacter(string name, string subText, string statsFormatado, Texture2D avatarTex)
    {
        BuildCharacterCard(_gridRT, name, subText, statsFormatado, avatarTex);

        Transform addBtn = _gridRT.Find("Card_Add");
        if (addBtn != null) addBtn.SetAsLastSibling();
    }

    private void BuildCharacterCard(RectTransform parent, string name, string subText, string statsFormatado, Texture2D avatarTex)
    {
        GameObject card = VTTLayout.New("Card_" + name, parent);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        Image bg = card.AddComponent<Image>();
        bg.color = VTTLayout.C_SEC_BG;

        VTTLayout.AccentBar(cardRT, 4f, VTTLayout.C_ACCENT);

        Image avImg = VTTLayout.MakeMaskedAvatar(cardRT, new Vector2(20f, 0f), new Vector2(90f, 90f), VTTLayout.C_CONTENT_BG);
        if (avatarTex != null)
        {
            avImg.sprite = Sprite.Create(avatarTex, new Rect(0, 0, avatarTex.width, avatarTex.height), new Vector2(0.5f, 0.5f));
            avImg.color = Color.white;
        }

        float textX = 130f;

        TMP_Text nameTxt = MakeText(cardRT, name, 20f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
        nameTxt.rectTransform.anchorMin = new Vector2(0, 1); nameTxt.rectTransform.anchorMax = new Vector2(1, 1);
        nameTxt.rectTransform.pivot = new Vector2(0, 1);
        nameTxt.rectTransform.anchoredPosition = new Vector2(textX, -20f);
        nameTxt.rectTransform.sizeDelta = new Vector2(-140f, 30f);

        TMP_Text lvlTxt = MakeText(cardRT, subText, 13f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        lvlTxt.rectTransform.anchorMin = new Vector2(0, 1); lvlTxt.rectTransform.anchorMax = new Vector2(1, 1);
        lvlTxt.rectTransform.pivot = new Vector2(0, 1);
        lvlTxt.rectTransform.anchoredPosition = new Vector2(textX, -50f);
        lvlTxt.rectTransform.sizeDelta = new Vector2(-140f, 20f);

        // O Texto Dinâmico de Stats (Fonte tamanho 12f para caber todos os stats)
        TMP_Text statsTxt = MakeText(cardRT, statsFormatado, 12f, VTTLayout.C_TEXT, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        statsTxt.rectTransform.anchorMin = new Vector2(0, 1); statsTxt.rectTransform.anchorMax = new Vector2(1, 1);
        statsTxt.rectTransform.pivot = new Vector2(0, 1);
        statsTxt.rectTransform.anchoredPosition = new Vector2(textX, -85f);
        statsTxt.rectTransform.sizeDelta = new Vector2(-140f, 20f);

        Button btnEdit = MakeButton(cardRT, "E", VTTLayout.C_BTN_SEC, VTTLayout.C_TEXT, 14f, true);
        RectTransform editRT = btnEdit.GetComponent<RectTransform>();
        editRT.anchorMin = new Vector2(1, 1); editRT.anchorMax = new Vector2(1, 1);
        editRT.pivot = new Vector2(1, 1);
        editRT.anchoredPosition = new Vector2(-10f, -10f);
        editRT.sizeDelta = new Vector2(30f, 30f);

        Button btnDel = MakeButton(cardRT, "X", VTTLayout.C_BTN_CLOSE, VTTLayout.C_TEXT, 14f, true);
        RectTransform delRT = btnDel.GetComponent<RectTransform>();
        delRT.anchorMin = new Vector2(1, 0); delRT.anchorMax = new Vector2(1, 0);
        delRT.pivot = new Vector2(1, 0);
        delRT.anchoredPosition = new Vector2(-10f, 10f);
        delRT.sizeDelta = new Vector2(30f, 30f);

        btnDel.onClick.AddListener(() => Destroy(card));
    }

    private void BuildAddCard(RectTransform parent)
    {
        GameObject card = VTTLayout.New("Card_Add", parent);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        Image bg = card.AddComponent<Image>();
        bg.color = VTTLayout.C_CONTENT_BG;

        Button btn = card.AddComponent<Button>();
        btn.targetGraphic = bg;

        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = bg.color;
        cb.highlightedColor = VTTLayout.Highlight(bg.color);
        cb.pressedColor = VTTLayout.Pressed(bg.color);
        cb.selectedColor = bg.color;
        btn.colors = cb;
        card.AddComponent<ButtonFeedback>();

        btn.onClick.AddListener(() => {
            SystemSelectorOverlay sso = FindAnyObjectByType<SystemSelectorOverlay>();
            if (sso != null) sso.OpenPanel();
            else Debug.LogWarning("SystemSelectorOverlay não encontrado na cena!");
        });

        GameObject circle = VTTLayout.New("CircleBtn", cardRT);
        RectTransform cRT = circle.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.5f, 0.5f); cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot = new Vector2(0.5f, 0.5f);
        cRT.sizeDelta = new Vector2(60f, 60f);
        Image cImg = circle.AddComponent<Image>();
        cImg.color = VTTLayout.C_SEC_BG;

        TMP_Text plusTxt = MakeText(cRT, "+", 36f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.Center);
        plusTxt.rectTransform.anchorMin = Vector2.zero; plusTxt.rectTransform.anchorMax = Vector2.one;
        plusTxt.rectTransform.sizeDelta = Vector2.zero;
    }

    private TMP_Text MakeText(RectTransform parent, string text, float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        GameObject go = VTTLayout.New("Text", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = align; t.raycastTarget = false; t.richText = true;
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

        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = bg; cb.highlightedColor = VTTLayout.Highlight(bg);
        cb.pressedColor = VTTLayout.Pressed(bg); cb.selectedColor = bg;
        btn.colors = cb;
        go.AddComponent<ButtonFeedback>();

        TMP_Text t = MakeText(rt, label, fontSize, textColor, bold ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.Center);
        return btn;
    }

    public void OpenPanel()
    {
        _mainScreen.SetActive(true);
        _mainScreen.transform.SetAsLastSibling();
        RefreshDashboard(); // NOVO: Carrega os dados gravados ao abrir
    }

    public void ClosePanel()
    {
        _mainScreen.SetActive(false);
    }

    public void RefreshDashboard()
    {
        if (_gridRT == null || CharacterManager.Instance == null) return;

        foreach (Transform child in _gridRT)
        {
            if (child.name != "Card_Add") Destroy(child.gameObject);
        }

        foreach (var record in CharacterManager.Instance.Database.records)
        {
            Texture2D tex = CharacterManager.Instance.LoadAvatar(record.avatarFileName);
            BuildCharacterCard(_gridRT, record, tex);
        }

        Transform addBtn = _gridRT.Find("Card_Add");
        if (addBtn != null) addBtn.SetAsLastSibling();

        // CORREÇÃO: Calcula a altura da grid UMA vez só, em vez de 60 vezes por segundo no Update
        UpdateGridHeight();
    }

    private void UpdateGridHeight()
    {
        Canvas.ForceUpdateCanvases();
        int childCount = _gridRT.childCount;
        float width = _gridRT.rect.width;

        if (width > 0 && childCount > 0)
        {
            int columns = Mathf.FloorToInt((width + _gridLayout.spacing.x) / (_gridLayout.cellSize.x + _gridLayout.spacing.x));
            if (columns < 1) columns = 1;
            int rows = Mathf.CeilToInt((float)childCount / columns);
            float requiredHeight = (rows * _gridLayout.cellSize.y) + ((rows - 1) * _gridLayout.spacing.y);

            LayoutElement le = _gridRT.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.minHeight = requiredHeight;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRT);
            }
        }
    }


    private void BuildCharacterCard(RectTransform parent, CharacterRecord record, Texture2D avatarTex)
    {
        GameObject card = VTTLayout.New("Card_" + record.id, parent);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        Image bg = card.AddComponent<Image>();
        bg.color = VTTLayout.C_SEC_BG;

        Color barColor = record.system == "D&D 5e" ? VTTLayout.C_TEXT_GOLD : new Color(0.85f, 0.25f, 0.25f, 1f);
        VTTLayout.AccentBar(cardRT, 4f, barColor);

        Image avImg = VTTLayout.MakeMaskedAvatar(cardRT, new Vector2(20f, 0f), new Vector2(90f, 90f), VTTLayout.C_CONTENT_BG);

        // --- CORREÇÃO DE VAZAMENTO ---
        if (avatarTex != null)
        {
            Sprite newSprite = Sprite.Create(avatarTex, new Rect(0, 0, avatarTex.width, avatarTex.height), new Vector2(0.5f, 0.5f));
            avImg.sprite = newSprite;
            avImg.color = Color.white;

            SpriteCleanup cleanup = card.AddComponent<SpriteCleanup>();
            cleanup.spriteToDestroy = newSprite;
        }

        float textX = 130f;

        TMP_Text nameTxt = MakeText(cardRT, record.name, 20f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
        nameTxt.rectTransform.anchorMin = new Vector2(0, 1); nameTxt.rectTransform.anchorMax = new Vector2(1, 1);
        nameTxt.rectTransform.pivot = new Vector2(0, 1);
        nameTxt.rectTransform.anchoredPosition = new Vector2(textX, -20f);
        nameTxt.rectTransform.sizeDelta = new Vector2(-140f, 30f);

        TMP_Text lvlTxt = MakeText(cardRT, record.subText, 13f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        lvlTxt.rectTransform.anchorMin = new Vector2(0, 1); lvlTxt.rectTransform.anchorMax = new Vector2(1, 1);
        lvlTxt.rectTransform.pivot = new Vector2(0, 1);
        lvlTxt.rectTransform.anchoredPosition = new Vector2(textX, -50f);
        lvlTxt.rectTransform.sizeDelta = new Vector2(-140f, 20f);

        TMP_Text statsTxt = MakeText(cardRT, record.statsStr, 12f, VTTLayout.C_TEXT, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        statsTxt.rectTransform.anchorMin = new Vector2(0, 1); statsTxt.rectTransform.anchorMax = new Vector2(1, 1);
        statsTxt.rectTransform.pivot = new Vector2(0, 1);
        statsTxt.rectTransform.anchoredPosition = new Vector2(textX, -85f);
        statsTxt.rectTransform.sizeDelta = new Vector2(-140f, 20f);

        // --- BOTAO EDITAR AGORA ABRE A FICHA ---
        Button btnEdit = MakeButton(cardRT, "E", VTTLayout.C_BTN_SEC, VTTLayout.C_TEXT, 14f, true);
        RectTransform editRT = btnEdit.GetComponent<RectTransform>();
        editRT.anchorMin = new Vector2(1, 1); editRT.anchorMax = new Vector2(1, 1);
        editRT.pivot = new Vector2(1, 1);
        editRT.anchoredPosition = new Vector2(-10f, -10f);
        editRT.sizeDelta = new Vector2(30f, 30f);

        CharacterRecord targetRecord = record; // Captura para o clique
        btnEdit.onClick.AddListener(() => {
            CharacterCreatorScreen.Instance.OpenForEdit(targetRecord);
        });

        // --- BOTAO DELETAR ---
        Button btnDel = MakeButton(cardRT, "X", VTTLayout.C_BTN_CLOSE, VTTLayout.C_TEXT, 14f, true);
        RectTransform delRT = btnDel.GetComponent<RectTransform>();
        delRT.anchorMin = new Vector2(1, 0); delRT.anchorMax = new Vector2(1, 0);
        delRT.pivot = new Vector2(1, 0);
        delRT.anchoredPosition = new Vector2(-10f, 10f);
        delRT.sizeDelta = new Vector2(30f, 30f);

        string targetId = record.id;
        btnDel.onClick.AddListener(() => {
            CharacterManager.Instance.DeleteCharacter(targetId);
            RefreshDashboard();
        });
    }
}

// Destrói o Sprite de vídeo/RAM automaticamente quando o objeto da UI for apagado
public class SpriteCleanup : MonoBehaviour
{
    public Sprite spriteToDestroy;
    private void OnDestroy() { if (spriteToDestroy != null) Destroy(spriteToDestroy); }
}