using System.Collections;
using UnityEngine;

public class Watermelon : MonoBehaviour
{
    [Header("等級（1 ~ N）")]
    public int level = 1;

    [Header("各等級西瓜 Prefab（從 GameManager 帶進來）")]
    public GameObject[] watermelonPrefabs;

   
    [HideInInspector] public bool isMerging = false;

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
       
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 遊戲結束就不處理
        if (BattleStageManager.instance != null && BattleStageManager.instance.currentHealth <= 0)
            return;

        if (isMerging)
            return;

        GameObject otherObj = collision.gameObject;

        // 撞牆（Tag=Wall）就播一下音效
        if (otherObj.CompareTag("Wall"))
        {
            PlayHitSound();
            return;
        }

        // 看有沒有撞到別顆西瓜
        Watermelon otherWatermelon = otherObj.GetComponent<Watermelon>();

        // 撞到不是西瓜就當一般碰撞
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

        // 給音效 / 特效一點時間
        yield return new WaitForSeconds(0.1f);

        if (newLevel >= watermelonPrefabs.Length)
        {
            // 已經是最高等級，兩顆直接收掉
            DestroyImmediateObjects(otherWatermelon);
            DestroyImmediateObjects(this);
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

                Collider2D newCollider = newWatermelon.GetComponent<Collider2D>();
                if (newCollider != null) newCollider.enabled = true;

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

    public void CheckForMerge()
    {
        if (isMerging)
            return;

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle == null)
            return;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, circle.radius);

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
