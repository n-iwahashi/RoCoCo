using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Replay : MonoBehaviour
{
    Manager manager;
    GameObject agent1;
    GameObject agent2;

    GameObject lineAgent1;
    GameObject lineAgent2;

    LineRenderer histLineAgent1;
    Vector2 prevPosAgent1;
    LineRenderer histLineAgent2;
    Vector2 prevPosAgent2;

    GameObject arrow1;
    GameObject arrow2;

    string logDir;
    InitReply initReply;

    List<DataLog.Row> eventList = new List<DataLog.Row>();
    int eventIndex = 0;
    List<DataLog.Row> polvelList = new List<DataLog.Row>();
    int polvelIndex = 0;

    DateTime currentTime;

    void Awake()
    {
        if (AppSettings.Replay)
        {
            GameObject.Find("Simulation").SetActive(false);
        }
        else
        {
            this.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        agent1 = GameObject.Find("Agent1");
        agent2 = GameObject.Find("Agent2");

        lineAgent1 = GameObject.Find("LineAgent1");
        lineAgent2 = GameObject.Find("LineAgent2");

        histLineAgent1 = agent1.GetComponent<LineRenderer>();
        histLineAgent2 = agent2.GetComponent<LineRenderer>();

        arrow1 = GameObject.Find("Arrow1");
        arrow2 = GameObject.Find("Arrow2");

        _Start(true);
    }

    void Update()
    {
        if (Input.GetButtonDown("Start"))
        {
            _Start(false);
        }
    }

    void _Start(bool isFirst)
    {
        if (!isFirst)
        {
            // restart
            AppSettings.Clear();

            Rigidbody2D rbAgent1 = agent1.transform.GetComponent<Rigidbody2D>();
            Rigidbody2D rbAgent2 = agent2.transform.GetComponent<Rigidbody2D>();
            rbAgent1.velocity = new Vector2();
            rbAgent2.velocity = new Vector2();

            lineAgent1.GetComponent<Line>().Clear();
            lineAgent2.GetComponent<Line>().Clear();

            arrow1.transform.GetComponent<Rigidbody2D>().velocity = new Vector2();
            arrow2.transform.GetComponent<Rigidbody2D>().velocity = new Vector2();
        }

        Time.timeScale = AppSettings.TimeScale;
        Time.fixedDeltaTime = AppSettings.ControlInterval;
        if (Time.timeScale < 1.0)
        {
            // Adjust fixed delta time according to timescale
            // The fixed delta time will now be 0.02 frames per real-time second
            Time.fixedDeltaTime = AppSettings.ControlInterval * Time.timeScale;
        }

        // get the lastest log directory.
        string[] dirs = Directory.GetDirectories(".", AppSettings.ReplayPath, SearchOption.TopDirectoryOnly);
        if (dirs.Length > 0)
        {
            Array.Sort(dirs);
            logDir = dirs[dirs.Length - 1];
            Debug.Log(logDir);
        }

        histLineAgent1.positionCount = 0;
        histLineAgent2.positionCount = 0;
        eventIndex = 0;
        polvelIndex = 0;

        // load events.
        // get init event.
        DateTime startTime = DateTime.Now;
        initReply = null;
        eventList = DataLog.Load(logDir + "/event.csv");
        foreach (DataLog.Row row in eventList)
        {
            if (row.cols[0] == "init")
            {
                startTime = row.dateTime;

                // initial information
                try
                {
                    string strInitReply = File.ReadAllText(logDir + "/" + row.cols[1]);
                    initReply = JsonUtility.FromJson<InitReply>(strInitReply);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                    manager.UpdateGuiMsg("[Replay] " + e.Message);
                    return;
                }
                break;
            }
        }
        if (initReply == null)
        {
            manager.UpdateGuiMsg("[Replay] No data.");
            return;
        }

        manager.Init(initReply);

        // load csv 
        // Agent1/Agent2 pos vel
        polvelList = DataLog.Load(logDir + "/posvel.csv");
        if (polvelList.Count > 0)
        {
            startTime = polvelList[0].dateTime;
        }

        // set current time
        currentTime = startTime;
    }

    void _OnEvent()
    {
        DataLog.Row row = eventList[eventIndex];

        Rigidbody2D rbAgent1 = agent1.transform.GetComponent<Rigidbody2D>();
        Rigidbody2D rbAgent2 = agent2.transform.GetComponent<Rigidbody2D>();

        if (row.cols[0] == "status")
        {
            string status = row.cols[1];
            if (status == "END" || status == "INIT")
            {
                rbAgent1.velocity = new Vector2();
                rbAgent2.velocity = new Vector2();
                lineAgent1.GetComponent<Line>().Clear();
                lineAgent2.GetComponent<Line>().Clear();
            }
            else if (status == "PAUSE")
            {
                // search the next status event
                int index = eventIndex + 1;
                while (index < eventList.Count)
                {
                    row = eventList[index];
                    if (row.cols[0] == "status")
                    {
                        // set currentTime
                        currentTime = row.dateTime;
                        break;
                    }
                    index++;
                }
            }
        }
        else if (row.cols[0] == "msg")
        {
            // show status message.
            manager.UpdateGuiMsg(File.ReadAllText(logDir + "/" + row.cols[1]));
        }
        else if (row.cols[0] == "recv")
        {
            // show path from reply
            string strReply = File.ReadAllText(logDir + "/" + row.cols[1]);
            Reply reply = JsonUtility.FromJson<Reply>(strReply);
            List<Vector2> agent1Path = new List<Vector2>();
            List<Vector2> agent2Path = new List<Vector2>();
            foreach (Pos pos in reply.path)
            {
                Vector2 pos1 = new Vector2(pos.pos1[0], pos.pos1[1]);
                Vector2 pos2 = new Vector2(pos.pos2[0], pos.pos2[1]);
                agent1Path.Add(pos1);
                agent2Path.Add(pos2);
            }
            lineAgent1.GetComponent<Line>().SetEnabled(AppSettings.ShowAgent1Path);
            lineAgent1.GetComponent<Line>().SetLine(agent1Path);
            lineAgent2.GetComponent<Line>().SetEnabled(AppSettings.ShowAgent2Path);
            lineAgent2.GetComponent<Line>().SetLine(agent2Path);
        }
    }

    void FixedUpdate()
    {
        Rigidbody2D rbAgent1 = agent1.transform.GetComponent<Rigidbody2D>();
        Rigidbody2D rbAgent2 = agent2.transform.GetComponent<Rigidbody2D>();

        currentTime = currentTime.AddSeconds(Time.deltaTime);

        // event
        while (eventIndex < eventList.Count && eventList[eventIndex].dateTime <= currentTime)
        {
            _OnEvent();
            eventIndex++;
        }

        // posvel
        if (polvelIndex >= polvelList.Count)
        {
            // end of data
            rbAgent1.velocity = new Vector2();
            rbAgent2.velocity = new Vector2();
            lineAgent1.GetComponent<Line>().Clear();
            lineAgent2.GetComponent<Line>().Clear();
        }
        else if (polvelList[polvelIndex].dateTime <= currentTime)
        {
            // apply position and velocity to objects
            DataLog.Row posvel = polvelList[polvelIndex];
            if (posvel.cols.Count >= 8)
            {
                float[] values = new float[posvel.cols.Count];
                for (int i = 0; i < posvel.cols.Count; i++)
                {
                    values[i] = float.Parse(posvel.cols[i]);
                }

                Vector2 pos1 = new Vector2(values[0], values[1]);
                Vector2 vel1 = new Vector2(values[2], values[3]);
                Vector2 pos2 = new Vector2(values[4], values[5]);
                Vector2 vel2 = new Vector2(values[6], values[7]);
                Vector2 raw_vel1 = new Vector2();
                Vector2 raw_vel2 = new Vector2();
                if (posvel.cols.Count >= 12)
                {
                    // 実際の速度に加えて、エージェントに加える速度が記録されている場合
                    raw_vel1 = new Vector2(values[4], values[5]);
                    pos2 = new Vector2(values[6], values[7]);
                    vel2 = new Vector2(values[8], values[9]);
                    raw_vel2 = new Vector2(values[10], values[11]);
                }               

                rbAgent1.position = pos1;
                rbAgent1.velocity = vel1;
                rbAgent2.position = pos2;
                rbAgent2.velocity = vel2;

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
                else
                {
                    if (posvel.cols.Count >= 12)
                    {
                        // エージェントに加える速度を矢印で表示
                        Rigidbody2D rbArrow1 = arrow1.transform.GetComponent<Rigidbody2D>();
                        rbArrow1.position = new Vector2(pos1.x * 0.85f + pos2.x * 0.15f, pos1.y * 0.85f + pos2.y * 0.15f);
                        rbArrow1.velocity = raw_vel1;

                        Rigidbody2D rbArrow2 = arrow2.transform.GetComponent<Rigidbody2D>();
                        rbArrow2.position = new Vector2(pos1.x * 0.15f + pos2.x * 0.85f, pos1.y * 0.15f + pos2.y * 0.85f);
                        rbArrow2.velocity = raw_vel2;
                    }
                }
                
                if (AppSettings.ShowRouteHistory)
                {
                    if (histLineAgent1.positionCount == 0)
                    {
                        prevPosAgent1 = pos1;
                        histLineAgent1.startWidth = 0.005f * AppSettings.cameraSize;
                        histLineAgent1.endWidth = histLineAgent1.startWidth;
                        histLineAgent1.positionCount = 1;
                        histLineAgent1.SetPosition(0, new Vector3(pos1.x, pos1.y, -0.1f));
                    }
                    if (pos1 != prevPosAgent1)
                    {
                        histLineAgent1.positionCount += 1;
                        histLineAgent1.SetPosition(histLineAgent1.positionCount - 1, new Vector3(pos1.x, pos1.y, -0.1f));
                        prevPosAgent1 = pos1;
                    }

                    if (histLineAgent2.positionCount == 0)
                    {
                        prevPosAgent2 = pos2;
                        histLineAgent2.startWidth = 0.005f * AppSettings.cameraSize;
                        histLineAgent2.endWidth = histLineAgent2.startWidth;
                        histLineAgent2.positionCount = 1;
                        histLineAgent2.SetPosition(0, new Vector3(pos2.x, pos2.y, -0.1f));
                    }
                    if (pos2 != prevPosAgent2)
                    {
                        histLineAgent2.positionCount += 1;
                        histLineAgent2.SetPosition(histLineAgent2.positionCount - 1, new Vector3(pos2.x, pos2.y, -0.1f));
                        prevPosAgent2 = pos2;
                    }
                }
            }
            polvelIndex++;
        }
    }
}
