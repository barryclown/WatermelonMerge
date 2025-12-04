using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    public float lifeTime = 1f; 
    private float lifeCounter;

    private void OnEnable()
    {
      
        lifeCounter = lifeTime;
    }

    private void Update()
    {
       
        lifeCounter -= Time.deltaTime;

       
        transform.position += Vector3.up * Time.deltaTime;

        if (lifeCounter <= 0)
        {
          
            DamageManager.instance.PlaceIntoPool(gameObject);
        }
    }
}
