using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Net;

public class Simulation : MonoBehaviour
{
    Manager manager;
    GameObject agent1;
    GameObject agent2;

    GameObject lineAgent1;
    GameObject lineAgent2;

    GameObject pos1_recv;
    GameObject pos2_recv;
    GameObject arrow1;
    GameObject arrow2;

    public enum Status { UNINIT, INIT, CONTROL, END }
    public static Status status = Status.UNINIT;

    string url;
    WebSocket ws;

    ILogger logger;
    DataLog posvelLog;

    Vector2 rep_vel1 = new Vector2();
    Vector2 rep_vel2 = new Vector2();
    bool isUpdateVel = false;
    int frameCount = 0;
    float roundTripAmount = 0.0f;
    bool isGoal = false;
    bool isTimeout = false;
    List<Vector2> agent1Path = new List<Vector2>();
    List<Vector2> agent2Path = new List<Vector2>();
    List<Vector2> agent1Vel = new List<Vector2>();
    List<Vector2> agent2Vel = new List<Vector2>();
    int vel_index = 0;
    Vector2 pos1_at_recv_path = new Vector2();
    Vector2 pos2_at_recv_path = new Vector2();
    bool isUpdatePath = false;
    int replyCount = 1;
    int msgCount = 1;

    DataLog eventLog;
    string logDir;

    InitReply initReply;
    bool isWaitReply = false;
    bool isConnectAndStart = false;

    // for FPS
    int counterFPS = 0;
    DateTime timeStartFPS;

    SynchronizationContext context;

    void Start()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        agent1 = GameObject.Find("Agent1");
        agent2 = GameObject.Find("Agent2");

        lineAgent1 = GameObject.Find("LineAgent1");
        lineAgent2 = GameObject.Find("LineAgent2");
        lineAgent1.GetComponent<SpriteRenderer>().enabled = false;
        lineAgent2.GetComponent<SpriteRenderer>().enabled = false;

        pos1_recv = GameObject.Find("Pos1");
        pos2_recv = GameObject.Find("Pos2");
        pos1_recv.GetComponent<SpriteRenderer>().enabled = false;
        pos2_recv.GetComponent<SpriteRenderer>().enabled = false;

        arrow1 = GameObject.Find("Arrow1");
        arrow2 = GameObject.Find("Arrow2");

        manager.SetCameraBkColor(Manager.COLOR_END);

        _CreateLog();

        // for ws 
        context = SynchronizationContext.Current;

        _CreateWS();
        
