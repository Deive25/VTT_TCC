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
    private float _lastGridWidth = -1f;

    private void Awake()
    {
        Instance = this;
        BuildFullScreenUI();
        _mainScreen.SetActive(false);
    }


    private void Update()
    {
        if (_mainScreen == null || !_mainScreen.activeSelf || _gridRT == null) return;
        float width = _gridRT.rect.width;
        if (Mathf.Abs(width - _lastGridWidth) > 8f)
        {
            _lastGridWidth = width;
            RebuildDashboardLayout();
        }
    }
    private void BuildFullScreenUI()
    {
        Canvas cv = VTTLayout.GetOverlayCanvas("VTT_MainOverlayCanvas", 12000);

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
        title.rectTransform.offsetMax = new Vector2(-240f, 0);
        title.enableAutoSizing = true;
        title.fontSizeMin = 14f;
        title.fontSizeMax = 22f;

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
        _gridLayout.cellSize = new Vector2(380f, 150f);
        _gridLayout.spacing = new Vector2(18f, 18f);
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

        card.AddComponent<RectMask2D>();
        Image avImg = VTTLayout.MakeMaskedAvatar(cardRT, new Vector2(18f, 6f), new Vector2(78f, 78f), VTTLayout.C_CONTENT_BG);
        if (avatarTex != null)
        {
            avImg.sprite = Sprite.Create(avatarTex, new Rect(0, 0, avatarTex.width, avatarTex.height), new Vector2(0.5f, 0.5f));
            avImg.color = Color.white;
        }

        float textX = 112f;

        TMP_Text nameTxt = MakeText(cardRT, name, 18f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
        nameTxt.rectTransform.anchorMin = new Vector2(0, 1); nameTxt.rectTransform.anchorMax = new Vector2(1, 1);
        nameTxt.rectTransform.pivot = new Vector2(0, 1);
        nameTxt.rectTransform.anchoredPosition = new Vector2(textX, -16f);
        nameTxt.rectTransform.sizeDelta = new Vector2(-156f, 28f);
        nameTxt.enableAutoSizing = true;
        nameTxt.fontSizeMin = 12f;
        nameTxt.fontSizeMax = 18f;
        nameTxt.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text lvlTxt = MakeText(cardRT, subText, 13f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        lvlTxt.rectTransform.anchorMin = new Vector2(0, 1); lvlTxt.rectTransform.anchorMax = new Vector2(1, 1);
        lvlTxt.rectTransform.pivot = new Vector2(0, 1);
        lvlTxt.rectTransform.anchoredPosition = new Vector2(textX, -46f);
        lvlTxt.rectTransform.sizeDelta = new Vector2(-156f, 34f);
        lvlTxt.enableWordWrapping = true;
        lvlTxt.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text statsTxt = MakeText(cardRT, statsFormatado, 12f, VTTLayout.C_TEXT, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        statsTxt.rectTransform.anchorMin = new Vector2(0, 1); statsTxt.rectTransform.anchorMax = new Vector2(1, 1);
        statsTxt.rectTransform.pivot = new Vector2(0, 1);
        statsTxt.rectTransform.anchoredPosition = new Vector2(textX, -88f);
        statsTxt.rectTransform.sizeDelta = new Vector2(-156f, 44f);
        statsTxt.enableWordWrapping = true;
        statsTxt.overflowMode = TextOverflowModes.Ellipsis;

        Button btnEdit = MakeButton(cardRT, "E", VTTLayout.C_BTN_SEC, VTTLayout.C_TEXT, 14f, true);
        RectTransform editRT = btnEdit.GetComponent<RectTransform>();
        editRT.anchorMin = new Vector2(1, 1); editRT.anchorMax = new Vector2(1, 1);
        editRT.pivot = new Vector2(1, 1);
        editRT.anchoredPosition = new Vector2(-10f, -10f);
        editRT.sizeDelta = new Vector2(28f, 28f);

        Button btnDel = MakeButton(cardRT, "X", VTTLayout.C_BTN_CLOSE, VTTLayout.C_TEXT, 14f, true);
        RectTransform delRT = btnDel.GetComponent<RectTransform>();
        delRT.anchorMin = new Vector2(1, 0); delRT.anchorMax = new Vector2(1, 0);
        delRT.pivot = new Vector2(1, 0);
        delRT.anchoredPosition = new Vector2(-10f, 10f);
        delRT.sizeDelta = new Vector2(28f, 28f);

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

        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = VTTLayout.ButtonColors(bg.color);
        card.AddComponent<ButtonFeedback>();

        btn.onClick.AddListener(() => {
            SystemSelectorOverlay sso = FindAnyObjectByType<SystemSelectorOverlay>();
            if (sso != null) sso.OpenPanel(false);
            else
            {
                Debug.LogWarning("SystemSelectorOverlay nao encontrado na cena!");
            }
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

        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = VTTLayout.ButtonColors(bg);
        go.AddComponent<ButtonFeedback>();

        TMP_Text t = MakeText(rt, label, fontSize, textColor, bold ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.Center);
        return btn;
    }

    public void OpenPanel()
    {
        _mainScreen.SetActive(true);
        _mainScreen.transform.SetAsLastSibling();
        RefreshDashboard();
    }

    public void ClosePanel()
    {
        _mainScreen.SetActive(false);
    }

    public void HideForChildPanel()
    {
        if (_mainScreen != null)
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

        RebuildDashboardLayout();
    }

    private void RebuildDashboardLayout()
    {
        Canvas.ForceUpdateCanvases();
        int childCount = _gridRT.childCount;
        float width = _gridRT.rect.width;

        if (width > 0 && childCount > 0)
        {
            float minCardW = 320f;
            float maxCardW = 420f;
            float spacing = _gridLayout.spacing.x;
            int columns = Mathf.Max(1, Mathf.FloorToInt((width + spacing) / (minCardW + spacing)));
            float cardW = Mathf.Clamp((width - spacing * (columns - 1)) / columns, minCardW, maxCardW);
            if (columns == 1) cardW = Mathf.Min(width, maxCardW);

            _gridLayout.cellSize = new Vector2(cardW, 150f);
            _gridLayout.childAlignment = columns == 1 ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;

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


    private Color GetCharacterTypeColor(CharacterRecord record)
    {
        if (record == null) return VTTLayout.C_ACCENT;
        switch (record.characterType)
        {
            case CharacterType.NPC: return new Color(0.25f, 0.55f, 0.90f, 1f);
            case CharacterType.Enemy: return new Color(0.90f, 0.22f, 0.20f, 1f);
            default: return record.system == "D&D 5e" ? VTTLayout.C_TEXT_GOLD : new Color(0.85f, 0.25f, 0.25f, 1f);
        }
    }
    private void BuildCharacterCard(RectTransform parent, CharacterRecord record, Texture2D avatarTex)
    {
        GameObject card = VTTLayout.New("Card_" + record.id, parent);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        Image bg = card.AddComponent<Image>();
        bg.color = VTTLayout.C_SEC_BG;

        Color barColor = GetCharacterTypeColor(record);
        VTTLayout.AccentBar(cardRT, 4f, barColor);

        card.AddComponent<RectMask2D>();
        Image avImg = VTTLayout.MakeMaskedAvatar(cardRT, new Vector2(18f, 6f), new Vector2(78f, 78f), VTTLayout.C_CONTENT_BG);

        if (avatarTex != null)
        {
            Sprite newSprite = VTTLayout.CreateCroppedAvatarSprite(avatarTex, record.avatarCrop, 100f, 256, true);
            avImg.sprite = newSprite;
            avImg.color = Color.white;

            SpriteCleanup cleanup = card.AddComponent<SpriteCleanup>();
            cleanup.spriteToDestroy = newSprite;
        }

        float textX = 112f;

        TMP_Text nameTxt = MakeText(cardRT, record.name, 20f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
        nameTxt.rectTransform.anchorMin = new Vector2(0, 1); nameTxt.rectTransform.anchorMax = new Vector2(1, 1);
        nameTxt.rectTransform.pivot = new Vector2(0, 1);
        nameTxt.rectTransform.anchoredPosition = new Vector2(textX, -16f);
        nameTxt.rectTransform.sizeDelta = new Vector2(-156f, 28f);
        nameTxt.enableAutoSizing = true;
        nameTxt.fontSizeMin = 12f;
        nameTxt.fontSizeMax = 18f;
        nameTxt.overflowMode = TextOverflowModes.Ellipsis;

        string typeLine = CharacterManager.GetCharacterTypeLabel(record.characterType) + " / " + CharacterManager.GetCharacterStateLabel(record.state) + " - " + record.subText;
        TMP_Text lvlTxt = MakeText(cardRT, typeLine, 13f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        lvlTxt.rectTransform.anchorMin = new Vector2(0, 1); lvlTxt.rectTransform.anchorMax = new Vector2(1, 1);
        lvlTxt.rectTransform.pivot = new Vector2(0, 1);
        lvlTxt.rectTransform.anchoredPosition = new Vector2(textX, -46f);
        lvlTxt.rectTransform.sizeDelta = new Vector2(-156f, 34f);
        lvlTxt.enableWordWrapping = true;
        lvlTxt.overflowMode = TextOverflowModes.Ellipsis;

         TMP_Text statsTxt = MakeText(cardRT, record.statsStr, 12f, VTTLayout.C_TEXT, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        statsTxt.rectTransform.anchorMin = new Vector2(0, 1); statsTxt.rectTransform.anchorMax = new Vector2(1, 1);
        statsTxt.rectTransform.pivot = new Vector2(0, 1);
        statsTxt.rectTransform.anchoredPosition = new Vector2(textX, -88f);
        statsTxt.rectTransform.sizeDelta = new Vector2(-156f, 44f);
        statsTxt.enableWordWrapping = true;
        statsTxt.overflowMode = TextOverflowModes.Ellipsis;

        Button btnEdit = MakeButton(cardRT, "E", VTTLayout.C_BTN_SEC, VTTLayout.C_TEXT, 14f, true);
        RectTransform editRT = btnEdit.GetComponent<RectTransform>();
        editRT.anchorMin = new Vector2(1, 1); editRT.anchorMax = new Vector2(1, 1);
        editRT.pivot = new Vector2(1, 1);
        editRT.anchoredPosition = new Vector2(-10f, -10f);
        editRT.sizeDelta = new Vector2(28f, 28f);

        CharacterRecord targetRecord = record;
        btnEdit.onClick.AddListener(() => {
            HideForChildPanel();

            if (CharacterCreatorScreen.Instance != null)
                CharacterCreatorScreen.Instance.OpenForEdit(targetRecord);
            else
            {
                Debug.LogError("[DashboardOverlay] CharacterCreatorScreen.Instance nao encontrado na cena.");
                OpenPanel();
            }
        });

        Button btnView = MakeButton(cardRT, "V", VTTLayout.C_BTN_PRI, VTTLayout.C_TEXT, 14f, true);
        RectTransform viewRT = btnView.GetComponent<RectTransform>();
        viewRT.anchorMin = new Vector2(1, 0.5f); viewRT.anchorMax = new Vector2(1, 0.5f);
        viewRT.pivot = new Vector2(1, 0.5f);
        viewRT.anchoredPosition = new Vector2(-10f, 0f);
        viewRT.sizeDelta = new Vector2(28f, 28f);
        btnView.onClick.AddListener(() => {
            HideForChildPanel();

            if (CharacterCreatorScreen.Instance != null)
                CharacterCreatorScreen.Instance.OpenForSession(targetRecord);
            else
            {
                Debug.LogError("[DashboardOverlay] CharacterCreatorScreen.Instance nao encontrado na cena.");
                OpenPanel();
            }
        });
        Button btnDel = MakeButton(cardRT, "X", VTTLayout.C_BTN_CLOSE, VTTLayout.C_TEXT, 14f, true);
        RectTransform delRT = btnDel.GetComponent<RectTransform>();
        delRT.anchorMin = new Vector2(1, 0); delRT.anchorMax = new Vector2(1, 0);
        delRT.pivot = new Vector2(1, 0);
        delRT.anchoredPosition = new Vector2(-10f, 10f);
        delRT.sizeDelta = new Vector2(28f, 28f);

        string targetId = record.id;
        btnDel.onClick.AddListener(() => {
            UIConfirmDialog.Show("Excluir personagem", "Esta acao remove a ficha e o retrato salvo deste personagem.", () => {
                CharacterManager.Instance.DeleteCharacter(targetId);
                RefreshDashboard();
            });
        });
    }
}
