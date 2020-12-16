using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class MultiDisplay : MonoBehaviour
{
    void Start()
    {
        Debug.Log("displays connected: " + Display.displays.Length);
        // Display.displays[0] は主要デフォルトディスプレイで常にON。
        // 追加ディスプレイが可能かを確認し、それぞれをアクティベートする。
        if (Display.displays.Length > 1)
            Display.displays[1].Activate();
        if (Display.displays.Length > 2)
            Display.displays[2].Activate();

        // ここでウィンドウスタイルを変更してもタイトルバーが無くなる
        // 一通り初期化が終わったあとで変更する→FixedUpdate
        // 
    }

    int counter = 0;
    const int INIT_COUNT = 100;

    void FixedUpdate()
    {
        // 初期化終了後、少ししてからウィンドウスタイルを変更
        if (counter < INIT_COUNT)
        {
            counter++;
        }
        else
        {
            if (counter == INIT_COUNT)
            {
                SetOverlappedWindow("RoCoCo");
                counter++;
            }
        }
    }

    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    static extern IntPtr FindWindow(System.String className, System.String windowName);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    const int GWL_STYLE = -16;
    const int WS_OVERLAPPEDWINDOW = 0x00CF0000; // WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX

    static void SetOverlappedWindow(string name)
    {
        IntPtr window = FindWindow(null, name);
        if (window != null)
        {
            int style = GetWindowLong(window, GWL_STYLE);
            style |= WS_OVERLAPPEDWINDOW;
            SetWindowLong(window, GWL_STYLE, style);
        }
        Debug.Log(name + ":" + window);
    }
}
