// ============================================================
// CharacterCreatorScreen.cs
// Fichas Definitivas com Sistema de Mapeamento (_fieldMap).
// Permite guardar e carregar centenas de inputs num piscar de olhos!
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class CharacterCreatorScreen : MonoBehaviour
{
    public static CharacterCreatorScreen Instance { get; private set; }

    // Ordem controlada para nao cobrir dialogs/dados e nao ficar acima de tudo.
    private const int SHEET_SORTING_ORDER = 16000;

    private Canvas _sheetCanvas;
    private GameObject _mainScreen;
    private TMP_Text _headerTitle;
    private string _currentSystem;
    private string _editingId = null;
    private bool _avatarChanged = false;
    private Sprite _previewSprite = null;
    private bool _isNewAvatar = false;
    private CharacterType _currentCharacterType = CharacterType.Player;
    private CharacterState _currentState = CharacterState.Active;
    private AvatarCropData _avatarCrop = new AvatarCropData();
    private TMP_Text _typeBtnText;
    private TMP_Text _stateBtnText;
    private Slider _avatarZoomSlider;
    private Slider _avatarOffsetXSlider;
    private Slider _avatarOffsetYSlider;
    private Sprite _croppedPreviewSprite = null;
    // DICION�RIO M�GICO: Guarda todos os campos criados para f�cil leitura/escrita
    private Dictionary<string, TMP_InputField> _fieldMap = new Dictionary<string, TMP_InputField>();

    // --- �reas da Interface ---
    private RectTransform formScrollContent;
    private GameObject dndContainer;
    private GameObject ordemContainer;
    private GameObject leftBarsDnD;
    private GameObject leftBarsOrdem;

    private float dndBaseHeight = 1000f;
    private float ordemBaseHeight = 1200f;
    private float magicSectionHeight = 450f;
    private float _formScale = 1f;
    private float _lastFormViewportWidth = -1f;

    // Vari�veis r�pidas apenas para o painel principal (O resto usa o _fieldMap)
    private TMP_InputField dndName, dndRace, dndClass, dndLevel, dndHPCurr, dndHPMax, dndAC, dndSpd;
    private TMP_InputField ordemName, ordemClass, ordemTrilha, ordemNEX, ordemPVCurr, ordemPVMax, ordemPECurr, ordemPEMax, ordemSANCurr, ordemSANMax, ordemDefesa;

    private GameObject dndSpellsContainer;
    private bool dndSpellsOpen = false;

    private GameObject ordemRituaisContainer;
    private bool ordemRituaisOpen = false;

    private Texture2D currentAvatarTex = null;
    private Image avatarPreview;
    private Button _avatarButton;
    private Button _typeButton;
    private Button _stateButton;
    private Button _resetCropButton;
    private Button _saveButton;
    private TMP_Text _saveButtonText;
    private TMP_Text _cancelButtonText;
    private bool _sessionOnlyMode = false;
    private bool _returnToDashboardOnClose = false;

    private static readonly HashSet<string> SessionEditableKeys = new HashSet<string>
    {
        "dnd_hp_curr", "dnd_hp_max", "dnd_insp", "dnd_traits", "dnd_equip", "dnd_magic_slots", "dnd_magic_list",
        "ord_pv_curr", "ord_pv_max", "ord_pe_curr", "ord_pe_max", "ord_san_curr", "ord_san_max", "ord_powers", "ord_inv", "ord_rit_list"
    };

    private void Awake()
    {
        Instance = this;
        BuildFullScreenUI();
        _mainScreen.SetActive(false);
    }


    private void Update()
    {
        if (_mainScreen == null || !_mainScreen.activeSelf || formScrollContent == null) return;
        RectTransform viewport = formScrollContent.parent as RectTransform;
        float width = viewport != null ? viewport.rect.width : 0f;
        if (Mathf.Abs(width - _lastFormViewportWidth) > 8f)
        {
            FitFormToViewport();
            UpdateScrollHeight();
        }
    }

    private void FitFormToViewport()
    {
        if (formScrollContent == null) return;
        RectTransform viewport = formScrollContent.parent as RectTransform;
        float width = viewport != null && viewport.rect.width > 0f ? viewport.rect.width : Screen.width;
        _lastFormViewportWidth = width;
        _formScale = Mathf.Clamp((width - 36f) / 1000f, 0.72f, 1f);

        if (dndContainer != null) dndContainer.transform.localScale = Vector3.one * _formScale;
        if (ordemContainer != null) ordemContainer.transform.localScale = Vector3.one * _formScale;
    }
    private void BuildFullScreenUI()
    {
        _sheetCanvas = VTTLayout.GetOverlayCanvas("VTT_CharacterSheetCanvas", SHEET_SORTING_ORDER);
        _mainScreen = VTTLayout.New("CharacterCreatorScreen", _sheetCanvas.transform);
        RectTransform screenRT = _mainScreen.AddComponent<RectTransform>();
        screenRT.anchorMin = Vector2.zero; screenRT.anchorMax = Vector2.one;
        screenRT.offsetMin = Vector2.zero; screenRT.offsetMax = Vector2.zero;
        screenRT.sizeDelta = Vector2.zero;
        Image bgImg = _mainScreen.AddComponent<Image>();
        bgImg.color = VTTLayout.C_BG;
        bgImg.raycastTarget = true;

        float headerH = 70f;
        GameObject header = VTTLayout.New("Header", screenRT);
        RectTransform headerRT = header.AddComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1); headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0, headerH);
        Image headerImg = header.AddComponent<Image>();
        headerImg.color = VTTLayout.C_HDR_BG;
        VTTLayout.AccentBar(headerRT, 4f, VTTLayout.C_ACCENT);

        _headerTitle = VTTLayout.LabelFixed(headerRT, 40f, 0f, 800f, headerH, 22f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold);
        _headerTitle.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject centerBox = VTTLayout.New("CenterBox", screenRT);
        RectTransform centerRT = centerBox.AddComponent<RectTransform>();
        centerRT.anchorMin = new Vector2(0, 0); centerRT.anchorMax = new Vector2(1, 1);
        centerRT.offsetMin = new Vector2(40f, 30f); centerRT.offsetMax = new Vector2(-40f, -headerH - 30f);
        VTTLayout.Deco(centerBox, VTTLayout.C_SEC_BG);

        float leftWidth = 280f;
        GameObject leftPanel = VTTLayout.New("LeftPanel", centerRT);
        RectTransform leftRT = leftPanel.AddComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 0); leftRT.anchorMax = new Vector2(0, 1);
        leftRT.offsetMin = new Vector2(0, 0); leftRT.offsetMax = new Vector2(leftWidth, 0);

        RectTransform leftContentRT;
        ScrollRect leftScroll = VTTLayout.MakeScrollView("CharacterSideScroll", leftRT, 0, 0, leftWidth, 0, out leftContentRT);
        RectTransform leftScrollRT = leftScroll.GetComponent<RectTransform>();
        leftScrollRT.anchorMin = new Vector2(0, 0); leftScrollRT.anchorMax = new Vector2(1, 1);
        leftScrollRT.offsetMin = new Vector2(0f, 145f);
        leftScrollRT.offsetMax = Vector2.zero;
        leftScroll.movementType = ScrollRect.MovementType.Clamped;
        leftContentRT.anchorMin = new Vector2(0, 1);
        leftContentRT.anchorMax = new Vector2(1, 1);
        leftContentRT.pivot = new Vector2(0.5f, 1f);
        leftContentRT.sizeDelta = new Vector2(0f, 780f);

        GameObject div = VTTLayout.New("Div", centerRT);
        RectTransform divRT = div.AddComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0, 0); divRT.anchorMax = new Vector2(0, 1);
        divRT.pivot = new Vector2(0, 0);
        divRT.offsetMin = new Vector2(leftWidth, 0); divRT.offsetMax = new Vector2(leftWidth + 2f, 0);
        VTTLayout.Deco(div, VTTLayout.C_BDR_DEFAULT);

        avatarPreview = VTTLayout.MakeMaskedAvatar(leftContentRT, Vector2.zero, new Vector2(160f, 160f), VTTLayout.C_CONTENT_BG);
        RectTransform maskRT = avatarPreview.transform.parent.GetComponent<RectTransform>();
        maskRT.anchorMin = new Vector2(0.5f, 1f); maskRT.anchorMax = new Vector2(0.5f, 1f);
        maskRT.pivot = new Vector2(0.5f, 1f);
        maskRT.sizeDelta = new Vector2(160f, 160f);
        maskRT.anchoredPosition = new Vector2(0f, -24f);

        Button btnAvatar = VTTLayout.BtnFixed(leftContentRT, 0, 0, 180f, 34f, "ESCOLHER IMAGEM", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 12f, true);
        RectTransform baRT = btnAvatar.transform.parent.GetComponent<RectTransform>();
        baRT.anchorMin = new Vector2(0.5f, 1f); baRT.anchorMax = new Vector2(0.5f, 1f);
        baRT.pivot = new Vector2(0.5f, 1f);
        baRT.sizeDelta = new Vector2(180f, 36f);
        baRT.anchoredPosition = new Vector2(0f, -196f);
        _avatarButton = btnAvatar;
        btnAvatar.onClick.AddListener(DoPickAvatar);

        BuildAvatarCustomizationControls(leftContentRT);

        leftBarsDnD = VTTLayout.New("Bars_DnD", leftContentRT);
        RectTransform lbDndRT = leftBarsDnD.AddComponent<RectTransform>();
        lbDndRT.anchorMin = new Vector2(0, 1); lbDndRT.anchorMax = new Vector2(1, 1);
        lbDndRT.pivot = new Vector2(0.5f, 1f);
        lbDndRT.anchoredPosition = Vector2.zero;

        VTTLayout.LabelFixed(lbDndRT, 20f, -500f, 240f, 20f, 10.5f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = "STATUS PRINCIPAL";
        CreateVisualBar(lbDndRT, -535f, "HP", "dnd_hp", new Color(0.85f, 0.2f, 0.3f), out dndHPCurr, out dndHPMax, "10");

        leftBarsOrdem = VTTLayout.New("Bars_Ordem", leftContentRT);
        RectTransform lbOrdemRT = leftBarsOrdem.AddComponent<RectTransform>();
        lbOrdemRT.anchorMin = new Vector2(0, 1); lbOrdemRT.anchorMax = new Vector2(1, 1);
        lbOrdemRT.pivot = new Vector2(0.5f, 1f);
        lbOrdemRT.anchoredPosition = Vector2.zero;

        VTTLayout.LabelFixed(lbOrdemRT, 20f, -500f, 240f, 20f, 10.5f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = "STATUS PRINCIPAL";
        CreateVisualBar(lbOrdemRT, -535f, "PV", "ord_pv", new Color(0.85f, 0.2f, 0.3f), out ordemPVCurr, out ordemPVMax, "20");
        CreateVisualBar(lbOrdemRT, -595f, "PE", "ord_pe", new Color(0.2f, 0.5f, 0.85f), out ordemPECurr, out ordemPEMax, "15");
        CreateVisualBar(lbOrdemRT, -655f, "SAN", "ord_san", new Color(0.6f, 0.2f, 0.85f), out ordemSANCurr, out ordemSANMax, "25");

        Button btnSave = VTTLayout.BtnFixed(leftRT, 0, 0, 240f, 50f, "SALVAR FICHA", VTTLayout.C_BTN_PRI, VTTLayout.C_BDR_ACC, VTTLayout.C_TEXT, 14f, true);
        _saveButton = btnSave;
        _saveButtonText = btnSave.GetComponentInChildren<TMP_Text>();
        RectTransform bsRT = btnSave.transform.parent.GetComponent<RectTransform>();
        bsRT.anchorMin = new Vector2(0.5f, 0f); bsRT.anchorMax = new Vector2(0.5f, 0f);
        bsRT.pivot = new Vector2(0.5f, 0f);
        bsRT.anchoredPosition = new Vector2(0f, 30f);
        _saveButton.onClick.AddListener(DoSave);

        Button btnCancel = VTTLayout.BtnFixed(leftRT, 0, 0, 240f, 40f, "CANCELAR", VTTLayout.C_BTN_CLOSE, VTTLayout.C_BDR_CLOSE, VTTLayout.C_TEXT, 12f, true);
        RectTransform bcRT = btnCancel.transform.parent.GetComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0.5f, 0f); bcRT.anchorMax = new Vector2(0.5f, 0f);
        bcRT.pivot = new Vector2(0.5f, 0f);
        bcRT.anchoredPosition = new Vector2(0f, 90f);
        _cancelButtonText = btnCancel.GetComponentInChildren<TMP_Text>();
        btnCancel.onClick.AddListener(ClosePanel);

        GameObject rightPanel = VTTLayout.New("RightPanel", centerRT);
        RectTransform rightRT = rightPanel.AddComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0, 0); rightRT.anchorMax = new Vector2(1, 1);
        rightRT.offsetMin = new Vector2(leftWidth + 2f, 0); rightRT.offsetMax = Vector2.zero;

        ScrollRect scroll = VTTLayout.MakeScrollView("FormScroll", rightRT, 0, 0, 0, 0, out formScrollContent);
        RectTransform scrollRT = scroll.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = Vector2.zero; scrollRT.offsetMax = Vector2.zero;

        formScrollContent.anchorMin = new Vector2(0, 1); formScrollContent.anchorMax = new Vector2(1, 1);
        formScrollContent.pivot = new Vector2(0, 1);

        BuildDnDForm();
        BuildOrdemForm();
    }


    private void BuildAvatarCustomizationControls(RectTransform leftRT)
    {
        Button typeBtn = VTTLayout.BtnFixed(leftRT, 20f, -248f, 115f, 32f, "TIPO", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 10f, true);
        _typeButton = typeBtn;
        _typeBtnText = typeBtn.GetComponentInChildren<TMP_Text>();
        typeBtn.onClick.AddListener(CycleCharacterType);

        Button stateBtn = VTTLayout.BtnFixed(leftRT, 145f, -248f, 115f, 32f, "ESTADO", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 10f, true);
        _stateButton = stateBtn;
        _stateBtnText = stateBtn.GetComponentInChildren<TMP_Text>();
        stateBtn.onClick.AddListener(CycleCharacterState);

        VTTLayout.LabelFixed(leftRT, 40f, -296f, 200f, 18f, 10f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = "ZOOM DO RETRATO";
        _avatarZoomSlider = VTTLayout.SliderFixed(leftRT, 40f, -318f, 200f, 18f, 0.75f, 3.5f, 1f);
        _avatarZoomSlider.onValueChanged.AddListener((value) => {
            _avatarCrop.zoom = value;
            UpdateAvatarPreview();
        });

        VTTLayout.LabelFixed(leftRT, 40f, -348f, 200f, 18f, 10f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = "POSICAO HORIZONTAL";
        _avatarOffsetXSlider = VTTLayout.SliderFixed(leftRT, 40f, -370f, 200f, 18f, -1f, 1f, 0f);
        _avatarOffsetXSlider.onValueChanged.AddListener((value) => {
            _avatarCrop.offsetX = value;
            UpdateAvatarPreview();
        });

        VTTLayout.LabelFixed(leftRT, 40f, -400f, 200f, 18f, 10f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = "POSICAO VERTICAL";
        _avatarOffsetYSlider = VTTLayout.SliderFixed(leftRT, 40f, -422f, 200f, 18f, -1f, 1f, 0f);
        _avatarOffsetYSlider.onValueChanged.AddListener((value) => {
            _avatarCrop.offsetY = value;
            UpdateAvatarPreview();
        });

        Button resetBtn = VTTLayout.BtnFixed(leftRT, 40f, -456f, 200f, 28f, "RESETAR ENQUADRAMENTO", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT_DIM, 9.5f, false);
        resetBtn.onClick.AddListener(ResetAvatarCrop);

        GameObject previewHitArea = avatarPreview.transform.parent.gameObject;
        EventTrigger trigger = previewHitArea.GetComponent<EventTrigger>();
        if (trigger == null) trigger = previewHitArea.AddComponent<EventTrigger>();

        EventTrigger.Entry dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        dragEntry.callback.AddListener((data) => {
            PointerEventData pointer = data as PointerEventData;
            if (pointer == null || currentAvatarTex == null) return;

            _avatarCrop.offsetX = Mathf.Clamp(_avatarCrop.offsetX + pointer.delta.x / 120f, -1f, 1f);
            _avatarCrop.offsetY = Mathf.Clamp(_avatarCrop.offsetY + pointer.delta.y / 120f, -1f, 1f);
            SyncAvatarCropSliders();
            UpdateAvatarPreview();
        });
        trigger.triggers.Add(dragEntry);

        UpdateEntityButtons();
        SyncAvatarCropSliders();
    }

    private void CycleCharacterType()
    {
        _currentCharacterType = _currentCharacterType == CharacterType.Player ? CharacterType.NPC : (_currentCharacterType == CharacterType.NPC ? CharacterType.Enemy : CharacterType.Player);
        UpdateEntityButtons();
    }

    private void CycleCharacterState()
    {
        _currentState = _currentState == CharacterState.Active ? CharacterState.Dead : (_currentState == CharacterState.Dead ? CharacterState.Hidden : CharacterState.Active);
        UpdateEntityButtons();
    }

    private void UpdateEntityButtons()
    {
        if (_typeBtnText != null) _typeBtnText.text = "TIPO: " + CharacterManager.GetCharacterTypeLabel(_currentCharacterType).ToUpper();
        if (_stateBtnText != null) _stateBtnText.text = "ESTADO: " + CharacterManager.GetCharacterStateLabel(_currentState).ToUpper();
    }

    private void ResetAvatarCrop()
    {
        _avatarCrop = new AvatarCropData();
        SyncAvatarCropSliders();
        UpdateAvatarPreview();
    }

    private void SyncAvatarCropSliders()
    {
        if (_avatarCrop == null) _avatarCrop = new AvatarCropData();
        if (_avatarZoomSlider != null) _avatarZoomSlider.SetValueWithoutNotify(_avatarCrop.zoom);
        if (_avatarOffsetXSlider != null) _avatarOffsetXSlider.SetValueWithoutNotify(_avatarCrop.offsetX);
        if (_avatarOffsetYSlider != null) _avatarOffsetYSlider.SetValueWithoutNotify(_avatarCrop.offsetY);
    }

    private AvatarCropData CloneAvatarCrop()
    {
        if (_avatarCrop == null) _avatarCrop = new AvatarCropData();
        return new AvatarCropData { zoom = _avatarCrop.zoom, offsetX = _avatarCrop.offsetX, offsetY = _avatarCrop.offsetY };
    }

    private void DestroyPreviewSprites()
    {
        if (_previewSprite != null) { Destroy(_previewSprite); _previewSprite = null; }
        if (_croppedPreviewSprite != null) { Destroy(_croppedPreviewSprite); _croppedPreviewSprite = null; }
    }

    private void UpdateAvatarPreview()
    {
        if (avatarPreview == null) return;
        if (_croppedPreviewSprite != null) { Destroy(_croppedPreviewSprite); _croppedPreviewSprite = null; }

        if (currentAvatarTex == null)
        {
            avatarPreview.sprite = null;
            avatarPreview.color = Color.clear;
            return;
        }

        _croppedPreviewSprite = VTTLayout.CreateCroppedAvatarSprite(currentAvatarTex, _avatarCrop, 100f, 256, true);
        avatarPreview.sprite = _croppedPreviewSprite;
        avatarPreview.color = Color.white;
    }
    // --- FUN��ES DE REGISTO ---
    private TMP_InputField Reg(string key, TMP_InputField input)
    {
        _fieldMap[key] = input;
        return input;
    }

    private void CreateVisualBar(RectTransform parent, float y, string label, string idKey, Color color, out TMP_InputField currIn, out TMP_InputField maxIn, string defVal)
    {
        float w = 240f; float h = 46f;
        RectTransform bg = VTTLayout.Box("BarBG_" + label, parent, 0, y, w, h, VTTLayout.C_CONTENT_BG);
        bg.anchorMin = new Vector2(0.5f, 1f); bg.anchorMax = new Vector2(0.5f, 1f); bg.pivot = new Vector2(0.5f, 1f);
        bg.sizeDelta = new Vector2(w, h);
        bg.anchoredPosition = new Vector2(0, y);
        VTTLayout.AccentBar(bg, 4f, color);

        GameObject fillGO = VTTLayout.New("Fill", bg);
        RectTransform fillRT = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0); fillRT.anchorMax = new Vector2(1, 1);
        fillRT.offsetMin = new Vector2(4f, 0); fillRT.offsetMax = Vector2.zero;
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(color.r, color.g, color.b, 0.35f);

        VTTLayout.LabelFixed(bg, 14f, 0, 60f, h, 14f, color, FontStyles.Bold, TextAlignmentOptions.MidlineLeft).text = label;

        float inputW = 46f; float startInX = w - (inputW * 2f) - 20f;

        currIn = Reg(idKey + "_curr", VTTLayout.InputFieldFixed(bg, startInX, -7f, inputW, 32f, 16f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, defVal));
        currIn.textComponent.alignment = TextAlignmentOptions.Center;

        VTTLayout.LabelFixed(bg, startInX + inputW, 0, 15f, h, 16f, VTTLayout.C_TEXT_DIM, FontStyles.Bold).text = "/";

        maxIn = Reg(idKey + "_max", VTTLayout.InputFieldFixed(bg, startInX + inputW + 15f, -7f, inputW, 32f, 16f, VTTLayout.C_TEXT_DIM, FontStyles.Bold, defVal));
        maxIn.textComponent.alignment = TextAlignmentOptions.Center;

        TMP_InputField c = currIn; TMP_InputField m = maxIn;
        UnityEngine.Events.UnityAction<string> updateFill = (val) => {
            if (float.TryParse(c.text, out float cv) && float.TryParse(m.text, out float mv) && mv > 0)
                fillRT.anchorMax = new Vector2(Mathf.Clamp01(cv / mv), 1f);
            else fillRT.anchorMax = new Vector2(0, 1f);
        };
        currIn.onValueChanged.AddListener(updateFill);
        maxIn.onValueChanged.AddListener(updateFill);
        updateFill("");
    }

    private TMP_InputField CreateField(RectTransform parent, float x, float y, float w, string label, string idKey)
    {
        VTTLayout.LabelFixed(parent, x, y, w, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = label;
        return Reg(idKey, VTTLayout.InputFieldFixed(parent, x, y - 25f, w, 36f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));
    }

    private void CreateSkillField(RectTransform parent, float x, float y, float w, string skillName, string idKey)
    {
        TMP_InputField input = VTTLayout.InputFieldFixed(parent, x, y, 40f, 32f, 13f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, "");
        VTTLayout.LabelFixed(parent, x + 48f, y, w - 48f, 32f, 12f, VTTLayout.C_TEXT, FontStyles.Normal, TextAlignmentOptions.MidlineLeft).text = skillName;
        Reg(idKey, input);
    }

    private void BuildDnDForm()
    {
        dndContainer = VTTLayout.New("Form_DnD", formScrollContent);
        RectTransform dndRT = dndContainer.AddComponent<RectTransform>();
        dndRT.anchorMin = new Vector2(0.5f, 1); dndRT.anchorMax = new Vector2(0.5f, 1);
        dndRT.pivot = new Vector2(0f, 1);
        dndRT.sizeDelta = new Vector2(1000f, 0);
        dndRT.anchoredPosition = new Vector2(-500f, 0);

        float x = 20f; float y = -40f;
        Color cDnd = VTTLayout.C_TEXT_GOLD;

        VTTLayout.LabelFixed(dndRT, x, y, 960f, 20f, 12f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "NOME DO AVENTUREIRO";
        dndName = Reg("dnd_name", VTTLayout.InputFieldFixed(dndRT, x, y - 25f, 960f, 40f, 16f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, ""));
        y -= 80f;

        dndRace = CreateField(dndRT, x, y, 310f, "RA�A", "dnd_race");
        dndClass = CreateField(dndRT, x + 325f, y, 310f, "CLASSE", "dnd_class");
        dndLevel = CreateField(dndRT, x + 650f, y, 310f, "N�VEL", "dnd_level");
        y -= 80f;

        CreateField(dndRT, x, y, 472.5f, "ANTECEDENTE", "dnd_bkg");
        CreateField(dndRT, x + 487.5f, y, 472.5f, "TEND�NCIA", "dnd_align");
        y -= 90f;

        VTTLayout.LabelFixed(dndRT, x, y, 960f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "ATRIBUTOS PRINCIPAIS";
        y -= 25f;
        float aw = 145f; float ag = 18f;
        CreateField(dndRT, x + (aw + ag) * 0, y, aw, "FOR�A", "dnd_str");
        CreateField(dndRT, x + (aw + ag) * 1, y, aw, "DESTREZA", "dnd_dex");
        CreateField(dndRT, x + (aw + ag) * 2, y, aw, "CONSTITUI��O", "dnd_con");
        CreateField(dndRT, x + (aw + ag) * 3, y, aw, "INTELIG�NCIA", "dnd_int");
        CreateField(dndRT, x + (aw + ag) * 4, y, aw, "SABEDORIA", "dnd_wis");
        CreateField(dndRT, x + (aw + ag) * 5, y, aw, "CARISMA", "dnd_cha");
        y -= 85f;

        VTTLayout.LabelFixed(dndRT, x, y, 960f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "COMBATE";
        y -= 25f;
        float cbW = 180f; float cbGap = 15f;
        dndAC = CreateField(dndRT, x + (cbW + cbGap) * 0, y, cbW, "CLASSE ARMADURA (CA)", "dnd_ac");
        dndSpd = CreateField(dndRT, x + (cbW + cbGap) * 1, y, cbW, "DESLOCAMENTO", "dnd_spd");
        CreateField(dndRT, x + (cbW + cbGap) * 2, y, cbW, "INICIATIVA", "dnd_init");
        CreateField(dndRT, x + (cbW + cbGap) * 3, y, cbW, "B�NUS PROFICI�NCIA", "dnd_prof");
        CreateField(dndRT, x + (cbW + cbGap) * 4, y, cbW, "INSPIRA��O", "dnd_insp");
        y -= 90f;

        VTTLayout.LabelFixed(dndRT, x, y, 960f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "PER�CIAS";
        y -= 25f;
        string[] dndSkills = { "Acrobacia", "Arcanismo", "Atletismo", "Atua��o", "Engana��o", "Furtividade", "Hist�ria", "Intimida��o", "Intui��o", "Investiga��o", "Lidar Animais", "Medicina", "Natureza", "Percep��o", "Persuas�o", "Prestidigita��o", "Religi�o", "Sobreviv�ncia" };
        int rows = Mathf.CeilToInt(dndSkills.Length / 3f);
        for (int i = 0; i < dndSkills.Length; i++)
        {
            CreateSkillField(dndRT, x + ((i % 3) * 330f), y - ((i / 3) * 40f), 300f, dndSkills[i], "dnd_skill_" + i);
        }
        y -= (rows * 40f) + 30f;

        VTTLayout.LabelFixed(dndRT, x, y, 472.5f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "TRA�OS E CARACTER�STICAS";
        VTTLayout.LabelFixed(dndRT, x + 487.5f, y, 472.5f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "EQUIPAMENTO E TESOURO";
        y -= 25f;
        Reg("dnd_traits", VTTLayout.InputFieldMultiline(dndRT, x, y, 472.5f, 180f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));
        Reg("dnd_equip", VTTLayout.InputFieldMultiline(dndRT, x + 487.5f, y, 472.5f, 180f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));
        y -= 210f;

        Button btnSpells = VTTLayout.BtnFixed(dndRT, x, y, 960f, 40f, "MAGIAS E ESPA�OS DE MAGIA ?", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, cDnd, 14f, true);
        btnSpells.onClick.AddListener(ToggleDnDSpells);
        y -= 60f;

        dndBaseHeight = Mathf.Abs(y) + 40f;

        dndSpellsContainer = VTTLayout.New("SpellsContainer", dndRT);
        RectTransform spRT = dndSpellsContainer.AddComponent<RectTransform>();
        spRT.anchorMin = new Vector2(0, 1); spRT.anchorMax = new Vector2(1, 1);
        spRT.pivot = new Vector2(0, 1);
        spRT.anchoredPosition = new Vector2(0, y);

        RectTransform magicBg = VTTLayout.Box("MagicBg", spRT, x, 0, 960f, 450f, VTTLayout.RGB(0.12f, 0.13f, 0.16f));
        magicBg.anchorMin = new Vector2(0, 1); magicBg.anchorMax = new Vector2(0, 1); magicBg.pivot = new Vector2(0, 1);
        VTTLayout.AccentBar(magicBg, 6f, cDnd);

        float sy = -20f;
        VTTLayout.LabelFixed(spRT, x + 15f, sy, 945f, 20f, 16f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "LIVRO DE MAGIAS";
        sy -= 35f;
        CreateField(spRT, x + 15f, sy, 305f, "ATRIBUTO CHAVE", "dnd_magic_attr");
        CreateField(spRT, x + 330f, sy, 305f, "CD DA MAGIA", "dnd_magic_dc");
        CreateField(spRT, x + 645f, sy, 300f, "B�NUS DE ATAQUE", "dnd_magic_atk");
        sy -= 80f;

        VTTLayout.LabelFixed(spRT, x + 15f, sy, 465f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "ESPA�OS DE MAGIA (SLOTS)";
        VTTLayout.LabelFixed(spRT, x + 490f, sy, 465f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "TRUQUES E MAGIAS CONHECIDAS";
        sy -= 25f;
        Reg("dnd_magic_slots", VTTLayout.InputFieldMultiline(spRT, x + 15f, sy, 460f, 250f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));
        Reg("dnd_magic_list", VTTLayout.InputFieldMultiline(spRT, x + 490f, sy, 455f, 250f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));

        dndSpellsContainer.SetActive(false);
    }

    private void BuildOrdemForm()
    {
        ordemContainer = VTTLayout.New("Form_Ordem", formScrollContent);
        RectTransform ordemRT = ordemContainer.AddComponent<RectTransform>();
        ordemRT.anchorMin = new Vector2(0.5f, 1); ordemRT.anchorMax = new Vector2(0.5f, 1);
        ordemRT.pivot = new Vector2(0f, 1);
        ordemRT.sizeDelta = new Vector2(1000f, 0);
        ordemRT.anchoredPosition = new Vector2(-500f, 0);

        float x = 20f; float y = -40f;
        Color cOrdem = new Color(0.85f, 0.25f, 0.25f, 1f);

        VTTLayout.LabelFixed(ordemRT, x, y, 960f, 20f, 12f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "NOME DO AGENTE";
        ordemName = Reg("ord_name", VTTLayout.InputFieldFixed(ordemRT, x, y - 25f, 960f, 40f, 18f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, ""));
        y -= 80f;

        CreateField(ordemRT, x, y, 310f, "ORIGEM", "ord_origem");
        ordemClass = CreateField(ordemRT, x + 325f, y, 310f, "CLASSE", "ord_class");
        ordemTrilha = CreateField(ordemRT, x + 650f, y, 310f, "TRILHA", "ord_trilha");
        y -= 80f;

        ordemNEX = CreateField(ordemRT, x, y, 472.5f, "NEX (%)", "ord_nex");
        CreateField(ordemRT, x + 487.5f, y, 472.5f, "PATENTE", "ord_patente");
        y -= 90f;

        VTTLayout.LabelFixed(ordemRT, x, y, 960f, 20f, 14f, cOrdem, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "ATRIBUTOS PRINCIPAIS";
        y -= 25f;
        float aw = 180f; float ag = 15f;
        CreateField(ordemRT, x + (aw + ag) * 0, y, aw, "AGILIDADE", "ord_agi");
        CreateField(ordemRT, x + (aw + ag) * 1, y, aw, "INTELECTO", "ord_int");
        CreateField(ordemRT, x + (aw + ag) * 2, y, aw, "VIGOR", "ord_vig");
        CreateField(ordemRT, x + (aw + ag) * 3, y, aw, "PRESEN�A", "ord_pre");
        CreateField(ordemRT, x + (aw + ag) * 4, y, aw, "FOR�A", "ord_for");
        y -= 85f;

        VTTLayout.LabelFixed(ordemRT, x, y, 960f, 20f, 14f, cOrdem, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "DEFESAS";
        y -= 25f;
        float cw = 310f; float cg = 15f;
        ordemDefesa = CreateField(ordemRT, x + (cw + cg) * 0, y, cw, "DEFESA BASE", "ord_defesa");
        CreateField(ordemRT, x + (cw + cg) * 1, y, cw, "ESQUIVA", "ord_esq");
        CreateField(ordemRT, x + (cw + cg) * 2, y, cw, "BLOQUEIO", "ord_bloq");
        y -= 90f;

        VTTLayout.LabelFixed(ordemRT, x, y, 960f, 20f, 14f, cOrdem, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "PER�CIAS";
        y -= 25f;
        string[] ordemSkillsList = { "Acrobacia", "Adestramento", "Artes", "Atletismo", "Atualidades", "Ci�ncias", "Crime", "Diplomacia", "Engana��o", "Fortitude", "Furtividade", "Iniciativa", "Intimida��o", "Intui��o", "Investiga��o", "Luta", "Medicina", "Ocultismo", "Percep��o", "Pilotagem", "Pontaria", "Profiss�o", "Reflexos", "Religi�o", "Sobreviv�ncia", "T�tica", "Tecnologia", "Vontade" };
        int rRows = Mathf.CeilToInt(ordemSkillsList.Length / 3f);
        for (int i = 0; i < ordemSkillsList.Length; i++)
        {
            CreateSkillField(ordemRT, x + ((i % 3) * 330f), y - ((i / 3) * 40f), 300f, ordemSkillsList[i], "ord_skill_" + i);
        }
        y -= (rRows * 40f) + 30f;

        VTTLayout.LabelFixed(ordemRT, x, y, 472.5f, 20f, 14f, cOrdem, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "HABILIDADES E PODERES";
        VTTLayout.LabelFixed(ordemRT, x + 487.5f, y, 472.5f, 20f, 14f, cOrdem, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "INVENT�RIO (Peso/Espa�os)";
        y -= 25f;
        Reg("ord_powers", VTTLayout.InputFieldMultiline(ordemRT, x, y, 472.5f, 200f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));
        Reg("ord_inv", VTTLayout.InputFieldMultiline(ordemRT, x + 487.5f, y, 472.5f, 200f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));
        y -= 230f;

        Button btnRituais = VTTLayout.BtnFixed(ordemRT, x, y, 960f, 40f, "RITUAIS PARANORMAIS ?", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, cOrdem, 14f, true);
        btnRituais.onClick.AddListener(ToggleOrdemRituais);
        y -= 60f;

        ordemBaseHeight = Mathf.Abs(y) + 40f;

        ordemRituaisContainer = VTTLayout.New("RituaisContainer", ordemRT);
        RectTransform rtRT = ordemRituaisContainer.AddComponent<RectTransform>();
        rtRT.anchorMin = new Vector2(0, 1); rtRT.anchorMax = new Vector2(1, 1);
        rtRT.pivot = new Vector2(0, 1);
        rtRT.anchoredPosition = new Vector2(0, y);

        RectTransform magicBg = VTTLayout.Box("MagicBg", rtRT, x, 0, 960f, 450f, VTTLayout.RGB(0.12f, 0.13f, 0.16f));
        magicBg.anchorMin = new Vector2(0, 1); magicBg.anchorMax = new Vector2(0, 1); magicBg.pivot = new Vector2(0, 1);
        VTTLayout.AccentBar(magicBg, 6f, cOrdem);

        float ry = -20f;
        VTTLayout.LabelFixed(rtRT, x + 15f, ry, 945f, 20f, 16f, cOrdem, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "OCULTISMO";
        ry -= 35f;
        CreateField(rtRT, x + 15f, ry, 305f, "DT DE RITUAIS", "ord_rit_dt");
        CreateField(rtRT, x + 330f, ry, 305f, "ATAQUE (OCULTISMO)", "ord_rit_atk");
        CreateField(rtRT, x + 645f, ry, 300f, "LIMITE DE PE / TURNO", "ord_rit_pe");
        ry -= 80f;

        VTTLayout.LabelFixed(rtRT, x + 15f, ry, 945f, 20f, 14f, cOrdem, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "LISTA DE RITUAIS CONHECIDOS";
        ry -= 25f;
        Reg("ord_rit_list", VTTLayout.InputFieldMultiline(rtRT, x + 15f, ry, 930f, 250f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));

        ordemRituaisContainer.SetActive(false);
    }

    private void ToggleDnDSpells() { dndSpellsOpen = !dndSpellsOpen; dndSpellsContainer.SetActive(dndSpellsOpen); UpdateScrollHeight(); }
    private void ToggleOrdemRituais() { ordemRituaisOpen = !ordemRituaisOpen; ordemRituaisContainer.SetActive(ordemRituaisOpen); UpdateScrollHeight(); }

    private void UpdateScrollHeight()
    {
        FitFormToViewport();
        float baseHeight = _currentSystem == "D&D 5e" ? dndBaseHeight : ordemBaseHeight;
        bool magicOpen = _currentSystem == "D&D 5e" ? dndSpellsOpen : ordemRituaisOpen;
        formScrollContent.sizeDelta = new Vector2(0, (baseHeight + (magicOpen ? magicSectionHeight : 0)) * _formScale + 40f);
    }


    private bool IsSessionEditableKey(string key)
    {
        return !string.IsNullOrEmpty(key) && SessionEditableKeys.Contains(key);
    }

    private void ApplySheetInteractionMode(bool sessionOnly)
    {
        _sessionOnlyMode = sessionOnly;

        foreach (var kvp in _fieldMap)
        {
            TMP_InputField input = kvp.Value;
            if (input == null) continue;

            bool canEdit = !sessionOnly || IsSessionEditableKey(kvp.Key);
            input.interactable = true;
            input.readOnly = !canEdit;
            if (input.textComponent != null)
                input.textComponent.color = canEdit ? VTTLayout.C_TEXT_PANEL : VTTLayout.C_TEXT;
            if (input.targetGraphic != null)
                input.targetGraphic.color = canEdit ? Color.white : new Color(1f, 1f, 1f, 0.72f);
        }

        if (_avatarButton != null) _avatarButton.interactable = !sessionOnly;
        if (_typeButton != null) _typeButton.interactable = !sessionOnly;
        if (_stateButton != null) _stateButton.interactable = !sessionOnly;
        if (_resetCropButton != null) _resetCropButton.interactable = !sessionOnly;
        if (_avatarZoomSlider != null) _avatarZoomSlider.interactable = !sessionOnly;
        if (_avatarOffsetXSlider != null) _avatarOffsetXSlider.interactable = !sessionOnly;
        if (_avatarOffsetYSlider != null) _avatarOffsetYSlider.interactable = !sessionOnly;

        if (_saveButtonText != null) _saveButtonText.text = sessionOnly ? "SALVAR SESSAO" : "SALVAR FICHA";
        if (_cancelButtonText != null) _cancelButtonText.text = sessionOnly ? "FECHAR" : "CANCELAR";
    }
    private void DoPickAvatar()
    {
        if (_sessionOnlyMode) return;
        if (MapFileLoader.Instance != null)
        {
            MapFileLoader.Instance.OpenFilePicker((tex) => {
                // Previne lixo se o usu�rio trocar a imagem duas vezes na mesma tela
                if (_isNewAvatar && currentAvatarTex != null) Destroy(currentAvatarTex);
                DestroyPreviewSprites();

                currentAvatarTex = tex;
                _isNewAvatar = true;
                _avatarChanged = true;
                _avatarCrop = new AvatarCropData();
                SyncAvatarCropSliders();
                UpdateAvatarPreview();
            });
        }
    }

    private void DoSave()
    {
        if (_sessionOnlyMode)
        {
            DoSaveSessionChanges();
            return;
        }
        string cGold = "#E8C84A", cBlue = "#598CD9", cRed = "#D95959", cPurp = "#9D59D9";

        CharacterRecord newChar = new CharacterRecord();
        newChar.id = string.IsNullOrEmpty(_editingId) ? System.Guid.NewGuid().ToString() : _editingId;
        newChar.system = _currentSystem;

        if (_currentSystem == "D&D 5e")
        {
            newChar.name = string.IsNullOrEmpty(dndName.text) ? "Aventureiro" : dndName.text;
            newChar.subText = $"Lvl {dndLevel.text} � {dndRace.text} {dndClass.text}";
            newChar.statsStr = $"HP: <color={cGold}>{dndHPCurr.text}/{dndHPMax.text}</color>   CA: <color={cRed}>{dndAC.text}</color>   MOV: <color={cBlue}>{dndSpd.text}</color>";
        }
        else
        {
            newChar.name = string.IsNullOrEmpty(ordemName.text) ? "Agente" : ordemName.text;
            newChar.subText = $"NEX {ordemNEX.text}% � {ordemClass.text} {ordemTrilha.text}";
            newChar.statsStr = $"PV: <color={cGold}>{ordemPVCurr.text}/{ordemPVMax.text}</color>   PE: <color={cBlue}>{ordemPECurr.text}/{ordemPEMax.text}</color>   SAN: <color={cPurp}>{ordemSANCurr.text}/{ordemSANMax.text}</color>   DEF: <color={cRed}>{ordemDefesa.text}</color>";
        }

        newChar.characterType = _currentCharacterType;
        newChar.state = _currentState;
        newChar.avatarCrop = CloneAvatarCrop();
        CharacterRecord existingRecord = CharacterManager.Instance != null && !string.IsNullOrEmpty(_editingId) ? CharacterManager.Instance.GetCharacter(_editingId) : null;
        newChar.defaultRenderInProjection = existingRecord == null ? true : existingRecord.defaultRenderInProjection;

        // Empacota toda a vida do personagem!
        foreach (var kvp in _fieldMap)
        {
            newChar.fields.Add(new CharField { key = kvp.Key, value = kvp.Value.text });
        }

        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.SaveCharacter(newChar, currentAvatarTex, !string.IsNullOrEmpty(_editingId), _avatarChanged);
        }
        if (DashboardOverlay.Instance != null) DashboardOverlay.Instance.RefreshDashboard();

        DestroyPreviewSprites();
        _isNewAvatar = false;

        ClosePanel();
    }


    private void DoSaveSessionChanges()
    {
        if (CharacterManager.Instance == null || string.IsNullOrEmpty(_editingId))
        {
            ClosePanel();
            return;
        }

        Dictionary<string, string> changes = new Dictionary<string, string>();
        foreach (var kvp in _fieldMap)
        {
            if (IsSessionEditableKey(kvp.Key) && kvp.Value != null)
                changes[kvp.Key] = kvp.Value.text;
        }

        CharacterManager.Instance.UpdateCharacterSessionFields(_editingId, changes);
        if (DashboardOverlay.Instance != null) DashboardOverlay.Instance.RefreshDashboard();
        ClosePanel();
    }

    private void ShowSheet(bool returnToDashboard)
    {
        _returnToDashboardOnClose = returnToDashboard;

        if (_mainScreen != null)
            _mainScreen.SetActive(true);

        if (_sheetCanvas != null)
        {
            _sheetCanvas.overrideSorting = true;
            _sheetCanvas.sortingOrder = SHEET_SORTING_ORDER;
        }

        if (_mainScreen != null)
            _mainScreen.transform.SetAsLastSibling();
    }

    // --- NOVA: ABRE A FICHA PARA EDI��O ---
    public void OpenForEdit(CharacterRecord record)
    {
        // 1. Limpeza inicial de lixo da sess�o anterior
        DestroyPreviewSprites();
        _isNewAvatar = false;

        _editingId = record.id;
        _currentSystem = record.system;
        _avatarChanged = false;
        _currentCharacterType = record.characterType;
        _currentState = record.state;
        _avatarCrop = record.avatarCrop != null ? new AvatarCropData { zoom = record.avatarCrop.zoom, offsetX = record.avatarCrop.offsetX, offsetY = record.avatarCrop.offsetY } : new AvatarCropData();
        UpdateEntityButtons();
        SyncAvatarCropSliders();
        _headerTitle.text = "EDITANDO FICHA: " + _currentSystem.ToUpper();

        // Limpa lixo anterior
        foreach (var kvp in _fieldMap) kvp.Value.text = "";

        // 2. Carrega foto do HD e aplica o enquadramento salvo.
        currentAvatarTex = CharacterManager.Instance.LoadAvatar(record.avatarFileName);
        UpdateAvatarPreview();

        // Carrega todos os campos!
        if (record.fields != null) foreach (var field in record.fields)
            {
                if (_fieldMap.TryGetValue(field.key, out var input))
                {
                    input.text = field.value;
                }
            }

        dndSpellsOpen = false; if (dndSpellsContainer != null) dndSpellsContainer.SetActive(false);
        ordemRituaisOpen = false; if (ordemRituaisContainer != null) ordemRituaisContainer.SetActive(false);

        leftBarsDnD.SetActive(_currentSystem == "D&D 5e"); leftBarsOrdem.SetActive(_currentSystem == "Ordem Paranormal");
        dndContainer.SetActive(_currentSystem == "D&D 5e"); ordemContainer.SetActive(_currentSystem == "Ordem Paranormal");

        UpdateScrollHeight();
        formScrollContent.anchoredPosition = new Vector2(formScrollContent.anchoredPosition.x, 0);
        ApplySheetInteractionMode(false);
        ShowSheet(true);
    }



    public void OpenForSession(CharacterRecord record)
    {
        OpenForEdit(record);
        _headerTitle.text = "FICHA EM SESSAO: " + _currentSystem.ToUpper();
        ApplySheetInteractionMode(true);
    }

    public void OpenPanel(string systemName)
    {
        // 1. Limpeza inicial de lixo da sess�o anterior
        DestroyPreviewSprites();
        _isNewAvatar = false;

        _editingId = null; // Modo de Cria��o Limpo
        _avatarChanged = true;
        _currentCharacterType = CharacterType.Player;
        _currentState = CharacterState.Active;
        _avatarCrop = new AvatarCropData();
        UpdateEntityButtons();
        SyncAvatarCropSliders();
        _currentSystem = systemName;
        _headerTitle.text = "MONTAGEM DE FICHA: " + systemName.ToUpper();

        // 2. Garante que a foto est� vazia para a ficha nova
        currentAvatarTex = null;
        UpdateAvatarPreview();

        foreach (var kvp in _fieldMap) kvp.Value.text = "";
        if (dndHPCurr != null) { dndHPCurr.text = "10"; dndHPMax.text = "10"; }
        if (ordemPVCurr != null) { ordemPVCurr.text = "20"; ordemPVMax.text = "20"; ordemPECurr.text = "10"; ordemPEMax.text = "10"; ordemSANCurr.text = "25"; ordemSANMax.text = "25"; }

        dndSpellsOpen = false; if (dndSpellsContainer != null) dndSpellsContainer.SetActive(false);
        ordemRituaisOpen = false; if (ordemRituaisContainer != null) ordemRituaisContainer.SetActive(false);

        leftBarsDnD.SetActive(systemName == "D&D 5e"); leftBarsOrdem.SetActive(systemName == "Ordem Paranormal");
        dndContainer.SetActive(systemName == "D&D 5e"); ordemContainer.SetActive(systemName == "Ordem Paranormal");

        UpdateScrollHeight();
        formScrollContent.anchoredPosition = new Vector2(formScrollContent.anchoredPosition.x, 0);
        ApplySheetInteractionMode(false);
        ShowSheet(true);
    }

    public void ClosePanel()
    {
        DestroyPreviewSprites();

        if (_isNewAvatar && currentAvatarTex != null)
        {
            Destroy(currentAvatarTex);
            currentAvatarTex = null;
        }

        _isNewAvatar = false;
        _sessionOnlyMode = false;

        if (_mainScreen != null)
            _mainScreen.SetActive(false);

        if (_returnToDashboardOnClose && DashboardOverlay.Instance != null)
        {
            DashboardOverlay.Instance.OpenPanel();
        }

        _returnToDashboardOnClose = false;
    }
}