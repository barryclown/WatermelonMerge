using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject deathPanel;   // 失敗（爆頂）面板，winPanel 未指定時也當通關面板用
    public GameObject askPanel;
    public GameObject searchPanel;
    public GameObject winPanel;      // 通關面板（可選；沒指定就退回 deathPanel）

    [Header("結算文字（可選）")]
    [Tooltip("指定後，勝/敗時會填入標題與分數；不指定則略過。")]
    public TextMeshProUGUI resultText;

    [Header("通關結局寵物圖（可選）")]
    [Tooltip("指定後通關畫面用這張當英雄圖；不指定則退回合成鏈最高階寵物。")]
    public Sprite endingPetSprite;

    [Header("再來一局按鈕（可選）")]
    [Tooltip("指定後，Start 時自動把 onClick 接到 Restart()；不必在 Inspector 手動拉事件。")]
    public Button restartButton;

    [Header("Text")]
    public TextMeshProUGUI score;
    [Tooltip("Boss 進度（可選），如「第 1 / 5 關」；不指定則執行期自建一個。")]
    public TextMeshProUGUI bossProgressText;

    [Header("Canvas Groups")]
    public CanvasGroup uiCanvasGroup;
    public CanvasGroup deathPanelGroup;

    [Header("需要被隱藏/顯示的遊戲物件")]
    [Tooltip("例如：整個遊戲世界的 Root、敵人群組、場景物件等，之後不用每次 Find。")]
    [SerializeField] private List<GameObject> objectsToHide = new List<GameObject>();

    public static UIManager instance;

    // 實際在流程中有被關閉的物件（避免多次呼叫時重複塞）
    private List<GameObject> hiddenObjects = new List<GameObject>();

    // 結算畫面是否正在顯示（給「按任意鍵重開」的保底機制用）
    private bool resultShowing = false;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void Start()
    {
        // 執行期把「再來一局」按鈕接到 Restart()，避免靠 Inspector 手動拉事件
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(Restart);
            restartButton.onClick.AddListener(Restart);
        }

        // 開場顯示主菜單（含玩法教學），按「開始遊戲」才進 run
        Time.timeScale = 0f;
        ShellUI.instance.ShowMainMenu(BeginRun);
    }

    // 主菜單按「開始遊戲」後：建立暫停鈕並顯示「第 N 關 / 開始！」卡片
    public void BeginRun()
    {
        ShellUI.instance.ShowPauseButton();
        StartSearch();   // SearchCoroutine 會自行管理 timeScale，卡片結束後恢復操作
    }

    private void Update()
    {
        // 結算畫面（勝/敗）顯示中：按任意鍵或點擊即重新開始。
        // 這是保底機制，確保就算沒有「再來一局」按鈕，迴圈也一定能閉合。
        if (resultShowing)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                resultShowing = false;
                Restart();
            }
        }
    }

    // ====== UI 專用 API ======

    public void UpdateScoreUI(string totalDamage)
    {
        if (score != null)
            score.text = totalDamage;
    }

    // 執行期建立的 HUD 文字（專案無 CJK 字型，改用 legacy Text + 動態字型）
    private Canvas hudCanvas;
    private Text bossProgressLegacy;
    private Text resultLegacy;

    private Canvas GetHudCanvas()
    {
        if (hudCanvas == null)
            hudCanvas = RuntimeUI.CreateOverlayCanvas("HUDCanvas", 900);
        return hudCanvas;
    }

    // Boss 進度（第 cur / total 關）。可選 TMP 欄位優先；否則執行期自建 legacy 文字置頂。
    public void UpdateBossProgress(int cur, int total)
    {
        string msg = "第 " + cur + " / " + total + " 關";
        if (bossProgressText != null) { bossProgressText.text = msg; return; }

        if (bossProgressLegacy == null)
            bossProgressLegacy = RuntimeUI.MakeText(GetHudCanvas().transform, msg, 58, FontStyle.Bold,
                new Vector2(0.15f, 0.90f), new Vector2(0.85f, 0.985f));
        bossProgressLegacy.text = msg;
    }

    // ====== Ask / End 流程 ======

    public void Ask()
    {
        HideAllNonUIObjects();

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.blocksRaycasts = false;
        }

        askPanel.SetActive(true);

        CanvasGroup askPanelCanvasGroup = askPanel.GetComponent<CanvasGroup>();
        if (askPanelCanvasGroup != null)
        {
            askPanelCanvasGroup.interactable = true;
            askPanelCanvasGroup.blocksRaycasts = true;
        }
    }

    public void End()
    {
        StartCoroutine(EndCoroutine());
    }

    private IEnumerator EndCoroutine()
    {
        HideAllNonUIObjects();

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.blocksRaycasts = false;
        }

        Time.timeScale = 0;
        deathPanel.SetActive(true);

        CanvasGroup deathPanelCanvasGroup = deathPanel.GetComponent<CanvasGroup>();
        if (deathPanelCanvasGroup != null)
        {
            deathPanelCanvasGroup.interactable = true;
            deathPanelCanvasGroup.blocksRaycasts = true;
        }

        yield return null;
    }

    // ====== 勝 / 敗（分開）＋ 重新開始 ======

    // 通關：打完最後一隻 Boss 後呼叫
    public void Win()
    {
        StartCoroutine(ResultCoroutine(true));
    }

    // 失敗：爆頂出局時呼叫（不再無痛續玩）
    public void Lose()
    {
        StartCoroutine(ResultCoroutine(false));
    }

    private IEnumerator ResultCoroutine(bool isWin)
    {
        HideAllNonUIObjects();

        // 停掉 Boss 干擾機制（爆頂死時 mechanic 仍在跑）
        if (BossMechanic.instance != null)
            BossMechanic.instance.StopMechanic();

        // 結算畫面時停止投放，避免玩家在面板後面繼續丟水果
        if (BattleStageManager.instance != null)
            BattleStageManager.instance.canSpawnItems = false;

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.blocksRaycasts = false;
        }

        // 用新美術 overlay 取代舊 deathPanel/winPanel（舊面板是早期美術，不再啟用）
        BuildResultOverlay(isWin);

        Time.timeScale = 0;

        // 等一影格再開放「按鍵重開」，避免造成結算當下殘留的輸入立刻觸發重開
        yield return null;
        resultShowing = true;
    }

    // 結算 overlay（程式美術）：通關＝漸層底＋寵物英雄圖；失敗＝暗底＋卡片
    private GameObject resultCanvasGo;
    private void BuildResultOverlay(bool isWin)
    {
        if (resultCanvasGo != null) Destroy(resultCanvasGo);
        Canvas c = RuntimeUI.CreateOverlayCanvas("ResultCanvas", 1300);
        resultCanvasGo = c.gameObject;

        GameObject panel = isWin
            ? RuntimeUI.MakeGradientBackground(c.transform)
            : RuntimeUI.MakeFullscreenPanel(c.transform, new Color(0.15f, 0.12f, 0.13f, 0.93f));

        string scoreStr = score != null ? score.text : "0";

        if (isWin)
        {
            RuntimeUI.MakeTitle(panel.transform, "恭喜通關！", 76, RuntimeUI.Palette.Melon,
                new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.91f));

            // 寵物英雄圖：優先用指定的結局圖（AI 生圖），否則退回合成鏈最高階寵物
            Sprite petSprite = endingPetSprite != null ? endingPetSprite : GetTopPetSprite();
            if (petSprite != null)
            {
                GameObject img = RuntimeUI.NewRect("PetImage", panel.transform);
                var rt = img.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.26f, 0.44f); rt.anchorMax = new Vector2(0.74f, 0.76f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var im = img.AddComponent<Image>();
                im.sprite = petSprite; im.preserveAspect = true;
            }

            RuntimeUI.MakeText(panel.transform, "恭喜你，你的玩具是最強的！", 42, FontStyle.Bold,
                new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.43f));
            RuntimeUI.MakeText(panel.transform, "總傷害：" + scoreStr, 34, FontStyle.Normal,
                new Vector2(0.1f, 0.27f), new Vector2(0.9f, 0.33f));
        }
        else
        {
            RuntimeUI.MakeCard(panel.transform,
                new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.66f), RuntimeUI.Palette.CardWhite);
            RuntimeUI.MakeTitle(panel.transform, "玩具被擠出去了…", 54, RuntimeUI.Palette.Melon,
                new Vector2(0.14f, 0.54f), new Vector2(0.86f, 0.63f));
            RuntimeUI.MakeText(panel.transform, "總傷害：" + scoreStr, 36, FontStyle.Bold,
                new Vector2(0.14f, 0.45f), new Vector2(0.86f, 0.52f));
        }

        RuntimeUI.MakeText(panel.transform, "（按任意鍵 / 點擊　再來一局）", 28, FontStyle.Normal,
            new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.22f));
    }

    private Sprite GetTopPetSprite()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.watermelonPrefabs != null && gm.watermelonPrefabs.Length > 0)
        {
            var top = gm.watermelonPrefabs[gm.watermelonPrefabs.Length - 1];
            if (top != null)
            {
                var sr = top.GetComponent<SpriteRenderer>();
                if (sr != null) return sr.sprite;
            }
        }
        return null;
    }

    // 結算：標題＋總傷害（resultText TMP 欄位優先；否則執行期自建 legacy 文字置中）
    private void ShowResultSummary(bool isWin)
    {
        string title = isWin ? "通關！" : "爆頂出局";
        string scoreStr = score != null ? score.text : "0";
        string msg = title + "\n總傷害：" + scoreStr + "\n\n（按任意鍵 / 點擊　再來一局）";

        if (resultText != null) { resultText.text = msg; return; }

        if (resultLegacy == null)
            resultLegacy = RuntimeUI.MakeText(GetHudCanvas().transform, msg, 60, FontStyle.Bold,
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.72f));
        resultLegacy.text = msg;
    }

    // 再來一局：重載當前場景（把 deathPanel/winPanel 的按鈕接到這個）
    public void Restart()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    public void Yes()
    {
        StartCoroutine(YesCoroutine());
    }

    private IEnumerator YesCoroutine()
    {
        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = true;
            uiCanvasGroup.blocksRaycasts = true;
        }

        foreach (GameObject obj in EndTrigger.instance.hasEnd)
        {
            Destroy(obj);
        }
        EndTrigger.instance.hasEnd.Clear();

        ShowAllNonUIObjects();
        askPanel.SetActive(false);
        Time.timeScale = 1;

        yield return null;
    }

    public void No()
    {
        StartCoroutine(NoCoroutine());
    }

    private IEnumerator NoCoroutine()
    {
        askPanel.SetActive(false);
        yield return StartCoroutine(EndCoroutine());
    }

    // ====== 顯示 / 隱藏場景物件（改過） ======

    public void ShowAllNonUIObjects()
    {
        foreach (GameObject obj in hiddenObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }
        hiddenObjects.Clear();
    }

    public void HideAllNonUIObjects()
    {
        hiddenObjects.Clear();

        // 只處理事先指定在 objectsToHide 裡面的物件
        foreach (GameObject obj in objectsToHide)
        {
            if (obj == null) continue;

            if (obj.activeSelf)
            {
                hiddenObjects.Add(obj);
                obj.SetActive(false);
            }
        }
    }

    // ====== 搜尋流程 ======

    public void StartSearch()
    {
        StartCoroutine(SearchCoroutine());
    }

    private IEnumerator SearchCoroutine()
    {
        HideAllNonUIObjects();
        BattleStageManager.instance.gameObject.SetActive(true);

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.blocksRaycasts = false;
        }

        if (searchPanel != null)
            searchPanel.SetActive(false);

        if (SearchBoss.instance != null)
            SearchBoss.instance.Anim();
        else
            Debug.LogError("UIManager: SearchBoss.instance is null!");

        Time.timeScale = 0;

        // 等開場動畫自己跑完（去掉固定 5 秒尾巴的空檔）；保險 timeout 6s
        float w = 0f;
        while ((SearchBoss.instance == null || !SearchBoss.instance.openingDone) && w < 6f)
        {
            w += Time.unscaledDeltaTime;
            yield return null;
        }

        StopSearch();
    }

    public void StopSearch()
    {
        StartCoroutine(StopSearchCoroutine());
    }

    private IEnumerator StopSearchCoroutine()
    {
        if (searchPanel != null)
            searchPanel.SetActive(false);

        ShowAllNonUIObjects();

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = true;
            uiCanvasGroup.blocksRaycasts = true;
        }

        Time.timeScale = 1;

        // 首戰「玩中學」引導：只在第一場 Boss1 入場後觸發一次
        if (BattleStageManager.instance != null && BattleStageManager.instance.currentBossIndex == 0
            && !TutorialManager.firstFightShown)
        {
            TutorialManager.instance.Begin();
        }

        yield return null;
    }
}
