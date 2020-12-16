using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : MonoBehaviour
{
    LineRenderer line;

    public Color startColor = Color.blue;
    public Color endColor = Color.blue;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        //line.SetColors(Color.blue, Color.green);
        {
            var colorKeys = new[]
            {
                new GradientColorKey( startColor, 0 ),
                new GradientColorKey( endColor, 1 ),
            };
            var alphaKeys = new[]
            {
                new GradientAlphaKey( 0.5f, 0 ),
                new GradientAlphaKey( 0.5f, 0 ),
            };
            var gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);
            line.colorGradient = gradient;
        }
    }

    public void SetEnabled(bool enabled)
    {
        line.enabled = enabled;

        line.startWidth = 0.01f * AppSettings.cameraSize;
        line.endWidth = line.startWidth;
    }

    public void SetLine(List<Vector2> position)
    {
        line.positionCount = position.Count;
        for (int i = 0; i < position.Count; i++)
        {
            line.SetPosition(i, new Vector3(position[i].x, position[i].y, -0.1f));
        }
    }

    public void Clear()
    {
        line.positionCount = 0;
    }
}
