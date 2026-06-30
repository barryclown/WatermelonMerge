using UnityEngine;

// 掛在 Boss sprite 物件上：idle 呼吸/微浮動 ＋ 受擊 squash 後座 ＋ 受擊閃白 ＋ 地面投影 ＋ 不定時可愛語音飄字。
// 純 transform / SpriteRenderer / TextMesh，不需動畫檔。執行期由 BossManager 掛上並設定語音。
public class BossJuice : MonoBehaviour
{
    private SpriteRenderer sr;
    private Vector3 baseScale;
    private Vector3 basePos;
    private Color baseColor = Color.white;
    private float idleT;
    private float hitT; // 受擊反應倒數（0=無）

    private const float HitDur = 0.22f;

    private Transform shadow;
    public string[] voices;       // 由 BossManager 依動物設定（貓/倉鼠…）
    private float voiceT;
    private float nextVoice;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        CaptureBase();
        EnsureShadow();
        nextVoice = Random.Range(2.5f, 5f);
    }

    private void OnEnable()
    {
        CaptureBase(); // 換 Boss / 重新啟用時刷新基準
        idleT = Random.Range(0f, 6f);
        hitT = 0f;
        voiceT = 0f;
        nextVoice = Random.Range(2.5f, 5f);
    }

    private void CaptureBase()
    {
        baseScale = transform.localScale;
        basePos = transform.localPosition;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    // 地面柔影：扁平柔邊圓點，畫在 Boss 腳下、Boss 之後（更立體）
    private void EnsureShadow()
    {
        if (shadow != null) return;
        var go = new GameObject("BossShadow");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, -1.0f, 0f);
        go.transform.localScale = new Vector3(2.0f, 0.6f, 1f);
        var ssr = go.AddComponent<SpriteRenderer>();
        ssr.sprite = FXManager.SoftCircle();
        ssr.color = new Color(0f, 0f, 0f, 0.22f);
        if (sr != null)
        {
            ssr.sortingLayerID = sr.sortingLayerID;
            ssr.sortingOrder = sr.sortingOrder - 1;
        }
        shadow = go.transform;
    }

    private void Update()
    {
        idleT += Time.deltaTime;

        // idle 呼吸：橫向變寬一點縱向變矮一點交替 ＋ 上下微浮
        float breathe = Mathf.Sin(idleT * 2.0f) * 0.03f;
        float bob = Mathf.Sin(idleT * 1.6f) * 0.06f;
        Vector3 scale = baseScale + new Vector3(-breathe, breathe, 0f) * baseScale.x;
        Vector3 pos = basePos + Vector3.up * bob;

        // 受擊：壓扁（變寬變矮）＋下沉後座 ＋閃白，HitDur 內回復
        if (hitT > 0f)
        {
            hitT -= Time.deltaTime;
            float k = Mathf.Clamp01(hitT / HitDur); // 1→0
            float squash = k * 0.18f;
            scale += new Vector3(squash, -squash, 0f) * baseScale.x;
            pos += Vector3.down * (k * 0.15f);
            if (sr != null) sr.color = Color.Lerp(baseColor, Color.white, k * 0.7f);
        }
        else if (sr != null)
        {
            sr.color = baseColor;
        }

        transform.localScale = scale;
        transform.localPosition = pos;

        // 影子保持貼地、不跟著呼吸縮放抖動太多
        if (shadow != null)
        {
            float sw = 2.0f - bob * 0.6f;
            shadow.localScale = new Vector3(sw, 0.55f, 1f);
        }

        // 不定時冒可愛語音飄字
        voiceT += Time.deltaTime;
        if (voiceT >= nextVoice)
        {
            voiceT = 0f;
            nextVoice = Random.Range(3.5f, 6.5f);
            SpawnVoice();
        }
    }

    public void Hit()
    {
        hitT = HitDur;
    }

    private static readonly string[] CatVoices = { "喵～", "喵喵～", "呼嚕嚕～", "喵嗚～", "喵？" };

    private void SpawnVoice()
    {
        string[] pool = (voices != null && voices.Length > 0) ? voices : CatVoices;
        string line = pool[Random.Range(0, pool.Length)];

        var go = new GameObject("BossVoice");
        go.transform.position = transform.position + new Vector3(Random.Range(0.4f, 1.1f), 1.2f, 0f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = line;
        tm.font = RuntimeUI.CjkFont;
        tm.fontSize = 64;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.36f, 0.27f, 0.22f, 1f); // 暖深棕
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && RuntimeUI.CjkFont != null)
        {
            mr.material = RuntimeUI.CjkFont.material;
            mr.sortingOrder = 70;
        }
        go.AddComponent<FloatingLabel>();
    }
}

// 飄字：上飄 ＋ 輕微彈出 ＋ 末段淡出，約 1.2 秒後銷毀
public class FloatingLabel : MonoBehaviour
{
    private float t;
    private const float Life = 1.3f;
    private TextMesh tm;
    private Color baseColor;

    private void Awake()
    {
        tm = GetComponent<TextMesh>();
        if (tm != null) baseColor = tm.color;
        transform.localScale = Vector3.one * 0.6f;
    }

    private void Update()
    {
        t += Time.deltaTime;
        transform.position += Vector3.up * 0.6f * Time.deltaTime;
        float pop = Mathf.Clamp01(t / 0.15f);
        transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, pop);
        if (tm != null)
        {
            float a = Mathf.Clamp01((Life - t) / (Life * 0.4f));
            var c = baseColor; c.a = a; tm.color = c;
        }
        if (t >= Life) Destroy(gameObject);
    }
}
