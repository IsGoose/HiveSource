using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Hive.Application.Enums;

namespace Hive.Application.Extern;

//TODO: Open for Consideration...
/*
 * This Attribute Stops the Run-Time Security Check & Stack Walk. Improves P/Invoke Calls.
 * "Incorrect use can create security weaknesses"
 * See: https://learn.microsoft.com/en-us/dotnet/api/system.security.suppressunmanagedcodesecurityattribute?view=net-7.0
*/ 
[SuppressUnmanagedCodeSecurity]
public static class Win32
{
    public const int PROCESS_VM_WRITE = 0x0020;
    public const int PROCESS_VM_OPERATION = 0x0008;
    public const int PROCESS_WM_READ = 0x0010;
    public const int MAX_LINE_COUNT = 500;
    


    public const int WM_GETTEXT = 0x000D;
    public const int WM_GETTEXTLENGTH = 0x000E;
    public const int WM_USER = 0x0400;
    public const int WM_SETTEXT = 0X000C;
    public const int EM_GETLINECOUNT = 186;
    public const int EM_SETTEXTEX = WM_USER + 97;
    public const int EM_SETCHARFORMAT = WM_USER + 68;
    public const int EM_FINDTEXTEX = WM_USER + 79;
    public const int EM_SCROLLCARET = WM_USER + 49;
    public const int CFM_BOLD = 0x00000001;
    public const int CFM_ITALIC = 0x00000002;
    public const int CFM_UNDERLINE = 0x00000004;
    public const int CFM_STRIKEOUT = 0x00000008;
    public const int CFM_PROTECTED = 0x00000010;
    public const int CFM_LINK = 0x00000020;
    public const int CFM_SIZE = unchecked((int)0x80000000);
    public const int CFM_COLOR = 0x40000000;
    public const int CFM_FACE = 0x20000000;
    public const int CFM_OFFSET = 0x10000000;
    public const int CFM_CHARSET = 0x08000000;
    
    public const int CFE_BOLD = 0x0001;
    public const int CFE_ITALIC = 0x0002;
    public const int CFE_UNDERLINE = 0x0004;
    public const int CFE_STRIKEOUT = 0x0008;
    public const int CFE_PROTECTED = 0x0010;
    public const int CFE_LINK = 0x0020;
    public const int CFE_AUTOCOLOR = 0x40000000;
    public const int yHeightCharPtsMost = 1638;


    public const int SCF_SELECTION = 0x0001;
    public const int SCF_WORD = 0x0002;
    public const int SCF_DEFAULT = 0x0000;
    public const int SCF_ALL = 0x0004;
    public const int SCF_USEUIRULES = 0x0008;

    public const int EM_SETSEL = 0x00B1;
    public const int FR_MATCHCASE = 0x00000004;
    public const int FR_DOWN = 0x00000001;
    public const int WM_SETREDRAW = 0x000B;
    
    public const uint ENABLE_QUICK_EDIT = 0x0040;
    public const uint ENABLE_MOUSE_INPUT = 0x0010;
    
    public const long MB_ICONERROR = 0x00000010L;
    public const long MB_OK = 0x00000000L;

    public static IntPtr GetRichEdit(int processId)
    {
        var getRichEditData = new GetRichEditInfo
        {
            targetProcId = (uint)processId
        };

        var richEditDataPtr = GCHandle.Alloc(getRichEditData);
        EnumWindows(new GetConsoleWindowCallback(GetConsoleWindowProc), GCHandle.ToIntPtr(richEditDataPtr));
        if (getRichEditData.targetConsole == IntPtr.Zero)
        {
            if(richEditDataPtr.IsAllocated)
                richEditDataPtr.Free();
            return IntPtr.Zero;
        }


        EnumChildWindows(getRichEditData.targetConsole, new GetRichEditCallback(GetRichEditProc),
            GCHandle.ToIntPtr(richEditDataPtr));
        if(richEditDataPtr.IsAllocated)
            richEditDataPtr.Free();
        return getRichEditData.targetRichEdit;
    }
    
    private static bool GetConsoleWindowProc(IntPtr handle, IntPtr getRichEditDataPtr)
    {
        var gcHandle = GCHandle.FromIntPtr(getRichEditDataPtr);
        var getRichEditData = (GetRichEditInfo)gcHandle.Target;
        var windowProcessId = 0U;
        GetWindowThreadProcessId(handle, out windowProcessId);
        if (windowProcessId != getRichEditData.targetProcId) return true;

        var classname = new StringBuilder(256);
        GetClassName(handle, classname, classname.Capacity);
        if (classname.ToString() != "#32770") return true;
        getRichEditData.targetConsole = handle;
        return false;
    }
    private static bool GetRichEditProc(IntPtr handle, IntPtr getRichEditDataPtr)
    {
        var gcHandle = GCHandle.FromIntPtr(getRichEditDataPtr);
        var getRichEditData = (GetRichEditInfo)gcHandle.Target;

        var classname = new StringBuilder(256);
        GetClassName(handle, classname, classname.Capacity);

        if (classname.ToString() != "RichEdit20A") return true;
        getRichEditData.targetRichEdit = handle;
        return false;
    }

    [DllImport("kernel32.dll")]
    public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    [DllImport("kernel32")]
    public static extern bool AllocConsole();
    [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr GetStdHandle(int nStdHandle);
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(GetConsoleWindowCallback enumProc, IntPtr lParam);
    [DllImport("user32.dll", SetLastError=true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    private delegate bool GetConsoleWindowCallback(IntPtr hWnd, IntPtr parameter);
    private delegate bool GetRichEditCallback(IntPtr hWnd, IntPtr parameter);
    
    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(IntPtr window, GetRichEditCallback callback, IntPtr i);
    
    
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, bool wParam, int lParam);
    
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SendMessageW(IntPtr hWnd, int msg, IntPtr wParam, char[] lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, RedrawWindowFlags flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    [DllImport("user32.dll",CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr h, string m, string c, long type);
    
    [DllImport("kernel32.dll")]
    public static extern void ExitProcess(uint exitCode);

}