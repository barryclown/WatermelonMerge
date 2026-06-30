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

    // 「空氣牆」根治：兩顆同級玩具靜止後，就算 collider 差一點點沒接觸、或物理睡眠/合成中漏掉接觸事件，
    // 也用持續的鄰近偵測補合成。判定距離＝兩顆世界半徑和 ×此容差（>1 = 貼著或差一點點縫也合，可調手感）。
    private const float MergeGapTolerance = 1.12f;
    private float nextProximityScan = 0f;

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

        ApplyVisualSnug();
    }

    // 視覺貼合：玩具圖有透明留白、碰撞器是規則圓/膠囊，靜止相切時兩張圖之間會露一條留白縫（空氣牆觀感）。
    // 把整體放大 VisualSnugScale，同時把碰撞器尺寸同步縮小 → 世界碰撞大小完全不變
    //（物理堆疊／爆頂平衡不動、合成判定距離不變），純粹讓圖略放大蓋掉留白縫。所有等級所有玩具一致生效。
    private const float VisualSnugScale = 1.18f;
    private bool snugApplied = false;
    private void ApplyVisualSnug()
    {
        if (snugApplied) return;
        snugApplied = true;
        transform.localScale *= VisualSnugScale;
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            if (col is CircleCollider2D cc) cc.radius /= VisualSnugScale;
            else if (col is CapsuleCollider2D cap) cap.size /= VisualSnugScale;
            else if (col is BoxCollider2D box) box.size /= VisualSnugScale;
        }
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

    // 持續鄰近合成偵測：不依賴物理接觸事件，落地後每 0.1s 掃一次鄰居，
    // 只要同級且距離 ≤ 兩半徑和 ×容差 就合 → 根治「貼著卻不合」的空氣牆。
    private void FixedUpdate()
    {
        if (isMerging || isBomb || !hasLanded) return;
        if (Time.time < nextProximityScan) return;
        nextProximityScan = Time.time + 0.1f;
        TryProximityMerge();
    }

    // 世界座標下的圓形碰撞半徑（含 lossyScale 縮放）
    private float WorldColliderRadius()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return 0f;
        Vector2 e = c.bounds.extents;   // 世界座標 AABB；圓/膠囊/方形都通用
        return Mathf.Min(e.x, e.y);
    }

    private void TryProximityMerge()
    {
        float myR = WorldColliderRadius();
        if (myR <= 0f) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, myR * 2.5f);
        foreach (var h in hits)
        {
            if (h == null || h.gameObject == gameObject) continue;
            Watermelon o = h.GetComponent<Watermelon>();
            if (o == null || o.isMerging || o.isBomb || o.level != level) continue;

            float oR = o.WorldColliderRadius();
            if (oR <= 0f) continue;

            float dist = Vector2.Distance(transform.position, o.transform.position);
            if (dist <= (myR + oR) * MergeGapTolerance)
            {
                if (GetInstanceID() < o.GetInstanceID()) StartMerge(o);
                else o.StartMerge(this);
                return;
            }
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

    // 炸彈每清掉一顆「碰到的」玩具造成的傷害（可調）
    private const float BombDamagePerToy = 12f;

    // 炸彈果爆炸：不用魔術半徑，改成清掉「正貼著炸彈」的每一顆玩具，每清一顆對 Boss 造成傷害。
    private void Explode()
    {
        if (exploded) return;
        exploded = true;
        isMerging = true;

        if (SoundManager.Instance != null) SoundManager.Instance.PlayFX(1);

        // 接觸範圍＝炸彈自己的碰撞半徑 ×一點點餘裕（不是魔術數字）→ 只清正貼著它的玩具。
        float reach = WorldColliderRadius() * 1.1f;
        int cleared = 0;
        if (reach > 0f)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, reach);
            var seen = new System.Collections.Generic.HashSet<Watermelon>();
            foreach (var h in hits)
            {
                if (h == null) continue;
                Watermelon w = h.GetComponent<Watermelon>();
                if (w != null && w != this && !w.isBomb && seen.Add(w))
                {
                    cleared++;
                    Destroy(w.gameObject);
                }
            }
        }

        // 碰到的每一顆都貢獻傷害；完全沒碰到就只是消掉自己、不造成傷害
        if (cleared > 0 && BattleStageManager.instance != null && BattleStageManager.instance.currentEnemy != null)
            BattleStageManager.instance.TakeRawDamage(cleared * BombDamagePerToy, BattleStageManager.instance.currentEnemy.transform.position);

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

    // 合成後檢查能不能再連鎖：直接走精準的鄰近偵測（與 FixedUpdate 同一套，圓/膠囊通用）
    public void CheckForMerge()
    {
        if (isMerging)
            return;
        TryProximityMerge();
    }
}
