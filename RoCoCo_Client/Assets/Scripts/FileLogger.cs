using System;
using System.IO;
using System.Text;
using UnityEngine;

public class FileLogger : ILogHandler
{
    StreamWriter writer;
    bool isDebugLog;
    object lockObj = new object(); // for thread safe

    public static ILogger Create(string filePath, bool isDebugLog = false)
    {
        FileLogger logger = new FileLogger(filePath, isDebugLog);
        return new Logger(logger);
    }

    private FileLogger(string filePath, bool isDebugLog)
    {
        this.isDebugLog = isDebugLog;
        try
        {
            this.writer = File.AppendText(filePath);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    // no output
    public static ILogger CreateDummy()
    {
        FileLogger logger = new FileLogger();
        return new Logger(logger);
    }

    private FileLogger()
    {
    }
    
    public void Close()
    {
        if (writer == null) return;
        writer.Close();
        writer = null;
    }

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        if (isDebugLog)
        {
            Debug.unityLogger.LogFormat(logType, context, format, args);
        }

        if (writer == null) return;
 
        StringBuilder sb = new StringBuilder();
        sb.Append(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));
        sb.Append(" [");
        sb.Append(logType.ToString());
        sb.Append("] ");
        sb.Append(string.Format(format, args));

        lock (lockObj)
        {
            writer.WriteLine(sb.ToString());
        }
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        if (isDebugLog)
        {
            Debug.unityLogger.LogException(exception, context);
        }

        if (writer == null) return;

        StringBuilder sb = new StringBuilder();
        sb.Append(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff"));
        sb.Append(" [");
        sb.Append(LogType.Exception.ToString());
        sb.Append("] ");
        sb.Append(exception.Message);
        sb.Append(" ");
        sb.Append(exception.StackTrace);

        lock (lockObj)
        {
            writer.WriteLine(sb.ToString());
        }
    }
}

