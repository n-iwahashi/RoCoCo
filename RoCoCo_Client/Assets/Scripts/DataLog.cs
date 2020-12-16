using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataLog
{
    StreamWriter writer;
    StreamReader reader;
    const string DATE_TIME_FORMAT = "yyyy/MM/dd HH:mm:ss.fff";
    object lockObj = new object(); // for thread safe
    string outputPrev;

    public class Row
    {
        public DateTime dateTime;
        public List<string> cols = new List<string>();
    }

    public static DataLog Writer(string filePath)
    {
        DataLog dataLog = new DataLog();
        dataLog.OpenWrite(filePath);
        return dataLog;
    }

    public static List<Row> Load(string filePath)
    {
        DataLog dataLog = new DataLog();
        dataLog.OpenRead(filePath);
        return dataLog.Load();
    }

    private DataLog()
    {

    }

    private bool OpenWrite(string filePath)
    {
        try
        {
            this.writer = File.CreateText(filePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        return false;
    }

    private bool OpenRead(string filePath)
    {
        try
        {
            this.reader = File.OpenText(filePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        return false;
    }

    public void Output(List<object> cols)
    {
        if (writer == null) return;

        StringBuilder sb = new StringBuilder();
        foreach(object col in cols)
        {
            sb.Append(",");
            if (col is float)
            {
                float value = (float)col;
                sb.Append(value.ToString("F3"));
            }
            else
            {
                sb.Append(col);
            }
        }

        if (outputPrev == sb.ToString())
        {
            // not output the same string as before.
            return;
        }
        outputPrev = sb.ToString();
        sb.Insert(0, DateTime.Now.ToString(DATE_TIME_FORMAT));

        lock (lockObj)
        {
            writer.WriteLine(sb.ToString());
        }
    }

    public List<Row> Load()
    {
        List<Row> rows = new List<Row>();

        if (reader == null) return rows;

        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine();
            string[] arr = line.Split(',');
            Row row = new Row();
            row.cols.AddRange(arr);
            if (row.cols.Count > 1)
            {
                try
                {
                    row.dateTime = DateTime.ParseExact(row.cols[0], DATE_TIME_FORMAT, null);
                    row.cols.RemoveAt(0);
                    rows.Add(row);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }
        return rows;
    }

    public void Close()
    {
        if (writer != null)
        {
            writer.Close();
            writer = null;
        }
        if (reader != null)
        {
            reader.Close();
            reader = null;
        }
    }
}
