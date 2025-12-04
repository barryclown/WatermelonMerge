using UnityEngine;

public class RotateAndScale : MonoBehaviour
{
    public float rotationSpeed = 50f;  // 臂锣硉
    public float scaleSpeed = 0.5f;    // 罽硉
    public float minScale = 0.5f;      // 程罽ゑㄒ
    public float maxScale = 1.5f;      // 程罽ゑㄒ

    private bool isScalingUp = true;   // 北罽よ

    void Update()
    {
        //  Z 禸臂锣
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

        // 罽砰
        if (isScalingUp)
        {
            transform.localScale += Vector3.one * scaleSpeed * Time.deltaTime;
            if (transform.localScale.x >= maxScale)  // 笷程罽秨﹍罽
                isScalingUp = false;
        }
        else
        {
            transform.localScale -= Vector3.one * scaleSpeed * Time.deltaTime;
            if (transform.localScale.x <= minScale)  // 笷程罽秨﹍
                isScalingUp = true;
        }
    }
}
