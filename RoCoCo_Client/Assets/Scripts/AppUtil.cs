using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AppUtil
{
    public static float GetDeg(Vector2 vec)
    {
        float rad = -Mathf.Atan2(vec.x, vec.y);
        float deg = rad * Mathf.Rad2Deg;
        return deg;
    }

    public static float GetDeg(Vector2 start, Vector2 target)
    {
        return GetDeg(target - start);
    }

    public static Vector2 RadToVector(float rad)
    {
        return new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
    }

    public static Vector2 DegToVector(float deg)
    {
        return RadToVector(deg * Mathf.Deg2Rad);
    }

    public static string VectorToStr(Vector2 vec)
    {
        return "(" + vec.x.ToString("F3") + "," + vec.y.ToString("F3") + ")";
    }

    public static double RoundF(float x)
    {
        return double.Parse(x.ToString("F3"));
    }

    public static float Cross2D(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    public static Color ToColor(string str)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(str, out color))
        {
            return color;
        }
        return Color.white;
    }
}
