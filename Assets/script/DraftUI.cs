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
            RuntimeUI.MakeText(panel.transform, subtitle, 38, FontStyle.Bold,
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.875f));

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

        Image cardBg = card.AddComponent<Image>();
        cardBg.sprite = RuntimeUI.RoundedSprite();
        cardBg.type = Image.Type.Sliced;
        cardBg.color = RuntimeUI.Palette.CardWhite;
        RuntimeUI.AddShadow(cardBg, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -8f));

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
            new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.95f));
        name.color = RuntimeUI.Palette.Melon;

        // 描述（卡片下半）
        Text desc = RuntimeUI.MakeText(card.transform, AbilityManager.DescOf(id), 30, FontStyle.Normal,
            new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.50f));
        desc.color = RuntimeUI.Palette.TextDark;
    }

    private void OnPick(AbilityId id)
    {
        var cb = onPicked;
        onPicked = null;
        if (canvasGo != null) { Destroy(canvasGo); canvasGo = null; }
        cb?.Invoke(id);
    }
}
