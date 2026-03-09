// ============================================================
// CharacterCreatorScreen.cs
// Ecrã dinâmico: Gera formulários específicos para D&D ou Ordem.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCreatorScreen : MonoBehaviour
{
    public static CharacterCreatorScreen Instance { get; private set; }

    private GameObject _mainScreen;
    private TMP_Text _headerTitle;
    private string _currentSystem;

    // --- Containers dos Formulários ---
    private GameObject dndContainer;
    private GameObject ordemContainer;

    // --- Inputs D&D 5e ---
    private TMP_InputField dndName;
    private TMP_InputField dndRace;
    private TMP_InputField dndClass;
    private TMP_InputField dndHP;
    private TMP_InputField dndAC;
    private TMP_InputField dndMov;

    // --- Inputs Ordem Paranormal ---
    private TMP_InputField ordemName;
    private TMP_InputField ordemClass;
    private TMP_InputField ordemNEX;
    private TMP_InputField ordemPV;
    private TMP_InputField ordemPE;
    private TMP_InputField ordemSan;
    private TMP_InputField ordemDef;

    // --- Avatar ---
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

        // 1. Painel Base
        _mainScreen = VTTLayout.New("CharacterCreatorScreen", cv.transform);
        RectTransform screenRT = _mainScreen.AddComponent<RectTransform>();
        screenRT.anchorMin = Vector2.zero; screenRT.anchorMax = Vector2.one;
        screenRT.sizeDelta = Vector2.zero;
        Image bgImg = _mainScreen.AddComponent<Image>();
        bgImg.color = VTTLayout.C_BG;
        bgImg.raycastTarget = true;

        // 2. Cabeçalho
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

        _headerTitle = VTTLayout.LabelFixed(headerRT, 40f, 0f, 600f, headerH, 22f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold);
        _headerTitle.alignment = TextAlignmentOptions.MidlineLeft;

        // 3. Área Central (Caixa do Formulário)
        GameObject formBox = VTTLayout.New("FormBox", screenRT);
        RectTransform formRT = formBox.AddComponent<RectTransform>();
        formRT.anchorMin = new Vector2(0.5f, 0.5f); formRT.anchorMax = new Vector2(0.5f, 0.5f);
        formRT.pivot = new Vector2(0.5f, 0.5f);
        formRT.anchoredPosition = new Vector2(0, -20f);
        formRT.sizeDelta = new Vector2(800f, 500f);
        VTTLayout.Deco(formBox, VTTLayout.C_SEC_BG);

        // --- COLUNA ESQUERDA: AVATAR ---
        avatarPreview = VTTLayout.MakeMaskedAvatar(formRT, new Vector2(50f, -60f), new Vector2(220f, 220f), VTTLayout.C_CONTENT_BG);
        avatarPreview.rectTransform.anchorMin = new Vector2(0, 1); avatarPreview.rectTransform.anchorMax = new Vector2(0, 1);
        avatarPreview.rectTransform.pivot = new Vector2(0, 1);

        Button btnAvatar = VTTLayout.BtnFixed(formRT, 50f, -300f, 220f, 40f, "ESCOLHER IMAGEM", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 12f, true);
        btnAvatar.onClick.AddListener(DoPickAvatar);

        // =======================================================
        // CONSTRUÇÃO DOS FORMULÁRIOS DINÂMICOS
        // =======================================================
        float startX = 310f;
        float formW = 440f;

        // --- FORMULÁRIO D&D 5E ---
        dndContainer = VTTLayout.New("Form_DnD", formRT);
        RectTransform dndRT = dndContainer.AddComponent<RectTransform>();
        dndRT.anchorMin = Vector2.zero; dndRT.anchorMax = Vector2.one; dndRT.sizeDelta = Vector2.zero;

        float y = -60f;
        float gapY = 80f;

        VTTLayout.LabelFixed(dndRT, startX, y, formW, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "NOME DO PERSONAGEM (D&D 5E)";
        dndName = VTTLayout.InputFieldFixed(dndRT, startX, y - 25f, formW, 40f, 16f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, "");

        y -= gapY;
        VTTLayout.LabelFixed(dndRT, startX, y, 210f, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "RAÇA / ANTECEDENTE";
        dndRace = VTTLayout.InputFieldFixed(dndRT, startX, y - 25f, 210f, 40f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, "");

        VTTLayout.LabelFixed(dndRT, startX + 230f, y, 210f, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "CLASSE E NÍVEL";
        dndClass = VTTLayout.InputFieldFixed(dndRT, startX + 230f, y - 25f, 210f, 40f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, "");

        y -= gapY;
        VTTLayout.LabelFixed(dndRT, startX, y, 130f, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "HP ATUAL/MÁX";
        dndHP = VTTLayout.InputFieldFixed(dndRT, startX, y - 25f, 130f, 40f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, "");

        VTTLayout.LabelFixed(dndRT, startX + 155f, y, 130f, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "ARMADURA (CA)";
        dndAC = VTTLayout.InputFieldFixed(dndRT, startX + 155f, y - 25f, 130f, 40f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, "");

        VTTLayout.LabelFixed(dndRT, startX + 310f, y, 130f, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "DESLOCAMENTO";
        dndMov = VTTLayout.InputFieldFixed(dndRT, startX + 310f, y - 25f, 130f, 40f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, "");

        // --- FORMULÁRIO ORDEM PARANORMAL ---
        ordemContainer = VTTLayout.New("Form_Ordem", formRT);
        RectTransform ordemRT = ordemContainer.AddComponent<RectTransform>();
        ordemRT.anchorMin = Vector2.zero; ordemRT.anchorMax = Vector2.one; ordemRT.sizeDelta = Vector2.zero;

        y = -60f;
        VTTLayout.LabelFixed(ordemRT, startX, y, formW, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "NOME DO AGENTE (ORDEM PARANORMAL)";
        ordemName = VTTLayout.InputFieldFixed(ordemRT, startX, y - 25f, formW, 40f, 16f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, "");

        y -= gapY;
        VTTLayout.LabelFixed(ordemRT, startX, y, 210f, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "ORIGEM E CLASSE";
        ordemClass = VTTLayout.InputFieldFixed(ordemRT, startX, y - 25f, 210f, 40f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, "");

        VTTLayout.LabelFixed(ordemRT, startX + 230f, y, 210f, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "NEX E PATENTE";
        ordemNEX = VTTLayout.InputFieldFixed(ordemRT, startX + 230f, y - 25f, 210f, 40f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, "");

        y -= gapY;
        float qW = 95f; // Quatro campos divididos
        VTTLayout.LabelFixed(ordemRT, startX, y, qW, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "VIDA (PV)";
        ordemPV = VTTLayout.InputFieldFixed(ordemRT, startX, y - 25f, qW, 40f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, "");

        VTTLayout.LabelFixed(ordemRT, startX + 115f, y, qW, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "ESFORÇO (PE)";
        ordemPE = VTTLayout.InputFieldFixed(ordemRT, startX + 115f, y - 25f, qW, 40f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, "");

        VTTLayout.LabelFixed(ordemRT, startX + 230f, y, qW, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "SANIDADE";
        ordemSan = VTTLayout.InputFieldFixed(ordemRT, startX + 230f, y - 25f, qW, 40f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, "");

        VTTLayout.LabelFixed(ordemRT, startX + 345f, y, qW, 20f, 11f, VTTLayout.C_TEXT_DIM, FontStyles.Normal, TextAlignmentOptions.BottomLeft).text = "DEFESA";
        ordemDef = VTTLayout.InputFieldFixed(ordemRT, startX + 345f, y - 25f, qW, 40f, 14f, VTTLayout.C_TEXT, FontStyles.Normal, "");

        // --- BOTÕES FINAIS ---
        float bottomY = -420f;
        Button btnCancel = VTTLayout.BtnFixed(formRT, startX, bottomY, 180f, 44f, "CANCELAR", VTTLayout.C_BTN_CLEAR, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT_DIM, 14f, true);
        btnCancel.onClick.AddListener(ClosePanel);

        Button btnSave = VTTLayout.BtnFixed(formRT, startX + 200f, bottomY, 240f, 44f, "CONCLUIR FICHA", VTTLayout.C_BTN_PRI, VTTLayout.C_BDR_ACC, VTTLayout.C_TEXT, 14f, true);
        btnSave.onClick.AddListener(DoSave);
    }

    private void DoPickAvatar()
    {
        if (MapFileLoader.Instance != null)
        {
            MapFileLoader.Instance.OpenFilePicker((tex) =>
            {
                currentAvatarTex = tex;
                avatarPreview.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                avatarPreview.color = Color.white;
            });
        }
    }

    private void DoSave()
    {
        string n, subText, statsStr;

        // Paleta de Cores para o RichText dos atributos
        string cGold = "#E8C84A"; // Amarelo/Vida
        string cBlue = "#598CD9"; // Azul/Movimento ou PE
        string cRed = "#D95959"; // Vermelho/Defesa ou CA
        string cPurp = "#9D59D9"; // Roxo/Sanidade

        if (_currentSystem == "D&D 5e")
        {
            n = string.IsNullOrEmpty(dndName.text) ? "Aventureiro" : dndName.text;
            subText = "D&D 5e • " + dndRace.text + " " + dndClass.text;
            statsStr = $"HP: <color={cGold}>{dndHP.text}</color>   CA: <color={cRed}>{dndAC.text}</color>   MOV: <color={cBlue}>{dndMov.text}</color>";
        }
        else
        {
            n = string.IsNullOrEmpty(ordemName.text) ? "Agente" : ordemName.text;
            subText = "Ordem Paranormal • " + ordemClass.text + " " + ordemNEX.text;
            statsStr = $"PV: <color={cGold}>{ordemPV.text}</color>   PE: <color={cBlue}>{ordemPE.text}</color>   SAN: <color={cPurp}>{ordemSan.text}</color>   DEF: <color={cRed}>{ordemDef.text}</color>";
        }

        if (DashboardOverlay.Instance != null)
        {
            DashboardOverlay.Instance.AddCharacter(n, subText, statsStr, currentAvatarTex);
        }

        ClosePanel();
    }

    public void OpenPanel(string systemName)
    {
        _currentSystem = systemName;
        _headerTitle.text = "CRIANDO PERSONAGEM - " + systemName.ToUpper();

        // Limpa os campos
        dndName.text = ""; dndRace.text = ""; dndClass.text = ""; dndHP.text = ""; dndAC.text = ""; dndMov.text = "";
        ordemName.text = ""; ordemClass.text = ""; ordemNEX.text = ""; ordemPV.text = ""; ordemPE.text = ""; ordemSan.text = ""; ordemDef.text = "";

        currentAvatarTex = null;
        avatarPreview.color = Color.clear;

        // Ativa apenas o painel do sistema escolhido
        dndContainer.SetActive(systemName == "D&D 5e");
        ordemContainer.SetActive(systemName == "Ordem Paranormal");

        _mainScreen.SetActive(true);
        _mainScreen.transform.SetAsLastSibling();
    }

    public void ClosePanel()
    {
        _mainScreen.SetActive(false);
    }
}