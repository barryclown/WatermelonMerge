using System.Collections;
using UnityEngine;

public class Watermelon : MonoBehaviour
{
    [Header("等級（1 ~ N）")]
    public int level = 1;

    [Header("各等級寵物 Prefab（從 GameManager 帶進來）")]
    public GameObject[] watermelonPrefabs;

   
    [HideInInspector] public bool isMerging = false;

    // 是否已落地（碰到牆/地或其他果）。地震(ShakeBoard)只影響已落地的果，
    // 還在空中掉落或玩家手上沒放開的不受影響，避免看起來像 bug。
    [HideInInspector] public bool hasLanded = false;

    // Boss 丟的垃圾果（死亡時清除）；炸彈手能力生成的炸彈果
    [HideInInspector] public bool isJunk = false;
    [HideInInspector] public bool isBomb = false;
    private bool exploded = false;

    // 連鎖 combo（給「連鎖大師」傷害加成用）：短時間內連續合成累加
    public static int CurrentCombo = 0;
    private static float lastMergeTime = -10f;

    // 本場累計合成次數（給新手引導偵測「完成第一次合成」）；新 run 在 BattleStageManager.Start 重置
    public static int TotalMerges = 0;

    // 「寵物大爆發」能力觸發門檻：合成出第 N 級以上才爆（初版可調；最高級 10 幾乎不可能達成，故降到做得到的高級）
    private const int MelonBurstLevel = 6;

    private void Awake()
    {
        if (GameManager.Instance != null)
        {
            watermelonPrefabs = GameManager.Instance.watermelonPrefabs;
        }
        else
        {
            Debug.LogError("GameManager Instance is null!");
        }

        // 防「空氣牆」：果子靜止後 Rigidbody 會進入睡眠，睡眠中不再觸發 OnCollisionStay2D，
        // 導致兩顆同級果貼在一起卻永遠不合成。關閉睡眠讓持續接觸照常觸發合成檢查
        // （場上果子有數量上限，這點成本可忽略）。
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 遊戲結束就不處理
        if (BattleStageManager.instance != null && BattleStageManager.instance.currentHealth <= 0)
            return;

        if (isMerging)
            return;

        GameObject otherObj = collision.gameObject;

        // 碰到牆/地或其他果 = 已進到容器堆疊裡 → 標記已落地（地震只影響已落地的果）
        if (otherObj.CompareTag("Wall") || otherObj.GetComponent<Watermelon>() != null)
            hasLanded = true;

        // 炸彈果：第一次撞到牆或其他果就爆炸（清周圍，不合成）
        if (isBomb && !exploded)
        {
            if (otherObj.CompareTag("Wall") || otherObj.GetComponent<Watermelon>() != null)
            {
                Explode();
                return;
            }
        }

        // 撞牆（Tag=Wall）就播一下音效
        if (otherObj.CompareTag("Wall"))
        {
            PlayHitSound();
            return;
        }

        // 看有沒有撞到別顆寵物
        Watermelon otherWatermelon = otherObj.GetComponent<Watermelon>();

        // 撞到不是寵物就當一般碰撞
        if (otherWatermelon == null)
        {
            PlayHitSound();
            return;
        }

        if (otherWatermelon.isMerging)
            return;

        // 同等級才合成
        if (otherWatermelon.level == this.level)
        {
            // 用 InstanceID 擋掉雙方同時開合成
            if (this.GetInstanceID() < otherWatermelon.GetInstanceID())
            {
                StartMerge(otherWatermelon);
            }
        }
        else
        {
            // 不同等級就單純碰撞音效
            PlayHitSound();
        }
    }

    // 持續接觸時的合成補檢：OnCollisionEnter2D 只在「第一次接觸」觸發，若兩顆同級果在其中一顆
    // 還在合成中(isMerging)時相撞、之後雙雙靜止貼著，就再也不會有新的 Enter → 形成「空氣牆」
    // 永不合成（大顆果靜止貼合特別容易遇到）。OnCollisionStay2D 在持續接觸時每幀觸發，補上缺口。
    private float lastStayMergeCheck = -1f;
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (BattleStageManager.instance != null && BattleStageManager.instance.currentHealth <= 0)
            return;
        if (isMerging || isBomb)
            return;

