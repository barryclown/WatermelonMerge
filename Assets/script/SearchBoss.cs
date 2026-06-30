using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SearchBoss : MonoBehaviour
{
    public static SearchBoss instance;

    // 舊欄位保留避免斷場景序列化引用（已不使用）
    [HideInInspector] public GameObject pet;

    // 開場序列是否跑完（UIManager.SearchCoroutine 等它，教學在它之後才開）
    public bool openingDone = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void Anim()
    {
        StartCoroutine(OpeningCo());
    }

    // 開場只保留「第 N 關 / 開始！」卡片。
    // Boss 登場影片與舊 searchPanel 過場已移除，避免進關前多一段不可操作畫面。
    // 全程 timeScale=0 → 一律 unscaled。
    private IEnumerator OpeningCo()
    {
        openingDone = false;
        int level = (BattleStageManager.instance != null) ? BattleStageManager.instance.currentBossIndex + 1 : 1;

        // ---- 1) 置中關卡卡 ----
        Canvas c = RuntimeUI.CreateOverlayCanvas("OpeningCanvas", 1050);
        GameObject canvasGo = c.gameObject;
        RuntimeUI.MakeGradientBackground(c.transform);

        // 全部用「中心點錨點」死釘螢幕正中（已用螢幕座標量過：卡片中心＝螢幕中心）
        GameObject card = RuntimeUI.MakeCard(c.transform, Vector2.zero, Vector2.one, RuntimeUI.Palette.CardWhite);
        var crt = card.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(770f, 330f); crt.anchoredPosition = Vector2.zero;
        crt.localScale = Vector3.zero;

        Text title = RuntimeUI.MakeTitle(c.transform, "第 " + level + " 關", 92, RuntimeUI.Palette.Melon, Vector2.zero, Vector2.one);
        var trt = title.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(700f, 150f); trt.anchoredPosition = new Vector2(0f, 62f);
        trt.localScale = Vector3.zero;

        Text go = RuntimeUI.MakeTitle(c.transform, "開始！", 74, RuntimeUI.Palette.Rind, Vector2.zero, Vector2.one);
        var grt = go.rectTransform;
        grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = new Vector2(520f, 120f); grt.anchoredPosition = new Vector2(0f, -72f);
        grt.localScale = Vector3.zero;

        yield return Pop(crt, 0.32f);
        yield return Pop(trt, 0.26f);
        yield return Pop(grt, 0.26f);
        yield return WaitU(0.7f);

        // 先通知 UIManager 收掉搜尋狀態，再晚一幀移除卡片，避免露出舊過場背景。
        openingDone = true;
        yield return null;
        if (canvasGo != null) Destroy(canvasGo);
    }

    // 彈入：scale 0 → 過衝 → 1（EaseOutBack）
    private IEnumerator Pop(RectTransform rt, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            float k = t / dur;
            rt.localScale = Vector3.one * EaseOutBack(k);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private float EaseOutBack(float k)
    {
        const float c1 = 1.70158f, c3 = 2.70158f;
        float p = k - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    private IEnumerator WaitU(float sec)
    {
        float t = 0f;
        while (t < sec) { t += Time.unscaledDeltaTime; yield return null; }
    }
}
