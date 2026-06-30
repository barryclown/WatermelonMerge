using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 遊戲外殼：主菜單（含玩法教學）＋暫停鈕／暫停面板。執行期自建 uGUI overlay，自舉單例。
public class ShellUI : MonoBehaviour
{
    private static ShellUI _instance;
    public static ShellUI instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ShellUI");
                _instance = go.AddComponent<ShellUI>();
            }
            return _instance;
        }
    }

    private GameObject menuCanvas;
    private GameObject pauseBtnCanvas;
    private GameObject pausePanelCanvas;
    private Action onStartCallback;

    private const string TutorialText =
        "玩法\n\n" +
        "- 拖動生成的玩具到想要的位置放開\n" +
        "- 兩顆同款玩具相撞會合成，並攻擊頭目\n" +
        "- 玩具堆超過頂線太久會爆出局\n" +
        "- 打倒頭目後三選一強化，盤面會延續到下一關";

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    // ===== 主菜單 =====
    public void ShowMainMenu(Action onStart)
    {
        onStartCallback = onStart;
        if (menuCanvas != null) Destroy(menuCanvas);

        Canvas c = RuntimeUI.CreateOverlayCanvas("MainMenuCanvas", 1100);
        menuCanvas = c.gameObject;

        GameObject panel = RuntimeUI.MakeGradientBackground(c.transform);

        // 封面 key art（AI 生圖）置中當相框 hero，作主菜單主視覺
        Sprite cover = Resources.Load<Sprite>("petcover_menu");
        if (cover != null)
        {
            GameObject heroGo = RuntimeUI.NewRect("CoverArt", panel.transform);
            var hrt = heroGo.GetComponent<RectTransform>();
            RuntimeUI.Stretch(hrt);                  // 9:16 封面滿版鋪到四邊
            var him = heroGo.AddComponent<Image>();
            him.sprite = cover; him.preserveAspect = false;
        }

        RuntimeUI.MakeTitle(panel.transform, "寵物養成物語", 84, RuntimeUI.Palette.Melon,
            new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.965f));

        // 菜單刻意精簡：只留封面＋開始（玩法改由遊戲內「玩中學」引導教，不丟文字牆）
        RuntimeUI.MakeButton(panel.transform, "開始遊戲", 54,
            new Vector2(0.19f, 0.105f), new Vector2(0.81f, 0.215f), OnStartClicked, RuntimeUI.Palette.Rind);
    }

    // 玩法說明彈窗（從主菜單「玩法說明」鈕開，看完點「知道了」關閉）
    public void ShowTutorialPopup()
    {
        Canvas c = RuntimeUI.CreateOverlayCanvas("TutorialPopupCanvas", 1250);
        GameObject canvasGo = c.gameObject;

        GameObject panel = RuntimeUI.MakeFullscreenPanel(c.transform, RuntimeUI.Palette.DimBack);
        RuntimeUI.MakeCard(panel.transform,
            new Vector2(0.10f, 0.26f), new Vector2(0.90f, 0.75f), RuntimeUI.Palette.CardWhite);
        RuntimeUI.MakeText(panel.transform, TutorialText, 34, FontStyle.Normal,
            new Vector2(0.15f, 0.40f), new Vector2(0.85f, 0.72f));
        RuntimeUI.MakeButton(panel.transform, "知道了", 42,
            new Vector2(0.30f, 0.30f), new Vector2(0.70f, 0.39f),
            () => { if (canvasGo != null) Destroy(canvasGo); }, RuntimeUI.Palette.Rind);
    }

    private void OnStartClicked()
    {
        if (menuCanvas != null) { Destroy(menuCanvas); menuCanvas = null; }
        var cb = onStartCallback;
        onStartCallback = null;
        cb?.Invoke();
    }

    // ===== 暫停 =====
    // 進入遊戲後叫一次，建立常駐暫停鈕（右上角）
    public void ShowPauseButton()
    {
        if (pauseBtnCanvas != null) return;
        Canvas c = RuntimeUI.CreateOverlayCanvas("PauseButtonCanvas", 800);
        pauseBtnCanvas = c.gameObject;
        RuntimeUI.MakeButton(c.transform, "暫停", 30,
            new Vector2(0.80f, 0.92f), new Vector2(0.97f, 0.985f), OpenPause, RuntimeUI.Palette.Melon);
    }

    private void OpenPause()
    {
        if (pausePanelCanvas != null) return;     // 已開
        if (Time.timeScale == 0f) return;          // 結算/抽能力等暫停狀態時不重複暫停

        Time.timeScale = 0f;

        Canvas c = RuntimeUI.CreateOverlayCanvas("PausePanelCanvas", 1200);
        pausePanelCanvas = c.gameObject;

        GameObject panel = RuntimeUI.MakeFullscreenPanel(c.transform, RuntimeUI.Palette.DimBack);

        // 圓角卡片裝暫停選單
        RuntimeUI.MakeCard(panel.transform,
            new Vector2(0.18f, 0.30f), new Vector2(0.82f, 0.70f), RuntimeUI.Palette.CardWhite);

        RuntimeUI.MakeTitle(panel.transform, "暫停", 60, RuntimeUI.Palette.Melon,
            new Vector2(0.2f, 0.60f), new Vector2(0.8f, 0.69f));

        RuntimeUI.MakeButton(panel.transform, "繼續遊戲", 44,
            new Vector2(0.27f, 0.47f), new Vector2(0.73f, 0.56f), ResumeGame, RuntimeUI.Palette.Rind);

        RuntimeUI.MakeButton(panel.transform, "回主菜單", 44,
            new Vector2(0.27f, 0.34f), new Vector2(0.73f, 0.43f), BackToMenu, RuntimeUI.Palette.Melon);
    }

    private void ResumeGame()
    {
        if (pausePanelCanvas != null) { Destroy(pausePanelCanvas); pausePanelCanvas = null; }
        Time.timeScale = 1f;
    }

    private void BackToMenu()
    {
        if (pausePanelCanvas != null) { Destroy(pausePanelCanvas); pausePanelCanvas = null; }
        Time.timeScale = 1f;
        // 重載場景 → 回到主菜單（UIManager.Start 會再顯示主菜單）
        if (UIManager.instance != null) UIManager.instance.Restart();
    }

    // ===== Boss 出場短台詞泡（玩中學：取代整段文字預告卡）=====
    // in-world 氣泡放在 Boss 上方，real-time 彈入＋停留＋淡出，不凍結遊戲、不丟字牆。
    public void ShowBossBubble(string text, float duration)
    {
        StartCoroutine(BossBubbleCo(text, duration));
    }

    private IEnumerator BossBubbleCo(string text, float duration)
    {
        Canvas c = RuntimeUI.CreateOverlayCanvas("BossBubbleCanvas", 980);
        GameObject canvasGo = c.gameObject;

        GameObject bubble = RuntimeUI.MakeCard(c.transform,
            new Vector2(0.15f, 0.66f), new Vector2(0.85f, 0.78f), RuntimeUI.Palette.CardWhite);
        Text t = RuntimeUI.MakeText(bubble.transform, text, 38, FontStyle.Bold,
            new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
        t.color = RuntimeUI.Palette.TextDark;

        var cg = canvasGo.AddComponent<CanvasGroup>();

        // 彈入（縮放＋淡入）
        float a = 0f;
        while (a < 0.2f)
        {
            a += Time.deltaTime;
            float k = Mathf.Clamp01(a / 0.2f);
            cg.alpha = k;
            bubble.transform.localScale = Vector3.one * (0.7f + 0.3f * k);
            yield return null;
        }
        cg.alpha = 1f;
        bubble.transform.localScale = Vector3.one;

        // 停留
        float hold = 0f;
        while (hold < duration) { hold += Time.deltaTime; yield return null; }

        // 淡出
        float fo = 0f;
        while (fo < 0.3f) { fo += Time.deltaTime; cg.alpha = 1f - fo / 0.3f; yield return null; }

        if (canvasGo != null) Destroy(canvasGo);
    }
}
