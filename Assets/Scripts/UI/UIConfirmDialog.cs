using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIConfirmDialog : MonoBehaviour
{
    private static UIConfirmDialog instance;

    private Canvas dialogCanvas;
    private GameObject panel;
    private TMP_Text titleText;
    private TMP_Text messageText;
    private System.Action onConfirm;

    public static void Show(string title, string message, System.Action confirmAction)
    {
        UIConfirmDialog dialog = GetOrCreate();
        dialog.Open(title, message, confirmAction);
    }

    private static UIConfirmDialog GetOrCreate()
    {
        if (instance != null) return instance;

        GameObject go = new GameObject("UIConfirmDialogCanvas");
        instance = go.AddComponent<UIConfirmDialog>();
        instance.Build();
        return instance;
    }

    private void Build()
    {
        dialogCanvas = gameObject.AddComponent<Canvas>();
        dialogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogCanvas.overrideSorting = true;
        dialogCanvas.sortingOrder = 30000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        panel = VTTLayout.New("ConfirmOverlay", transform);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        panelRT.sizeDelta = Vector2.zero;

        Image dim = panel.AddComponent<Image>();
        dim.color = new Color(0.02f, 0.025f, 0.035f, 0.82f);
        dim.raycastTarget = true;

        RectTransform cardRT = VTTLayout.Box("ConfirmCard", panelRT, 0f, 0f, 430f, 220f, VTTLayout.C_SEC_BG);
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.sizeDelta = new Vector2(430f, 220f);
        VTTLayout.AccentBar(cardRT, 5f, VTTLayout.C_TEXT_WARN);

        titleText = VTTLayout.LabelFixed(cardRT, 28f, -22f, 374f, 34f, 18f, VTTLayout.C_TEXT_PANEL, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        messageText = VTTLayout.LabelFixed(cardRT, 28f, -66f, 374f, 72f, 12.5f, VTTLayout.C_TEXT, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        messageText.enableWordWrapping = true;

        Button cancelBtn = VTTLayout.BtnFixed(cardRT, 28f, -160f, 176f, 38f, "CANCELAR", VTTLayout.C_BTN_SEC, VTTLayout.C_BDR_DEFAULT, VTTLayout.C_TEXT, 11f, true);
        cancelBtn.onClick.AddListener(Close);

        Button confirmBtn = VTTLayout.BtnFixed(cardRT, 226f, -160f, 176f, 38f, "CONFIRMAR", VTTLayout.C_BTN_CLOSE, VTTLayout.C_BDR_CLOSE, VTTLayout.C_TEXT, 11f, true);
        confirmBtn.onClick.AddListener(() => {
            System.Action action = onConfirm;
            Close();
            action?.Invoke();
        });

        panel.SetActive(false);
    }

    private void Open(string title, string message, System.Action confirmAction)
    {
        onConfirm = confirmAction;
        titleText.text = title;
        messageText.text = message;
        panel.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void Close()
    {
        onConfirm = null;
        panel.SetActive(false);
    }
}
