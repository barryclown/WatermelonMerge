using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageManager : MonoBehaviour
{
    //掛在場景傷害數字控制器

    public static DamageManager instance;
    public GameObject numberToSpawn;
    private List<GameObject> numberPool = new List<GameObject>();

    private void Awake()
    {
        instance = this;
    }


    public void SpawnDamage(float damageAmount, Vector3 location)
    {
        int roundedDamage = Mathf.RoundToInt(damageAmount);
        GameObject newDamage = GetFromPool();
        newDamage.SetActive(true);
        SetUp(newDamage, roundedDamage, location);
        var dn = newDamage.GetComponent<DamageNumber>();
        if (dn != null) dn.Configure(damageAmount, false);
    }

    // 閃避：顯示 "MISS"
    public void SpawnMiss(Vector3 location)
    {
        GameObject newDamage = GetFromPool();
        newDamage.SetActive(true);
        TMP_Text t = newDamage.GetComponent<TMP_Text>();
        if (t != null) t.text = "MISS";
        location.x += Random.Range(-0.5f, 0.2f);
        newDamage.transform.position = location;
        var dn = newDamage.GetComponent<DamageNumber>();
        if (dn != null) dn.Configure(0f, true);
    }

    public GameObject GetFromPool()
    {
        GameObject numberToOutput;
        if (numberPool.Count == 0)
        {
            numberToOutput = Instantiate(numberToSpawn);
           
            if (numberToOutput.GetComponent<DamageNumber>() == null)
            {
                numberToOutput.AddComponent<DamageNumber>();
            }
        }
        else
        {
            numberToOutput = numberPool[0];
            numberPool.RemoveAt(0);
        }
        return numberToOutput;
    }

    // 将伤害数字放回对象池
    public void PlaceIntoPool(GameObject numberToPool)
    {
        numberToPool.SetActive(false);
        numberPool.Add(numberToPool);
    }

    // 初始化伤害数字的属性
    public void SetUp(GameObject number, int damageDisplay, Vector3 location)
    {
        TMP_Text damageText = number.GetComponent<TMP_Text>();
        if (damageText != null)
        {
            damageText.text = damageDisplay.ToString();
        }

        // 为 X 轴添加随机偏移量
        location.x +=  Random.Range(-0.5f, 0.2f); 
        number.transform.position = location;
    }
}
