using UnityEngine;

public class RotateAndScale : MonoBehaviour
{
    public float rotationSpeed = 50f;  // 旋轉速度
    public float scaleSpeed = 0.5f;    // 縮放速度
    public float minScale = 0.5f;      // 最小縮放比例
    public float maxScale = 1.5f;      // 最大縮放比例

    private bool isScalingUp = true;   // 控制縮放方向

    void Update()
    {
        // 只在 Z 軸上旋轉
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

        // 縮放物體
        if (isScalingUp)
        {
            transform.localScale += Vector3.one * scaleSpeed * Time.deltaTime;
            if (transform.localScale.x >= maxScale)  // 到達最大縮放時，開始縮小
                isScalingUp = false;
        }
        else
        {
            transform.localScale -= Vector3.one * scaleSpeed * Time.deltaTime;
            if (transform.localScale.x <= minScale)  // 到達最小縮放時，開始放大
                isScalingUp = true;
        }
    }
}
