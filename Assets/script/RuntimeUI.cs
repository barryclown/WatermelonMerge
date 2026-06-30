using System.Text;
using UnityEngine;
using UnityEngine.UI;

// 執行期 UI 工具：提供「能渲染中文的動態字型」與建立 uGUI 元件的小工具。
// 專案沒有 CJK 的 TMP 字型，且動態 OS 字型無法即時轉 TMP_FontAsset，
// 因此中文一律用 legacy UI.Text + 動態 OS 字型（Microsoft JhengHei）。
//
// 美術方向（2026-06-19 拍板，見 遊戲迴圈_roguelite版.md §5.6）：
// 柔和粉彩 × 暖色系居家溫馨 × 蠟筆手繪貼紙感。圓角卡片＋厚描邊標題＋落下陰影。
// 全部純程式生成（圓角 sprite／漸層／描邊／陰影），無外部美術素材。
public static class RuntimeUI
{
    // ===== 配色盤（寵物主題暖粉彩） =====
    public static class Palette
    {
        public static readonly Color Cream    = Hex(0xFBF3E4); // 背景頂
        public static readonly Color Peach     = Hex(0xF7D9C4); // 背景底
        public static readonly Color Melon     = Hex(0xFF6B7A); // 主題粉紅（主行動色）
        public static readonly Color Rind      = Hex(0x6FBF73); // 草綠（次行動色）
        public static readonly Color SeedBrown = Hex(0x3A2E2A); // 籽棕（描邊/陰影）
        public static readonly Color TextDark  = Hex(0x4A3B33); // 暖深棕（淺底文字）
        public static readonly Color CardWhite = new Color(1f, 0.99f, 0.96f, 0.92f); // 卡片底
        public static readonly Color DimBack   = new Color(0.20f, 0.13f, 0.12f, 0.55f); // 半透明遮罩
    }