        logger.Log("WebSocket Connecting: " + url);
        isWaitReply = true;
        isConnectAndStart = false;
        ws.Connect();
    }

    void _CreateWS()
    {
        // start WebSocket
        url = "ws://" + AppSettings.Host + ":" + AppSettings.Port + "/";
        ws = new WebSocket(url);

        ws.OnOpen += (sender, e) =>
        {
            logger.Log("WebSocket Open");

            CommandConnect cmd = new CommandConnect();
            cmd.id = AppSettings.UserId;
            ws.Send(JsonUtility.ToJson(cmd));
        };

        ws.OnMessage += (sender, e) =>
        {
            logger.Log("WebSocket Recv: " + e.Data);

            // run on main thread
            context.Post(data => {
                string strData = (string)data;
                if (status == Status.UNINIT)
                {
                    if (strData.Contains("camera"))
                    {
                        initReply = JsonUtility.FromJson<InitReply>(strData);
                        File.WriteAllText(logDir + "/init_reply.json", strData);
                        eventLog.Output(new List<object> { "init", "init_reply.json" });
                    }
                }
                else if (status == Status.INIT)
                {
                    CommandReply cmdReply = JsonUtility.FromJson<CommandReply>(strData);
                    if (cmdReply.status == "OK")
                    {
                        status = Status.CONTROL;
                        eventLog.Output(new List<object> { "status", "CONTROL" });
                        manager.SetCameraBkColor(Manager.COLOR_PLAY);
                        //isUpdateVel = false;
                    }
                }
                else if (status == Status.CONTROL)
                {
                    // receive the results of task dynamics and path.
                    Reply reply = JsonUtility.FromJson<Reply>(strData);
                    rep_vel1 = new Vector2(reply.vel1[0], reply.vel1[1]);
                    rep_vel2 = new Vector2(reply.vel2[0], reply.vel2[1]);
                    vel_index = reply.vel_index;
                    isGoal = reply.goal;
                    isTimeout = reply.timeout;
                    isUpdateVel = true;
                    if (reply.path.Count > 0)
                    {
                        string reply_filename = String.Format("reply_{0:D3}.json", replyCount);
                        replyCount++;
                        File.WriteAllText(logDir + "/" + reply_filename, strData);
                        eventLog.Output(new List<object> { "recv", reply_filename });

                        agent1Path.Clear();
                        agent2Path.Clear();
                        agent1Vel.Clear();
                        agent2Vel.Clear();
                        foreach (Pos pos in reply.path)
                        {
                            Vector2 pos1 = new Vector2(pos.pos1[0], pos.pos1[1]);
                            Vector2 pos2 = new Vector2(pos.pos2[0], pos.pos2[1]);
                            Vector2 vel1 = new Vector2(pos.vel1[0], pos.vel1[1]);
                            Vector2 vel2 = new Vector2(pos.vel2[0], pos.vel2[1]);

                            agent1Path.Add(pos1);
                            agent2Path.Add(pos2);
                            agent1Vel.Add(vel1);
                            agent2Vel.Add(vel2);
                        }
                        if (reply.pos1.Count == 2) pos1_at_recv_path = new Vector2(reply.pos1[0], reply.pos1[1]);
                        if (reply.pos2.Count == 2) pos2_at_recv_path = new Vector2(reply.pos2[0], reply.pos2[1]);
                        //vel_index = reply.vel_index;
                        isUpdatePath = true;
                    }
                    // 通信速度計測用の計算
                    roundTripAmount += Time.realtimeSinceStartup - reply.time;
                    frameCount++;
                }
                isWaitReply = false;
            }, e.Data);
        };

        ws.OnError += (sender, e) =>
        {
            _UpdateMessage("WebSocket Error Message: " + e.Message);
        };

        ws.OnClose += (sender, e) =>
        {
            if (sender != ws)
            {
                return;
            }

            if (status != Status.UNINIT)
            {
                _UpdateMessage("WebSocket Close: " + url);
                initReply = null;
                status = Status.UNINIT;
                eventLog.Output(new List<object> { "status", "UNINIT" });

                context.Post(_ =>
                {
                    Rigidbody2D rbAgent1 = agent1.transform.GetComponent<Rigidbody2D>();
                    Rigidbody2D rbAgent2 = agent2.transform.GetComponent<Rigidbody2D>();
                    rbAgent1.velocity = new Vector2();
                    rbAgent2.velocity = new Vector2();

                    manager.SetCameraBkColor(Manager.COLOR_END);
                }, null);
            }
            isWaitReply = false;
        };
    }

    void Update()
    {
        // see [Project Settings]->[Input] menu for joystick and key assignments
        
        // Start
        if (status == Status.UNINIT || status == Status.INIT || status == Status.END)
        {
            if (isWaitReply)
            {
                // press twice
                return;
            }

            // ★数字キーで設定切り替え＋[Start]でシミュレーション開始
            int startSettingNo = -1;
            if (Input.GetButtonDown("Start"))
            {
                startSettingNo = 0;
                //AppSettings.FILE_NAME = "settings.json";
            }
            else
            {
                // [1]～[9]
                for (int i = 1; i <= 9; i++)
                {
                    if (Input.GetButtonDown(String.Format("Start_{0}", i)))
                    {
                        startSettingNo = i;
                        AppSettings.FILE_NAME = String.Format("settings_{0}.json", i);
                        break;
                    }
                }
            }

            if (startSettingNo >= 0)
            {
                AppSettings.Clear();

                Time.timeScale = AppSettings.TimeScale;
                Time.fixedDeltaTime = AppSettings.ControlInterval;

                if (ws.IsAlive)
                {
                    if (startSettingNo == 0)
                    {
                        // 接続を保持したまま開始
                        if (status == Status.END)
                        {
                            // restart!
                            string prevLogDir = logDir;

                            // reset log when restart.
                            _ClearLog();
                            _CreateLog();

                            // copy init_reply
                            File.Copy(prevLogDir + "/init_reply.json", logDir + "/init_reply.json");
                            eventLog.Output(new List<object> { "init", "init_reply.json" });

                            _Initialize();
                            return; // ★初期化のみ、再度押下で開始
                        }
                        _Start();
                        return;
                    }
                    else
                    {
                        // 数字キーの場合
                        // 一度接続を切る。早く切断するためエラーとしてCloseする
                        ws.Close(CloseStatusCode.Undefined);
                    }
                }

                _ClearLog();
                _CreateLog();

                _CreateWS();

                // Connect and Start
                isWaitReply = true;
                // 数字キーの場合、接続＋ロードのみ→コメントアウト
                //isConnectAndStart = (startSettingNo == 0);
                // ★開始ボタンの場合も初期化のみ、再度押下で開始
                isConnectAndStart = false;
                ws.Connect();

                manager.UpdateGuiMsg("[" + AppSettings.FILE_NAME + "] Loading... ");
            }
        }
        // Stop/Pause/Resume
        else if (status == Status.CONTROL)
        {
            // [Stop]: "joystick button 2"[X] or [esc] key
            if (Input.GetButtonDown("Stop"))
            {
                _Stop();
            }
            // [Pause]: "joystick button 0"[A] or [left ctrl] key
            else if (Input.GetButtonDown("Pause"))
            {
                if (Time.timeScale != 0)
                {
                    Time.timeScale = 0;
                    eventLog.Output(new List<object> { "status", "PAUSE" });
                }
                else
                {
                    Time.timeScale = AppSettings.TimeScale;
                    eventLog.Output(new List<object> { "status", "RESUME" });
                }
            }
        }
    }

    void FixedUpdate()
    {
        Rigidbody2D rbAgent1 = agent1.transform.GetComponent<Rigidbody2D>();
        Rigidbody2D rbAgent2 = agent2.transform.GetComponent<Rigidbody2D>();
        Rigidbody2D rbArrow1 = arrow1.transform.GetComponent<Rigidbody2D>();
        Rigidbody2D rbArrow2 = arrow2.transform.GetComponent<Rigidbody2D>();

        if (status != Status.CONTROL)
        {
            rbAgent1.velocity = new Vector2();
            rbAgent2.velocity = new Vector2();
            rbArrow1.velocity = new Vector2();
            rbArrow2.velocity = new Vector2();
        }

        Vector2 pos1 = rbAgent1.position;
        Vector2 vel1 = rbAgent1.velocity;
        Vector2 pos2 = rbAgent2.position;
        Vector2 vel2 = rbAgent2.velocity;

        if (status == Status.UNINIT)
        {
            if (initReply != null)
            {
                _Initialize();

                if (isConnectAndStart)
                {
                    isConnectAndStart = false;
                    _Start();
                }
            }
            return;
        }
        else if (status == Status.INIT)
        {
            return;
        }
        else if (status == Status.END)
        {
            return;
        }
        // status == Status.CONTROL

        if (isGoal)
        {
            // サーバによるゴール判定
            _Stop();
            isGoal = false;
            return;
        }
        if (isTimeout)
        {
            // サーバによるタイムアウト判定
            _Stop();
            isTimeout = false;
            return;
        }

        // 軌道更新
        if (isUpdatePath)
        {
            lineAgent1.GetComponent<Line>().SetEnabled(AppSettings.ShowAgent1Path);
            lineAgent1.GetComponent<Line>().SetLine(agent1Path);
            lineAgent2.GetComponent<Line>().SetEnabled(AppSettings.ShowAgent2Path);
            lineAgent2.GetComponent<Line>().SetLine(agent2Path);
            if (AppSettings.ShowAgent1Path && agent1Path.Count > 0)
            {
                pos1_recv.GetComponent<SpriteRenderer>().enabled = true;
                pos1_recv.transform.position = pos1_at_recv_path;
            }
            else
            {
                pos1_recv.GetComponent<SpriteRenderer>().enabled = false;
            }
            if (AppSettings.ShowAgent2Path && agent2Path.Count > 0)
            {
                pos2_recv.GetComponent<SpriteRenderer>().enabled = true;
                pos2_recv.transform.position = pos2_at_recv_path;
            }
            else
            {
                pos2_recv.GetComponent<SpriteRenderer>().enabled = false;
            }

            isUpdatePath = false;
        }

        if (isUpdateVel)
        {
            vel1 = rep_vel1;
            vel2 = rep_vel2;
            isUpdateVel = false;
        }

        // 入力から速度を得る
        if (AppSettings.PlayerNum >= 1)
        {
            float horz1 = Input.GetAxis("Horizontal_1") * AppSettings.InputRatio;
            if (AppSettings.Camera1Axis == 1 || AppSettings.Camera1Axis == 3) // 左右反転
            {
                horz1 = -horz1;
            }
            float vert1 = Input.GetAxis("Vertical_1") * AppSettings.InputRatio;
            if (AppSettings.Camera1Axis == 2 || AppSettings.Camera1Axis == 3) // 上下反転
            {
                vert1 = -vert1;
            }
            vel1 = new Vector2(horz1, vert1);
        }
        if (AppSettings.PlayerNum == 2)
        {
            float horz2 = Input.GetAxis("Horizontal_2") * AppSettings.InputRatio;
            if (AppSettings.Camera2Axis == 1 || AppSettings.Camera2Axis == 3) // 左右反転
            {
                horz2 = -horz2;
            }
            float vert2 = Input.GetAxis("Vertical_2") * AppSettings.InputRatio;
            if (AppSettings.Camera2Axis == 2 || AppSettings.Camera2Axis == 3) // 上下反転
            {
                vert2 = -vert2;
            }
            vel2 = new Vector2(horz2, vert2);
        }

        ///// 机の制約
        Vector2 raw_vel1 = vel1;
        Vector2 raw_vel2 = vel2;

        Vector2[] real_vel = TaskDynamics.TwoAgentControl2(manager.tl, pos1, vel1, AppSettings.Agent1TaskDynamicsWeight,
            pos2, vel2, AppSettings.Agent2TaskDynamicsWeight, AppSettings.ControlInterval);

        Vector2 real_vel1 = real_vel[0];
        Vector2 real_vel2 = real_vel[1];
        /////

        rbAgent1.velocity = real_vel1;
        rbAgent2.velocity = real_vel2;
        vel1 = real_vel1;
        vel2 = real_vel2;

        // パス非表示の場合も各エージェントに加える速度ベクトルを表示する
        rbArrow1.position = new Vector2(pos1.x * 0.85f + pos2.x * 0.15f, pos1.y * 0.85f + pos2.y * 0.15f);
        rbArrow1.velocity = raw_vel1;
        rbArrow2.position = new Vector2(pos1.x * 0.15f + pos2.x * 0.85f, pos1.y * 0.15f + pos2.y * 0.85f);
        rbArrow2.velocity = raw_vel2;
       
        if (AppSettings.PlayerNum == 0 && AppSettings.ShowAgent1Path && vel_index < agent1Path.Count)
        {
            lineAgent1.GetComponent<SpriteRenderer>().enabled = true;
            Vector2 vel_pos1 = agent1Path[vel_index];
            lineAgent1.transform.position = new Vector3(vel_pos1.x, vel_pos1.y, 1);
        }
        else
        {
            lineAgent1.GetComponent<SpriteRenderer>().enabled = false;
        }
        if (AppSettings.PlayerNum <= 1 && AppSettings.ShowAgent2Path && vel_index < agent2Path.Count)
        {
            lineAgent2.GetComponent<SpriteRenderer>().enabled = true;
            Vector2 vel_pos2 = agent2Path[vel_index];
            lineAgent2.transform.position = new Vector3(vel_pos2.x, vel_pos2.y, 1);
        }
        else
        {
            lineAgent2.GetComponent<SpriteRenderer>().enabled = false;
        }

        if (!manager.isAgentConnect)
        {
            if (vel1.sqrMagnitude > 0.0001f)
            {
                rbAgent1.MoveRotation(AppUtil.GetDeg(vel1) + initReply.agents[0].rotation);
            }
            if (vel2.sqrMagnitude > 0.0001f)
            {
                rbAgent2.MoveRotation(AppUtil.GetDeg(vel2) + initReply.agents[1].rotation);
            }
        }

        if (counterFPS == 0)
        {
            timeStartFPS = DateTime.Now;
        }
        else if (counterFPS % 100 == 0)
        {
            TimeSpan span = DateTime.Now - timeStartFPS;
            float roundTripTime = roundTripAmount / frameCount;
            manager.UpdateGuiMsg(String.Format("FPS: {0:0.0}  Round-Trip: {1:0.0}[msec]", 1000.0/(span.TotalMilliseconds / 100.0), roundTripTime*1000.0));
            timeStartFPS = DateTime.Now;
        }
        counterFPS++;

        // posvel log
        posvelLog.Output(new List<object> { pos1.x, pos1.y, vel1.x, vel1.y, raw_vel1.x, raw_vel1.y, pos2.x, pos2.y, vel2.x, vel2.y, raw_vel2.x, raw_vel2.y });

        // ★リアルタイムに速度をサーバに送信
        CommandFrame cmd = new CommandFrame();
        cmd.pos1.Add(AppUtil.RoundF(pos1.x));
        cmd.pos1.Add(AppUtil.RoundF(pos1.y));
        cmd.vel1.Add(AppUtil.RoundF(vel1.x));
        cmd.vel1.Add(AppUtil.RoundF(vel1.y));
        cmd.raw_vel1.Add(AppUtil.RoundF(raw_vel1.x));
        cmd.raw_vel1.Add(AppUtil.RoundF(raw_vel1.y));
        cmd.pos2.Add(AppUtil.RoundF(pos2.x));
        cmd.pos2.Add(AppUtil.RoundF(pos2.y));
        cmd.vel2.Add(AppUtil.RoundF(vel2.x));
        cmd.vel2.Add(AppUtil.RoundF(vel2.y));
        cmd.raw_vel2.Add(AppUtil.RoundF(raw_vel2.x));
        cmd.raw_vel2.Add(AppUtil.RoundF(raw_vel2.y));
        ws.Send(JsonUtility.ToJson(cmd));
    }

    void _Initialize()
    {
        // initialize the positions of agent1 and agent2 and ...
        manager.Init(initReply);

        Rigidbody2D rbAgent1 = agent1.transform.GetComponent<Rigidbody2D>();
        Rigidbody2D rbAgent2 = agent2.transform.GetComponent<Rigidbody2D>();
        string msg = "[" + AppSettings.FILE_NAME + "] INIT " + agent1.name + ":" + AppUtil.VectorToStr(rbAgent1.position) + " " + agent2.name + ":" + AppUtil.VectorToStr(rbAgent2.position);
        _UpdateMessage(msg);

        status = Status.INIT;
        eventLog.Output(new List<object> { "status", "INIT" });
        manager.SetCameraBkColor(Manager.COLOR_INIT);
    }

    void _Start()
    {
        // client ---> {"command":"start"} ---> server
        // client <--- {"status":"OK"}     <--- server
        CommandStart cmd = new CommandStart();
        cmd.player_num = AppSettings.PlayerNum;
        cmd.task_dynamics_weight1 = AppSettings.Agent1TaskDynamicsWeight;
        cmd.task_dynamics_weight2 = AppSettings.Agent2TaskDynamicsWeight;
        cmd.control_interval = AppSettings.ControlInterval;
        string msg = JsonUtility.ToJson(cmd);
        ws.Send(msg);

        logger.Log("WebSocket Send: msg=" + msg);
        //string send_filename = "command_start.json";
        //File.WriteAllText(logDir + "/" + send_filename, msg);
        //eventLog.Output(new List<object> { "send", send_filename });
        isWaitReply = true;

        logger.Log("Start");
        agent1Path.Clear();
        agent2Path.Clear();
        agent1Vel.Clear();
        agent2Vel.Clear();
        isUpdateVel = false;
        frameCount = 0;
        roundTripAmount = 0.0f;
        isGoal = false;
        isTimeout = false;
        vel_index = 0;
        counterFPS = 0;
    }

    void _Stop()
    {
        status = Status.END;
        manager.SetCameraBkColor(Color.black);
        eventLog.Output(new List<object> { "status", "END" });

        Rigidbody2D rbAgent1 = agent1.transform.GetComponent<Rigidbody2D>();
        Rigidbody2D rbAgent2 = agent2.transform.GetComponent<Rigidbody2D>();
        rbAgent1.velocity = new Vector2();
        rbAgent2.velocity = new Vector2();
        string msg = "END " + agent1.name + ":" + AppUtil.VectorToStr(rbAgent1.position) + " " + agent2.name + ":" + AppUtil.VectorToStr(rbAgent2.position);
        if (isTimeout)
        {
            msg = "TIMEOUT";
        }
        _UpdateMessage(msg);

        lineAgent1.GetComponent<Line>().Clear();
        lineAgent2.GetComponent<Line>().Clear();
        lineAgent1.GetComponent<SpriteRenderer>().enabled = false;
        lineAgent2.GetComponent<SpriteRenderer>().enabled = false;
        pos1_recv.GetComponent<SpriteRenderer>().enabled = false;
        pos2_recv.GetComponent<SpriteRenderer>().enabled = false;

        Vector2 pos1 = rbAgent1.position;
        Vector2 vel1 = rbAgent1.velocity;
        Vector2 pos2 = rbAgent2.position;
        Vector2 vel2 = rbAgent2.velocity;
        posvelLog.Output(new List<object> { pos1.x, pos1.y, vel1.x, vel1.y, vel1.x, vel1.y, pos2.x, pos2.y, vel2.x, vel2.y, vel2.x, vel2.y });

        Time.timeScale = AppSettings.TimeScale;

        // ★サーバにゴール時点の情報を送信
        CommandFrame cmd = new CommandFrame();
        cmd.command = "stop";
        cmd.pos1.Add(AppUtil.RoundF(pos1.x));
        cmd.pos1.Add(AppUtil.RoundF(pos1.y));
        cmd.vel1.Add(AppUtil.RoundF(vel1.x));
        cmd.vel1.Add(AppUtil.RoundF(vel1.y));
        cmd.raw_vel1.Add(AppUtil.RoundF(vel1.x));
        cmd.raw_vel1.Add(AppUtil.RoundF(vel1.y));
        cmd.pos2.Add(AppUtil.RoundF(pos2.x));
        cmd.pos2.Add(AppUtil.RoundF(pos2.y));
        cmd.vel2.Add(AppUtil.RoundF(vel2.x));
        cmd.vel2.Add(AppUtil.RoundF(vel2.y));
        cmd.raw_vel2.Add(AppUtil.RoundF(vel2.x));
        cmd.raw_vel2.Add(AppUtil.RoundF(vel2.y));
        ws.Send(JsonUtility.ToJson(cmd));
    }

    void _CreateLog()
    {
        logDir = "log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(logDir);
        
        logger = FileLogger.CreateDummy(); // no app log
        //logger = FileLogger.Create(logDir + "/app.log");

        eventLog = DataLog.Writer(logDir + "/event.csv");
        posvelLog = DataLog.Writer(logDir + "/posvel.csv");

        replyCount = 1;
        msgCount = 1;
    }

    void _ClearLog()
    {
        if (logger != null)
        {
            ((FileLogger)logger.logHandler).Close();
            logger = null;
        }
        if (posvelLog != null)
        {
            posvelLog.Close();
            posvelLog = null;
        }
        if (eventLog != null)
        {
            eventLog.Close();
            eventLog = null;
        }
    }

    void _UpdateMessage(string msg)
    {
        logger.Log(msg);
        string msg_filename = String.Format("msg_{0:D3}.txt", msgCount);
        msgCount++;
        File.WriteAllText(logDir + "/" + msg_filename, msg);
        eventLog.Output(new List<object> { "msg", msg_filename });
        manager.UpdateGuiMsg(msg);
    }

    void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close(CloseStatusCode.Undefined);
            ws = null;
        }
        _ClearLog();
    }
}
