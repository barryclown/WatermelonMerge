using UnityEngine;
using TMPro;

// 傷害數字 juice：彈出 punch scale ＋ 依傷害量分級大小/顏色 ＋ 緩動上飄 ＋ 末段淡出。
public class DamageNumber : MonoBehaviour
{
    public float lifeTime = 0.9f;
    private float lifeCounter;

    private TMP_Text tmp;
    private float baseFontSize;
    private bool baseCaptured;

    private Vector3 driftVel;
    private Color faceColor = Color.white;
    private float elapsed;

    private void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        if (tmp != null && !baseCaptured) { baseFontSize = tmp.fontSize; baseCaptured = true; }
    }

    // 由 DamageManager 在啟用後呼叫：依傷害量決定大小、顏色、彈跳力道
    public void Configure(float dmg, bool isMiss)
    {
        if (tmp == null) tmp = GetComponent<TMP_Text>();
        if (tmp != null && !baseCaptured) { baseFontSize = tmp.fontSize; baseCaptured = true; }

        float sizeMul;
        if (isMiss)               { faceColor = new Color(0.72f, 0.72f, 0.75f, 1f); sizeMul = 1.5f; }
        else if (dmg >= 60f)      { faceColor = new Color(1f, 0.35f, 0.30f, 1f);    sizeMul = 2.6f; } // 重擊：橘紅特大
        else if (dmg >= 20f)      { faceColor = new Color(1f, 0.78f, 0.25f, 1f);    sizeMul = 2.0f; } // 中：金黃
        else                      { faceColor = new Color(1f, 0.97f, 0.9f, 1f);     sizeMul = 1.5f; } // 小：奶白

        if (tmp != null)
        {
            tmp.fontSize = baseFontSize * sizeMul;
            tmp.color = faceColor;
        }

        // 越大的傷害飄越高
        driftVel = Vector3.up * (1.3f + 0.7f * (sizeMul - 1f)) + Vector3.right * Random.Range(-0.3f, 0.3f);
        elapsed = 0f;
        lifeCounter = lifeTime;
        transform.localScale = Vector3.one * 0.3f;
    }

    private void OnEnable()
    {
        lifeCounter = lifeTime;
        elapsed = 0f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        lifeCounter -= dt;
        elapsed += dt;

        // 上飄：前快後慢
        float driftDamp = Mathf.Exp(-3f * elapsed);
        transform.position += driftVel * driftDamp * dt;

        // 彈出 punch scale：0.3 → 過衝 → 1.0（ease out back，前 0.18s）
        float popT = Mathf.Clamp01(elapsed / 0.18f);
        float s = Mathf.Lerp(0.3f, 1f, EaseOutBack(popT));
        transform.localScale = Vector3.one * s;

        // 末段淡出
        if (tmp != null)
        {
            float a = Mathf.Clamp01(lifeCounter / (lifeTime * 0.4f));
            Color c = faceColor; c.a = a; tmp.color = c;
        }

        if (lifeCounter <= 0 && DamageManager.instance != null)
            DamageManager.instance.PlaceIntoPool(gameObject);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
