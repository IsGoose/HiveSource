using System;
using System.IO;
using System.Runtime.InteropServices;
using Hive.Application.Extern;
using Microsoft.Win32.SafeHandles;

namespace Hive.Application.Logging.Internal;

public class ConsoleLogger : IInternalLogger
{
    private InternalFileLogger FileLogger;
    private object _lock;
    public void Trace(string log) => Log(log,LogLevel.Trace);

    public void Debug(string log) => Log(log,LogLevel.Debug);

    public void Info(string log) => Log(log,LogLevel.Info);
    public void Warn(string log) => Log(log,LogLevel.Warn);

    public void Error(string log) => Log(log,LogLevel.Error);

    public void Fatal(string log) => Log(log,LogLevel.Fatal);

    public void Initialise()
    {
        _lock = new object();
        FileLogger = new InternalFileLogger();
        FileLogger.Initialise();
        this.Debug("ConsoleLogger Initialised");
        AllocConsole();
        
        //Get and Set StdOut. This Fixes an Issue where Nothing Can be Outputted to the Console Window @Truece
        var stdHandle = Win32.GetStdHandle(-11);
        var safeFileHandle = new SafeFileHandle(stdHandle, true);
        var fileStream = new FileStream(safeFileHandle, FileAccess.Write);
        var encoding = System.Text.Encoding.UTF8;
        var standardOutput = new StreamWriter(fileStream, encoding);
        standardOutput.AutoFlush = true;
        Console.Clear();
        Console.SetOut(standardOutput);
        Console.SetError(standardOutput);
    }

    private void Log(string log, LogLevel logLevel)
    {
        
        FileLogger.Log(logLevel is LogLevel.Fatal or LogLevel.Error ? "Error" : "Log", $"{DateTime.Now:HH:mm:ss} [{Enum.GetName(typeof(LogLevel), logLevel)}] {log}", logLevel);
        if (logLevel < IoC.Configuration.LogLevel)
            return;
        var consoleColour = logLevel switch
        {
            //Green
            LogLevel.Trace or LogLevel.Debug => ConsoleColor.White,
            LogLevel.Info=> ConsoleColor.Green,
            //Yellow (Since Console Does not Have an Orange Colour)
            LogLevel.Warn => ConsoleColor.Yellow,
            //Red
            LogLevel.Error or LogLevel.Fatal or _ => ConsoleColor.Red
        };
        Console.ForegroundColor = consoleColour;
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} [{Enum.GetName(typeof(LogLevel), logLevel)}] {log}");
        Console.ResetColor();
    }

    [DllImport("kernel32")]
    private static extern bool AllocConsole();
}