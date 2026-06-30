using System.Collections.Generic;
using UnityEngine;

public class BossManager : MonoBehaviour
{
    public static BossManager instance;

    public List<Sprite> bossSprites;        // BOSS 圖（順序＝Boss1~5，對應 BossMechanic 機制）
    public List<Sprite> enemySprites;       // 雜兵圖（V2 已不用雜兵，保留欄位避免斷引用）
    public SpriteRenderer bossSpriteRenderer;
    public SpriteRenderer enemySpriteRenderer;

    // 履歷展示版正式血量：6 隻 Boss 約 15~20 分鐘完整流程。
    private float[] bossHealth = { 300f, 450f, 600f, 800f, 1050f, 1400f };

    // 各 Boss 語音（前 5 隻是貓，第 6 隻是倉鼠）
    private static readonly string[] CatVoices = { "喵～", "喵喵～", "呼嚕嚕～", "喵嗚～", "喵？" };
    private static readonly string[] HamsterVoices = { "啾啾～", "吱吱～", "啾？" };

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
    }

    // V2：保留方法名給 BattleStageManager 呼叫；不再洗牌，Boss 依序對應機制
    public void InitializeBossCombinations()
    {
        // 確保 AbilityManager / BossMechanic 單例在場（自舉）
        var _ = AbilityManager.instance;
        var __ = BossMechanic.instance;
    }

    public float CalculateDamage(int level)
    {
        switch (level)
        {
            case 1: return 2f;
            case 2: return 6f;
            case 3: return 12f;
            case 4: return 20f;
            case 5: return 32f;
            case 6: return 48f;
            case 7: return 66f;
            case 8: return 90f;
            case 9: return 120f;
            case 10: return 150f;
            default: return 0f;
        }
    }

    // V2：依 healthManager.currentBossIndex 生成「當前這隻」Boss（不在這裡自增 index）
    public void SetNextBossOrEnemy(BattleStageManager healthManager)
    {
        int index = healthManager.currentBossIndex;
        if (index < 0 || index >= bossHealth.Length) return;

        // 不再使用雜兵
        if (healthManager.enemyPrefab != null)
            healthManager.enemyPrefab.SetActive(false);

        healthManager.maxHealth = bossHealth[index];
        healthManager.currentHealth = healthManager.maxHealth;

        if (bossSpriteRenderer != null && bossSprites != null && index < bossSprites.Count)
            bossSpriteRenderer.sprite = bossSprites[index];

        healthManager.currentEnemy = healthManager.bossPrefab;

        // Boss 登場影片已移除，保留簡短關卡開始卡片，避免進關前拖長不可操作時間。

        if (healthManager.bossPrefab != null)
        {
            healthManager.bossPrefab.SetActive(true);
            // 掛上 idle 呼吸＋受擊動畫＋地面影＋語音（自動抓 SpriteRenderer，重啟用會刷新基準）
            var juice = healthManager.bossPrefab.GetComponent<BossJuice>();
            if (juice == null) juice = healthManager.bossPrefab.AddComponent<BossJuice>();
            juice.voices = (index >= 5) ? HamsterVoices : CatVoices;
        }

        // 啟動這隻 Boss 的盤面干擾機制
        BossMechanic.instance.StartForBoss(index);
    }
}
