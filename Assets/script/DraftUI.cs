using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 關間「三選一抽能力」面板。執行期自建 ScreenSpaceOverlay Canvas（直式），
// 中文用 legacy Text + 動態 OS 字型，onClick 程式 AddListener。自舉單例。
public class DraftUI : MonoBehaviour
{
    private static DraftUI _instance;
    public static DraftUI instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DraftUI");
                _instance = go.AddComponent<DraftUI>();
            }
            return _instance;
        }
    }

    private GameObject canvasGo;
    private Action<AbilityId> onPicked;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    public void Show(List<AbilityId> options, string subtitle, Action<AbilityId> picked)
    {
        onPicked = picked;
        Build(options, subtitle);
    }

    private void Build(List<AbilityId> options, string subtitle)
    {
        if (canvasGo != null) Destroy(canvasGo);

        Canvas canvas = RuntimeUI.CreateOverlayCanvas("DraftCanvas", 1000);
        canvasGo = canvas.gameObject;

        // 漸層全螢幕底（擋點擊）
        GameObject panel = RuntimeUI.MakeGradientBackground(canvas.transform);

        // 標題 + 副標
        RuntimeUI.MakeTitle(panel.transform, "選擇一個能力", 64, RuntimeUI.Palette.Melon,
            new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f));
        if (!string.IsNullOrEmpty(subtitle))
        {
            Text sub = RuntimeUI.MakeText(panel.transform, subtitle, 36, FontStyle.Bold,
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.875f));
            sub.color = RuntimeUI.Palette.TextDark;
        }

        // 三張卡片直排（直式螢幕）
        int n = options.Count;
        // 卡片區佔 y 0.08~0.80
        float top = 0.78f, bottom = 0.08f;
        float slot = (top - bottom) / n;
        for (int i = 0; i < n; i++)
        {
            AbilityId id = options[i];
            float cTop = top - i * slot;
            float cBottom = cTop - slot * 0.86f; // 卡片間留間隙
            MakeCard(panel.transform, id, cBottom, cTop);
        }
    }

    private void MakeCard(Transform parent, AbilityId id, float yMinAnchor, float yMaxAnchor)
    {
        GameObject card = RuntimeUI.NewRect("Card_" + id, parent);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.10f, yMinAnchor);
        rt.anchorMax = new Vector2(0.90f, yMaxAnchor);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        Color accent = AccentFor(id);

        Image cardBg = card.AddComponent<Image>();
        cardBg.sprite = RuntimeUI.RoundedSprite();
        cardBg.type = Image.Type.Sliced;
        cardBg.color = new Color(1f, 0.985f, 0.94f, 0.98f);
        RuntimeUI.AddShadow(cardBg, new Color(0f, 0f, 0f, 0.20f), new Vector2(0f, -9f));

        GameObject header = RuntimeUI.NewRect("HeaderTint", card.transform);
        RectTransform hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 0.58f);
        hrt.anchorMax = Vector2.one;
        hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
        Image headerBg = header.AddComponent<Image>();
        headerBg.sprite = RuntimeUI.RoundedSprite();
        headerBg.type = Image.Type.Sliced;
        headerBg.color = new Color(accent.r, accent.g, accent.b, 0.16f);
        headerBg.raycastTarget = false;

        GameObject dot = RuntimeUI.NewRect("AccentDot", card.transform);
        RectTransform drt = dot.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.065f, 0.665f);
        drt.anchorMax = new Vector2(0.155f, 0.845f);
        drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
        Image dotImg = dot.AddComponent<Image>();
        dotImg.sprite = RuntimeUI.RoundedSprite();
        dotImg.type = Image.Type.Sliced;
        dotImg.color = accent;
        dotImg.raycastTarget = false;

        Button btn = card.AddComponent<Button>();
        btn.targetGraphic = cardBg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.97f, 0.88f, 1f);
        cb.pressedColor = new Color(0.92f, 0.88f, 0.82f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        AbilityId captured = id;
        btn.onClick.AddListener(() => OnPick(captured));

        // 名稱（卡片上半）
        Text name = RuntimeUI.MakeText(card.transform, AbilityManager.NameOf(id), 46, FontStyle.Bold,
            new Vector2(0.18f, 0.58f), new Vector2(0.94f, 0.92f));
        name.alignment = TextAnchor.MiddleLeft;
        name.color = RuntimeUI.Palette.TextDark;

        // 描述（卡片下半）
        Text desc = RuntimeUI.MakeText(card.transform, AbilityManager.DescOf(id), 32, FontStyle.Bold,
            new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.53f));
        desc.alignment = TextAnchor.MiddleLeft;
        desc.lineSpacing = 1.05f;
        desc.color = RuntimeUI.Palette.TextDark;
    }

    private Color AccentFor(AbilityId id)
    {
        switch (id)
        {
            case AbilityId.Heavy:
            case AbilityId.MelonBurst:
                return RuntimeUI.Palette.Melon;
            case AbilityId.Bomber:
            case AbilityId.Sweep:
                return new Color(1f, 0.58f, 0.27f, 1f);
            case AbilityId.Calm:
            case AbilityId.LuckyStart:
                return RuntimeUI.Palette.Rind;
            case AbilityId.ChainMaster:
                return new Color(0.48f, 0.62f, 1f, 1f);
            default:
                return RuntimeUI.Palette.Melon;
        }
    }

    private void OnPick(AbilityId id)
    {
        var cb = onPicked;
        onPicked = null;
        if (canvasGo != null) { Destroy(canvasGo); canvasGo = null; }
        cb?.Invoke(id);
    }
}
