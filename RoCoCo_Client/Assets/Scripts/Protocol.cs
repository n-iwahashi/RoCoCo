using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InitReply
{
    public CameraObj camera = new CameraObj();
    public List<Agent> agents = new List<Agent>();
    public List<Obj> objects = new List<Obj>();
}

[System.Serializable]
public class CameraObj
{
    public List<float> pos = new List<float>();
    public float size;
    public string background;
}

[System.Serializable]
public class Agent
{
    public string name;
    public string shape = "circle";  // "circle", "box"
    public string sprite;
    public float rotation = 0.0f;
    public float init_rotation = 0.0f;
    public List<float> pos = new List<float>();
    public List<float> scale = new List<float>();
    public string color; // "#FFFFFF"
    public bool colider = false;
}

[System.Serializable]
public class Obj
{
    public string name;
    public string type;   // "static", "connector"
    public string shape;  // "circle", "box", "line"
    public string sprite;
    public float rotation = 0.0f;
    public List<float> pos = new List<float>();
    public List<float> scale = new List<float>();
    public string color; // "#FFFFFF"
    public int order = 0;
    public bool colider = false;

    // for "connector"
    public string a;
    public string b;

    // for "line"
    public List<float> pos1 = new List<float>();
    public List<float> pos2 = new List<float>();
    public float width = 0.1f;
}

[System.Serializable]
public class CommandConnect
{
    public string command = "connect";
    public string id = "default";
}

[System.Serializable]
public class CommandStart
{
    public string command = "start";
    public int player_num = 1;
    public double task_dynamics_weight1 = 0.5f;
    public double task_dynamics_weight2 = 0.5f;
    public double control_interval = 0.02f;
}

[System.Serializable]
public class CommandReply
{
    public string status;
}

[System.Serializable]
public class CommandFrame
{
    public string command = "frame";
    // ※floatではToJson()で誤差が生じるのでdoubleを使用
    public double time = AppUtil.RoundF(Time.realtimeSinceStartup); // 起動からの秒数
    public List<double> pos1 = new List<double>();
    public List<double> pos2 = new List<double>();
    public List<double> vel1 = new List<double>();
    public List<double> vel2 = new List<double>();
    public List<double> raw_vel1 = new List<double>(); // エージェント１に加えた速度
    public List<double> raw_vel2 = new List<double>(); // エージェント２に加えた速度
}

[System.Serializable]
public class Pos
{
    public List<float> pos1 = new List<float>();
    public List<float> pos2 = new List<float>();
    public List<float> vel1 = new List<float>();
    public List<float> vel2 = new List<float>();
}

[System.Serializable]
public class Reply
{
    public string reply = ""; // command
    public float time = 0.0f; // "frame"コマンドのtime

    public List<float> vel1 = new List<float>();
    public List<float> vel2 = new List<float>();

    public List<Pos> path = new List<Pos>();
    public List<float> pos1 = new List<float>();
    public List<float> pos2 = new List<float>();
    public int vel_index = 0;

    public bool goal = false;
    public bool timeout = false;
}

