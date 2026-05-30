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
    private RectTransform _modalRT;
    private bool _returnDashboardOnClose = false;

    private void Awake()
    {
        Instance = this;
        BuildUI();
        _panel.SetActive(false);
    }

    private void BuildUI()
    {
        Canvas cv = VTTLayout.GetOverlayCanvas("VTT_MainOverlayCanvas", 13000);

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
        _modalRT = modal.AddComponent<RectTransform>();
        _modalRT.anchorMin = new Vector2(0.5f, 0.5f); _modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        _modalRT.pivot = new Vector2(0.5f, 0.5f);
        _modalRT.sizeDelta = new Vector2(460f, 260f);
        VTTLayout.Deco(modal, VTTLayout.C_SEC_BG);
        VTTLayout.AccentBar(_modalRT, 4f, VTTLayout.C_ACCENT);

        // Textos
        VTTLayout.LabelFixed(_modalRT, 0, -30f, 460f, 30f, 18f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold).text = "NOVA FICHA";
        VTTLayout.LabelFixed(_modalRT, 0, -70f, 460f, 20f, 14f, VTTLayout.C_TEXT_DIM, FontStyles.Normal).text = "Qual sistema de RPG deseja utilizar?";

        // Bot�es de Sistema
        float btnW = 180f;
        float btnH = 60f;
        float gap = 20f;
        float startX = (460f - (btnW * 2 + gap)) / 2f;

        Button btnDnD = VTTLayout.BtnFixed(_modalRT, startX, -120f, btnW, btnH, "D&D 5E", VTTLayout.C_BTN_PRI, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 14f, true);
        btnDnD.onClick.AddListener(() => OpenCreator("D&D 5e"));

        Button btnOrdem = VTTLayout.BtnFixed(_modalRT, startX + btnW + gap, -120f, btnW, btnH, "ORDEM\nPARANORMAL", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 14f, true);
        btnOrdem.onClick.AddListener(() => OpenCreator("Ordem Paranormal"));

        // Bot�o Cancelar
        Button btnCancel = VTTLayout.BtnFixed(_modalRT, 130f, -200f, 200f, 34f, "CANCELAR", VTTLayout.C_BTN_CLEAR, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT_DIM, 12f, true);
        btnCancel.onClick.AddListener(ClosePanel);
    }


    private void FitModalToScreen()
    {
        if (_modalRT == null || _panel == null) return;

        RectTransform panelRT = _panel.GetComponent<RectTransform>();
        float w = panelRT.rect.width > 0f ? panelRT.rect.width : Screen.width;
        float h = panelRT.rect.height > 0f ? panelRT.rect.height : Screen.height;
        float scale = Mathf.Min(1f, (w - 48f) / 460f, (h - 48f) / 260f);
        _modalRT.localScale = Vector3.one * Mathf.Clamp(scale, 0.72f, 1f);
        _modalRT.anchoredPosition = Vector2.zero;
    }
    private void OpenCreator(string systemName)
    {
        // Fecha apenas o popup de escolha.
        ClosePanelInternal(false);

        // Agora sim o Dashboard sai da frente, pois a proxima tela sera a ficha.
        if (DashboardOverlay.Instance != null)
            DashboardOverlay.Instance.HideForChildPanel();

        CharacterCreatorScreen creator = FindAnyObjectByType<CharacterCreatorScreen>();
        if (creator != null)
            creator.OpenPanel(systemName);
        else
        {
            Debug.LogError("[SystemSelector] CharacterCreatorScreen nao encontrado na cena!");
            if (DashboardOverlay.Instance != null)
                DashboardOverlay.Instance.OpenPanel();
        }
    }

    public void OpenPanel()
    {
        OpenPanel(false);
    }

    public void OpenPanel(bool returnDashboardOnClose)
    {
        _returnDashboardOnClose = returnDashboardOnClose;

        _panel.SetActive(true);
        FitModalToScreen();
        _panel.transform.SetAsLastSibling();
    }

    public void ClosePanel()
    {
        ClosePanelInternal(_returnDashboardOnClose);
    }

    private void ClosePanelInternal(bool reopenDashboard)
    {
        _panel.SetActive(false);

        bool shouldReopenDashboard = reopenDashboard;
        _returnDashboardOnClose = false;

        if (shouldReopenDashboard && DashboardOverlay.Instance != null)
            DashboardOverlay.Instance.OpenPanel();
    }
}