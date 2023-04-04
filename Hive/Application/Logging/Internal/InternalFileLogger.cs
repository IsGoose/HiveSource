using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArmaTools.ArrayParser.DataTypes;

namespace Hive.Application.Logging.Internal;

public class InternalFileLogger : IFileLogger
{
    private object _lock;
    private Dictionary<string, string> LogMap;
    private string[] OverrideLogLevel;

    public void Initialise()
    {

        _lock = new object();
        LogMap = new Dictionary<string, string>
        {
            { "Error", @"\HiveLogs\Error.txt" }, //Error Log
            { "Log", @"\HiveLogs\Log.txt" } //Standard Internal Logging, Subject to LogLevel
        };
        OverrideLogLevel = new[]
        {
            "Error"
        };
        
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        lock (_lock)
        {
            for (var i = 0; i < LogMap.Count; i++)
            {
                var pair = LogMap.ElementAt(i);
                var logPath = pair.Value;
                if (logPath.StartsWith("\\"))
                    logPath = Path.Combine(basePath, logPath.Remove(0, 1));
                var logDirectory = Path.GetDirectoryName(logPath);
                var fileName = Path.GetFileName(logPath);
                if (logDirectory is null or "" || fileName is null or "")
                {
                    logDirectory = Path.Combine(basePath, "HiveLogs");
                    logPath = Path.Combine(logDirectory, $"{pair.Key}.txt");
                }

                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                LogMap[pair.Key] = logPath;
                File.AppendAllText(logPath, $"New Session {DateTime.Now:MM/dd/yyyy HH:mm:ss}\n");
            }
        }
    }

    public void Log(string alias,string log, LogLevel logLevel, ArmaArray wildcards = null)
    {
        if (logLevel < IoC.Configuration.LogLevel && !OverrideLogLevel.Contains(alias))
            return;

        //Yes this Can Infinitely Recurse if you Remove Error from LogMap...
        if (!LogMap.ContainsKey(alias))
        {
            Log("Error",$"Log Alias \"{alias}\" is Not in LogMap",LogLevel.Error);
        }

        var logDir = LogMap[alias];

        lock (_lock)
        {
            File.AppendAllText(logDir,log + "\n");
        }



    }
}