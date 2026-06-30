using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // === Singleton ===
    public static GameManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // === 生成區域設定 ===
    [Header("Spawn Area")]
    public float xMin;
    public float xMax;
    public float yMin;
    public float yMax;

    // === 水果 Prefabs (原 item1~item4，現在用陣列取代) ===
    [Header("Watermelon Prefabs")]
    public GameObject[] watermelonPrefabs; // 放 4 個水果 [0]-[3]

    // === 技能物品 (原 areaItem 改名 skillItem) ===
    [Header("Skill Item")]
    public GameObject skillItem;

    // === 拖曳 & 預測相關 ===
    private Rigidbody2D itemrb;
    private GameObject instantiatedItem;
    private GameObject predictedItem;

    [Header("Spawn Control")]
    public float timer = 0f;
    private GameObject lastFruit;
    private GameObject secondLastFruit;

    public static bool isDragging = false;

    private int dropCount = 0;  // 累計投放數（給「炸彈手」每第 5 顆用）
    public int DropCount => dropCount;  // 給新手引導偵測「已放下第一隻」
    public bool forceTutorialLv1 = false;  // 新手引導期間強制生成 Lv1，前兩顆必同款→必合成

    [Header("Prediction Raycast")]
    public LayerMask collisionLayerMask;

    private void Update()
    {
        HandleSpawnAndDrag();
    }

    private void HandleSpawnAndDrag()
    {
        // 防呆：BattleStageManager 尚未就緒（非遊戲場景／初始化順序）時直接跳過，避免 NullReference
        if (BattleStageManager.instance == null) return;
        if (!BattleStageManager.instance.canSpawnItems) return;

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        timer -= Time.deltaTime;

        // 按下滑鼠左鍵
        if (Input.GetMouseButtonDown(0))
        {
            if (IsWithinTargetRange(mousePosition) && timer < 0)
            {
                instantiatedItem = Instantiate(GetRandomFruit(), mousePosition, Quaternion.identity);
                itemrb = instantiatedItem.GetComponent<Rigidbody2D>();
                timer = 2.5f;

                dropCount++;
                // 「炸彈手」能力：每投放第 5 顆變成炸彈果（落地清周圍）
                if (AbilityManager.instance != null && AbilityManager.instance.bomber && dropCount % 5 == 0)
                {
                    Watermelon w = instantiatedItem.GetComponent<Watermelon>();
                    if (w != null) w.isBomb = true;
                    SpriteRenderer sr = instantiatedItem.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(0.4f, 0.4f, 0.45f, 1f); // 炸彈染深色
                }

                ShowPrediction(instantiatedItem);
                isDragging = true;
            }
        }

        // 拖曳中
        if (Input.GetMouseButton(0) && instantiatedItem != null)
        {
            instantiatedItem.transform.position = mousePosition;

            if (predictedItem != null)
                UpdatePrediction(instantiatedItem.transform.position);
        }

        // 超出區域
        if (Input.GetMouseButton(0) && instantiatedItem != null && !IsWithinTargetRange(mousePosition))
        {
            CancelDrag();
        }

        // 放開
        if (Input.GetMouseButtonUp(0))
        {
            CancelDrag();
        }
    }

    private void CancelDrag()
    {
        isDragging = false;

        if (itemrb != null)
        {
            itemrb.velocity = Vector2.zero;
            itemrb.angularVelocity = 0f;
        }

        itemrb = null;
        instantiatedItem = null;

        DestroyPrediction();
    }


    GameObject GetRandomFruit()
    {
        if (watermelonPrefabs.Length < 4)
        {
            Debug.LogError("watermelonPrefabs 必須至少有 4 個水果 prefab！");
            return null;
        }

        // 新手引導：強制 Lv1，確保前兩顆同款穩定合成
        if (forceTutorialLv1)
            return watermelonPrefabs[0];

        GameObject selectedFruit;

        // 「好運起手」能力：投放偏向高級果
        bool lucky = AbilityManager.instance != null && AbilityManager.instance.luckyStart;

        do
        {
            float randomValue = Random.Range(0f, 100f);

            if (!lucky)
            {
                if (randomValue < 40f) selectedFruit = watermelonPrefabs[0];
                else if (randomValue < 70f) selectedFruit = watermelonPrefabs[1];
                else if (randomValue < 90f) selectedFruit = watermelonPrefabs[2];
                else selectedFruit = watermelonPrefabs[3];
            }
            else
            {
                // 偏高級：25 / 30 / 30 / 15
                if (randomValue < 25f) selectedFruit = watermelonPrefabs[0];
                else if (randomValue < 55f) selectedFruit = watermelonPrefabs[1];
                else if (randomValue < 85f) selectedFruit = watermelonPrefabs[2];
                else selectedFruit = watermelonPrefabs[3];
            }

        } while (selectedFruit == lastFruit && selectedFruit == secondLastFruit);

        secondLastFruit = lastFruit;
        lastFruit = selectedFruit;

        return selectedFruit;
    }

    bool IsWithinTargetRange(Vector2 point)
    {
        return point.x >= xMin && point.x <= xMax &&
               point.y >= yMin && point.y <= yMax;
    }


    public void SpawnSkillItem()
    {
        Vector2 randomPosition = new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));

        if (IsWithinTargetRange(randomPosition))
            Instantiate(skillItem, randomPosition, Quaternion.identity);
        else
            Debug.LogWarning("位置超出區域，無法生成 SkillItem");
    }

    // === 預測顯示 ===
    void ShowPrediction(GameObject originalItem)
    {
        DestroyPrediction();

        predictedItem = Instantiate(originalItem, originalItem.transform.position, originalItem.transform.rotation);

        SpriteRenderer sr = predictedItem.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.5f;
            sr.color = c;
        }

        Rigidbody2D rb = predictedItem.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.isKinematic = true;

        Collider2D col = predictedItem.GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    void DestroyPrediction()
    {
        if (predictedItem != null)
        {
            Destroy(predictedItem);
            predictedItem = null;
        }
    }

    void UpdatePrediction(Vector2 startPosition)
    {
        if (predictedItem == null) return;

        Vector2 gravity = Physics2D.gravity;

        // 守衛：球被拖到 yMin 以下時 (startPosition.y - yMin) 為負，Sqrt 會變 NaN → 夾住避免崩潰
        float dy = Mathf.Max(0f, startPosition.y - yMin);
        float fallTime = Mathf.Sqrt(2f * dy / -gravity.y);

        float predictedY = startPosition.y + 0.5f * gravity.y * fallTime * fallTime;
        if (float.IsNaN(predictedY) || float.IsInfinity(predictedY)) predictedY = yMin;

        predictedItem.transform.position = new Vector2(startPosition.x, predictedY);

        RaycastHit2D[] hits = Physics2D.RaycastAll(startPosition, Vector2.down, Mathf.Infinity, collisionLayerMask);

        if (hits.Length > 1)
        {
            predictedItem.transform.position = hits[1].point;
        }
    }
}
