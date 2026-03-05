// ============================================================
// VTTLayout.cs  v2
//
// Constantes de layout, paleta e fabrica de widgets
// compartilhados entre GMUIController e DiceRollOverlay.
//
// REGRA DE RAYCAST (aplicada em cada widget):
//   Elemento decorativo (Image de fundo, barra, label)
//       -> raycastTarget = FALSE
//   targetGraphic de Button ou handle de Slider
//       -> raycastTarget = TRUE
//
// SISTEMA DE COORDENADAS:
//   anchorMin=(0,1) anchorMax=(x,1) pivot=(0,1)
//   anchoredPosition.y = negativo (desce a partir do topo)
//   sizeDelta.y = negativo (altura do elemento)
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class VTTLayout
{
    // --- Grid ------------------------------------------------
    public const float PAD = 12f;    // padding lateral interno
    public const float GAP = 6f;     // espaco entre elementos
    public const float SGAP = 14f;    // espaco entre secoes
    public const float BTN_H = 34f;    // altura padrao de botao
    public const float HDR_H = 28f;    // altura de section header
    public const float PHDR_H = 42f;    // altura de panel header

    // --- Tamanhos de fonte -----------------------------------
    public const float F_PANEL = 12f;
    public const float F_SEC = 9.5f;
    public const float F_BTN = 10.5f;
    public const float F_LABEL = 9.5f;
    public const float F_SMALL = 9f;

    // --- Paleta ----------------------------------------------
    public static readonly Color C_BG = RGB(0.10f, 0.11f, 0.14f);
    public static readonly Color C_LEFT_BG = RGB(0.09f, 0.10f, 0.13f);
    public static readonly Color C_SEC_BG = RGB(0.13f, 0.15f, 0.19f);
    public static readonly Color C_HDR_BG = RGB(0.14f, 0.19f, 0.28f);
    public static readonly Color C_CONTENT_BG = RGB(0.07f, 0.08f, 0.10f);
    public static readonly Color C_ACCENT = RGB(0.24f, 0.42f, 0.65f);
    public static readonly Color C_ACCENT_LT = RGB(0.35f, 0.55f, 0.80f);

    // botoes
    public static readonly Color C_BTN_PRI = RGB(0.18f, 0.34f, 0.56f);
    public static readonly Color C_BTN_SEC = RGB(0.14f, 0.17f, 0.23f);
    public static readonly Color C_BTN_PAINT = RGB(0.12f, 0.22f, 0.36f);
    public static readonly Color C_BTN_ERASE = RGB(0.32f, 0.14f, 0.14f);
    public static readonly Color C_BTN_ACTIVE = RGB(0.24f, 0.42f, 0.65f);
    public static readonly Color C_BTN_DICE = RGB(0.20f, 0.16f, 0.32f);
    public static readonly Color C_BTN_CLOSE = RGB(0.40f, 0.14f, 0.14f);
    public static readonly Color C_BTN_CLEAR = RGB(0.18f, 0.18f, 0.24f);
    public static readonly Color C_BTN_ROLL = RGB(0.22f, 0.38f, 0.62f);

    // bordas (levemente mais claras que o botao correspondente)
    public static readonly Color C_BDR_DEFAULT = RGB(0.26f, 0.32f, 0.44f);
    public static readonly Color C_BDR_ACC = RGB(0.34f, 0.54f, 0.80f);
    public static readonly Color C_BDR_PAINT = RGB(0.20f, 0.40f, 0.65f);
    public static readonly Color C_BDR_ERASE = RGB(0.65f, 0.22f, 0.22f);
    public static readonly Color C_BDR_DICE = RGB(0.40f, 0.32f, 0.60f);
    public static readonly Color C_BDR_CLOSE = RGB(0.65f, 0.22f, 0.22f);
    public static readonly Color C_BDR_ROLL = RGB(0.40f, 0.60f, 0.90f);

    // texto
    public static readonly Color C_TEXT = RGB(0.84f, 0.88f, 0.96f);
    public static readonly Color C_TEXT_DIM = RGB(0.42f, 0.48f, 0.60f);
    public static readonly Color C_TEXT_HDR = RGB(0.68f, 0.76f, 0.90f);
    public static readonly Color C_TEXT_PANEL = RGB(0.92f, 0.95f, 1.00f);
    public static readonly Color C_TEXT_WARN = RGB(0.80f, 0.50f, 0.20f);
    public static readonly Color C_TEXT_OK = RGB(0.30f, 0.70f, 0.42f);
    public static readonly Color C_TEXT_GOLD = RGB(0.92f, 0.78f, 0.28f);
    public static readonly Color C_TEXT_RED = RGB(0.82f, 0.26f, 0.22f);

    // dados (por indice em DICE_TYPES)
    public static readonly Color[] C_DICE = {
        RGB(0.20f, 0.60f, 0.55f),   // D4  teal
        RGB(0.24f, 0.50f, 0.82f),   // D6  azul
        RGB(0.55f, 0.28f, 0.75f),   // D8  roxo
        RGB(0.20f, 0.42f, 0.70f),   // D10 azul escuro
        RGB(0.40f, 0.20f, 0.60f),   // D12 violeta
        RGB(0.80f, 0.62f, 0.10f),   // D20 ouro
    };

    // --- Utilitarios de cor ----------------------------------

    public static Color RGB(float r, float g, float b, float a = 1f)
    {
        return new Color(r, g, b, a);
    }

    public static Color Highlight(Color c) { return Color.Lerp(c, Color.white, 0.22f); }
    public static Color Pressed(Color c) { return Color.Lerp(c, Color.black, 0.28f); }
    public static Color Selected(Color c) { return Color.Lerp(c, Color.white, 0.10f); }
    public static Color Disabled(Color c) { return new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 0.6f); }

    // =========================================================
    // Fabrica de widgets
    // =========================================================

    // --- Painel raiz -----------------------------------------

    /// <summary>
    /// Painel que ocupa toda a altura do Canvas (anchorMin.y=0, anchorMax.y=1).
    /// Fica colado na borda indicada por anchorMin.x / anchorMax.x.
    /// </summary>
    public static RectTransform Panel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        float width, Color bgColor)
    {
        GameObject go = New(name, parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, 0f);
        Deco(go, bgColor);
        go.AddComponent<GraphicRaycaster>();
        return rt;
    }

    // Sobrecarga com cor padrao
    public static RectTransform Panel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, float width)
    {
        return Panel(name, parent, anchorMin, anchorMax, pivot, width, C_BG);
    }

    // --- Caixa de fundo decorativa ---------------------------
    // Ancora de topo-esquerdo a topo-direito, pivo topo-esquerdo.
    // x=0 e dw=0 significa "igual a largura do parent".

    public static RectTransform Box(string name, RectTransform parent,
        float x, float y, float dw, float dh, Color color)
    {
        return Box(name, parent, x, y, dw, dh, color,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
    }

    public static RectTransform Box(string name, RectTransform parent,
        float x, float y, float dw, float dh, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = New(name, parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(dw, dh);
        Deco(go, color);
        return rt;
    }

    // --- Barra decorativa de acento lateral ------------------

    public static void AccentBar(RectTransform parent, float width, Color color)
    {
        GameObject go = New("AccBar", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, 0f);
        Deco(go, color);
    }

    // --- Label full-width ancorado no topo ------------------

    public static TMP_Text Label(RectTransform parent,
        float y, float height, float fontSize,
        Color color, FontStyles style = FontStyles.Normal,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
        float padLeft = PAD, float padRight = PAD)
    {
        GameObject go = New("Label", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(padLeft, y);
        rt.sizeDelta = new Vector2(-(padLeft + padRight), -height);
        return TxtNode(go, fontSize, color, style, align);
    }

    // --- Label de posicao e largura fixas --------------------

    public static TMP_Text LabelFixed(RectTransform parent,
        float x, float y, float width, float height,
        float fontSize, Color color, FontStyles style = FontStyles.Normal,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = New("Label", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, -height);
        return TxtNode(go, fontSize, color, style, align);
    }

    // --- Label esticado preenchendo o parent -----------------

    public static TMP_Text LabelStretch(string name, RectTransform parent,
        Vector2 offsetMin, Vector2 offsetMax,
        float fontSize, Color color, FontStyles style = FontStyles.Normal,
        TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        GameObject go = New(name, parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return TxtNode(go, fontSize, color, style, align);
    }

    // --- Botao full-width ------------------------------------
    // dw = delta de largura em relacao ao parent
    // (ex: -PAD*2 para inset bilateral de PAD em cada lado)

    public static Button BtnFull(RectTransform parent,
        float y, float height, float dw,
        string label, Color bg, Color border,
        Color textColor, float fontSize, bool bold = true)
    {
        GameObject wrap = New("BtnWrap", parent);
        RectTransform wrt = wrap.AddComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0f, 1f);
        wrt.anchorMax = new Vector2(1f, 1f);
        wrt.pivot = new Vector2(0.5f, 1f);
        wrt.anchoredPosition = new Vector2(0f, y);
        wrt.sizeDelta = new Vector2(dw, -height);
        Deco(wrap, border);

        return BtnCore(wrap.transform, label, bg, textColor, fontSize, bold);
    }

    // --- Botao de posicao e tamanho fixos --------------------

    public static Button BtnFixed(RectTransform parent,
        float x, float y, float width, float height,
        string label, Color bg, Color border,
        Color textColor, float fontSize, bool bold = true)
    {
        GameObject wrap = New("BtnWrap", parent);
        RectTransform wrt = wrap.AddComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0f, 1f);
        wrt.anchorMax = new Vector2(0f, 1f);
        wrt.pivot = new Vector2(0f, 1f);
        wrt.anchoredPosition = new Vector2(x, y);
        wrt.sizeDelta = new Vector2(width, -height);
        Deco(wrap, border);

        return BtnCore(wrap.transform, label, bg, textColor, fontSize, bold);
    }

    // Nucleo interno do botao (insetado 1px dentro do wrapper de borda)
    private static Button BtnCore(Transform parent,
        string label, Color bg, Color textColor, float fontSize, bool bold)
    {
        GameObject go = New("Btn", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(-2f, -2f);

        Image img = go.AddComponent<Image>();
        img.color = bg;
        img.raycastTarget = true;   // targetGraphic do botao - UNICO true

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = bg;
        cb.highlightedColor = Highlight(bg);
        cb.pressedColor = Pressed(bg);
        cb.selectedColor = Selected(bg);
        cb.disabledColor = Disabled(bg);
        cb.fadeDuration = 0.08f;
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        // Label filho (nao captura raycasts)
        GameObject lgo = New("Lbl", go.transform);
        RectTransform lrt = lgo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;
        FontStyles fs = bold ? FontStyles.Bold : FontStyles.Normal;
        TMP_Text t = TxtNode(lgo, fontSize, textColor, fs, TextAlignmentOptions.Center);
        t.text = label;

        return btn;
    }

    // --- Atualiza cor de um botao existente ------------------

    public static void SetBtnColor(Button btn, Color color)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = Highlight(color);
        cb.pressedColor = Pressed(color);
        btn.colors = cb;
    }

    // --- Slider ----------------------------------------------

    public static Slider MakeSlider(RectTransform parent,
        float y, float height, float min, float max, float val)
    {
        // Wrapper borda
        GameObject wrap = New("SliderWrap", parent);
        RectTransform wrt = wrap.AddComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0f, 1f);
        wrt.anchorMax = new Vector2(1f, 1f);
        wrt.pivot = new Vector2(0.5f, 1f);
        wrt.anchoredPosition = new Vector2(0f, y);
        wrt.sizeDelta = new Vector2(-PAD * 2f, -height);
        Deco(wrap, C_BDR_DEFAULT);

        // Container insetado
        GameObject go = New("Slider", wrap.transform);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(-2f, -2f);
        Deco(go, C_CONTENT_BG);

        Slider s = go.AddComponent<Slider>();
        s.minValue = min;
        s.maxValue = max;
        s.value = val;
        s.direction = Slider.Direction.LeftToRight;

        // Track
        {
            GameObject t = New("Track", go.transform);
            RectTransform trt = t.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.30f);
            trt.anchorMax = new Vector2(1f, 0.70f);
            trt.sizeDelta = Vector2.zero;
            Deco(t, RGB(0.07f, 0.08f, 0.11f));
        }

        // Fill
        RectTransform fillRT;
        {
            GameObject fa = New("FillArea", go.transform);
            RectTransform faRT = fa.AddComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0f, 0.30f);
            faRT.anchorMax = new Vector2(1f, 0.70f);
            faRT.sizeDelta = new Vector2(-10f, 0f);
            faRT.anchoredPosition = new Vector2(5f, 0f);
            faRT.pivot = new Vector2(0.5f, 0.5f);

            GameObject fill = New("Fill", fa.transform);
            fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.sizeDelta = Vector2.zero;
            fillRT.pivot = new Vector2(0f, 0.5f);
            fillRT.anchoredPosition = Vector2.zero;
            Deco(fill, C_ACCENT);
        }

        // Handle
        RectTransform handleRT;
        {
            GameObject ha = New("HandleArea", go.transform);
            RectTransform haRT = ha.AddComponent<RectTransform>();
            haRT.anchorMin = Vector2.zero;
            haRT.anchorMax = Vector2.one;
            haRT.sizeDelta = new Vector2(-10f, 0f);
            haRT.anchoredPosition = Vector2.zero;
            haRT.pivot = new Vector2(0.5f, 0.5f);

            GameObject handle = New("Handle", ha.transform);
            handleRT = handle.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(14f, 14f);
            handleRT.pivot = new Vector2(0.5f, 0.5f);
            Image hImg = handle.AddComponent<Image>();
            hImg.color = RGB(0.74f, 0.82f, 0.93f);
            hImg.raycastTarget = true;   // handle e interativo
        }

        s.fillRect = fillRT;
        s.handleRect = handleRT;
        s.targetGraphic = handleRT.GetComponent<Image>();

        return s;
    }

    // =========================================================
    // Primitivas internas
    // =========================================================

    public static GameObject New(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>Image decorativa - raycastTarget sempre false.</summary>
    public static void Deco(GameObject go, Color color)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    public static void Deco(RectTransform rt, Color color)
    {
        Deco(rt.gameObject, color);
    }

    /// <summary>TMP_Text - raycastTarget sempre false.</summary>
    private static TMP_Text TxtNode(GameObject go,
        float fontSize, Color color, FontStyles style, TextAlignmentOptions align)
    {
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.color = color;
        t.fontStyle = style;
        t.alignment = align;
        t.raycastTarget = false;   // texto nao captura raycast nunca
        return t;
    }
}