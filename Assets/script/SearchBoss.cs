using System.Collections;
using UnityEngine;

public class SearchBoss : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSpeed = 50f;
    public float rotationRange = 10f;

    [Header("Spawn")]
    public GameObject pet;
    public float spawnDelay = 2f;
    public float moveDuration = 3f;
    public Vector3 spawnPosition = new Vector3(-10f, 0f, 0f);
    public Vector3 targetPosition = new Vector3(10f, 0f, 0f);

    public static SearchBoss instance;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void Anim()
    {
        StartCoroutine(RotateCoroutine());
        StartCoroutine(SpawnAndMoveObject());
    }

    private IEnumerator RotateCoroutine()
    {
        float elapsed = 0f;
        float duration = 5f;

        while (elapsed < duration)
        {
            float rotationZ = Mathf.PingPong(elapsed * rotationSpeed, rotationRange * 2) - rotationRange;
            if (pet != null)
                pet.transform.rotation = Quaternion.Euler(0, 0, rotationZ);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (pet != null)
            pet.transform.rotation = Quaternion.identity;
    }

    private IEnumerator SpawnAndMoveObject()
    {
        yield return new WaitForSecondsRealtime(spawnDelay);

        if (BattleStageManager.instance == null || BattleStageManager.instance.currentEnemy == null)
            yield break;

        GameObject newObject = Instantiate(BattleStageManager.instance.currentEnemy, spawnPosition, Quaternion.identity);

        SetSortingLayerRecursively(newObject, "CutScene", 5);

        yield return StartCoroutine(MoveObject(newObject, targetPosition, moveDuration));

        Destroy(newObject);
    }

    private IEnumerator MoveObject(GameObject obj, Vector3 target, float duration)
    {
        Vector3 startPos = obj.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            obj.transform.position = Vector3.Lerp(startPos, target, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        obj.transform.position = target;
    }

    private void SetSortingLayerRecursively(GameObject obj, string sortingLayerName, int sortingOrder)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
        }

        foreach (Transform child in obj.transform)
            SetSortingLayerRecursively(child.gameObject, sortingLayerName, sortingOrder);
    }
}