        // 輕量節流：靜止接觸每物理幀都會進來，這裡同一顆最多每 0.1s 檢一次
        if (Time.time - lastStayMergeCheck < 0.1f)
            return;
        lastStayMergeCheck = Time.time;

        Watermelon otherWatermelon = collision.gameObject.GetComponent<Watermelon>();
        if (otherWatermelon == null || otherWatermelon.isMerging)
            return;

        // 同等級才合成，並用 InstanceID 擋掉雙方同時開合成（與 OnCollisionEnter2D 一致）
        if (otherWatermelon.level == this.level
            && this.GetInstanceID() < otherWatermelon.GetInstanceID())
        {
            StartMerge(otherWatermelon);
        }
    }

    private void PlayHitSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayFX(2);
        }
        else
        {
            Debug.LogError("SoundManager Instance is null!");
        }
    }

    private void StartMerge(Watermelon otherWatermelon)
    {
        isMerging = true;
        otherWatermelon.isMerging = true;

        // 關掉碰撞避免亂彈
        Collider2D thisCollider = GetComponent<Collider2D>();
        Collider2D otherCollider = otherWatermelon.GetComponent<Collider2D>();

        if (thisCollider != null) thisCollider.enabled = false;
        if (otherCollider != null) otherCollider.enabled = false;

        // 合成音效
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayFX(1);
        }
        else
        {
            Debug.LogError("SoundManager Instance is null!");
        }

        Vector3 mergePosition = (transform.position + otherWatermelon.transform.position) / 2f;
        StartCoroutine(MergeWatermelons(otherWatermelon, mergePosition));
    }

    private IEnumerator MergeWatermelons(Watermelon otherWatermelon, Vector3 mergePosition)
    {
        int newLevel = level + 1;

        // 連鎖 combo：短時間內連續合成則累加，否則歸零（給「連鎖大師」加成用）
        if (Time.time - lastMergeTime <= 0.5f) CurrentCombo++;
        else CurrentCombo = 0;
        lastMergeTime = Time.time;
        TotalMerges++;

        // 合成順便打一下敵人
        if (BattleStageManager.instance != null && BattleStageManager.instance.currentEnemy != null)
        {
            BattleStageManager.instance.TakeDamage(newLevel - 1, BattleStageManager.instance.currentEnemy.transform.position);
        }

        // 閃一下特效
        if (FX.Instance != null)
        {
            FX.Instance.StartCoroutine("FlashFX");
        }
        else
        {
            Debug.LogError("FX Instance is null!");
        }

        // 合成爆裂特效：等級越高、連鎖越長 → 爆得越大越暖
        if (FXManager.instance != null)
        {
            float lvlT = Mathf.Clamp01((newLevel - 1) / 8f);
            // 暖橘→桃紅（避免奶白在奶白底上看不見），高等級更紅更大
            Color burstColor = Color.Lerp(new Color(1f, 0.66f, 0.32f), new Color(1f, 0.32f, 0.42f), lvlT);
            float burstScale = 0.8f + lvlT * 0.9f + Mathf.Min(CurrentCombo, 4) * 0.12f;
            FXManager.instance.Burst(mergePosition, burstColor, burstScale);
        }

        // 給音效 / 特效一點時間
        yield return new WaitForSeconds(0.1f);

        if (newLevel >= watermelonPrefabs.Length)
        {
            // 已經是最高等級，兩顆直接收掉
            Vector3 burstPos = mergePosition;
            DestroyImmediateObjects(otherWatermelon);
            DestroyImmediateObjects(this);

            // 「寵物大爆發」：撐到最高級也算高級合成 → 炸開周圍一圈並重擊頭目
            if (AbilityManager.instance != null && AbilityManager.instance.melonBurst)
            {
                MelonBurstAround(burstPos, null);
            }
        }
        else
        {
            DestroyImmediateObjects(otherWatermelon);
            DestroyImmediateObjects(this);

            GameObject nextPrefab = watermelonPrefabs[newLevel - 1];

            if (nextPrefab != null)
            {
                GameObject newWatermelon = Instantiate(nextPrefab, mergePosition, Quaternion.identity);
                Watermelon newWatermelonScript = newWatermelon.GetComponent<Watermelon>();
                newWatermelonScript.level = newLevel;
                newWatermelonScript.hasLanded = true;   // 合成出來的果直接視為已落地（生在堆疊中）

                Collider2D newCollider = newWatermelon.GetComponent<Collider2D>();
                if (newCollider != null) newCollider.enabled = true;

                // 「寵物大爆發」：合成出高級寵物（第 N 級以上）就觸發，炸開周圍（不含這隻新寵物）並重擊頭目
                if (AbilityManager.instance != null && AbilityManager.instance.melonBurst && newLevel >= MelonBurstLevel)
                {
                    MelonBurstAround(mergePosition, newWatermelon);
                }

                // 等一幀物理更新
                yield return new WaitForFixedUpdate();

                // 看附近能不能再連鎖合成
                newWatermelonScript.CheckForMerge();
            }
            else
            {
                Debug.LogError("Watermelon prefab is missing for level: " + newLevel);
            }
        }

     
    }

    private void DestroyImmediateObjects(Watermelon watermelon)
    {
        if (watermelon != null && watermelon.gameObject != null)
        {
            Destroy(watermelon.gameObject);
        }
    }

    // 炸彈果爆炸：清掉周圍一圈果，每清一顆對 Boss 造成傷害
    private void Explode()
    {
        if (exploded) return;
        exploded = true;
        isMerging = true;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayFX(1);

        float radius = 1.8f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        int cleared = 0;
        foreach (var h in hits)
        {
            if (h == null) continue;
            Watermelon w = h.GetComponent<Watermelon>();
            if (w != null && w != this)
            {
                cleared++;
                Destroy(w.gameObject);
            }
        }

        if (BattleStageManager.instance != null && BattleStageManager.instance.currentEnemy != null)
            BattleStageManager.instance.TakeRawDamage(20f + cleared * 10f, BattleStageManager.instance.currentEnemy.transform.position);

        Destroy(gameObject);
    }

    // 寵物大爆發：高級合成時炸開周圍一圈（exclude 之外），並對頭目追加傷害
    private void MelonBurstAround(Vector3 pos, GameObject exclude)
    {
        float radius = 2.0f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius);
        int cleared = 0;
        foreach (var h in hits)
        {
            if (h == null) continue;
            if (exclude != null && h.gameObject == exclude) continue; // 不炸掉剛合成出的高級寵物
            Watermelon w = h.GetComponent<Watermelon>();
            if (w != null && w.gameObject != exclude)
            {
                cleared++;
                Destroy(w.gameObject);
            }
        }

        // 追加傷害：基礎 ＋ 清掉數量（初版可調）
        if (BattleStageManager.instance != null && BattleStageManager.instance.currentEnemy != null)
            BattleStageManager.instance.TakeRawDamage(60f + cleared * 8f, BattleStageManager.instance.currentEnemy.transform.position);

        if (FX.Instance != null) FX.Instance.StartCoroutine("FlashFX");
        if (FXManager.instance != null)
        {
            FXManager.instance.Burst(pos, new Color(1f, 0.5f, 0.55f), 2.2f);
            FXManager.instance.Shake(0.5f, 0.35f);
        }
    }

    public void CheckForMerge()
    {
        if (isMerging)
            return;

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle == null)
            return;

        // 用「世界半徑」(含 lossyScale 縮放) 再加一點餘裕，確保剛好貼著的同級大果也偵測得到。
        // 原本只用 circle.radius(本地半徑)，若大果是靠放大 localScale 變大，偵測範圍會偏小而漏掉鄰居。
        float worldRadius = circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, worldRadius * 1.2f);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider == null || hitCollider.gameObject == gameObject)
                continue;

            Watermelon otherWatermelon = hitCollider.GetComponent<Watermelon>();
            if (otherWatermelon != null && !otherWatermelon.isMerging && otherWatermelon.level == level)
            {
                // 一樣用 InstanceID 擋雙重觸發
                if (this.GetInstanceID() < otherWatermelon.GetInstanceID())
                {
                    StartMerge(otherWatermelon);
                }
                break;
            }
        }
    }
}
