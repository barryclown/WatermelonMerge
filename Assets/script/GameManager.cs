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

    [Header("Prediction Raycast")]
    public LayerMask collisionLayerMask;

    private void Update()
    {
        HandleSpawnAndDrag();
    }

    private void HandleSpawnAndDrag()
    {
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

        GameObject selectedFruit;

        do
        {
            float randomValue = Random.Range(0f, 100f);

            if (randomValue < 40f)
                selectedFruit = watermelonPrefabs[0];
            else if (randomValue < 70f)
                selectedFruit = watermelonPrefabs[1];
            else if (randomValue < 90f)
                selectedFruit = watermelonPrefabs[2];
            else
                selectedFruit = watermelonPrefabs[3];

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

        float fallTime = Mathf.Sqrt(2 * (startPosition.y - yMin) / -gravity.y);

        float predictedY = startPosition.y + 0.5f * gravity.y * Mathf.Pow(fallTime, 2);

        predictedItem.transform.position = new Vector2(startPosition.x, predictedY);

        RaycastHit2D[] hits = Physics2D.RaycastAll(startPosition, Vector2.down, Mathf.Infinity, collisionLayerMask);

        if (hits.Length > 1)
        {
            predictedItem.transform.position = hits[1].point;
        }
    }
}
