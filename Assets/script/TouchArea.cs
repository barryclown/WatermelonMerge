using UnityEngine;

public class TouchArea : MonoBehaviour
{
    public float xMin;
    public float xMax;
    public float yMin;
    public float yMax;

    public GameObject item1;
    public GameObject item2;
    public GameObject item3;
    public GameObject item4;
    public GameObject areaItem;

    private Rigidbody2D itemrb;
    private GameObject instantiatedItem;
    private GameObject predictedItem; // 預測物品
    public float timer = 0;
    private GameObject lastFruit;
    private GameObject secondLastFruit;
    public static TouchArea instance;

    public static bool isDragging = false;

    public LayerMask collisionLayerMask; // 用來設定檢測的層（例如：剛體的層）

    private void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (!BattleStageManager.instance.canSpawnItems) return;  // 如果不能生成物品，直接跳過

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        timer -= Time.deltaTime;

        // 當左鍵按下
        if (Input.GetMouseButtonDown(0))
        {
            if (IsWithinTargetRange(mousePosition) && timer < 0)
            {
                instantiatedItem = Instantiate(GetRandomFruit(), mousePosition, Quaternion.identity);
                itemrb = instantiatedItem.GetComponent<Rigidbody2D>();
                timer = 2.5f;

                // 顯示預測物品
                ShowPrediction(instantiatedItem);

                if (instantiatedItem != null)
                {
                    isDragging = true;
                }
            }
        }

        // 拖動物品
        if (Input.GetMouseButton(0) && instantiatedItem != null)
        {
            instantiatedItem.transform.position = mousePosition;
            try
            {
                if (predictedItem != null)
                {
                    // 計算預測的掉落位置並更新預測物品
                    UpdatePrediction(instantiatedItem.transform.position);
                }
            }
            catch
            {
                Debug.Log("No Rigidbody2D found on the instantiated item.");
            }
           
        }

        // 當超出範圍，停止拖動並移除預測物品
        if (Input.GetMouseButton(0) && instantiatedItem != null && !IsWithinTargetRange(mousePosition))
        {
            isDragging = false;
            if (itemrb != null)
            {
                itemrb.velocity = Vector2.zero;
                itemrb.angularVelocity = 0f;
            }
            itemrb = null;
            instantiatedItem = null;

            // 移除預測物品
            DestroyPrediction();
        }

        // 當左鍵釋放
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            if (itemrb != null)
            {
                itemrb.velocity = Vector2.zero;
                itemrb.angularVelocity = 0f;
            }
            itemrb = null;
            instantiatedItem = null;

            // 移除預測物品
            DestroyPrediction();
        }
    }

    GameObject GetRandomFruit()
    {
        GameObject selectedFruit;
        do
        {
            float randomValue = Random.Range(0f, 100f);

            if (randomValue < 40f)
            {
                selectedFruit = item1;
            }
            else if (randomValue < 70f)
            {
                selectedFruit = item2;
            }
            else if (randomValue < 90f)
            {
                selectedFruit = item3;
            }
            else
            {
                selectedFruit = item4;
            }

        } while (selectedFruit == lastFruit && selectedFruit == secondLastFruit);

        secondLastFruit = lastFruit;
        lastFruit = selectedFruit;

        return selectedFruit;
    }

    bool IsWithinTargetRange(Vector2 point)
    {
        return point.x >= xMin && point.x <= xMax && point.y >= yMin && point.y <= yMax;
    }

    public void InstantiateInTargetRange()
    {
        Vector2 randomPosition = new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));
        if (IsWithinTargetRange(randomPosition))
        {
            Instantiate(areaItem, randomPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Generated position is out of target range. Cannot instantiate item.");
        }
    }

    // 顯示預測物品
    void ShowPrediction(GameObject originalItem)
    {
        if (predictedItem != null)
        {
            Destroy(predictedItem);  // 如果已經有預測物品，先銷毀它
        }

        // 複製物品並設置透明度
        predictedItem = Instantiate(originalItem, originalItem.transform.position, originalItem.transform.rotation);
        SpriteRenderer sr = predictedItem.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color color = sr.color;
            color.a = 0.5f; // 設置透明度
            sr.color = color;
        }

        // 禁用剛體和碰撞體
        Rigidbody2D rb = predictedItem.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Collider2D col = predictedItem.GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false; // 禁用碰撞體
        }
    }

    // 銷毀預測物品
    void DestroyPrediction()
    {
        if (predictedItem != null)
        {
            Destroy(predictedItem);
            predictedItem = null;
        }
    }

    // 計算預測掉落位置並更新預測物品
    void UpdatePrediction(Vector2 startPosition)
    {
        if (predictedItem != null)
        {
            // 使用 Unity 默認的重力加速度
            Vector2 gravity = Physics2D.gravity;  // 默認為 (0, -9.81)

            // 計算從當前位置到最終位置的時間
            float fallTime = Mathf.Sqrt(2 * (startPosition.y - yMin) / -gravity.y);  // 使用 y 分量的重力加速度計算時間

            // 計算預測的 y 位置
            float predictedY = startPosition.y + 0.5f * gravity.y * Mathf.Pow(fallTime, 2);

            // 設置預測物品的 y 位置
            predictedItem.transform.position = new Vector2(startPosition.x, predictedY);

            // 使用 RaycastAll 來檢測所有的碰撞物
            RaycastHit2D[] hits = Physics2D.RaycastAll(startPosition, Vector2.down, Mathf.Infinity, collisionLayerMask);

            if (hits.Length > 1)
            {
                // 如果有多個碰撞物，選擇第二個碰撞物
                RaycastHit2D secondHit = hits[1]; // 第二個碰撞點
                if (secondHit.collider != null)
                {
                    // 如果找到了第二個碰撞物，更新預測物品的位置
                    predictedItem.transform.position = secondHit.point;
                   
                }
            }
        }
    }
}
