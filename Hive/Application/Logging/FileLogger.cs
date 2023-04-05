using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ArmaTools.ArrayParser.DataTypes;

namespace Hive.Application.Logging;

public class FileLogger : IFileLogger
{
    private Regex _wildcardCountRegex = new Regex(@"\{\d+\}",RegexOptions.Compiled);
    private object _lock;
    private Dictionary<string, LogMapInfo> LogMap;
    private char[] _invalidPathChars = Path.GetInvalidPathChars();
    private char[] _invalidFilenameChars = Path.GetInvalidFileNameChars();

    public void Initialise()
    {

        _lock = new object();
        LogMap = new Dictionary<string, LogMapInfo>();

        foreach (var logMap in IoC.Configuration.GameLogMap)
        {
            if (logMap.Length != 2)
                throw new IndexOutOfRangeException(
                    "The Arrays in GameLogMap Config Option Must only Contain 2 Elements");

            var alias = logMap[0];
            var path = logMap[1];
            
            var wildcardMatches = _wildcardCountRegex.Matches(path);
            var logPathWildcards = new List<int>();
            foreach (Match match in wildcardMatches)
            {
                var value = match.Groups[0].Value;
                var numStringValue = value.Substring(1, value.Length - 2);
                if(int.TryParse(numStringValue,out var val) && !logPathWildcards.Contains(val))
                    logPathWildcards.Add(val);
            }
            var numberOfWildcards = logPathWildcards.Count;
            LogMap.Add(alias, new LogMapInfo(Path.Combine(IoC.Configuration.GameLogsDirectory,path),numberOfWildcards));
        }
        IoC.InternalLogger.Debug("FileLogger Initialised");
    }

    public void Log(string alias,string log, ArmaArray wildcards = null)
    {
        if (!LogMap.ContainsKey(alias))
        {
            IoC.InternalLogger.Error($"Log Alias \"{alias}\" is Not in GameLogMap (Alias is Case Sensitive!)");
            return;
        }

        var logMapInfo = LogMap[alias];
        wildcards ??= new ArmaArray();
        if (wildcards.Length != logMapInfo.NumberOfWildcards)
        {
            IoC.InternalLogger.Error($"Log Alias \"{alias}\" Wildcard Count Does Not Match Provided Wildcard Count. ({logMapInfo.NumberOfWildcards}:{wildcards.Length})");
            return;
        }

        var formattedLogPath = $"{logMapInfo.FilePath.Format(wildcards)}.txt";
        var logFileDirectory = Path.GetDirectoryName(formattedLogPath);
        var logFileName = Path.GetFileName(formattedLogPath);
        if (string.IsNullOrEmpty(logFileDirectory) || string.IsNullOrEmpty(logFileName) ||
            logFileDirectory.IndexOfAny(_invalidPathChars) >= 0 ||
            logFileName.IndexOfAny(_invalidFilenameChars) >= 0)
        {
            IoC.InternalLogger.Error($"Log Alias \"{alias}\" Log File Path was Invalid: {formattedLogPath}");
            return;
        }

        lock (_lock)
        {
            if (!Directory.Exists(logFileDirectory))
                Directory.CreateDirectory(logFileDirectory);
            
            File.AppendAllText(formattedLogPath,$"{DateTime.Now:HH:mm:ss} {log}\n");
        }
    }
}