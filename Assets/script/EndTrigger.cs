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

    // 已經觸發過爆頂結束，避免每幀重複呼叫
    private bool ended = false;

    public static EndTrigger instance;

    [Header("警告圖示設定")]
    public GameObject warningImagePrefab;   // 顯示在物件上方的警告圖
    public float warningScaleFactor = 1.5f; // 警告相對於物件的放大倍率

    // 爆頂線位移：能力「加高天花板」上移、Boss4「壓頂」暫時下降
    private float baselineY;
    private float ceilingRaise = 0f;
    private float tempLower = 0f;

    private void Awake()
    {
        instance = this;
        baselineY = transform.position.y;
    }

    // 能力：把爆頂線整體上移 raise（累計值，由 AbilityManager 算好傳入）
    public void SetCeilingRaise(float raise)
    {
        ceilingRaise = raise;
        Reposition();
    }

    // Boss4：暫時把爆頂線往下壓 amount（更危險）
    public void SetTemporaryLower(float amount)
    {
        tempLower = amount;
        Reposition();
    }

    public void ClearTemporaryLower()
    {
        tempLower = 0f;
        Reposition();
    }

    private void Reposition()
    {
        Vector3 p = transform.position;
        p.y = baselineY + ceilingRaise - tempLower;
        transform.position = p;
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

        // 超過門檻秒數 = 爆頂出局（不再無痛續玩，直接判敗）。門檻吃「沉著」能力加成。
        float overflowSeconds = (AbilityManager.instance != null) ? AbilityManager.instance.OverflowSeconds : 5f;
        if (stayDuration > overflowSeconds && !ended)
        {
            ended = true;

            if (!hasEnd.Contains(obj))
                hasEnd.Add(obj);

            UIManager.instance.Lose();
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
