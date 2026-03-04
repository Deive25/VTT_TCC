// DiceRollOverlay.cs  v6
// ASCII puro. Layout Groups. Multi-dado com historico.
// Nao fecha automaticamente. Botao X para fechar.

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceRollOverlay : MonoBehaviour
{
    private static readonly int[] DICE = { 4, 6, 8, 10, 12, 20 };

    public struct RollEntry
    {
        public string descriptor;
        public string breakdown;
        public int total;
    }

    public static DiceRollOverlay Instance { get; private set; }

    public List<RollEntry> History { get; } = new List<RollEntry>();
    private const int MAX_HIST = 20;
    public event System.Action OnHistoryChanged;

    private readonly int[] _counts = new int[6];
    private TMP_Text[] _countLabels;
    private bool _rolling = false;

    private GameObject _panel;
    private TMP_Text _resultsText;
    private TMP_Text _totalText;
    private TMP_Text _historyText;

    // Cores por tipo de dado (indices paralelos a DICE[])
    private static readonly Color[] DICE_COLORS =
    {
        new Color(0.20f, 0.65f, 0.60f, 1f),  // D4
        new Color(0.28f, 0.52f, 0.85f, 1f),  // D6
        new Color(0.60f, 0.30f, 0.80f, 1f),  // D8
        new Color(0.22f, 0.45f, 0.75f, 1f),  // D10
        new Color(0.45f, 0.22f, 0.65f, 1f),  // D12
        new Color(0.85f, 0.65f, 0.12f, 1f),  // D20
    };

    // --- Lifecycle ---

    private void Awake()
    {
        Instance = this;
        Build();
        _panel.SetActive(false);
    }

    // =========================================================
    // Construcao
    // =========================================================

    private void Build()
    {
        Canvas cv = GetComponent<Canvas>() ?? FindObjectOfType<Canvas>();

        // Overlay de fundo (bloqueia cliques no mapa)
        _panel = VTTUIBuilder.MakeGO("DicePanel", cv.transform);
        RectTransform panelRT = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;
        Image overlayImg = _panel.AddComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.03f, 0.06f, 0.90f);
        overlayImg.raycastTarget = true;

        // Card central
        GameObject card = VTTUIBuilder.MakeGO("DiceCard", _panel.transform);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.sizeDelta = new Vector2(400f, 0f);

        Image cardImg = card.AddComponent<Image>();
        cardImg.color = VTTStyles.BG_SECTION;
        cardImg.raycastTarget = false;

        // VLG no card
        VerticalLayoutGroup cardVLG = card.AddComponent<VerticalLayoutGroup>();
        cardVLG.padding = new RectOffset(0, 0, 0, VTTStyles.GAP);
        cardVLG.spacing = 0;
        cardVLG.childAlignment = TextAnchor.UpperLeft;
        cardVLG.childControlWidth = true;
        cardVLG.childControlHeight = false;
        cardVLG.childForceExpandWidth = true;
        cardVLG.childForceExpandHeight = false;

        ContentSizeFitter csf = card.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Secoes
        BuildCardHeader(cardRT);

        CardSectionHdr(cardRT, "CONFIGURAR DADOS");
        var configSec = CardSection(cardRT);
        _countLabels = new TMP_Text[DICE.Length];
        BuildDiceGrid(configSec);

        var actionSec = CardSection(cardRT, padV: 4);
        BuildActionRow(actionSec);

        CardSectionHdr(cardRT, "RESULTADO");
        var resultSec = CardSection(cardRT);
        BuildResultArea(resultSec);

        CardSectionHdr(cardRT, "HISTORICO DA SESSAO");
        var histSec = CardSection(cardRT);
        _historyText = VTTUIBuilder.InfoBox(histSec, 72f);
        _historyText.text = "Nenhuma rolagem ainda";
        _historyText.color = VTTStyles.TXT_SECOND;
        _historyText.lineSpacing = 6f;
    }

    // --- Header do card ---

    private void BuildCardHeader(RectTransform parent)
    {
        GameObject hdr = VTTUIBuilder.MakeGO("CardHeader", parent);
        Image hdrImg = hdr.AddComponent<Image>();
        hdrImg.color = VTTStyles.BG_HEADER;
        hdrImg.raycastTarget = false;
        LayoutElement hdrLE = hdr.AddComponent<LayoutElement>();
        hdrLE.preferredHeight = VTTStyles.H_PANEL_HDR;
        hdrLE.flexibleWidth = 1f;

        VTTUIBuilder.AccentBar(hdr.GetComponent<RectTransform>(), 3f, VTTStyles.ACCENT_LT);

        TMP_Text title = VTTUIBuilder.StretchText("Title", hdr.transform,
            new Vector2(VTTStyles.PAD_PANEL + 5f, -4f),
            new Vector2(-52f, 4f));
        title.text = "ROLAGEM DE DADOS";
        title.fontSize = VTTStyles.F_TITLE;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.94f, 0.96f, 1.00f, 1f);
        title.alignment = TextAlignmentOptions.MidlineLeft;

        // Botao X (posicionado manualmente dentro do header fixo)
        GameObject xWrap = VTTUIBuilder.MakeGO("CloseWrap", hdr.transform);
        RectTransform xWRT = xWrap.AddComponent<RectTransform>();
        xWRT.anchorMin = new Vector2(1f, 0.5f);
        xWRT.anchorMax = new Vector2(1f, 0.5f);
        xWRT.pivot = new Vector2(1f, 0.5f);
        xWRT.anchoredPosition = new Vector2(-VTTStyles.PAD_PANEL, 0f);
        xWRT.sizeDelta = new Vector2(32f, 26f);
        Image xBdr = xWrap.AddComponent<Image>();
        xBdr.color = VTTStyles.BDR_DANGER;
        xBdr.raycastTarget = false;

        Button closeBtn = VTTUIBuilder.InnerBtn(xWrap.transform, "X",
            VTTStyles.BTN_DANGER, VTTStyles.TXT_PRIMARY, VTTStyles.F_BUTTON, true);
        closeBtn.onClick.AddListener(ClosePanel);
    }

    // --- Grid 2x3 de dados ---

    private void BuildDiceGrid(RectTransform parent)
    {
        int cols = 2;
        for (int row = 0; row < 3; row++)
        {
            GameObject rowGO = VTTUIBuilder.MakeGO("DiceRow" + row, parent);
            Image rowImg = rowGO.AddComponent<Image>();
            rowImg.color = VTTStyles.BG_CLEAR;
            rowImg.raycastTarget = false;
            LayoutElement rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = VTTStyles.H_BUTTON_SM + 4f;
            rowLE.flexibleWidth = 1f;

            HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = VTTStyles.GAP;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            for (int col = 0; col < cols; col++)
            {
                int idx = row * cols + col;
                if (idx >= DICE.Length) break;
                BuildDiceCell(rowGO.transform, idx);
            }
        }
    }

    private void BuildDiceCell(Transform parent, int diceIdx)
    {
        int sides = DICE[diceIdx];
        Color dc = DICE_COLORS[diceIdx];

        GameObject cell = VTTUIBuilder.MakeGO("Cell_D" + sides, parent);
        Image cellImg = cell.AddComponent<Image>();
        cellImg.color = VTTStyles.BG_ITEM;
        cellImg.raycastTarget = false;

        HorizontalLayoutGroup hlg = cell.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(VTTStyles.GAP, VTTStyles.GAP, 2, 2);
        hlg.spacing = VTTStyles.GAP_SM;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Barra de acento colorida
        GameObject ac = VTTUIBuilder.MakeGO("Ac", cell.transform);
        Image acImg = ac.AddComponent<Image>();
        acImg.color = dc;
        acImg.raycastTarget = false;
        ac.AddComponent<LayoutElement>().preferredWidth = 3f;

        // Label do dado
        GameObject lblGO = VTTUIBuilder.MakeGO("DLbl", cell.transform);
        TMP_Text lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text = "D" + sides;
        lbl.fontSize = VTTStyles.F_INFO;
        lbl.fontStyle = FontStyles.Bold;
        lbl.color = dc;
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.raycastTarget = false;
        lblGO.AddComponent<LayoutElement>().preferredWidth = 38f;

        // Spacer
        GameObject sp = VTTUIBuilder.MakeGO("Sp", cell.transform);
        Image spImg = sp.AddComponent<Image>();
        spImg.color = VTTStyles.BG_CLEAR;
        spImg.raycastTarget = false;
        sp.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Botao -
        int capturedIdx = diceIdx;
        GameObject mW = VTTUIBuilder.MakeGO("MinusWrap", cell.transform);
        Image mI = mW.AddComponent<Image>();
        mI.color = VTTStyles.BDR_DEFAULT;
        mI.raycastTarget = false;
        mW.AddComponent<LayoutElement>().preferredWidth = 24f;
        Button minusBtn = VTTUIBuilder.InnerBtn(mW.transform, "-",
            VTTStyles.BTN_SECOND, VTTStyles.TXT_PRIMARY, VTTStyles.F_BUTTON, true);
        minusBtn.onClick.AddListener(() => ChangeCount(capturedIdx, -1));

        // Count label
        GameObject cG = VTTUIBuilder.MakeGO("CntBg", cell.transform);
        Image cI = cG.AddComponent<Image>();
        cI.color = VTTStyles.BG_INSET;
        cI.raycastTarget = false;
        cG.AddComponent<LayoutElement>().preferredWidth = 28f;
        TMP_Text cntLbl = VTTUIBuilder.StretchText("Cnt", cG.transform,
            Vector2.zero, Vector2.zero);
        cntLbl.text = "0";
        cntLbl.fontSize = VTTStyles.F_INFO;
        cntLbl.fontStyle = FontStyles.Bold;
        cntLbl.color = VTTStyles.TXT_PRIMARY;
        cntLbl.alignment = TextAlignmentOptions.Center;
        _countLabels[diceIdx] = cntLbl;

        // Botao +
        GameObject pW = VTTUIBuilder.MakeGO("PlusWrap", cell.transform);
        Image pI = pW.AddComponent<Image>();
        pI.color = VTTStyles.BDR_DEFAULT;
        pI.raycastTarget = false;
        pW.AddComponent<LayoutElement>().preferredWidth = 24f;
        Button plusBtn = VTTUIBuilder.InnerBtn(pW.transform, "+",
            VTTStyles.BTN_SECOND, VTTStyles.TXT_PRIMARY, VTTStyles.F_BUTTON, true);
        plusBtn.onClick.AddListener(() => ChangeCount(capturedIdx, +1));
    }

    // --- Linha de acoes ---

    private void BuildActionRow(RectTransform parent)
    {
        GameObject row = VTTUIBuilder.MakeGO("ActionRow", parent);
        Image rowImg = row.AddComponent<Image>();
        rowImg.color = VTTStyles.BG_CLEAR;
        rowImg.raycastTarget = false;
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = VTTStyles.H_BUTTON;
        le.flexibleWidth = 1f;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = VTTStyles.GAP;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        GameObject cW = VTTUIBuilder.MakeGO("ClearWrap", row.transform);
        Image cI = cW.AddComponent<Image>();
        cI.color = VTTStyles.BDR_DEFAULT;
        cI.raycastTarget = false;
        Button clearBtn = VTTUIBuilder.InnerBtn(cW.transform, "LIMPAR",
            VTTStyles.BTN_NEUTRAL, VTTStyles.TXT_SECOND, VTTStyles.F_BUTTON, true);
        clearBtn.onClick.AddListener(ClearSelection);

        GameObject rW = VTTUIBuilder.MakeGO("RollWrap", row.transform);
        Image rI = rW.AddComponent<Image>();
        rI.color = VTTStyles.BDR_ACCENT;
        rI.raycastTarget = false;
        Button rollBtn = VTTUIBuilder.InnerBtn(rW.transform, "ROLAR",
            VTTStyles.BTN_PRIMARY, VTTStyles.TXT_PRIMARY, VTTStyles.F_BUTTON + 1f, true);
        rollBtn.onClick.AddListener(DoRoll);
    }

    // --- Area de resultado ---

    private void BuildResultArea(RectTransform parent)
    {
        _resultsText = VTTUIBuilder.InfoBox(parent, 36f);
        _resultsText.text = "--";
        _resultsText.color = VTTStyles.TXT_SECOND;
        _resultsText.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject tGO = VTTUIBuilder.MakeGO("TotalCont", parent);
        Image tImg = tGO.AddComponent<Image>();
        tImg.color = VTTStyles.BG_CLEAR;
        tImg.raycastTarget = false;
        LayoutElement tLE = tGO.AddComponent<LayoutElement>();
        tLE.preferredHeight = 24f;
        tLE.flexibleWidth = 1f;

        _totalText = VTTUIBuilder.StretchText("Total", tGO.transform,
            Vector2.zero, Vector2.zero);
        _totalText.text = "";
        _totalText.fontSize = VTTStyles.F_TITLE;
        _totalText.fontStyle = FontStyles.Bold;
        _totalText.color = VTTStyles.TXT_GOLD;
        _totalText.alignment = TextAlignmentOptions.Center;
    }

    // --- Section header interno do card ---

    private void CardSectionHdr(RectTransform parent, string label)
    {
        GameObject go = VTTUIBuilder.MakeGO("CSHdr_" + label, parent);
        Image img = go.AddComponent<Image>();
        img.color = VTTStyles.BG_INSET;
        img.raycastTarget = false;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = VTTStyles.H_SEC_HDR;
        le.flexibleWidth = 1f;

        VTTUIBuilder.AccentBar(go.GetComponent<RectTransform>(), 3f, VTTStyles.ACCENT);

        TMP_Text t = VTTUIBuilder.StretchText("Lbl", go.transform,
            new Vector2(VTTStyles.PAD_PANEL + 5f, 0f),
            new Vector2(-VTTStyles.PAD_PANEL, 0f));
        t.text = label;
        t.fontSize = VTTStyles.F_SECTION;
        t.fontStyle = FontStyles.Bold;
        t.color = VTTStyles.TXT_HEADER;
        t.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private RectTransform CardSection(RectTransform parent, int padV = 0)
    {
        GameObject go = VTTUIBuilder.MakeGO("CSec", parent);
        Image img = go.AddComponent<Image>();
        img.color = VTTStyles.BG_SECTION;
        img.raycastTarget = false;

        VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(
            VTTStyles.PAD_PANEL, VTTStyles.PAD_PANEL,
            VTTStyles.GAP + padV, VTTStyles.GAP + padV);
        vlg.spacing = VTTStyles.GAP;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return go.GetComponent<RectTransform>();
    }

    // =========================================================
    // Logica de rolagem
    // =========================================================

    private void ChangeCount(int idx, int delta)
    {
        _counts[idx] = Mathf.Max(0, _counts[idx] + delta);
        if (_countLabels[idx] != null)
            _countLabels[idx].text = _counts[idx].ToString();
    }

    private void ClearSelection()
    {
        for (int i = 0; i < _counts.Length; i++)
        {
            _counts[i] = 0;
            if (_countLabels[i] != null) _countLabels[i].text = "0";
        }
        if (_resultsText != null)
        {
            _resultsText.text = "--";
            _resultsText.color = VTTStyles.TXT_SECOND;
        }
        if (_totalText != null) _totalText.text = "";
    }

    private void DoRoll()
    {
        if (_rolling) return;
        bool any = false;
        for (int i = 0; i < _counts.Length; i++)
            if (_counts[i] > 0) { any = true; break; }
        if (!any) return;
        StartCoroutine(AnimateRoll());
    }

    private IEnumerator AnimateRoll()
    {
        _rolling = true;

        var spec = new List<(int sides, int count)>();
        for (int i = 0; i < DICE.Length; i++)
            if (_counts[i] > 0) spec.Add((DICE[i], _counts[i]));

        var results = new List<(int sides, int value)>();
        foreach (var (sides, count) in spec)
            for (int k = 0; k < count; k++)
                results.Add((sides, Random.Range(1, sides + 1)));

        var descParts = new List<string>();
        foreach (var (sides, count) in spec)
            descParts.Add(count + "D" + sides);
        string descriptor = string.Join(" + ", descParts);

        _resultsText.text = "...";
        _resultsText.color = VTTStyles.TXT_SECOND;
        _totalText.text = "";

        float elapsed = 0f;
        while (elapsed < 0.65f)
        {
            var sb2 = new StringBuilder();
            foreach (var (sides, _) in results)
                sb2.Append("D" + sides + ":" + Random.Range(1, sides + 1) + "  ");
            _resultsText.text = sb2.ToString().TrimEnd();
            elapsed += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }

        int total = 0;
        var brkParts = new List<string>();
        foreach (var (sides, val) in results)
        {
            string col = val == sides ? "#E8BE3C"
                       : val == 1 ? "#CC3C34"
                                     : "#8AACD0";
            brkParts.Add("<color=" + col + ">D" + sides + ":" + val + "</color>");
            total += val;
        }

        _resultsText.text = string.Join("  ", brkParts);
        _resultsText.color = VTTStyles.TXT_PRIMARY;
        _totalText.text = "TOTAL:  " + total;

        History.Insert(0, new RollEntry
        {
            descriptor = descriptor,
            breakdown = string.Join("  ", brkParts),
            total = total
        });
        if (History.Count > MAX_HIST) History.RemoveAt(History.Count - 1);
        OnHistoryChanged?.Invoke();
        RefreshHistoryText();

        _rolling = false;
    }

    private void RefreshHistoryText()
    {
        if (_historyText == null) return;
        if (History.Count == 0)
        {
            _historyText.text = "Nenhuma rolagem ainda";
            _historyText.color = VTTStyles.TXT_SECOND;
            return;
        }
        var sb = new StringBuilder();
        int show = Mathf.Min(History.Count, 4);
        for (int i = 0; i < show; i++)
        {
            sb.Append(History[i].descriptor + " -> " + History[i].total);
            if (i < show - 1) sb.Append("\n");
        }
        _historyText.text = sb.ToString();
        _historyText.color = VTTStyles.TXT_PRIMARY;
    }

    // --- API Publica ---

    public void OpenPanel()
    {
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
    }

    public void ClosePanel()
    {
        StopAllCoroutines();
        _rolling = false;
        _panel.SetActive(false);
    }
}