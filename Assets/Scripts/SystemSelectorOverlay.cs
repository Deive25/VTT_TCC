// ============================================================
// SystemSelectorOverlay.cs
// Pop-up inicial para escolher o Sistema de RPG antes de criar a ficha.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SystemSelectorOverlay : MonoBehaviour
{
    public static SystemSelectorOverlay Instance { get; private set; }
    private GameObject _panel;

    private void Awake()
    {
        Instance = this;
        BuildUI();
        _panel.SetActive(false);
    }

    private void BuildUI()
    {
        Canvas cv = FindAnyObjectByType<Canvas>();
        if (cv == null) return;

        // Fundo Escuro Overlay
        _panel = VTTLayout.New("SystemSelectorPanel", cv.transform);
        RectTransform panelRT = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero; panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;
        Image imgOverlay = _panel.AddComponent<Image>();
        imgOverlay.color = new Color(0, 0, 0, 0.85f);
        imgOverlay.raycastTarget = true;

        // Caixa Modal Central
        GameObject modal = VTTLayout.New("Modal", _panel.transform);
        RectTransform modalRT = modal.AddComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f); modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(460f, 260f);
        VTTLayout.Deco(modal, VTTLayout.C_SEC_BG);
        VTTLayout.AccentBar(modalRT, 4f, VTTLayout.C_ACCENT);

        // Textos
        VTTLayout.LabelFixed(modalRT, 0, -30f, 460f, 30f, 18f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold).text = "NOVA FICHA";
        VTTLayout.LabelFixed(modalRT, 0, -70f, 460f, 20f, 14f, VTTLayout.C_TEXT_DIM, FontStyles.Normal).text = "Qual sistema de RPG deseja utilizar?";

        // Botões de Sistema
        float btnW = 180f;
        float btnH = 60f;
        float gap = 20f;
        float startX = (460f - (btnW * 2 + gap)) / 2f;

        Button btnDnD = VTTLayout.BtnFixed(modalRT, startX, -120f, btnW, btnH, "D&D 5E", VTTLayout.C_BTN_PRI, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 14f, true);
        btnDnD.onClick.AddListener(() => OpenCreator("D&D 5e"));

        Button btnOrdem = VTTLayout.BtnFixed(modalRT, startX + btnW + gap, -120f, btnW, btnH, "ORDEM\nPARANORMAL", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 14f, true);
        btnOrdem.onClick.AddListener(() => OpenCreator("Ordem Paranormal"));

        // Botão Cancelar
        Button btnCancel = VTTLayout.BtnFixed(modalRT, 130f, -200f, 200f, 34f, "CANCELAR", VTTLayout.C_BTN_CLEAR, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT_DIM, 12f, true);
        btnCancel.onClick.AddListener(ClosePanel);
    }

    private void OpenCreator(string systemName)
    {
        ClosePanel();
        CharacterCreatorScreen creator = FindAnyObjectByType<CharacterCreatorScreen>();
        if (creator != null)
            creator.OpenPanel(systemName);
        else
            Debug.LogError("[SystemSelector] CharacterCreatorScreen não encontrado na cena!");
    }

    public void OpenPanel()
    {
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
    }

    public void ClosePanel()
    {
        _panel.SetActive(false);
    }
}