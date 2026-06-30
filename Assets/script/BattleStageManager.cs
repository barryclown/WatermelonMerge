using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattleStageManager : MonoBehaviour
{
    public static BattleStageManager instance;
    public Slider healthSlider;
    public float maxHealth = 100f;
    public float currentHealth;

    private float totalDamage;
    public bool isDeathHandling;
    public GameObject bossPrefab;
    public GameObject enemyPrefab;   // V2 已不用雜兵，保留欄位避免斷場景引用
    public int currentBossIndex = 0; // 0~4，當前這隻 Boss
    public GameObject currentEnemy;

    public const int TotalBosses = 6;

    public bool canSpawnItems = true;  // 允許生成物品的標誌

    public float lastHitTime = -10f;   // 最後一次打到 Boss 的時間（給 Boss5 回血判定）

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 新 run：重置能力＋合成計數（給新手引導用）
        if (AbilityManager.instance != null)
            AbilityManager.instance.ResetRun();
        Watermelon.TotalMerges = 0;
        Watermelon.CurrentCombo = 0;

        BossManager.instance.InitializeBossCombinations();
        currentBossIndex = 0;
        SetNextBossOrEnemy();

        totalDamage = 0;
        UIManager.instance.UpdateScoreUI(totalDamage.ToString());
        UIManager.instance.UpdateBossProgress(currentBossIndex + 1, TotalBosses);

        if (healthSlider != null)
            healthSlider.interactable = false;
    }

    // 合成傷害：吃「重擊」倍率與「連鎖大師」combo 加成
    public void TakeDamage(int level, Vector3 damagePosition)
    {
        float damageAmount = BossManager.instance.CalculateDamage(level);

        if (AbilityManager.instance != null)
        {
            damageAmount *= AbilityManager.instance.DamageMultiplier;
            damageAmount *= (1f + AbilityManager.instance.ChainBonusPerStep * Watermelon.CurrentCombo);
        }

        ApplyDamage(damageAmount, damagePosition);
    }

    // 純數值傷害（炸彈、寵物大爆發用）
    public void TakeRawDamage(float amount, Vector3 damagePosition)
    {
        ApplyDamage(amount, damagePosition);
    }

    private void ApplyDamage(float damageAmount, Vector3 damagePosition)
    {
        // Boss5 起：10% 機率閃避，這次傷害 MISS（看得到、公平的壓力，取代壓頂線）
        if (currentBossIndex >= 4 && Random.value < 0.1f)
        {
            if (DamageManager.instance != null) DamageManager.instance.SpawnMiss(damagePosition);
            return;
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        totalDamage += damageAmount;
        lastHitTime = Time.time;

        UpdateHealthUI();
        UIManager.instance.UpdateScoreUI(Mathf.RoundToInt(totalDamage).ToString());

        if (DamageManager.instance != null)
            DamageManager.instance.SpawnDamage(damageAmount, damagePosition);

        // Boss 受擊反應：squash 後座＋閃白
        if (currentEnemy != null)
        {
            var juice = currentEnemy.GetComponent<BossJuice>();
            if (juice != null) juice.Hit();
        }

        // 重擊震屏：傷害越大震越兇（純爽感回饋）
        if (FXManager.instance != null)
        {
            if (damageAmount >= 100f) FXManager.instance.Shake(0.45f, 0.30f);
            else if (damageAmount >= 50f) FXManager.instance.Shake(0.22f, 0.20f);
        }

        // Boss5 二階段：血量快見底時一次性回到 50%，並進入狂暴（擋在死亡判定前，大招也打不死它一次）
        if (currentBossIndex >= TotalBosses - 1 && BossMechanic.instance != null
            && !BossMechanic.instance.boss5SecondWindUsed
            && currentHealth <= maxHealth * 0.15f)
        {
            BossMechanic.instance.boss5SecondWindUsed = true;
            BossMechanic.instance.boss5Enraged = true;
            currentHealth = maxHealth * 0.5f;
            UpdateHealthUI();
            if (SoundManager.Instance != null) SoundManager.Instance.PlayFX(1);
        }

        if (currentHealth <= 0 && !isDeathHandling)
        {
            StartCoroutine(HandleEnemyDeath());
        }
    }

    // Boss5 回血
    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHealthUI();
    }

    private void SetNextBossOrEnemy()
    {
        BossManager.instance.SetNextBossOrEnemy(this);
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
    }

    private IEnumerator HandleEnemyDeath()
    {
        isDeathHandling = true;
        canSpawnItems = false;

        // 停掉這隻 Boss 的干擾機制
        BossMechanic.instance.StopMechanic();

        // 播放敵人死亡特效（V2 已無掉落物，傳 null）
        EnemyDeathEffect deathEffect = currentEnemy != null ? currentEnemy.GetComponent<EnemyDeathEffect>() : null;
        if (deathEffect != null)
            yield return StartCoroutine(deathEffect.PlayDeath(null));
        else if (currentEnemy != null)
            currentEnemy.SetActive(false);

        // 防螺旋：清掉 Boss 丟的垃圾果，保留玩家自己養的果
        BossMechanic.instance.ClearJunk();

        // 通關判定：打死最後一隻 Boss = 勝
        if (currentBossIndex >= TotalBosses - 1)
        {
            UIManager.instance.Win();
            yield break;
        }

        // 關間：三選一抽能力（暫停遊戲，等玩家選）
        yield return StartCoroutine(DraftPhase());

        // 進入下一關
        currentBossIndex++;
        SetNextBossOrEnemy();
        UIManager.instance.UpdateBossProgress(currentBossIndex + 1, TotalBosses);

        // 新手教學：新 Boss 出場前彈出機制預告卡（Boss1 沒有，這裡 index 已 >=1）
        yield return StartCoroutine(BossIntroPhase());

        canSpawnItems = true;
        isDeathHandling = false;
    }

    // 機制介紹「玩中學」：不再凍結＋丟整段文字。改成 Boss 短台詞泡（≤一句）＋當場
    // 視覺演示一次該機制，讓玩家「看一次就懂」。全程 real-time，演示時暫不開放投放。
    private IEnumerator BossIntroPhase()
    {
        string quip = BossBubbleText(currentBossIndex);
        if (string.IsNullOrEmpty(quip)) yield break;

        ShellUI.instance.ShowBossBubble(quip, 2.0f);

        // 會「丟玩具」的 Boss 當場演示一次（含既有落點預告），玩家看一次就懂
        yield return new WaitForSeconds(0.7f);
        DemoMechanic(currentBossIndex);

        // 留時間看演示，再交還控制權（canSpawnItems 在本 phase 結束後才開）
        yield return new WaitForSeconds(1.9f);
    }

    // 當場演示這隻 Boss 的新機制（只演「看得出來」的；晃盤/閃避/狂暴靠實戰各自視覺回饋學）
    private void DemoMechanic(int index)
    {
        if (BossMechanic.instance == null) return;
        switch (index)
        {
            case 1: BossMechanic.instance.ThrowJunk(1, 1); break;   // 丟玩具
            case 3: BossMechanic.instance.ThrowJunk(2, 2); break;   // 一大波玩具
        }
    }

    // 每隻 Boss 出場的短台詞（in-world 氣泡、可愛口吻；index 0 無）
    private string BossBubbleText(int index)
    {
        switch (index)
        {
            case 1: return "玩具大放送～！";
            case 2: return "搖搖～站得穩嗎？";
            case 3: return "一大波玩具來囉！";
            case 4: return "想打中我？沒那麼簡單～";
            case 5: return "我要全力爆發啦！";
            default: return null;
        }
    }

    private IEnumerator DraftPhase()
    {
        bool picked = false;
        var options = AbilityManager.instance.OfferThree();
        string subtitle = "第 " + (currentBossIndex + 1) + " 關通過！選一個能力帶進下一關";

        Time.timeScale = 0f;
        DraftUI.instance.Show(options, subtitle, id =>
        {
            AbilityManager.instance.Apply(id);
            picked = true;
        });

        // Update 在 timeScale 0 仍會跑，UI 仍可點
        yield return new WaitUntil(() => picked);

        Time.timeScale = 1f;
    }
}
