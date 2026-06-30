using System.Collections;
using UnityEngine;

// 五隻 Boss 的「盤面干擾」驅動。自舉單例，從 GameManager/BattleStageManager 抓資料，不需場景手動掛載。
// BattleStageManager 在每隻 Boss 進場時呼叫 StartForBoss(index)，死亡時呼叫 StopMechanic()。
public class BossMechanic : MonoBehaviour
{
    private static BossMechanic _instance;
    public static BossMechanic instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("BossMechanic");
                _instance = go.AddComponent<BossMechanic>();
            }
            return _instance;
        }
    }

    private Coroutine running;

    // Boss5 二階段：由 BattleStageManager 在血量快見底時觸發一次（回 50%＋狂暴）
    public bool boss5Enraged = false;
    public bool boss5SecondWindUsed = false;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    public void StartForBoss(int index)
    {
        StopMechanic();
        boss5Enraged = false;
        boss5SecondWindUsed = false;
        running = StartCoroutine(MechanicRoutine(index));
    }

    public void StopMechanic()
    {
        if (running != null) StopCoroutine(running);
        running = null;
        // Boss 死時若壓頂線還壓著，復原，避免帶到下一關
        if (EndTrigger.instance != null) EndTrigger.instance.ClearTemporaryLower();
    }

    // 累積式（6 隻，第 1 隻純教學）：每隻 Boss = 前面所有機制 ＋ 一個新的，越後面越兇。
    //  B1 無 → B2 丟玩具 → B3 +晃盤 → B4 +玩具波 → B5 +閃避(被動,在傷害結算) → B6 +二階段狂暴
    private IEnumerator MechanicRoutine(int index)
    {
        // Boss1（index 0）＝純教學，完全無干擾，讓玩家先學會合成＝攻擊
        if (index < 1) yield break;

        // 開場加壓：Boss2 起進場先丟一波入場玩具（關卡越後越多）
        yield return new WaitForSeconds(1.5f);
        ThrowJunk(Mathf.Clamp(index, 1, 3), Mathf.Clamp(index, 1, 3));

        float junkT = 0f, shakeT = 0f, waveT = 0f;

        while (true)
        {
            yield return null;
            float dt = Time.deltaTime;
            var bs = BattleStageManager.instance;
            if (bs == null) continue;
            bool enraged = boss5Enraged;   // 最終 Boss 二階段觸發後才狂暴（BattleStageManager 設旗標）

            // (1) 丟玩具：Boss2 起都有，頻率隨關卡變快，狂暴再加倍
            junkT += dt;
            float junkInterval = Mathf.Max(4f, 13f - index * 1.7f) / (enraged ? 2f : 1f);
            if (junkT >= junkInterval) { junkT = 0f; ThrowJunk(1, Mathf.Clamp(index, 1, 3)); }

            // (2) 晃盤：Boss3 起，狂暴後更頻繁
            if (index >= 2)
            {
                shakeT += dt;
                if (shakeT >= (enraged ? 5f : 8f)) { shakeT = 0f; ShakeBoard(3.5f + index * 0.3f); }
            }

            // (3) 玩具波（額外整波）：Boss4 起
            if (index >= 3)
            {
                waveT += dt;
                if (waveT >= 10f) { waveT = 0f; ThrowJunk(2, 2); }
            }

            // (Boss5 的閃避、Boss6 的二階段回血＝在 BattleStageManager 傷害結算裡處理)
        }
    }

    // 從頂端丟 count 顆垃圾果（等級 1~maxLevel 隨機）。先在落點顯示「掉落預告」再落下，不突兀。
    public void ThrowJunk(int count, int maxLevel)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.watermelonPrefabs == null || gm.watermelonPrefabs.Length == 0) return;

        for (int i = 0; i < count; i++)
            StartCoroutine(TelegraphAndDrop(maxLevel));
    }

    // 預告：在落點上方顯示警告圖、脈動閃爍約 1 秒＋音效，然後生成垃圾果落下
    private IEnumerator TelegraphAndDrop(int maxLevel)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.watermelonPrefabs == null || gm.watermelonPrefabs.Length == 0) yield break;

        int lvl = Mathf.Clamp(Random.Range(1, maxLevel + 1), 1, gm.watermelonPrefabs.Length);
        GameObject prefab = gm.watermelonPrefabs[lvl - 1];
        if (prefab == null) yield break;

        float x = Random.Range(gm.xMin, gm.xMax);
        float y = gm.yMax;
        Vector3 dropPos = new Vector3(x, y, 0f);

        // 預告標記：優先用場景的 warningImagePrefab，沒有就程式自建一個紅色警示
        GameObject marker;
        if (EndTrigger.instance != null && EndTrigger.instance.warningImagePrefab != null)
        {
            marker = Instantiate(EndTrigger.instance.warningImagePrefab, dropPos, Quaternion.identity);
            marker.SetActive(true);
        }
        else
        {
            marker = CreateWarningMarker(dropPos);
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayFX(2);

        // 脈動閃爍約 1 秒
        float t = 0f, dur = 1.0f;
        SpriteRenderer msr = marker != null ? marker.GetComponent<SpriteRenderer>() : null;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (marker != null)
            {
                float pulse = 0.8f + 0.3f * Mathf.Abs(Mathf.Sin(t * 10f));
                marker.transform.localScale = Vector3.one * pulse;
                if (msr != null)
                {
                    Color c = msr.color;
                    c.a = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(t * 10f));
                    msr.color = c;
                }
            }
            yield return null;
        }

        if (marker != null) Destroy(marker);

        // 生成垃圾果落下
        GameObject junk = Instantiate(prefab, dropPos, Quaternion.identity);
        Watermelon w = junk.GetComponent<Watermelon>();
        if (w != null)
        {
            w.level = lvl;
            w.isJunk = true;
        }
    }

    // 程式生成的警示標記 sprite（一次生成、重複用）
    private static Sprite _markerSprite;
    private static Sprite MarkerSprite()
    {
        if (_markerSprite != null) return _markerSprite;
        var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        var px = new Color[64];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _markerSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
        return _markerSprite;
    }

    // 在落點上方建一個紅色警示標記（往下的感覺），高 sorting 確保可見
    private GameObject CreateWarningMarker(Vector3 pos)
    {
        GameObject go = new GameObject("JunkWarning");
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MarkerSprite();
        sr.color = new Color(1f, 0.2f, 0.2f, 1f);
        sr.sortingOrder = 50;
        return go;
    }

    // 對盤面所有水果施一次隨機水平衝量
    public void ShakeBoard(float force)
    {
        Watermelon[] all = FindObjectsOfType<Watermelon>();
        foreach (var w in all)
        {
            if (w == null) continue;
            Rigidbody2D rb = w.GetComponent<Rigidbody2D>();
            if (rb != null && !rb.isKinematic)
                rb.AddForce(new Vector2(Random.Range(-force, force), Random.Range(0f, force * 0.3f)), ForceMode2D.Impulse);
        }
    }

    // 清掉場上所有「Boss 丟的垃圾果」，保留玩家自己養的果（防死亡螺旋）
    public void ClearJunk()
    {
        Watermelon[] all = FindObjectsOfType<Watermelon>();
        foreach (var w in all)
        {
            if (w != null && w.isJunk)
                Destroy(w.gameObject);
        }
    }
}
