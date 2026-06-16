using System.Collections.Generic;
using UnityEngine;

public class BossManager : MonoBehaviour
{
    public static BossManager instance;

    public List<Sprite> bossSprites;        // BOSS 圖
    public List<Sprite> enemySprites;       // 雜兵圖
    public SpriteRenderer bossSpriteRenderer;
    public SpriteRenderer enemySpriteRenderer;

    private Dictionary<string, float> bossHealth = new Dictionary<string, float>
    {
        {"Boss1", 400f},
        {"Boss2", 450f},
        {"Boss3", 550f},
        {"Boss4", 700f},
        {"Boss5", 900f}
    };

    private List<Sprite> selectedBosses = new List<Sprite>();
    private float enemyHealth = 100f;
    private int lastEnemyIndex = -1;

    public int eggCount = 0;   // 數字在這裡維護
    public int coinCount = 0;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // 一開始把目前數字丟給 UI（通常是 0）
        if (UIManager.instance != null)
        {
            UIManager.instance.UpdateEggUI(eggCount);
            UIManager.instance.UpdateCoinUI(coinCount);
        }
    }

    public void InitializeBossCombinations()
    {
        selectedBosses = new List<Sprite>(bossSprites);
        for (int i = selectedBosses.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Sprite temp = selectedBosses[i];
            selectedBosses[i] = selectedBosses[randomIndex];
            selectedBosses[randomIndex] = temp;
        }

        selectedBosses = selectedBosses.GetRange(0, Mathf.Min(5, selectedBosses.Count));
    }

    public float CalculateDamage(int level)
    {
        switch (level)
        {
            case 1: return 2f;
            case 2: return 6f;
            case 3: return 12f;
            case 4: return 20f;
            case 5: return 30f;
            case 6: return 42f;
            case 7: return 56f;
            case 8: return 72f;
            case 9: return 80f;
            case 10: return 100f;
            default: return 0f;
        }
    }

    // 掉落物邏輯：只算數字 + 回傳 Prefab，UI 交給 UIManager
    public GameObject DetermineDropItem(ref int eggDropCount, GameObject[] eggPrefabs, GameObject coinPrefab, bool isBoss)
    {
        bool shouldDropEgg = false;

        if (isBoss)
        {
            if (eggDropCount < 3)
            {
                switch (BattleStageManager.instance.currentBossIndex)
                {
                    case 0: shouldDropEgg = Random.value < 0.1f; break;
                    case 1: shouldDropEgg = Random.value < 0.2f; break;
                    case 2: shouldDropEgg = Random.value < 0.3f; break;
                    case 3: shouldDropEgg = Random.value < 0.4f; break;
                    case 4: shouldDropEgg = true; break;
                }
            }
        }
        else
        {
            shouldDropEgg = false;
        }

        if (shouldDropEgg)
        {
            eggDropCount++;
            eggCount++;

            // 通知 UI
            if (UIManager.instance != null)
                UIManager.instance.UpdateEggUI(eggCount);

            int eggIndex = Random.Range(0, eggPrefabs.Length);

            BattleStageManager.instance.eggHolders[eggCount - 1].SetActive(true);
            BattleStageManager.instance.eggHolders[eggCount - 1].GetComponent<SpriteRenderer>().sprite =
                BattleStageManager.instance.eggPrefabs[eggIndex].GetComponent<SpriteRenderer>().sprite;

            return eggPrefabs[eggIndex];
        }

        // 金幣
        coinCount++;
        if (UIManager.instance != null)
            UIManager.instance.UpdateCoinUI(coinCount);

        return coinPrefab;
    }

    public void SetNextBossOrEnemy(BattleStageManager healthManager)
    {
        float randomValue = Random.value;

        if (randomValue < 0.5f)
        {
            // 雜兵
            healthManager.bossPrefab.SetActive(false);
            int selectedEnemyIndex = (lastEnemyIndex == 0) ? 1 : 0;
            lastEnemyIndex = selectedEnemyIndex;

            healthManager.maxHealth = enemyHealth;
            healthManager.currentHealth = healthManager.maxHealth;

            enemySpriteRenderer.sprite = enemySprites[selectedEnemyIndex];
            healthManager.currentEnemy = healthManager.enemyPrefab;
            healthManager.currentEnemy.SetActive(true);
        }
        else
        {
            // Boss
            healthManager.enemyPrefab.SetActive(false);

            if (healthManager.currentBossIndex < selectedBosses.Count)
            {
                Sprite bossSprite = selectedBosses[healthManager.currentBossIndex];

                string key = "Boss" + (healthManager.currentBossIndex + 1);
                if (bossHealth.ContainsKey(key))
                    healthManager.maxHealth = bossHealth[key];

                healthManager.currentHealth = healthManager.maxHealth;
                healthManager.currentBossIndex++;

                bossSpriteRenderer.sprite = bossSprite;
                healthManager.currentEnemy = healthManager.bossPrefab;

                VideoManager.instance.PlayAndStopVideo();
                healthManager.currentEnemy.SetActive(true);
            }
        }
    }
}
