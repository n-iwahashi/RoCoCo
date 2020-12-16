using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public static class AppSettings
{
    [System.Serializable]
    private class MySettings
    {
        public string host = "localhost";
        public ushort port = 12345;
        public string user_id = "";
        public float control_interval = 0.02f;
        public float time_scale = 1.0f;
        public float input_ratio = 0.2f;
        public bool show_agent1_path = false;
        public bool show_agent2_path = false;
        public float agent1_arrow = 1.0f;
        public float agent2_arrow = 1.0f;
        public int player_num = 1;
        public float agent1_task_dynamics_weight = 1.0f;
        public float agent2_task_dynamics_weight = 0.0f;
        public int camera1_axis = 0; // 0:通常,1:左右反転,2:上下反転,3:上下左右反転
        public int camera2_axis = 3; // 同上
        public bool show_route_history = true;
        public bool replay = false;
        public string replay_path = "";
    }

    public static string FILE_NAME = "settings.json";

    public const int SCREEN_WIDTH = 1600;
    public const int SCREEN_HEIGHT = 800;

    public static float cameraSize = 1.0f;

    private static MySettings _settings;

    private static MySettings Settings
    {
        get
        {
            if (_settings != null)
            {
                return _settings;
            }
            string path = Application.dataPath + "/" + FILE_NAME;
            try
            {
                string jsonString = File.ReadAllText(path);
                _settings = JsonUtility.FromJson<MySettings>(jsonString);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
            if (_settings == null)
            {
                _settings = new MySettings();
            }
            return _settings;
        }
    }

    public static void Clear()
    {
        _settings = null;
    }

    public static string Host
    {
        get
        {
            return Settings.host;
        }
    }

    public static ushort Port
    {
        get
        {
            return Settings.port;
        }
    }

    public static string UserId
    {
        get
        {
            return Settings.user_id;
        }
    }

    public static float ControlInterval
    {
        get
        {
            return Settings.control_interval;
        }
    }

    public static float TimeScale
    {
        get
        {
            return Settings.time_scale;
        }
    }

    public static float InputRatio
    {
        get
        {
            return Settings.input_ratio;
        }
    }

    public static bool ShowAgent1Path
    {
        get
        {
            return Settings.show_agent1_path;
        }
    }

    public static bool ShowAgent2Path
    {
        get
        {
            return Settings.show_agent2_path;
        }
    }

    public static float Agent1Arrow
    {
        get
        {
            return Settings.agent1_arrow;
        }
    }

    public static float Agent2Arrow
    {
        get
        {
            return Settings.agent2_arrow;
        }
    }

    public static int PlayerNum
    {
        get
        {
            return Settings.player_num;
        }
    }

    public static float Agent1TaskDynamicsWeight
    {
        get
        {
            return Settings.agent1_task_dynamics_weight;
        }
    }

    public static float Agent2TaskDynamicsWeight
    {
        get
        {
            return Settings.agent2_task_dynamics_weight;
        }
    }

    public static int Camera1Axis
    {
        get
        {
            return Settings.camera1_axis;
        }
    }

    public static int Camera2Axis
    {
        get
        {
            return Settings.camera2_axis;
        }
    }

    public static bool ShowRouteHistory
    {
        get
        {
            return Settings.show_route_history;
        }
    }

    public static bool Replay
    {
        get
        {
            return Settings.replay;
        }
    }

    public static string ReplayPath
    {
        get
        {
            return Settings.replay_path;
        }
    }
}
