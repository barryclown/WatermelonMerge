using System.Collections;
using UnityEngine;

// 程式生成特效：合成爆裂 burst（擴散環＋飛濺）、相機震屏。自舉單例，無需場景掛載或美術素材。
public class FXManager : MonoBehaviour
{
    private static FXManager _instance;
    public static FXManager instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("FXManager");
                _instance = go.AddComponent<FXManager>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    // ---- 程式生成：柔邊圓點 ----
    private static Sprite _soft;
    public static Sprite SoftCircle()
    {
        if (_soft != null) return _soft;
        int s = 64; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false); tex.wrapMode = TextureWrapMode.Clamp;
        float r = s * 0.5f; var px = new Color32[s * s];
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float dx = x + 0.5f - r, dy = y + 0.5f - r;
            float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
            float a = Mathf.Clamp01(1f - d); a *= a;
            px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(px); tex.Apply();
        _soft = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        return _soft;
    }

    // ---- 程式生成：空心環 ----
    private static Sprite _ring;
    public static Sprite RingSprite()
    {
        if (_ring != null) return _ring;
        int s = 64; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false); tex.wrapMode = TextureWrapMode.Clamp;
        float r = s * 0.5f; var px = new Color32[s * s];
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float dx = x + 0.5f - r, dy = y + 0.5f - r;
            float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
            float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.18f);
            px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(px); tex.Apply();
        _ring = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        return _ring;
    }

    // 合成爆裂：擴散環 ＋ 數顆飛濺
    public void Burst(Vector3 pos, Color color, float scale)
    {
        StartCoroutine(RingCo(pos, color, scale));
        int n = 6;
        for (int i = 0; i < n; i++)
        {
            float ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            StartCoroutine(ParticleCo(pos, dir * Random.Range(2f, 3.5f) * scale, color, scale));
        }
    }

    private IEnumerator RingCo(Vector3 pos, Color color, float scale)
    {
        var go = new GameObject("FXRing"); go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = RingSprite(); sr.color = color; sr.sortingOrder = 60;
        float t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.deltaTime; float k = t / dur;
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 2.2f, k) * scale;
            var c = color; c.a = 1f - k; sr.color = c;
            yield return null;
        }
        Destroy(go);
    }

    private IEnumerator ParticleCo(Vector3 pos, Vector2 vel, Color color, float scale)
    {
        var go = new GameObject("FXParticle"); go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = SoftCircle(); sr.color = color; sr.sortingOrder = 61;
        float t = 0f, dur = 0.4f; Vector3 p = pos;
        while (t < dur)
        {
            t += Time.deltaTime; float k = t / dur;
            p += (Vector3)vel * Time.deltaTime; vel *= 0.9f;
            go.transform.position = p;
            go.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 0.05f, k) * scale;
            var c = color; c.a = 1f - k; sr.color = c;
            yield return null;
        }
        Destroy(go);
    }

    // ---- 相機震屏 ----
    private Coroutine shakeCo;
    private Vector3 camBase;
    private bool shaking;
    public void Shake(float amplitude, float duration)
    {
        var cam = Camera.main; if (cam == null) return;
        if (!shaking) camBase = cam.transform.localPosition;
        if (shakeCo != null) StopCoroutine(shakeCo);
        shakeCo = StartCoroutine(ShakeCo(cam.transform, amplitude, duration));
    }

    private IEnumerator ShakeCo(Transform cam, float amp, float dur)
    {
        shaking = true;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - (t / dur);
            Vector2 off = Random.insideUnitCircle * amp * k;
            cam.localPosition = camBase + new Vector3(off.x, off.y, 0f);
            yield return null;
        }
        cam.localPosition = camBase;
        shaking = false;
    }
}
