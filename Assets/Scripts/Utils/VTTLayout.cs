// ============================================================
// VTTLayout.cs
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class VTTLayout
{
    public const float PAD = 12f;
    public const float GAP = 6f;
    public const float SGAP = 14f;
    public const float BTN_H = 34f;
    public const float HDR_H = 28f;
    public const float PHDR_H = 42f;

    public const float F_PANEL = 12f;
    public const float F_SEC = 9.5f;
    public const float F_BTN = 10.5f;
    public const float F_LABEL = 9.5f;
    public const float F_SMALL = 9f;

    public static readonly Color C_BG = RGB(0.10f, 0.11f, 0.14f);
    public static readonly Color C_LEFT_BG = RGB(0.09f, 0.10f, 0.13f);
    public static readonly Color C_SEC_BG = RGB(0.13f, 0.15f, 0.19f);
    public static readonly Color C_HDR_BG = RGB(0.14f, 0.19f, 0.28f);
    public static readonly Color C_CONTENT_BG = RGB(0.07f, 0.08f, 0.10f);
    public static readonly Color C_ACCENT = RGB(0.24f, 0.42f, 0.65f);
    public static readonly Color C_ACCENT_LT = RGB(0.35f, 0.55f, 0.80f);

    public static readonly Color C_BTN_PRI = RGB(0.18f, 0.34f, 0.56f);
    public static readonly Color C_BTN_SEC = RGB(0.14f, 0.17f, 0.23f);
    public static readonly Color C_BTN_PAINT = RGB(0.12f, 0.22f, 0.36f);
    public static readonly Color C_BTN_ERASE = RGB(0.32f, 0.14f, 0.14f);
    public static readonly Color C_BTN_ACTIVE = RGB(0.24f, 0.42f, 0.65f);
    public static readonly Color C_BTN_DICE = RGB(0.20f, 0.16f, 0.32f);
    public static readonly Color C_BTN_CLOSE = RGB(0.40f, 0.14f, 0.14f);
    public static readonly Color C_BTN_CLEAR = RGB(0.18f, 0.18f, 0.24f);
    public static readonly Color C_BTN_ROLL = RGB(0.22f, 0.38f, 0.62f);

    public static readonly Color C_BDR_DEFAULT = RGB(0.26f, 0.32f, 0.44f);
    public static readonly Color C_BDR_ACC = RGB(0.34f, 0.54f, 0.80f);
    public static readonly Color C_BDR_PAINT = RGB(0.20f, 0.40f, 0.65f);
    public static readonly Color C_BDR_ERASE = RGB(0.65f, 0.22f, 0.22f);
    public static readonly Color C_BDR_DICE = RGB(0.40f, 0.32f, 0.60f);
    public static readonly Color C_BDR_CLOSE = RGB(0.65f, 0.22f, 0.22f);
    public static readonly Color C_BDR_ROLL = RGB(0.40f, 0.60f, 0.90f);

    public static readonly Color C_TEXT = RGB(0.84f, 0.88f, 0.96f);
    public static readonly Color C_TEXT_DIM = RGB(0.42f, 0.48f, 0.60f);
    public static readonly Color C_TEXT_HDR = RGB(0.68f, 0.76f, 0.90f);
    public static readonly Color C_TEXT_PANEL = RGB(0.92f, 0.95f, 1.00f);
    public static readonly Color C_TEXT_WARN = RGB(0.80f, 0.50f, 0.20f);
    public static readonly Color C_TEXT_OK = RGB(0.30f, 0.70f, 0.42f);
    public static readonly Color C_TEXT_GOLD = RGB(0.92f, 0.78f, 0.28f);

    public static Color RGB(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);
    public static Color Highlight(Color c) => Color.Lerp(c, Color.white, 0.15f);
    public static Color Pressed(Color c) => Color.Lerp(c, Color.black, 0.35f);
    public static Color Selected(Color c) => Color.Lerp(c, Color.white, 0.05f);
    public static Color Disabled(Color c) => new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 0.6f);

    public static RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, float width, Color bgColor)
    {
        GameObject go = New(name, parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot; rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(width, 0f);
        Deco(go, bgColor); go.AddComponent<GraphicRaycaster>();
        return rt;
    }

    public static RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, float width) => Panel(name, parent, anchorMin, anchorMax, pivot, width, C_BG);

    public static RectTransform Box(string name, RectTransform parent, float x, float y, float dw, float dh, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = New(name, parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = new Vector2(0f, 1f); rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(dw, dh);
        Deco(go, color); return rt;
    }

    public static RectTransform Box(string name, RectTransform parent, float x, float y, float dw, float dh, Color color) => Box(name, parent, x, y, dw, dh, color, new Vector2(0f, 1f), new Vector2(1f, 1f));

    public static void AccentBar(RectTransform parent, float width, Color color)
    {
        GameObject go = New("AccBar", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 0.5f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(width, 0f);
        Deco(go, color);
    }

    public static TMP_Text Label(RectTransform parent, float y, float height, float fontSize, Color color, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, float padLeft = PAD, float padRight = PAD)
    {
        GameObject go = New("Label", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0f, 1f); rt.anchoredPosition = new Vector2(padLeft, y); rt.sizeDelta = new Vector2(-(padLeft + padRight), height);
        return TxtNode(go, fontSize, color, style, align);
    }

    public static TMP_Text LabelFixed(RectTransform parent, float x, float y, float width, float height, float fontSize, Color color, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = New("Label", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f); rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(width, height);
        return TxtNode(go, fontSize, color, style, align);
    }

    public static TMP_Text LabelStretch(string name, RectTransform parent, Vector2 offsetMin, Vector2 offsetMax, float fontSize, Color color, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        GameObject go = New(name, parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        return TxtNode(go, fontSize, color, style, align);
    }

    public static TMP_InputField InputFieldFixed(RectTransform parent, float x, float y, float w, float h, float fontSize, Color textColor, FontStyles style, string defaultText)
    {
        GameObject go = New("InputField", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);

        Image bg = go.AddComponent<Image>();
        bg.color = Color.white;

        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.targetGraphic = bg;

        GameObject vpGO = New("Viewport", go.transform);
        RectTransform vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(8f, 4f);
        vpRT.offsetMax = new Vector2(-8f, -4f);
        vpGO.AddComponent<RectMask2D>();

        GameObject textGO = New("Text", vpGO.transform);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero; textRT.anchoredPosition = Vector2.zero;

        TMP_Text textComp = textGO.AddComponent<TextMeshProUGUI>();
        textComp.fontSize = fontSize; textComp.color = textColor; textComp.fontStyle = style;
        textComp.alignment = TextAlignmentOptions.MidlineLeft; textComp.enableWordWrapping = false; textComp.extraPadding = true;

        input.textViewport = vpRT; input.textComponent = textComp; input.text = defaultText;
        input.customCaretColor = true; input.caretColor = textColor; input.caretWidth = 2; input.caretBlinkRate = 0.85f;

        ColorBlock cb = input.colors;
        cb.normalColor = new Color(0f, 0f, 0f, 0.25f);
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        cb.pressedColor = new Color(0f, 0f, 0f, 0.4f);
        cb.selectedColor = new Color(0f, 0f, 0f, 0.6f);
        input.colors = cb;
        input.selectionColor = new Color(0.24f, 0.42f, 0.65f, 0.6f);

        return input;
    }

    // --- NOVO: InputField Multiline (Para Inventário, História e Habilidades) ---
    public static TMP_InputField InputFieldMultiline(RectTransform parent, float x, float y, float w, float h, float fontSize, Color textColor, FontStyles style, string defaultText)
    {
        TMP_InputField input = InputFieldFixed(parent, x, y, w, h, fontSize, textColor, style, defaultText);
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.textComponent.alignment = TextAlignmentOptions.TopLeft;
        input.textComponent.enableWordWrapping = true;
        return input;
    }

    public static Button BtnFull(RectTransform parent, float y, float height, float dw, string label, Color bg, Color border, Color textColor, float fontSize, bool bold = true)
    {
        GameObject wrap = New("BtnWrap", parent);
        RectTransform wrt = wrap.AddComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0f, 1f); wrt.anchorMax = new Vector2(1f, 1f); wrt.pivot = new Vector2(0.5f, 1f); wrt.anchoredPosition = new Vector2(0f, y); wrt.sizeDelta = new Vector2(dw, height);
        Deco(wrap, border); return BtnCore(wrap.transform, label, bg, textColor, fontSize, bold);
    }

    public static Button BtnFixed(RectTransform parent, float x, float y, float width, float height, string label, Color bg, Color border, Color textColor, float fontSize, bool bold = true)
    {
        GameObject wrap = New("BtnWrap", parent);
        RectTransform wrt = wrap.AddComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0f, 1f); wrt.anchorMax = new Vector2(0f, 1f); wrt.pivot = new Vector2(0f, 1f); wrt.anchoredPosition = new Vector2(x, y); wrt.sizeDelta = new Vector2(width, height);
        Deco(wrap, border); return BtnCore(wrap.transform, label, bg, textColor, fontSize, bold);
    }

    private static Button BtnCore(Transform parent, string label, Color bg, Color textColor, float fontSize, bool bold)
    {
        GameObject go = New("Btn", parent);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(-2f, -2f);
        Image img = go.AddComponent<Image>(); img.color = bg; img.raycastTarget = true;

        Button btn = go.AddComponent<Button>(); btn.targetGraphic = img; btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = ColorBlock.defaultColorBlock; cb.normalColor = bg; cb.highlightedColor = Highlight(bg); cb.pressedColor = Pressed(bg); cb.selectedColor = Selected(bg); cb.disabledColor = Disabled(bg); cb.fadeDuration = 0.08f; cb.colorMultiplier = 1f; btn.colors = cb;

        go.AddComponent<ButtonFeedback>();

        GameObject lgo = New("Lbl", go.transform);
        RectTransform lrt = lgo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        FontStyles fs = bold ? FontStyles.Bold : FontStyles.Normal;
        TMP_Text t = TxtNode(lgo, fontSize, textColor, fs, TextAlignmentOptions.Center); t.text = label;
        return btn;
    }

    public static void SetBtnColor(Button btn, Color color)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>(); if (img != null) img.color = color;
        ColorBlock cb = btn.colors; cb.normalColor = color; cb.highlightedColor = Highlight(color); cb.pressedColor = Pressed(color); btn.colors = cb;
    }

    public static Slider MakeSlider(RectTransform parent, float y, float height, float min, float max, float val)
    {
        GameObject wrap = New("SliderWrap", parent);
        RectTransform wrt = wrap.AddComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0f, 1f); wrt.anchorMax = new Vector2(1f, 1f); wrt.pivot = new Vector2(0.5f, 1f); wrt.anchoredPosition = new Vector2(0f, y); wrt.sizeDelta = new Vector2(-PAD * 2f, height);
        Deco(wrap, C_BDR_DEFAULT);
        GameObject go = New("Slider", wrap.transform);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(-2f, -2f);
        Deco(go, C_CONTENT_BG);
        Slider s = go.AddComponent<Slider>(); s.minValue = min; s.maxValue = max; s.value = val; s.direction = Slider.Direction.LeftToRight;
        GameObject t = New("Track", go.transform);
        RectTransform trt = t.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0.30f); trt.anchorMax = new Vector2(1f, 0.70f); trt.sizeDelta = Vector2.zero; Deco(t, RGB(0.07f, 0.08f, 0.11f));
        GameObject fa = New("FillArea", go.transform);
        RectTransform faRT = fa.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0f, 0.30f); faRT.anchorMax = new Vector2(1f, 0.70f); faRT.sizeDelta = new Vector2(-10f, 0f); faRT.anchoredPosition = new Vector2(5f, 0f); faRT.pivot = new Vector2(0.5f, 0.5f);
        GameObject fill = New("Fill", fa.transform);
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(0f, 1f); fillRT.sizeDelta = Vector2.zero; fillRT.pivot = new Vector2(0f, 0.5f); fillRT.anchoredPosition = Vector2.zero; Deco(fill, C_ACCENT);
        GameObject ha = New("HandleArea", go.transform);
        RectTransform haRT = ha.AddComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one; haRT.sizeDelta = new Vector2(-10f, 0f); haRT.anchoredPosition = Vector2.zero; haRT.pivot = new Vector2(0.5f, 0.5f);
        GameObject handle = New("Handle", ha.transform);
        RectTransform handleRT = handle.AddComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(14f, 14f); handleRT.pivot = new Vector2(0.5f, 0.5f);
        Image hImg = handle.AddComponent<Image>(); hImg.color = RGB(0.74f, 0.82f, 0.93f); hImg.raycastTarget = true;
        s.fillRect = fillRT; s.handleRect = handleRT; s.targetGraphic = hImg;
        return s;
    }

    public static ScrollRect MakeScrollView(string name, RectTransform parent, float x, float y, float w, float h, out RectTransform content)
    {
        GameObject svGO = New(name, parent);
        RectTransform svRT = svGO.AddComponent<RectTransform>();
        svRT.anchorMin = new Vector2(0, 1); svRT.anchorMax = new Vector2(0, 1); svRT.pivot = new Vector2(0, 1);
        svRT.anchoredPosition = new Vector2(x, y); svRT.sizeDelta = new Vector2(w, h);

        ScrollRect scroll = svGO.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 35f;

        GameObject vpGO = New("Viewport", svGO.transform);
        RectTransform vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one; vpRT.pivot = new Vector2(0, 1);
        vpRT.sizeDelta = Vector2.zero; vpRT.anchoredPosition = Vector2.zero;

        Image vpImg = vpGO.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        Mask mask = vpGO.AddComponent<Mask>(); mask.showMaskGraphic = false;

        GameObject ctGO = New("Content", vpGO.transform);
        content = ctGO.AddComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0, 1);
        content.sizeDelta = new Vector2(0, 0); content.anchoredPosition = Vector2.zero;

        scroll.viewport = vpRT; scroll.content = content;
        return scroll;
    }

    private static Sprite _circleSprite;
    public static Sprite GetCircleSprite()
    {
        if (_circleSprite == null)
        {
            int r = 128, d = 256;
            Texture2D tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
            Color32[] p = new Color32[d * d];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 white = new Color32(255, 255, 255, 255);
            for (int y = 0; y < d; y++)
            {
                for (int x = 0; x < d; x++)
                {
                    int dx = x - r, dy = y - r;
                    p[y * d + x] = (dx * dx + dy * dy <= r * r) ? white : clear;
                }
            }
            tex.SetPixels32(p); tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), 100f);
        }
        return _circleSprite;
    }

    public static Image MakeMaskedAvatar(RectTransform parent, Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        GameObject maskGO = New("AvatarMask", parent);
        RectTransform maskRT = maskGO.AddComponent<RectTransform>();
        maskRT.anchorMin = new Vector2(0, 0.5f); maskRT.anchorMax = new Vector2(0, 0.5f);
        maskRT.pivot = new Vector2(0, 0.5f);
        maskRT.anchoredPosition = anchoredPos; maskRT.sizeDelta = size;

        Image maskImg = maskGO.AddComponent<Image>();
        maskImg.sprite = GetCircleSprite();
        maskImg.color = bgColor;
        Mask mask = maskGO.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        GameObject avGO = New("AvatarImg", maskGO.transform);
        RectTransform avRT = avGO.AddComponent<RectTransform>();
        avRT.anchorMin = Vector2.zero; avRT.anchorMax = Vector2.one;
        avRT.sizeDelta = Vector2.zero;

        Image avImg = avGO.AddComponent<Image>();
        avImg.color = Color.clear;
        return avImg;
    }

    // --- NOVO: Cria um Token circular 2D para o mapa! ---
    // --- ATUALIZADO: Normaliza qualquer resolução para caber dentro da Borda! ---
    public static Sprite CreateCircularWorldSprite(Texture2D sourceTex)
    {
        if (sourceTex == null) return GetCircleSprite();
        int w = sourceTex.width; int h = sourceTex.height;
        int d = Mathf.Min(w, h);
        int r = d / 2;
        int offsetX = (w - d) / 2; int offsetY = (h - d) / 2;

        Texture2D tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
        Color[] sourcePixels = sourceTex.GetPixels(offsetX, offsetY, d, d);
        Color[] finalPixels = new Color[d * d];
        Color clear = new Color(0, 0, 0, 0);

        for (int y = 0; y < d; y++)
        {
            for (int x = 0; x < d; x++)
            {
                float dx = x - r; float dy = y - r;
                if (dx * dx + dy * dy <= r * r)
                    finalPixels[y * d + x] = sourcePixels[y * d + x];
                else
                    finalPixels[y * d + x] = clear;
            }
        }
        tex.SetPixels(finalPixels); tex.Apply();

        // MATEMÁTICA CORRIGIDA: Força o PPU para que a imagem fique com o tamanho exato da borda
        float ppu = d / 2.56f;
        return Sprite.Create(tex, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), ppu);
    }

    public static GameObject New(string name, Transform parent) { var go = new GameObject(name); go.transform.SetParent(parent, false); return go; }
    public static void Deco(GameObject go, Color color) { Image img = go.AddComponent<Image>(); img.color = color; img.raycastTarget = false; }
    public static void Deco(RectTransform rt, Color color) { Deco(rt.gameObject, color); }
    private static TMP_Text TxtNode(GameObject go, float fontSize, Color color, FontStyles style, TextAlignmentOptions align) { TMP_Text t = go.AddComponent<TextMeshProUGUI>(); t.fontSize = fontSize; t.color = color; t.fontStyle = style; t.alignment = align; t.raycastTarget = false; return t; }
}



public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        originalScale = rt.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => rt.localScale = originalScale * 1.02f;
    public void OnPointerExit(PointerEventData eventData) => rt.localScale = originalScale;
    public void OnPointerDown(PointerEventData eventData) => rt.localScale = originalScale * 0.94f;
    public void OnPointerUp(PointerEventData eventData) => rt.localScale = originalScale * (eventData.pointerEnter == gameObject ? 1.02f : 1f);
    void OnDisable() { if (rt != null) rt.localScale = originalScale; }
}