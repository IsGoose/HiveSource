using System;

namespace Hive.Application.Extern;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct COLORREF
{
    public COLORREF(byte r, byte g, byte b)
    {
        this.Value = 0;
        this.R = r;
        this.G = g;
        this.B = b;
    }

    public COLORREF(uint value)
    {
        this.R = 0;
        this.G = 0;
        this.B = 0;
        this.Value = value & 0x00FFFFFF;
    }

    [FieldOffset(0)]
    public byte R;
    [FieldOffset(1)]
    public byte G;
    [FieldOffset(2)]
    public byte B;

    [FieldOffset(0)]
    public uint Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct CHARFORMATW
{
    public uint cbSize;
    public uint dwMask;
    public uint dwEffects;
    public int yHeight;
    public int yOffset;
    public COLORREF crTextColor;
    public byte bCharSet;
    public byte bPitchAndFamily;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)]
    public string szFaceName;
}

[StructLayout(LayoutKind.Sequential)]
public struct CHARRANGE
{
    public int cpMin;
    public int cpMax;
}
[StructLayout(LayoutKind.Sequential)]
public struct FINDTEXTEXA
{
    public CHARRANGE chrg;
    public string lpstrText;
    public CHARRANGE chrgText;
}

public struct SETTEXTEX
{
    public uint flags;
    public uint codepage;

}

public class GetRichEditInfo
{
    public uint targetProcId;
    public IntPtr targetConsole;
    public IntPtr targetRichEdit;
}