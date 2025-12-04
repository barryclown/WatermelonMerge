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
    public GameObject enemyPrefab;
    public GameObject[] eggPrefabs;
    public GameObject coinPrefab;
    public int currentBossIndex = 0;
    public GameObject currentEnemy;
    public int bossHitCount = 0;
    public int eggDropCount = 0;

    public GameObject[] eggHolders;

    public bool canSpawnItems = true;  // 允許生成物品的標誌

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
        BossManager.instance.InitializeBossCombinations();
        SetNextBossOrEnemy();
        totalDamage = 0;
        UIManager.instance.UpdateScoreUI(totalDamage.ToString());
        if (healthSlider != null)
            healthSlider.interactable = false;
    }

    public void TakeDamage(int level, Vector3 damagePosition)
    {
        float damageAmount = BossManager.instance.CalculateDamage(level);

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        totalDamage += damageAmount;
        UpdateHealthUI();
        UIManager.instance.UpdateScoreUI(totalDamage.ToString());

        DamageManager.instance.SpawnDamage(damageAmount, damagePosition);

        if (currentEnemy == bossPrefab)
        {
            bossHitCount++;
            if (bossHitCount >= 10)
            {
                Handheld.Vibrate();
                GameManager.Instance.SpawnSkillItem();
                bossHitCount = 0;
            }
        }

        if (currentHealth <= 0 && !isDeathHandling)
        {
            StartCoroutine(HandleEnemyDeath());
        }
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

        // 根據當前的敵人是 Boss 還是雜兵，設置 isBoss 標誌
        bool isBoss = currentEnemy == bossPrefab;

        // 調用 BossManager 的掉落邏輯
        GameObject dropItem = BossManager.instance.DetermineDropItem(
            ref eggDropCount,
            eggPrefabs,
            coinPrefab,
            isBoss
        );

        // 播放敵人死亡特效
        EnemyDeathEffect deathEffect = currentEnemy.GetComponent<EnemyDeathEffect>();
        if (deathEffect != null)
        {
            yield return StartCoroutine(deathEffect.PlayDeath(dropItem));
        }
        else
        {
            // 沒掛元件就簡單關掉
            currentEnemy.SetActive(false);
        }

        // 進入下一階段或結束遊戲
        if (currentBossIndex == 4)
        {
            UIManager.instance.End();
        }
        else
        {
            SetNextBossOrEnemy();
        }

        canSpawnItems = true;
        isDeathHandling = false;
    }
}
