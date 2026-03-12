// ============================================================
// DiceRollOverlay.cs  v5
//
// Sem emotes ou caracteres especiais (apenas ASCII + PT-BR).
// Operadores C# corretos (ternario, null-coalescing).
// Multi-dado: contador por tipo.
// Nao fecha automaticamente; botao FECHAR manual.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceRollOverlay : MonoBehaviour
{
    // --- Tipos de dado disponíveis ---------------------------
    private static readonly int[] DICE_TYPES = { 4, 6, 8, 10, 12, 20 };

    // --- Estrutura de entrada no historico -------------------
    public struct RollEntry
    {
        public string descriptor;   // ex: "2D6 + 1D20"
        public int total;
    }

    // --- Singleton -------------------------------------------
    public static DiceRollOverlay Instance { get; private set; }

    // --- Historico publico -----------------------------------
    public List<RollEntry> History { get; } = new List<RollEntry>();
    private const int MAX_HISTORY = 20;
    public event System.Action OnHistoryChanged;

    // --- Estado interno --------------------------------------
    private int[] _counts = new int[6];
    private TMP_Text[] _countLabels = new TMP_Text[6];
    private bool _rolling = false;

    // --- Referencias de UI -----------------------------------
    private GameObject _panel;
    private TMP_Text _resultBreakdown;
    private TMP_Text _resultTotal;
    private TMP_Text _historyText;

    // --- Paleta (local, sem deps externas) -------------------
    private static Color CP(float r, float g, float b, float a = 1f) { return new Color(r, g, b, a); }

    private static readonly Color COL_OVERLAY = CP(0.03f, 0.04f, 0.07f, 0.92f);
    private static readonly Color COL_CARD = CP(0.10f, 0.12f, 0.17f, 1.00f);
    private static readonly Color COL_HDR = CP(0.14f, 0.19f, 0.28f, 1.00f);
    private static readonly Color COL_SEC_BG = CP(0.08f, 0.10f, 0.14f, 1.00f);
    private static readonly Color COL_CONTENT = CP(0.06f, 0.07f, 0.10f, 1.00f);
    private static readonly Color COL_ACCENT = CP(0.24f, 0.42f, 0.65f, 1.00f);
    private static readonly Color COL_BTN_PRI = CP(0.18f, 0.34f, 0.56f, 1.00f);
    private static readonly Color COL_BTN_SEC = CP(0.14f, 0.17f, 0.23f, 1.00f);
    private static readonly Color COL_BTN_CLR = CP(0.20f, 0.20f, 0.26f, 1.00f);
    private static readonly Color COL_BTN_CLSE = CP(0.40f, 0.14f, 0.14f, 1.00f);
    private static readonly Color COL_BDR_DEF = CP(0.24f, 0.32f, 0.46f, 1.00f);
    private static readonly Color COL_BDR_PRI = CP(0.34f, 0.54f, 0.80f, 1.00f);
    private static readonly Color COL_BDR_CLSE = CP(0.65f, 0.22f, 0.22f, 1.00f);
    private static readonly Color COL_TEXT = CP(0.86f, 0.90f, 0.97f, 1.00f);
    private static readonly Color COL_DIM = CP(0.42f, 0.48f, 0.60f, 1.00f);
    private static readonly Color COL_HDR_TEXT = CP(0.68f, 0.76f, 0.90f, 1.00f);
    private static readonly Color COL_GOLD = CP(0.92f, 0.78f, 0.28f, 1.00f);
    private static readonly Color COL_RED = CP(0.82f, 0.26f, 0.22f, 1.00f);

    private static readonly Color[] COL_DICE = {
        CP(0.20f, 0.60f, 0.55f),   // D4  teal
        CP(0.24f, 0.50f, 0.82f),   // D6  azul
        CP(0.55f, 0.28f, 0.75f),   // D8  roxo
        CP(0.20f, 0.42f, 0.70f),   // D10 azul escuro
        CP(0.40f, 0.20f, 0.60f),   // D12 violeta
        CP(0.80f, 0.62f, 0.10f),   // D20 ouro
    };

    // =========================================================
    // Lifecycle
    // =========================================================

    private void Awake()
    {
        Instance = this;
        Build();
        _panel.SetActive(false);
    }

    // =========================================================
    // Construcao do painel
    // =========================================================

    private void Build()
    {
        Canvas cv = GetComponent<Canvas>();
        if (cv == null) cv = FindAnyObjectByType<Canvas>();

        // --- Overlay (bloqueia cliques no mapa) --------------
        _panel = MakeGO("DicePanel", cv.transform);
        RectTransform panelRT = _panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;
        Image overlayImg = _panel.AddComponent<Image>();
        overlayImg.color = COL_OVERLAY;
        overlayImg.raycastTarget = true;  // bloqueia cliques no mapa

        // --- Card central ------------------------------------
        GameObject card = MakeGO("Card", _panel.transform);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.sizeDelta = new Vector2(400f, 520f);
        MakeDeco(card, COL_CARD);

        // Monta o conteudo de cima para baixo
        float y = 0f;
        float cw = 400f;
        float pad = 14f;
        float gap = 6f;

        y = BuildHeader(cardRT, y, cw, pad, gap);
        y = BuildSecHdr(cardRT, y, "CONFIGURAR DADOS");
        y -= 4f;
        y = BuildDiceGrid(cardRT, y, cw, pad, gap);
        y -= 8f;
        y = BuildActionRow(cardRT, y, cw, pad, gap);
        y = BuildSecHdr(cardRT, y - 6f, "RESULTADO");
        y -= 4f;
        y = BuildResultArea(cardRT, y, cw, pad);
        y = BuildSecHdr(cardRT, y - 6f, "HISTORICO");
        y -= 4f;
        BuildHistoryArea(cardRT, y, cw, pad);
    }

    // --- Header (titulo + botao FECHAR) ----------------------

    private float BuildHeader(RectTransform parent, float y, float cw, float pad, float gap)
    {
        float h = 42f;
        float closeSz = 30f;

        // Fundo do header
        GameObject hdrBg = MakeGO("HdrBg", parent);
        RectTransform hrtBG = hdrBg.AddComponent<RectTransform>();
        hrtBG.anchorMin = new Vector2(0f, 1f);
        hrtBG.anchorMax = new Vector2(1f, 1f);
        hrtBG.pivot = new Vector2(0.5f, 1f);
        hrtBG.anchoredPosition = new Vector2(0f, y);
        // Altura Positiva
        hrtBG.sizeDelta = new Vector2(0f, h);
        MakeDeco(hdrBg, COL_HDR);

        // Barra de acento
        GameObject acc = MakeGO("Acc", hrtBG);
        RectTransform accRT = acc.AddComponent<RectTransform>();
        accRT.anchorMin = Vector2.zero;
        accRT.anchorMax = new Vector2(0f, 1f);
        accRT.pivot = new Vector2(0f, 0.5f);
        accRT.anchoredPosition = Vector2.zero;
        accRT.sizeDelta = new Vector2(3f, 0f);
        MakeDeco(acc, COL_ACCENT);

        // Titulo
        TMP_Text title = MakeLabel("Title", parent);
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0f, 1f);
        titleRT.anchoredPosition = new Vector2(pad + 5f, y);
        // Altura Positiva
        titleRT.sizeDelta = new Vector2(-(pad * 2f + closeSz + gap), h);
        title.text = "ROLAGEM DE DADOS";
        title.fontSize = 12f;
        title.fontStyle = FontStyles.Bold;
        title.color = COL_TEXT;
        title.alignment = TextAlignmentOptions.MidlineLeft;

        // Botao FECHAR (posicao fixa no topo direito)
        float btnY = y - (h - closeSz) * 0.5f;
        MakeBtn("BtnClose", parent,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(cw - pad - closeSz, btnY),
            new Vector2(closeSz, closeSz),
            "X", COL_BTN_CLSE, COL_BDR_CLSE, COL_TEXT, 11f, true)
            .onClick.AddListener(ClosePanel);

        return y - h;
    }

    // --- Section header interno ------------------------------

    private float BuildSecHdr(RectTransform parent, float y, string label)
    {
        float h = 22f;

        GameObject bg = MakeGO("SH_" + label, parent);
        RectTransform bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 1f);
        bgRT.anchorMax = new Vector2(1f, 1f);
        bgRT.pivot = new Vector2(0.5f, 1f);
        bgRT.anchoredPosition = new Vector2(0f, y);
        // Altura Positiva
        bgRT.sizeDelta = new Vector2(0f, h);
        MakeDeco(bg, COL_SEC_BG);

        GameObject acc = MakeGO("Acc", bgRT);
        RectTransform accRT = acc.AddComponent<RectTransform>();
        accRT.anchorMin = Vector2.zero;
        accRT.anchorMax = new Vector2(0f, 1f);
        accRT.pivot = new Vector2(0f, 0.5f);
        accRT.anchoredPosition = Vector2.zero;
        accRT.sizeDelta = new Vector2(3f, 0f);
        MakeDeco(acc, COL_ACCENT);

        TMP_Text t = MakeLabel("SHL", parent);
        RectTransform tRT = t.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot = new Vector2(0f, 1f);
        tRT.anchoredPosition = new Vector2(8f, y);
        // Altura Positiva
        tRT.sizeDelta = new Vector2(-8f, h);
        t.text = label;
        t.fontSize = 9f;
        t.fontStyle = FontStyles.Bold;
        t.color = COL_HDR_TEXT;
        t.alignment = TextAlignmentOptions.MidlineLeft;

        return y - h;
    }

    // --- Grid de dados (2 colunas x 3 linhas) ----------------

    private float BuildDiceGrid(RectTransform parent, float y,
        float cw, float pad, float gap)
    {
        float inner = cw - pad * 2f;
        float colGap = 6f;
        float cellW = (inner - colGap) * 0.5f;
        float cellH = 28f;
        float rowGap = 5f;
        int cols = 2;
        int rows = (DICE_TYPES.Length + cols - 1) / cols;

        for (int row = 0; row < rows; row++)
        {
            float rowY = y - row * (cellH + rowGap);
            for (int col = 0; col < cols; col++)
            {
                int idx = row * cols + col;
                if (idx >= DICE_TYPES.Length) break;
                float cellX = pad + col * (cellW + colGap);
                BuildDiceCell(parent, idx, cellX, rowY, cellW, cellH, pad, gap);
            }
        }

        float totalH = rows * cellH + (rows - 1) * rowGap;
        return y - totalH;
    }

    // BuildDiceCell: todos os filhos sao filhos de bgRT (posicao relativa a celula).
    private void BuildDiceCell(RectTransform parent, int diceIdx,
        float cellX, float cellY, float cellW, float cellH,
        float pad, float gap)
    {
        int sides = DICE_TYPES[diceIdx];
        Color dc = COL_DICE[diceIdx];

        // Fundo da celula (pai de todos os elementos internos)
        GameObject bg = MakeGO("Cell_D" + sides, parent);
        RectTransform bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 1f);
        bgRT.anchorMax = new Vector2(0f, 1f);
        bgRT.pivot = new Vector2(0f, 1f);
        bgRT.anchoredPosition = new Vector2(cellX, cellY);
        // Altura Positiva
        bgRT.sizeDelta = new Vector2(cellW, cellH);
        MakeDeco(bg, COL_SEC_BG);

        // Barra de acento colorida na esquerda (filho de bgRT)
        GameObject acc = MakeGO("Acc", bgRT);
        RectTransform accRT = acc.AddComponent<RectTransform>();
        accRT.anchorMin = Vector2.zero;
        accRT.anchorMax = new Vector2(0f, 1f);
        accRT.pivot = new Vector2(0f, 0.5f);
        accRT.anchoredPosition = Vector2.zero;
        accRT.sizeDelta = new Vector2(3f, 0f);
        MakeDeco(acc, dc);

        // Label do tipo de dado (filho de bgRT, posicionado relativamente a celula)
        TMP_Text dLabel = MakeLabel("DL_D" + sides, bgRT);
        RectTransform dlRT = dLabel.GetComponent<RectTransform>();
        dlRT.anchorMin = new Vector2(0f, 0f);
        dlRT.anchorMax = new Vector2(0f, 1f);
        dlRT.pivot = new Vector2(0f, 0.5f);
        dlRT.anchoredPosition = new Vector2(8f, 0f);
        dlRT.sizeDelta = new Vector2(42f, 0f);
        dLabel.text = "D" + sides;
        dLabel.fontSize = 10f;
        dLabel.fontStyle = FontStyles.Bold;
        dLabel.color = dc;
        dLabel.alignment = TextAlignmentOptions.MidlineLeft;

        // Controles (- count +): ancorados a direita da celula, centralizados verticalmente
        float btnSz = 22f;
        float cntW = 30f;
        float vPad = 3f;   // padding vertical
        float btnH = cellH - vPad * 2f;

        // Botao mais (mais a direita)
        Button plusBtn = MakeCellBtn("Plus", bgRT,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-4f, 0f), new Vector2(btnSz, btnH),
            "+", COL_BTN_SEC, COL_BDR_DEF, COL_TEXT, 14f, true);

        // Contador (a esquerda do +)
        TMP_Text cntLabel = MakeLabel("Cnt_D" + sides, bgRT);
        RectTransform cntRT = cntLabel.GetComponent<RectTransform>();
        cntRT.anchorMin = new Vector2(1f, 0.5f);
        cntRT.anchorMax = new Vector2(1f, 0.5f);
        cntRT.pivot = new Vector2(1f, 0.5f);
        cntRT.anchoredPosition = new Vector2(-(4f + btnSz + 2f), 0f);
        // Altura Positiva
        cntRT.sizeDelta = new Vector2(cntW, btnH);
        cntLabel.text = "0";
        cntLabel.fontSize = 12f;
        cntLabel.fontStyle = FontStyles.Bold;
        cntLabel.color = COL_TEXT;
        cntLabel.alignment = TextAlignmentOptions.Center;
        _countLabels[diceIdx] = cntLabel;

        // Botao menos (a esquerda do contador)
        Button minusBtn = MakeCellBtn("Minus", bgRT,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-(4f + btnSz + 2f + cntW + 2f), 0f), new Vector2(btnSz, btnH),
            "-", COL_BTN_SEC, COL_BDR_DEF, COL_TEXT, 14f, true);

        int capturedIdx = diceIdx;
        minusBtn.onClick.AddListener(() => ChangeCount(capturedIdx, -1));
        plusBtn.onClick.AddListener(() => ChangeCount(capturedIdx, +1));
    }

    // --- Linha de acao (LIMPAR + ROLAR) ----------------------

    private float BuildActionRow(RectTransform parent, float y,
        float cw, float pad, float gap)
    {
        float h = 36f;
        float hw = (cw - pad * 2f - gap) * 0.5f;

        MakeBtn("BtnClear", parent,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(pad, y), new Vector2(hw, h),
            "LIMPAR", COL_BTN_CLR, COL_BDR_DEF, COL_DIM, 10f, true)
            .onClick.AddListener(ClearSelection);

        MakeBtn("BtnRoll", parent,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(pad + hw + gap, y), new Vector2(hw, h),
            "ROLAR", COL_BTN_PRI, COL_BDR_PRI, COL_TEXT, 12f, true)
            .onClick.AddListener(DoRoll);

        return y - h;
    }

    // --- Area de resultado -----------------------------------

    private float BuildResultArea(RectTransform parent, float y,
        float cw, float pad)
    {
        float boxH = 44f;

        GameObject box = MakeGO("ResultBox", parent);
        RectTransform boxRT = box.AddComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0f, 1f);
        boxRT.anchorMax = new Vector2(1f, 1f);
        boxRT.pivot = new Vector2(0f, 1f);
        boxRT.anchoredPosition = new Vector2(0f, y);
        // Altura Positiva
        boxRT.sizeDelta = new Vector2(0f, boxH);
        MakeDeco(box, COL_CONTENT);

        // Barra de acento
        GameObject acc = MakeGO("Acc", boxRT);
        RectTransform accRT = acc.AddComponent<RectTransform>();
        accRT.anchorMin = Vector2.zero;
        accRT.anchorMax = new Vector2(0f, 1f);
        accRT.pivot = new Vector2(0f, 0.5f);
        accRT.anchoredPosition = Vector2.zero;
        accRT.sizeDelta = new Vector2(2f, 0f);
        MakeDeco(acc, COL_ACCENT);

        // Breakdown (dados individuais)
        _resultBreakdown = MakeStretchLabel("Breakdown", boxRT,
            new Vector2(pad, 5f), new Vector2(-4f, -5f));
        _resultBreakdown.fontSize = 10f;
        _resultBreakdown.color = COL_DIM;
        _resultBreakdown.text = "--";
        _resultBreakdown.alignment = TextAlignmentOptions.MidlineLeft;

        y -= boxH + 4f;

        // Total
        _resultTotal = MakeLabel("Total", parent);
        RectTransform totalRT = _resultTotal.GetComponent<RectTransform>();
        totalRT.anchorMin = new Vector2(0f, 1f);
        totalRT.anchorMax = new Vector2(1f, 1f);
        totalRT.pivot = new Vector2(0.5f, 1f);
        totalRT.anchoredPosition = new Vector2(0f, y);
        // Altura Positiva
        totalRT.sizeDelta = new Vector2(0f, 22f);
        _resultTotal.fontSize = 14f;
        _resultTotal.fontStyle = FontStyles.Bold;
        _resultTotal.color = COL_GOLD;
        _resultTotal.text = "";
        _resultTotal.alignment = TextAlignmentOptions.Center;

        y -= 22f;
        return y;
    }

    // --- Area de historico -----------------------------------

    private void BuildHistoryArea(RectTransform parent, float y,
        float cw, float pad)
    {
        float boxH = 76f;

        GameObject box = MakeGO("HistBox", parent);
        RectTransform boxRT = box.AddComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0f, 1f);
        boxRT.anchorMax = new Vector2(1f, 1f);
        boxRT.pivot = new Vector2(0f, 1f);
        boxRT.anchoredPosition = new Vector2(0f, y);
        // Altura Positiva
        boxRT.sizeDelta = new Vector2(0f, boxH);
        MakeDeco(box, COL_CONTENT);

        GameObject acc = MakeGO("Acc", boxRT);
        RectTransform accRT = acc.AddComponent<RectTransform>();
        accRT.anchorMin = Vector2.zero;
        accRT.anchorMax = new Vector2(0f, 1f);
        accRT.pivot = new Vector2(0f, 0.5f);
        accRT.anchoredPosition = Vector2.zero;
        accRT.sizeDelta = new Vector2(2f, 0f);
        MakeDeco(acc, COL_ACCENT);

        _historyText = MakeStretchLabel("HistText", boxRT,
            new Vector2(pad, 5f), new Vector2(-4f, -5f));
        _historyText.fontSize = 9.5f;
        _historyText.color = COL_DIM;
        _historyText.text = "Nenhuma rolagem ainda";
        _historyText.lineSpacing = 5f;
        _historyText.alignment = TextAlignmentOptions.TopLeft;
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

        // Coleta a especificacao
        List<int> rollSides = new List<int>();
        List<string> descParts = new List<string>();
        for (int i = 0; i < DICE_TYPES.Length; i++)
        {
            if (_counts[i] <= 0) continue;
            for (int k = 0; k < _counts[i]; k++)
                rollSides.Add(DICE_TYPES[i]);
            descParts.Add(_counts[i] + "D" + DICE_TYPES[i]);
        }
        string descriptor = string.Join(" + ", descParts);

        // Rola
        int[] results = new int[rollSides.Count];
        for (int i = 0; i < results.Length; i++)
            results[i] = Random.Range(1, rollSides[i] + 1);

        // Animacao
        _resultBreakdown.text = "rolando...";
        _resultTotal.text = "";
        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < rollSides.Count; i++)
            {
                // CORREÇÃO: Uso inteligente do StringBuilder encadeado (sem '+' na memória)
                sb.Append("D").Append(rollSides[i]).Append(":").Append(Random.Range(1, rollSides[i] + 1));
                if (i < rollSides.Count - 1) sb.Append("  ");
            }
            _resultBreakdown.text = sb.ToString();
            elapsed += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }

        // Resultado final
        int total = 0;
        StringBuilder brkSB = new StringBuilder();
        for (int i = 0; i < rollSides.Count; i++)
        {
            int val = results[i];
            int sides = rollSides[i];
            total += val;

            string colHex;
            if (val == sides) colHex = "#E8C84A";
            else if (val == 1) colHex = "#CF4040";
            else colHex = "#8AACCC";

            brkSB.Append("<color=").Append(colHex).Append(">D").Append(sides).Append(":").Append(val).Append("</color>");
            if (i < rollSides.Count - 1) brkSB.Append("  ");
        }
        _resultBreakdown.text = brkSB.ToString();
        _resultTotal.text = "TOTAL:  " + total;

        // Registro no historico
        History.Insert(0, new RollEntry { descriptor = descriptor, total = total });
        if (History.Count > MAX_HISTORY) History.RemoveAt(History.Count - 1);
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
            return;
        }
        StringBuilder sb = new StringBuilder();
        int show = Mathf.Min(History.Count, 4);
        for (int i = 0; i < show; i++)
        {
            sb.Append(History[i].descriptor + "  =>  " + History[i].total);
            if (i < show - 1) sb.Append("\n");
        }
        _historyText.text = sb.ToString();
    }

    // =========================================================
    // API Publica
    // =========================================================

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

    // =========================================================
    // Fabrica de widgets (raycast discipline aplicada)
    // =========================================================

    private GameObject MakeGO(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>Image decorativa: raycastTarget = false.</summary>
    private void MakeDeco(GameObject go, Color color)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    /// <summary>TMP_Text: raycastTarget = false.</summary>
    private TMP_Text MakeLabel(string name, Transform parent)
    {
        GameObject go = MakeGO(name, parent);
        go.AddComponent<RectTransform>();   // layout definido pelo chamador
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.raycastTarget = false;
        return t;
    }

    /// <summary>TMP_Text esticado dentro de um parent.</summary>
    private TMP_Text MakeStretchLabel(string name, RectTransform parent,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = MakeGO(name, parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.raycastTarget = false;
        return t;
    }

    // Botao para uso interno nas celulas de dado.
    // Usa anchorMin/Max/pivot explicitos passados pelo chamador.
    private Button MakeCellBtn(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size,
        string label, Color bg, Color border, Color tc, float fs, bool bold)
    {
        // Wrapper borda (decorativo)
        GameObject wrap = MakeGO(name + "_Border", parent);
        RectTransform wRT = wrap.AddComponent<RectTransform>();
        wRT.anchorMin = anchorMin;
        wRT.anchorMax = anchorMax;
        wRT.pivot = pivot;
        wRT.anchoredPosition = anchoredPos;
        wRT.sizeDelta = size;
        MakeDeco(wrap, border);

        // Botao interior
        GameObject btnGO = MakeGO(name, wrap.transform);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = Vector2.zero;
        btnRT.anchorMax = Vector2.one;
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = Vector2.zero;
        btnRT.sizeDelta = Vector2.zero;

        Image img = btnGO.AddComponent<Image>();
        img.color = bg;
        img.raycastTarget = true;

        // Outline visivel
        UnityEngine.UI.Outline ol = btnGO.AddComponent<UnityEngine.UI.Outline>();
        ol.effectColor = Color.Lerp(bg, Color.white, 0.55f);
        ol.effectDistance = new Vector2(2f, -2f);
        ol.useGraphicAlpha = false;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.30f);
        cb.pressedColor = Color.Lerp(bg, Color.black, 0.35f);
        cb.selectedColor = Color.Lerp(bg, Color.white, 0.15f);
        cb.disabledColor = new Color(bg.r * 0.45f, bg.g * 0.45f, bg.b * 0.45f, 0.5f);
        cb.fadeDuration = 0.06f;
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        GameObject lGO = MakeGO("Lbl", btnGO.transform);
        RectTransform lRT = lGO.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.sizeDelta = Vector2.zero;
        TMP_Text t = lGO.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = fs;
        t.color = tc;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        return btn;
    }

    /// <summary>
    /// Botao com borda visivel.
    /// Wrapper borda:   Image raycastTarget = false
    /// Botao interior:  Image raycastTarget = true  (targetGraphic)
    /// Label filho:     TMP_Text raycastTarget = false
    /// </summary>
    private Button MakeBtn(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size,
        string label, Color bg, Color border, Color tc, float fs, bool bold)
    {
        // Wrapper de borda
        GameObject wrap = MakeGO(name + "_Border", parent);
        RectTransform wRT = wrap.AddComponent<RectTransform>();
        wRT.anchorMin = anchorMin;
        wRT.anchorMax = anchorMax;
        wRT.pivot = pivot;
        wRT.anchoredPosition = anchoredPos;
        wRT.sizeDelta = size;
        MakeDeco(wrap, border);

        // Botao insetado 1px
        GameObject btnGO = MakeGO(name, wrap.transform);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = Vector2.zero;
        btnRT.anchorMax = Vector2.one;
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = Vector2.zero;
        btnRT.sizeDelta = new Vector2(-2f, -2f);

        Image img = btnGO.AddComponent<Image>();
        img.color = bg;
        img.raycastTarget = true;   // UNICO true

        // Outline visivel (2px, cor mais clara que o bg)
        UnityEngine.UI.Outline ol = btnGO.AddComponent<UnityEngine.UI.Outline>();
        ol.effectColor = Color.Lerp(bg, Color.white, 0.55f);
        ol.effectDistance = new Vector2(2f, -2f);
        ol.useGraphicAlpha = false;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.30f);
        cb.pressedColor = Color.Lerp(bg, Color.black, 0.35f);
        cb.selectedColor = Color.Lerp(bg, Color.white, 0.15f);
        cb.disabledColor = new Color(bg.r * 0.45f, bg.g * 0.45f, bg.b * 0.45f, 0.5f);
        cb.fadeDuration = 0.06f;
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        // Label filho
        GameObject lGO = MakeGO("Lbl", btnGO.transform);
        RectTransform lRT = lGO.AddComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.sizeDelta = Vector2.zero;
        TMP_Text t = lGO.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = fs;
        t.color = tc;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;   // texto nao bloqueia raycasts

        return btn;
    }
}