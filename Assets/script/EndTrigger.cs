using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndTrigger : MonoBehaviour
{
    // key：被偵測到的物件，value：進入區域的時間
    private Dictionary<GameObject, float> objectStayTime = new Dictionary<GameObject, float>();

    // key：被偵測到的物件，value：該物件上方的警告圖示
    private Dictionary<GameObject, GameObject> warningMap = new Dictionary<GameObject, GameObject>();

    // 已經被判定 End 的物件（給外面查用）
    public List<GameObject> hasEnd = new List<GameObject>();

    public static EndTrigger instance;

    [Header("警告圖示設定")]
    public GameObject warningImagePrefab;   // 顯示在物件上方的警告圖
    public float warningScaleFactor = 1.5f; // 警告相對於物件的放大倍率

    private void Awake()
    {
        instance = this;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject obj = other.gameObject;

        if (!objectStayTime.ContainsKey(obj))
            objectStayTime[obj] = Time.time;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        GameObject obj = other.gameObject;

        if (!objectStayTime.ContainsKey(obj))
            return;

        float stayDuration = Time.time - objectStayTime[obj];

        // 超過 0.5 秒開始顯示警告（但不重複生成）
        if (stayDuration > 0.5f)
            ShowWarningEffect(obj);

        // 超過 5 秒進入結束流程
        if (stayDuration > 5f)
        {
            if (!hasEnd.Contains(obj))
                hasEnd.Add(obj);

            UIManager.instance.Ask();
        }
    }

    private void ShowWarningEffect(GameObject obj)
    {
        if (warningImagePrefab == null)
            return;

        // 已經有警告圖，就只更新位置/大小
        if (warningMap.TryGetValue(obj, out GameObject warningImage) && warningImage != null)
        {
            UpdateWarningTransform(obj, warningImage);
            return;
        }

        // 第一次進來，建立一個新的警告圖示並記錄
        warningImage = Instantiate(warningImagePrefab, obj.transform);
        warningImage.SetActive(true);

        UpdateWarningTransform(obj, warningImage);

        warningMap[obj] = warningImage;
    }

    private void UpdateWarningTransform(GameObject obj, GameObject warningImage)
    {
        // 貼在物件正上方一點點
        warningImage.transform.position = obj.transform.position + Vector3.up * 0.1f;

        // 跟著物件大小一起放大
        float scaleFactor = obj.transform.localScale.x * warningScaleFactor;
        warningImage.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GameObject obj = other.gameObject;

        if (objectStayTime.ContainsKey(obj))
            objectStayTime.Remove(obj);

        // 出區域就把警告圖示也移除
        if (warningMap.TryGetValue(obj, out GameObject warningImage))
        {
            if (warningImage != null)
                Destroy(warningImage);

            warningMap.Remove(obj);
        }
    }
}
