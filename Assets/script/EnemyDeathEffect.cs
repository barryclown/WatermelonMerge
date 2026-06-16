using System.Collections;
using UnityEngine;

public class EnemyDeathEffect : MonoBehaviour
{
    [Header("死亡特效參數")]
    public float fadeDuration = 1.0f;
    public float jumpHeight = 1f;
    public float jumpDuration = 0.5f;
    public float stayDuration = 1.0f;
    public float dropFadeDuration = 1.0f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

   
    public IEnumerator PlayDeath(GameObject dropPrefab)
    {
        // 1. 自己淡出
        if (spriteRenderer != null)
        {
            Color originColor = spriteRenderer.color;
            float time = 0f;

            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / fadeDuration);
                spriteRenderer.color = new Color(originColor.r, originColor.g, originColor.b, 1f - t);
                yield return null;
            }
        }

        // 2. 掉落物動畫
        if (dropPrefab != null)
        {
            GameObject droppedObject = Instantiate(dropPrefab, transform.position, Quaternion.identity);
            SpriteRenderer droppedSpriteRenderer = droppedObject.GetComponent<SpriteRenderer>();

            // 跳起
            Vector3 startPos = droppedObject.transform.position;
            Vector3 targetPos = startPos + Vector3.up * jumpHeight;
            float elapsed = 0f;

            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / jumpDuration);
                droppedObject.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            // 掉下
            elapsed = 0f;
            startPos = droppedObject.transform.position;
            targetPos = startPos - Vector3.up * jumpHeight;

            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / jumpDuration);
                droppedObject.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            // 停留
            yield return new WaitForSeconds(stayDuration);

            // 掉落物淡出
            if (droppedSpriteRenderer != null)
            {
                Color dropOriginColor = droppedSpriteRenderer.color;
                float time = 0f;

                while (time < dropFadeDuration)
                {
                    time += Time.deltaTime;
                    float t = Mathf.Clamp01(time / dropFadeDuration);
                    droppedSpriteRenderer.color = new Color(
                        dropOriginColor.r,
                        dropOriginColor.g,
                        dropOriginColor.b,
                        1f - t
                    );
                    yield return null;
                }
            }

            Destroy(droppedObject);
        }

        // 3. 還原顏色，關掉物件，等下一輪被啟用
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, 1f);
        }

        gameObject.SetActive(false);
    }
}
