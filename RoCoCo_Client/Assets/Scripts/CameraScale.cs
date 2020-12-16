using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScale : MonoBehaviour
{
    public Vector3 scale = new Vector3(1.0f, 1.0f, 1.0f);
    Camera cameraObj;

    void Start()
    {
        cameraObj = GetComponent<Camera>();
    }

    void OnPreCull()
    {
        cameraObj.ResetWorldToCameraMatrix();
        cameraObj.ResetProjectionMatrix();
        cameraObj.projectionMatrix = cameraObj.projectionMatrix * Matrix4x4.Scale(scale);
    }

    void OnPreRender()
    {
        //if (scale.x * scale.y * scale.z < 0)
        {
            GL.invertCulling = true;
        }
    }

    void OnPostRender()
    {
        GL.invertCulling = false;
    }

}
