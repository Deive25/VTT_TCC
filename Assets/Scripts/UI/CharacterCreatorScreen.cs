// ============================================================
// CharacterCreatorScreen.cs
// Fichas Definitivas com Sistema de Mapeamento (_fieldMap).
// Permite guardar e carregar centenas de inputs num piscar de olhos!
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterCreatorScreen : MonoBehaviour
{
    public static CharacterCreatorScreen Instance { get; private set; }

    private GameObject _mainScreen;
    private TMP_Text _headerTitle;
    private string _currentSystem;
    private string _editingId = null;
    private bool _avatarChanged = false;
    private Sprite _previewSprite = null;
    private bool _isNewAvatar = false;

    // DICIONÁRIO MÁGICO: Guarda todos os campos criados para fácil leitura/escrita
    private Dictionary<string, TMP_InputField> _fieldMap = new Dictionary<string, TMP_InputField>();

    // --- Áreas da Interface ---
    private RectTransform formScrollContent;
    private GameObject dndContainer;
    private GameObject ordemContainer;
    private GameObject leftBarsDnD;
    private GameObject leftBarsOrdem;

    private float dndBaseHeight = 1000f;
    private float ordemBaseHeight = 1200f;
    private float magicSectionHeight = 450f;

    // Variáveis rápidas apenas para o painel principal (O resto usa o _fieldMap)
    private TMP_InputField dndName, dndRace, dndClass, dndLevel, dndHPCurr, dndHPMax, dndAC, dndSpd;
    private TMP_InputField ordemName, ordemClass, ordemTrilha, ordemNEX, ordemPVCurr, ordemPVMax, ordemPECurr, ordemPEMax, ordemSANCurr, ordemSANMax, ordemDefesa;

    private GameObject dndSpellsContainer;
    private bool dndSpellsOpen = false;

    private GameObject ordemRituaisContainer;
    private bool ordemRituaisOpen = false;

    private Texture2D currentAvatarTex = null;
    private Image avatarPreview;

    private void Awake()
    {
        Instance = this;
        BuildFullScreenUI();
        _mainScreen.SetActive(false);
    }

    private void BuildFullScreenUI()
    {
        Canvas cv = FindAnyObjectByType<Canvas>();
        if (cv == null) return;

        _mainScreen = VTTLayout.New("CharacterCreatorScreen", cv.transform);
        RectTransform screenRT = _mainScreen.AddComponent<RectTransform>();
        screenRT.anchorMin = Vector2.zero; screenRT.anchorMax = Vector2.one;
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

        GameObject div = VTTLayout.New("Div", centerRT);
        RectTransform divRT = div.AddComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0, 0); divRT.anchorMax = new Vector2(0, 1);
        divRT.pivot = new Vector2(0, 0);
        divRT.offsetMin = new Vector2(leftWidth, 0); divRT.offsetMax = new Vector2(leftWidth + 2f, 0);
        VTTLayout.Deco(div, VTTLayout.C_BDR_DEFAULT);

        avatarPreview = VTTLayout.MakeMaskedAvatar(leftRT, Vector2.zero, new Vector2(180f, 180f), VTTLayout.C_CONTENT_BG);
        RectTransform maskRT = avatarPreview.transform.parent.GetComponent<RectTransform>();
        maskRT.anchorMin = new Vector2(0.5f, 1f); maskRT.anchorMax = new Vector2(0.5f, 1f);
        maskRT.pivot = new Vector2(0.5f, 1f);
        maskRT.sizeDelta = new Vector2(180f, 180f);
        maskRT.anchoredPosition = new Vector2(0f, -40f);

        Button btnAvatar = VTTLayout.BtnFixed(leftRT, 0, 0, 180f, 36f, "ESCOLHER IMAGEM", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 12f, true);
        RectTransform baRT = btnAvatar.transform.parent.GetComponent<RectTransform>();
        baRT.anchorMin = new Vector2(0.5f, 1f); baRT.anchorMax = new Vector2(0.5f, 1f);
        baRT.pivot = new Vector2(0.5f, 1f);
        baRT.sizeDelta = new Vector2(180f, 36f);
        baRT.anchoredPosition = new Vector2(0f, -240f);
        btnAvatar.onClick.AddListener(DoPickAvatar);

        leftBarsDnD = VTTLayout.New("Bars_DnD", leftRT);
        RectTransform lbDndRT = leftBarsDnD.AddComponent<RectTransform>();
        lbDndRT.anchorMin = new Vector2(0, 1); lbDndRT.anchorMax = new Vector2(1, 1);
        lbDndRT.pivot = new Vector2(0.5f, 1f);
        lbDndRT.anchoredPosition = Vector2.zero;

        CreateVisualBar(lbDndRT, -320f, "HP", "dnd_hp", new Color(0.85f, 0.2f, 0.3f), out dndHPCurr, out dndHPMax, "10");

        leftBarsOrdem = VTTLayout.New("Bars_Ordem", leftRT);
        RectTransform lbOrdemRT = leftBarsOrdem.AddComponent<RectTransform>();
        lbOrdemRT.anchorMin = new Vector2(0, 1); lbOrdemRT.anchorMax = new Vector2(1, 1);
        lbOrdemRT.pivot = new Vector2(0.5f, 1f);
        lbOrdemRT.anchoredPosition = Vector2.zero;

        CreateVisualBar(lbOrdemRT, -320f, "PV", "ord_pv", new Color(0.85f, 0.2f, 0.3f), out ordemPVCurr, out ordemPVMax, "20");
        CreateVisualBar(lbOrdemRT, -380f, "PE", "ord_pe", new Color(0.2f, 0.5f, 0.85f), out ordemPECurr, out ordemPEMax, "15");
        CreateVisualBar(lbOrdemRT, -440f, "SAN", "ord_san", new Color(0.6f, 0.2f, 0.85f), out ordemSANCurr, out ordemSANMax, "25");

        Button btnSave = VTTLayout.BtnFixed(leftRT, 0, 0, 240f, 50f, "SALVAR FICHA", VTTLayout.C_BTN_PRI, VTTLayout.C_BDR_ACC, VTTLayout.C_TEXT, 14f, true);
        RectTransform bsRT = btnSave.transform.parent.GetComponent<RectTransform>();
        bsRT.anchorMin = new Vector2(0.5f, 0f); bsRT.anchorMax = new Vector2(0.5f, 0f);
        bsRT.pivot = new Vector2(0.5f, 0f);
        bsRT.anchoredPosition = new Vector2(0f, 30f);
        btnSave.onClick.AddListener(DoSave);

        Button btnCancel = VTTLayout.BtnFixed(leftRT, 0, 0, 240f, 40f, "CANCELAR", VTTLayout.C_BTN_CLOSE, VTTLayout.C_BDR_CLOSE, VTTLayout.C_TEXT, 12f, true);
        RectTransform bcRT = btnCancel.transform.parent.GetComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0.5f, 0f); bcRT.anchorMax = new Vector2(0.5f, 0f);
        bcRT.pivot = new Vector2(0.5f, 0f);
        bcRT.anchoredPosition = new Vector2(0f, 90f);
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

    // --- FUNÇÕES DE REGISTO ---
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

        dndRace = CreateField(dndRT, x, y, 310f, "RAÇA", "dnd_race");
        dndClass = CreateField(dndRT, x + 325f, y, 310f, "CLASSE", "dnd_class");
        dndLevel = CreateField(dndRT, x + 650f, y, 310f, "NÍVEL", "dnd_level");
        y -= 80f;

        CreateField(dndRT, x, y, 472.5f, "ANTECEDENTE", "dnd_bkg");
        CreateField(dndRT, x + 487.5f, y, 472.5f, "TENDÊNCIA", "dnd_align");
        y -= 90f;

        VTTLayout.LabelFixed(dndRT, x, y, 960f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "ATRIBUTOS PRINCIPAIS";
        y -= 25f;
        float aw = 145f; float ag = 18f;
        CreateField(dndRT, x + (aw + ag) * 0, y, aw, "FORÇA", "dnd_str");
        CreateField(dndRT, x + (aw + ag) * 1, y, aw, "DESTREZA", "dnd_dex");
        CreateField(dndRT, x + (aw + ag) * 2, y, aw, "CONSTITUIÇÃO", "dnd_con");
        CreateField(dndRT, x + (aw + ag) * 3, y, aw, "INTELIGÊNCIA", "dnd_int");
        CreateField(dndRT, x + (aw + ag) * 4, y, aw, "SABEDORIA", "dnd_wis");
        CreateField(dndRT, x + (aw + ag) * 5, y, aw, "CARISMA", "dnd_cha");
        y -= 85f;

        VTTLayout.LabelFixed(dndRT, x, y, 960f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "COMBATE";
        y -= 25f;
        float cbW = 180f; float cbGap = 15f;
        dndAC = CreateField(dndRT, x + (cbW + cbGap) * 0, y, cbW, "CLASSE ARMADURA (CA)", "dnd_ac");
        dndSpd = CreateField(dndRT, x + (cbW + cbGap) * 1, y, cbW, "DESLOCAMENTO", "dnd_spd");
        CreateField(dndRT, x + (cbW + cbGap) * 2, y, cbW, "INICIATIVA", "dnd_init");
        CreateField(dndRT, x + (cbW + cbGap) * 3, y, cbW, "BÔNUS PROFICIÊNCIA", "dnd_prof");
        CreateField(dndRT, x + (cbW + cbGap) * 4, y, cbW, "INSPIRAÇÃO", "dnd_insp");
        y -= 90f;

        VTTLayout.LabelFixed(dndRT, x, y, 960f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "PERÍCIAS";
        y -= 25f;
        string[] dndSkills = { "Acrobacia", "Arcanismo", "Atletismo", "Atuação", "Enganação", "Furtividade", "História", "Intimidação", "Intuição", "Investigação", "Lidar Animais", "Medicina", "Natureza", "Percepção", "Persuasão", "Prestidigitação", "Religião", "Sobrevivência" };
        int rows = Mathf.CeilToInt(dndSkills.Length / 3f);
        for (int i = 0; i < dndSkills.Length; i++)
        {
            CreateSkillField(dndRT, x + ((i % 3) * 330f), y - ((i / 3) * 40f), 300f, dndSkills[i], "dnd_skill_" + i);
        }
        y -= (rows * 40f) + 30f;

        VTTLayout.LabelFixed(dndRT, x, y, 472.5f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "TRAÇOS E CARACTERÍSTICAS";
        VTTLayout.LabelFixed(dndRT, x + 487.5f, y, 472.5f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "EQUIPAMENTO E TESOURO";
        y -= 25f;
        Reg("dnd_traits", VTTLayout.InputFieldMultiline(dndRT, x, y, 472.5f, 180f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));
        Reg("dnd_equip", VTTLayout.InputFieldMultiline(dndRT, x + 487.5f, y, 472.5f, 180f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));
        y -= 210f;

        Button btnSpells = VTTLayout.BtnFixed(dndRT, x, y, 960f, 40f, "MAGIAS E ESPAÇOS DE MAGIA ▼", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, cDnd, 14f, true);
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
        CreateField(spRT, x + 645f, sy, 300f, "BÔNUS DE ATAQUE", "dnd_magic_atk");
        sy -= 80f;

        VTTLayout.LabelFixed(spRT, x + 15f, sy, 465f, 20f, 14f, cDnd, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "ESPAÇOS DE MAGIA (SLOTS)";
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
        CreateField(ordemRT, x + (aw + ag) * 3, y, aw, "PRESENÇA", "ord_pre");
        CreateField(ordemRT, x + (aw + ag) * 4, y, aw, "FORÇA", "ord_for");
        y -= 85f;

        VTTLayout.LabelFixed(ordemRT, x, y, 960f, 20f, 14f, cOrdem, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "DEFESAS";
        y -= 25f;
        float cw = 310f; float cg = 15f;
        ordemDefesa = CreateField(ordemRT, x + (cw + cg) * 0, y, cw, "DEFESA BASE", "ord_defesa");
        CreateField(ordemRT, x + (cw + cg) * 1, y, cw, "ESQUIVA", "ord_esq");
        CreateField(ordemRT, x + (cw + cg) * 2, y, cw, "BLOQUEIO", "ord_bloq");
        y -= 90f;

        VTTLayout.LabelFixed(ordemRT, x, y, 960f, 20f, 14f, cOrdem, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "PERÍCIAS";
        y -= 25f;
        string[] ordemSkillsList = { "Acrobacia", "Adestramento", "Artes", "Atletismo", "Atualidades", "Ciências", "Crime", "Diplomacia", "Enganação", "Fortitude", "Furtividade", "Iniciativa", "Intimidação", "Intuição", "Investigação", "Luta", "Medicina", "Ocultismo", "Percepção", "Pilotagem", "Pontaria", "Profissão", "Reflexos", "Religião", "Sobrevivência", "Tática", "Tecnologia", "Vontade" };
        int rRows = Mathf.CeilToInt(ordemSkillsList.Length / 3f);
        for (int i = 0; i < ordemSkillsList.Length; i++)
        {
            CreateSkillField(ordemRT, x + ((i % 3) * 330f), y - ((i / 3) * 40f), 300f, ordemSkillsList[i], "ord_skill_" + i);
        }
        y -= (rRows * 40f) + 30f;

        VTTLayout.LabelFixed(ordemRT, x, y, 472.5f, 20f, 14f, cOrdem, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "HABILIDADES E PODERES";
        VTTLayout.LabelFixed(ordemRT, x + 487.5f, y, 472.5f, 20f, 14f, cOrdem, FontStyles.Bold, TextAlignmentOptions.BottomLeft).text = "INVENTÁRIO (Peso/Espaços)";
        y -= 25f;
        Reg("ord_powers", VTTLayout.InputFieldMultiline(ordemRT, x, y, 472.5f, 200f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));
        Reg("ord_inv", VTTLayout.InputFieldMultiline(ordemRT, x + 487.5f, y, 472.5f, 200f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, ""));
        y -= 230f;

        Button btnRituais = VTTLayout.BtnFixed(ordemRT, x, y, 960f, 40f, "RITUAIS PARANORMAIS ▼", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, cOrdem, 14f, true);
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
        if (_currentSystem == "D&D 5e") formScrollContent.sizeDelta = new Vector2(0, dndBaseHeight + (dndSpellsOpen ? magicSectionHeight : 0));
        else formScrollContent.sizeDelta = new Vector2(0, ordemBaseHeight + (ordemRituaisOpen ? magicSectionHeight : 0));
    }

    private void DoPickAvatar()
    {
        if (MapFileLoader.Instance != null)
        {
            MapFileLoader.Instance.OpenFilePicker((tex) => {
                // Previne lixo se o usuário trocar a imagem duas vezes na mesma tela
                if (_isNewAvatar && currentAvatarTex != null) Destroy(currentAvatarTex);
                if (_previewSprite != null) Destroy(_previewSprite);

                currentAvatarTex = tex;
                _isNewAvatar = true;
                _avatarChanged = true;

                _previewSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                avatarPreview.sprite = _previewSprite;
                avatarPreview.color = Color.white;
            });
        }
    }

    private void DoSave()
    {
        string cGold = "#E8C84A", cBlue = "#598CD9", cRed = "#D95959", cPurp = "#9D59D9";

        CharacterRecord newChar = new CharacterRecord();
        newChar.id = string.IsNullOrEmpty(_editingId) ? System.Guid.NewGuid().ToString() : _editingId;
        newChar.system = _currentSystem;

        if (_currentSystem == "D&D 5e")
        {
            newChar.name = string.IsNullOrEmpty(dndName.text) ? "Aventureiro" : dndName.text;
            newChar.subText = $"Lvl {dndLevel.text} • {dndRace.text} {dndClass.text}";
            newChar.statsStr = $"HP: <color={cGold}>{dndHPCurr.text}/{dndHPMax.text}</color>   CA: <color={cRed}>{dndAC.text}</color>   MOV: <color={cBlue}>{dndSpd.text}</color>";
        }
        else
        {
            newChar.name = string.IsNullOrEmpty(ordemName.text) ? "Agente" : ordemName.text;
            newChar.subText = $"NEX {ordemNEX.text}% • {ordemClass.text} {ordemTrilha.text}";
            newChar.statsStr = $"PV: <color={cGold}>{ordemPVCurr.text}/{ordemPVMax.text}</color>   PE: <color={cBlue}>{ordemPECurr.text}/{ordemPEMax.text}</color>   SAN: <color={cPurp}>{ordemSANCurr.text}/{ordemSANMax.text}</color>   DEF: <color={cRed}>{ordemDefesa.text}</color>";
        }

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

        if (_previewSprite != null) { Destroy(_previewSprite); _previewSprite = null; }
        _isNewAvatar = false;

        ClosePanel();
    }

    // --- NOVA: ABRE A FICHA PARA EDIÇÃO ---
    public void OpenForEdit(CharacterRecord record)
    {
        // 1. Limpeza inicial de lixo da sessão anterior
        if (_previewSprite != null) { Destroy(_previewSprite); _previewSprite = null; }
        _isNewAvatar = false;

        _editingId = record.id;
        _currentSystem = record.system;
        _avatarChanged = false;
        _headerTitle.text = "EDITANDO FICHA: " + _currentSystem.ToUpper();

        // Limpa lixo anterior
        foreach (var kvp in _fieldMap) kvp.Value.text = "";

        // 2. Carrega foto do HD e aplica a correção do Sprite AQUI!
        currentAvatarTex = CharacterManager.Instance.LoadAvatar(record.avatarFileName);
        if (currentAvatarTex != null)
        {
            _previewSprite = Sprite.Create(currentAvatarTex, new Rect(0, 0, currentAvatarTex.width, currentAvatarTex.height), new Vector2(0.5f, 0.5f));
            avatarPreview.sprite = _previewSprite;
            avatarPreview.color = Color.white;
        }
        else
        {
            avatarPreview.color = Color.clear;
        }

        // Carrega todos os campos!
        foreach (var field in record.fields)
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
        _mainScreen.SetActive(true);
        _mainScreen.transform.SetAsLastSibling();
    }

    public void OpenPanel(string systemName)
    {
        // 1. Limpeza inicial de lixo da sessão anterior
        if (_previewSprite != null) { Destroy(_previewSprite); _previewSprite = null; }
        _isNewAvatar = false;

        _editingId = null; // Modo de Criação Limpo
        _avatarChanged = true;
        _currentSystem = systemName;
        _headerTitle.text = "MONTAGEM DE FICHA: " + systemName.ToUpper();

        // 2. Garante que a foto está vazia para a ficha nova
        currentAvatarTex = null;
        avatarPreview.color = Color.clear;

        foreach (var kvp in _fieldMap) kvp.Value.text = "";
        if (dndHPCurr != null) { dndHPCurr.text = "10"; dndHPMax.text = "10"; }
        if (ordemPVCurr != null) { ordemPVCurr.text = "20"; ordemPVMax.text = "20"; ordemPECurr.text = "10"; ordemPEMax.text = "10"; ordemSANCurr.text = "25"; ordemSANMax.text = "25"; }

        dndSpellsOpen = false; if (dndSpellsContainer != null) dndSpellsContainer.SetActive(false);
        ordemRituaisOpen = false; if (ordemRituaisContainer != null) ordemRituaisContainer.SetActive(false);

        leftBarsDnD.SetActive(systemName == "D&D 5e"); leftBarsOrdem.SetActive(systemName == "Ordem Paranormal");
        dndContainer.SetActive(systemName == "D&D 5e"); ordemContainer.SetActive(systemName == "Ordem Paranormal");

        UpdateScrollHeight();
        formScrollContent.anchoredPosition = new Vector2(formScrollContent.anchoredPosition.x, 0);
        _mainScreen.SetActive(true);
        _mainScreen.transform.SetAsLastSibling();
    }

    public void ClosePanel()
    {
        if (_previewSprite != null) { Destroy(_previewSprite); _previewSprite = null; }
        if (_isNewAvatar && currentAvatarTex != null) { Destroy(currentAvatarTex); currentAvatarTex = null; }
        _isNewAvatar = false;

        _mainScreen.SetActive(false);
    }
}