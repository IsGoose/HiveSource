using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Hive.Application.Enums;
using Hive.Application.Extern;

namespace Hive.Application.Logging.Internal;

public class ProcessLogger : IInternalLogger
{
    private InternalFileLogger FileLogger;
    private object _lock;
    private Process _serverProcess;
    private IntPtr _textBoxHandle = IntPtr.Zero;
    public void Initialise()
    {
        _lock = new object();
        FileLogger = new InternalFileLogger();
        FileLogger.Initialise();
        lock (_lock)
        {
            _serverProcess = Process.GetProcessesByName("arma2oaserver")[0];
            var processId = _serverProcess.Id;
            _textBoxHandle = Win32.GetRichEdit(processId);
        }

        this.Debug("ProcessLogger Initialised");
        if (_textBoxHandle != IntPtr.Zero) return;
        FileLogger.Log("Error","Hive could Not Get OA Server MainWindowHandle",LogLevel.Fatal);
        Win32.MessageBox(IntPtr.Zero, "Unable to Get MainWindowHandle", "Internal Hive Error",
            Win32.MB_ICONERROR | Win32.MB_OK);
        //Win32.ExitProcess(1);
    }
    public void Trace(string log) => Log(log,LogLevel.Trace);

    public void Debug(string log) => Log(log,LogLevel.Debug);

    public void Info(string log) => Log(log,LogLevel.Info);
    public void Warn(string log) => Log(log,LogLevel.Warn);

    public void Error(string log) => Log(log,LogLevel.Error);

    public void Fatal(string log) => Log(log,LogLevel.Fatal);

    
    private void Log(string log,LogLevel logLevel)
    {
        Task.Run(() =>
        {
            FileLogger.Log(logLevel is LogLevel.Fatal or LogLevel.Error ? "Error" : "Log",
                $"{DateTime.Now:HH:mm:ss} [{Enum.GetName(typeof(LogLevel), logLevel)}] {log}", logLevel);
            if (logLevel < IoC.Configuration.LogLevel)
                return;

            var colourRef = logLevel switch
            {
                //Grey
                LogLevel.Trace or LogLevel.Debug => new COLORREF(0xBB, 0xBB, 0xBB),
                //Green
                LogLevel.Info => new COLORREF(0x33,0x99,0x33),
                //Orange
                LogLevel.Warn => new COLORREF(0xFF, 0x99, 0x33),
                //Red
                LogLevel.Error or LogLevel.Fatal or _ => new COLORREF(0xFF, 0x00, 0x00)
            };

            //This is All Very Slow, This Should ALWAYS be Passed on to a Background Thread..
            lock (_lock)
            {
                var lineCount = (int)Win32.SendMessage(_textBoxHandle, Win32.EM_GETLINECOUNT, 0, 0);
                var lastPoint = -1;
                while (lineCount > Win32.MAX_LINE_COUNT)
                {
                    FINDTEXTEXA fta;
                    fta.chrg.cpMin = lastPoint + 1;
                    fta.chrg.cpMax = -1;
                    fta.lpstrText = "\r";
                    fta.chrgText.cpMin = -1;
                    fta.chrgText.cpMax = -1;
                    var ftaAlloc = GCHandle.Alloc(fta);
                    if (Win32.SendMessage(_textBoxHandle, Win32.EM_FINDTEXTEX, Win32.FR_DOWN | Win32.FR_MATCHCASE,
                            GCHandle.ToIntPtr(ftaAlloc)) == -1)
                    {
                        ftaAlloc.Free();
                        break;
                    }

                    lastPoint = fta.chrgText.cpMax;
                    lineCount--;
                    ftaAlloc.Free();
                }

                Win32.SendMessage(_textBoxHandle, Win32.WM_SETREDRAW, false, 0);
                Win32.SendMessage(_textBoxHandle, Win32.EM_SETSEL, -1, -1);

                var colourData = new CHARFORMATW
                {
                    crTextColor = colourRef,
                    dwMask = Win32.CFM_COLOR
                };
                colourData.cbSize = (uint)Marshal.SizeOf(colourData);

                if (logLevel is LogLevel.Fatal or LogLevel.Error)
                {
                    colourData.dwMask |= Win32.CFM_BOLD;
                    colourData.dwEffects = Win32.CFE_BOLD;
                }

                var colourPtr = Marshal.AllocHGlobal(Marshal.SizeOf(colourData));
                Marshal.StructureToPtr(colourData, colourPtr, false);

                Win32.SendMessage(_textBoxHandle, Win32.EM_SETCHARFORMAT, Win32.SCF_SELECTION, colourPtr);

                SETTEXTEX text;
                text.flags = 0xFF;
                text.codepage = 1200;
                log = $"{DateTime.Now:H:mm:ss} [{Enum.GetName(typeof(LogLevel), logLevel)}] {log}\r\n";

                var textPtr = Marshal.AllocHGlobal(Marshal.SizeOf(text));
                Marshal.StructureToPtr(text, textPtr, false);
                Win32.SendMessageW(_textBoxHandle, Win32.EM_SETTEXTEX, textPtr, log.ToCharArray());

                Marshal.FreeHGlobal(textPtr);

                Win32.SendMessage(_textBoxHandle, Win32.WM_SETREDRAW, true, 0);
                Win32.SendMessage(_textBoxHandle, Win32.EM_SETSEL, -1, -1);
                Win32.SendMessage(_textBoxHandle, Win32.EM_SCROLLCARET, 0, 0);
                Win32.RedrawWindow(_textBoxHandle, IntPtr.Zero, IntPtr.Zero,
                    RedrawWindowFlags.Erase | RedrawWindowFlags.Frame | RedrawWindowFlags.Invalidate |
                    RedrawWindowFlags.UpdateNow | RedrawWindowFlags.AllChildren);

            }
        });
    }

}