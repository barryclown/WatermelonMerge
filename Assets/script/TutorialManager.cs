using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 首戰「玩中學」引導：高亮生成區虛線框＋一句提示，帶玩家親手完成第一次合成，取代文字牆。
// 引導期間強制生成 Lv1（前兩顆同款→必合成），合成後恢復正常。自舉單例、零場景接線。
// 每個 play session 的第一場 Boss1 只觸發一次。
public class TutorialManager : MonoBehaviour
{
    private static TutorialManager _instance;
    public static TutorialManager instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("TutorialManager");
                _instance = go.AddComponent<TutorialManager>();
            }
            return _instance;
        }
    }

    // 跨場景重載保留（Restart 不重播）；play 停止才重置（方便試玩重看）
    public static bool firstFightShown = false;
    private bool running = false;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    public void Begin()
    {
        if (running || firstFightShown) return;
        firstFightShown = true;
        running = true;
        StartCoroutine(Flow());
    }

    private IEnumerator Flow()
    {
        Canvas c = RuntimeUI.CreateOverlayCanvas("TutorialCanvas", 950);
        GameObject canvasGo = c.gameObject;

        // 提示泡（不擋點擊）— 放在盤面下方，避免壓住 Boss / 血條。
        GameObject hint = RuntimeUI.NewRect("Hint", c.transform);
        var hrt = hint.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0.08f, 0.265f); hrt.anchorMax = new Vector2(0.92f, 0.345f);
        hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
        Image hb = hint.AddComponent<Image>();
        hb.sprite = RuntimeUI.RoundedSprite(); hb.type = Image.Type.Sliced;
        hb.color = new Color(1f, 0.985f, 0.94f, 0.95f);
        hb.raycastTarget = false;
        RuntimeUI.AddShadow(hb, new Color(0f, 0f, 0f, 0.25f), new Vector2(0f, -5f));
        Text hintText = RuntimeUI.MakeText(hint.transform, "", 32, FontStyle.Bold, Vector2.zero, Vector2.one);
        hintText.color = RuntimeUI.Palette.TextDark;
        hintText.raycastTarget = false;

        // 世界空間「生成區」虛線框高亮（取代意義不明的紅塊）
        SpriteRenderer zoneSr = null;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            GameObject zone = new GameObject("TutorialZone");
            float zxMin = gm.xMin + 0.35f;
            float zxMax = gm.xMax - 0.35f;
            float zyMin = gm.yMin + 0.20f;
            float zyMax = gm.yMax - 0.85f;
            zone.transform.position = new Vector3((zxMin + zxMax) * 0.5f, (zyMin + zyMax) * 0.5f, 0f);
            zoneSr = zone.AddComponent<SpriteRenderer>();
            zoneSr.sprite = RuntimeUI.DashedRectSprite(256, 224, 6, 14);
            zoneSr.color = new Color(RuntimeUI.Palette.Melon.r, RuntimeUI.Palette.Melon.g, RuntimeUI.Palette.Melon.b, 0.46f);
            zoneSr.sortingOrder = 35;
            float zw = zxMax - zxMin, zh = zyMax - zyMin;
            zone.transform.localScale = new Vector3(zw / 4f, zh / 3.5f, 1f);
        }

        // 引導期間強制 Lv1，確保前兩顆同款穩定合成（合格穩定的教學）
        if (gm != null) gm.forceTutorialLv1 = true;

        float t = 0f;

        // Step 1：點擊生成區，生成第一隻
        hintText.text = "點擊這個區域，生成玩具球！";
        // 要等玩家「確實生成並放開」第一隻才進下一步：DropCount 在按下滑鼠當下就 +1，
        // 若只看 DropCount，會在玩家還握著第一隻沒放開時就跳「再放一隻」造成混亂。
        while (GameManager.Instance == null || GameManager.Instance.DropCount < 1 || GameManager.isDragging)
        {
            t += Time.deltaTime;
            if (zoneSr != null)
            {
                Color zc = zoneSr.color;
                zc.a = 0.24f + 0.26f * Mathf.Abs(Mathf.Sin(t * 4f));
                zoneSr.color = zc;
            }
            yield return null;
        }

        // 生成第一隻後，虛線框淡出（任務轉向合成）
        if (zoneSr != null)
        {
            float zf = 0f;
            while (zf < 0.4f)
            {
                zf += Time.deltaTime;
                Color zc = zoneSr.color; zc.a = (1f - zf / 0.4f) * 0.7f; zoneSr.color = zc;
                yield return null;
            }
            UnityEngine.Object.Destroy(zoneSr.gameObject);
        }

        // Step 2：再放一隻同款，讓牠們相撞合成（不改）
        int startMerges = Watermelon.TotalMerges;
        hintText.text = "再放一顆一樣的，相撞就會合成！";
        while (Watermelon.TotalMerges <= startMerges)
            yield return null;

        // 第一次合成達成 → 解除強制 Lv1，恢復正常投放
        if (GameManager.Instance != null) GameManager.Instance.forceTutorialLv1 = false;

        // Step 3：合成＝攻擊頭目（不改）
        hintText.text = "太棒了！合成就是攻擊，把頭目打倒～";
        float hold = 0f;
        while (hold < 2.6f) { hold += Time.deltaTime; yield return null; }

        // 淡出
        var cg = canvasGo.AddComponent<CanvasGroup>();
        float fo = 0f;
        while (fo < 0.6f) { fo += Time.deltaTime; cg.alpha = 1f - fo / 0.6f; yield return null; }

        if (canvasGo != null) Destroy(canvasGo);
        running = false;
    }
}