    public static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }

    private static Font _cjk;
    public static Font CjkFont
    {
        get
        {
            if (_cjk != null) return _cjk;
            string[] candidates = { "Microsoft JhengHei", "Microsoft YaHei", "PMingLiU", "MingLiU", "SimSun" };
            foreach (var n in candidates)
            {
                try { _cjk = Font.CreateDynamicFontFromOSFont(n, 48); } catch { _cjk = null; }
                if (_cjk != null) break;
            }
            if (_cjk == null)
            {
                string[] all = Font.GetOSInstalledFontNames();
                if (all != null && all.Length > 0)
                    _cjk = Font.CreateDynamicFontFromOSFont(all[0], 48);
            }
            if (_cjk != null) Prewarm(_cjk);
            return _cjk;
        }
    }

    // 一次把所有會用到的字、所有尺寸/樣式預熱進動態字型圖集，
    // 避免之後設定文字時觸發圖集重建、把先前建立的文字 mesh 弄壞（大標題/BOSS 變方塊的根因）。
    private static readonly int[] PrewarmSizes = { 26, 30, 38, 40, 46, 54, 60, 64, 72, 84 };
    public static void Prewarm(Font f)
    {
        if (f == null) return;
        StringBuilder sb = new StringBuilder();
        foreach (AbilityId id in System.Enum.GetValues(typeof(AbilityId)))
        {
            sb.Append(AbilityManager.NameOf(id));
            sb.Append(AbilityManager.DescOf(id));
        }
        sb.Append("選擇一個能力第關通過！選一個能力帶進下一關通關爆頂出局總傷害（按任意鍵/點擊　再來一局）BOSS 0123456789／：＋%()。、，");
        // 外殼：主菜單／暫停／新手教學用字（已 rebrand 寵物主題）
        sb.Append("寵物養成物語開始遊戲玩法拖動生成的果到想要位置放開兩顆同款同級相撞會合並攻擊頭目王堆超過頂線太久爆出局打倒後三選一強化盤面延續下關暫停繼回主菜單注意說明點住投擲");
        // 新手教學機制預告卡（各 Boss 一句）＋ Boss 語音 ＋ 勝敗結算用字
        sb.Append("新的搗蛋鬼現牠把玩具亂丟場上這隻坐不住搖晃整個地火力全還次一大波身手敏捷對機被閃過最終登瀕死狂暴發第通選能帶進喵呼嚕嗚啾吱恭喜你是最強失敗總傷害被擠去了再來局按任意鍵知道！？～…，、。（）・‧·");
        // 玩中學引導句＋Boss 出場台詞泡用字
        sb.Append("拖隻寶貝下來放在這裡再樣讓牠們相撞合成太棒了就是攻擊把頭目打倒玩具大放送搖站得穩嗎一波想中我沒那麼簡單全力爆發啦點擊區域生球");
        string chars = sb.ToString();
        foreach (int s in PrewarmSizes)
        {
            f.RequestCharactersInTexture(chars, s, FontStyle.Normal);
            f.RequestCharactersInTexture(chars, s, FontStyle.Bold);
        }
    }

    // ===== 程式生成 sprite：圓角矩形（可 tint，9-slice 不變形） =====
    private static Sprite _rounded;
    public static Sprite RoundedSprite()
    {
        if (_rounded != null) return _rounded;
        int s = 64, r = 22;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float cx = Mathf.Clamp(fx, r, s - r);
                float cy = Mathf.Clamp(fy, r, s - r);
                float d = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                float a = Mathf.Clamp01(r - d + 0.5f); // 內部=1，邊緣 1px 抗鋸齒
                px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        _rounded = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        return _rounded;
    }

    // 程式生成：垂直漸層 sprite（top→bottom）
    public static Sprite VerticalGradient(Color top, Color bottom)
    {
        int h = 128;
        Texture2D tex = new Texture2D(1, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // 程式生成：虛線矩形外框（給新手引導「生成區」高亮用）。四邊斷續、四角實心。
    // ppu=64：256×224 預設＝世界 4×3.5（可再用 transform.localScale 對齊實際生成區）。
    public static Sprite DashedRectSprite(int w, int h, int border, int dashLen)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[w * h];
        Color32 on = new Color32(255, 255, 255, 255);
        Color32 off = new Color32(255, 255, 255, 0);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool nearL = x < border, nearR = x >= w - border;
                bool nearB = y < border, nearT = y >= h - border;
                bool onBorder = nearL || nearR || nearB || nearT;
                bool draw = false;
                if (onBorder)
                {
                    bool corner = (nearL || nearR) && (nearB || nearT);
                    if (corner) draw = true;                                  // 四角實心，框更清楚
                    else if (nearB || nearT) draw = ((x / dashLen) % 2 == 0); // 水平邊：依 x 斷續
                    else draw = ((y / dashLen) % 2 == 0);                     // 垂直邊：依 y 斷續
                }
                px[y * w + x] = draw ? on : off;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
    }

    // ===== 描邊／陰影 helper =====
    public static void AddOutline(Graphic g, Color c, float thickness)
    {
        var o = g.gameObject.AddComponent<Outline>();
        o.effectColor = c;
        o.effectDistance = new Vector2(thickness, -thickness);
        o.useGraphicAlpha = false;
    }

    public static void AddShadow(Graphic g, Color c, Vector2 dist)
    {
        var sh = g.gameObject.AddComponent<Shadow>();
        sh.effectColor = c;
        sh.effectDistance = dist;
        sh.useGraphicAlpha = false;
    }

    // ===== Canvas / Rect 基礎 =====
    public static Canvas CreateOverlayCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortingOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    public static GameObject NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    // 純色全螢幕底板（擋點擊／半透明遮罩）
    public static GameObject MakeFullscreenPanel(Transform parent, Color color)
    {
        GameObject panel = NewRect("Panel", parent);
        Stretch(panel.GetComponent<RectTransform>());
        Image bg = panel.AddComponent<Image>();
        bg.color = color;
        return panel;
    }

    // 全螢幕漸層背景（奶油→蜜桃）
    public static GameObject MakeGradientBackground(Transform parent)
    {
        GameObject panel = NewRect("Background", parent);
        Stretch(panel.GetComponent<RectTransform>());
        Image bg = panel.AddComponent<Image>();
        bg.sprite = VerticalGradient(Palette.Cream, Palette.Peach);
        bg.type = Image.Type.Simple;
        bg.color = Color.white;
        return panel;
    }

    // 圓角卡片（帶柔和落下陰影），以畫面比例錨點放置
    public static GameObject MakeCard(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = NewRect("Card", parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.sprite = RoundedSprite();
        img.type = Image.Type.Sliced;
        img.color = color;
        AddShadow(img, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -10f));
        return go;
    }

    // ===== 文字 =====
    // 一般置中文字（預設暖深棕，淺底可讀）
    public static Text MakeText(Transform parent, string content, int fontSize, FontStyle style,
                                Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = NewRect("Text", parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        Text t = go.AddComponent<Text>();
        t.font = CjkFont;
        t.text = content;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.color = Palette.TextDark;
        return t;
    }

    // 大標題：粗體＋厚白描邊＋深色投影（蠟筆貼紙感）
    public static Text MakeTitle(Transform parent, string content, int fontSize, Color faceColor,
                                 Vector2 anchorMin, Vector2 anchorMax)
    {
        Text t = MakeText(parent, content, fontSize, FontStyle.Bold, anchorMin, anchorMax);
        t.color = faceColor;
        AddShadow(t, new Color(Palette.SeedBrown.r, Palette.SeedBrown.g, Palette.SeedBrown.b, 0.85f), new Vector2(0f, -6f));
        AddOutline(t, Color.white, 4f);
        AddOutline(t, new Color(Palette.SeedBrown.r, Palette.SeedBrown.g, Palette.SeedBrown.b, 0.9f), 6f);
        return t;
    }

    // ===== 按鈕 =====
    // 圓角按鈕（預設西瓜紅），文字白底深描邊＋落下陰影＋按下回饋。onClick 程式接線
    public static Button MakeButton(Transform parent, string label, int fontSize,
                                    Vector2 anchorMin, Vector2 anchorMax,
                                    UnityEngine.Events.UnityAction onClick)
    {
        return MakeButton(parent, label, fontSize, anchorMin, anchorMax, onClick, Palette.Melon);
    }

    public static Button MakeButton(Transform parent, string label, int fontSize,
                                    Vector2 anchorMin, Vector2 anchorMax,
                                    UnityEngine.Events.UnityAction onClick, Color baseColor)
    {
        GameObject go = NewRect("Button_" + label, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // 底層＝深色「糖果厚邊」(rim)，露在下緣做出立體厚度＋柔和落影
        Color rim = new Color(baseColor.r * 0.70f, baseColor.g * 0.70f, baseColor.b * 0.70f, 1f);
        Image rimImg = go.AddComponent<Image>();
        rimImg.sprite = RoundedSprite();
        rimImg.type = Image.Type.Sliced;
        rimImg.color = rim;
        AddShadow(rimImg, new Color(0f, 0f, 0f, 0.24f), new Vector2(0f, -9f));

        // 面層＝亮色按鈕面，底部上縮露出 rim
        GameObject faceGo = NewRect("Face", go.transform);
        RectTransform frt = faceGo.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(0f, 10f);
        frt.offsetMax = Vector2.zero;
        Image faceImg = faceGo.AddComponent<Image>();
        faceImg.sprite = RoundedSprite();
        faceImg.type = Image.Type.Sliced;
        faceImg.color = baseColor;

        // 頂部高光（gloss）＝糖果光澤
        GameObject glossGo = NewRect("Gloss", faceGo.transform);
        RectTransform grt = glossGo.GetComponent<RectTransform>();
        grt.anchorMin = new Vector2(0.07f, 0.50f); grt.anchorMax = new Vector2(0.93f, 0.90f);
        grt.offsetMin = Vector2.zero; grt.offsetMax = Vector2.zero;
        Image glossImg = glossGo.AddComponent<Image>();
        glossImg.sprite = RoundedSprite();
        glossImg.type = Image.Type.Sliced;
        glossImg.color = new Color(1f, 1f, 1f, 0.30f);
        glossImg.raycastTarget = false;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = faceImg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
        cb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        Text t = MakeText(faceGo.transform, label, fontSize, FontStyle.Bold, Vector2.zero, Vector2.one);
        t.color = Color.white;
        AddShadow(t, new Color(Palette.SeedBrown.r, Palette.SeedBrown.g, Palette.SeedBrown.b, 0.6f), new Vector2(0f, -3f));
        AddOutline(t, new Color(Palette.SeedBrown.r, Palette.SeedBrown.g, Palette.SeedBrown.b, 0.8f), 3f);

        if (onClick != null) btn.onClick.AddListener(onClick);
        return btn;
    }
}
