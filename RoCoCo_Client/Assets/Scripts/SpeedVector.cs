using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedVector : MonoBehaviour
{
    Rigidbody2D rb;
    const float ANGLE_BASE = 90.0f;
    float ratio = 1.0f;

    void Start()
    {
        rb = transform.root.gameObject.GetComponent<Rigidbody2D>();
    }

    public void SetRatio(float _ratio)
    {
        ratio = _ratio;
    }

    void FixedUpdate()
    {
        Vector2 vec = rb.velocity;
        float mag = vec.magnitude * ratio / (AppSettings.cameraSize * 0.5f);
        transform.localScale = new Vector3(mag, mag, 1.0f);
        float angle = AppUtil.GetDeg(vec) + ANGLE_BASE;
        transform.eulerAngles = Vector3.forward * angle;
    }
}
