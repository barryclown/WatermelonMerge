using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashCounter : MonoBehaviour
{
    public int count = 0;
}
public class FX : MonoBehaviour
{
    public static FX Instance;
    public Material ouo;
    public GameObject fx;

    private Queue<GameObject> flashOverlayPool = new Queue<GameObject>();  // 存放可重用的 flashOverlay

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator FlashFX()
    {
        BattleStageManager hm = BattleStageManager.instance;
        if (hm == null || hm.currentEnemy == null)
        {
            Debug.LogError("HealthManager or currentEnemy is null.");
            yield break;
        }

     
        GameObject enemy = hm.currentEnemy;

   
        GameObject flashOverlay;
        if (flashOverlayPool.Count > 0)
        {
            flashOverlay = flashOverlayPool.Dequeue();
            flashOverlay.SetActive(true); 
        }
        else
        {
            
            flashOverlay = new GameObject("FlashOverlay");
            flashOverlay.transform.SetParent(enemy.transform);
            flashOverlay.transform.localPosition = Vector3.zero;
            flashOverlay.transform.localRotation = Quaternion.identity;
            flashOverlay.transform.localScale = Vector3.one;
        }

   
        SpriteRenderer enemySR = enemy.GetComponent<SpriteRenderer>();
        if (enemySR == null)
        {
            Debug.LogError("Enemy SpriteRenderer not found.");
            yield break;
        }

        SpriteRenderer flashSR = flashOverlay.GetComponent<SpriteRenderer>();
        if (flashSR == null)
        {
            flashSR = flashOverlay.AddComponent<SpriteRenderer>();  
        }
        flashSR.sprite = enemySR.sprite;
        flashSR.material = ouo;
        flashSR.sortingLayerID = enemySR.sortingLayerID;
        flashSR.sortingOrder = enemySR.sortingOrder + 1; // 确保渲染在敌人之上
        flashSR.flipX = enemySR.flipX; // 如果有翻转，需要同步
        flashSR.flipY = enemySR.flipY;

        // 使用 FlashCounter 跟踪闪烁次数
        FlashCounter counter = flashOverlay.GetComponent<FlashCounter>();
        if (counter == null)
        {
            counter = flashOverlay.AddComponent<FlashCounter>();
        }
        counter.count = 1; // 设置初始计数为 1，表示闪烁正在进行

        // 激活闪烁叠加层
        fx.SetActive(true);

        // 等待闪烁持续时间
        yield return new WaitForSeconds(0.2f);

        // 减少闪烁计数
        counter.count--;

        // 如果没有闪烁在进行，禁用叠加层
        if (counter.count <= 0)
        {
            flashOverlay.SetActive(false);
            fx.SetActive(false);

            // 将 flashOverlay 放回对象池
            flashOverlayPool.Enqueue(flashOverlay);
        }
    }
}

