using System.Runtime.InteropServices;

namespace Batuta.Motor;

/// <summary>As chamadas do Windows que o motor usa (user32, kernel32, hid).</summary>
public static unsafe class Nativo
{
    public const int WM_INPUT = 0x00FF, WM_INPUT_DEVICE_CHANGE = 0x00FE, WM_TIMER = 0x0113, WM_APP = 0x8000;
    public const int WM_BATUTA_DECIDIR = WM_APP + 0x51, WM_BATUTA_TAREFA = WM_APP + 0x52, WM_QUIT = 0x0012;
    public const uint RIDEV_INPUTSINK = 0x100, RIDEV_DEVNOTIFY = 0x2000, RIDEV_REMOVE = 0x1;
    public const uint RID_INPUT = 0x10000003, RIDI_DEVICENAME = 0x20000007, RIDI_DEVICEINFO = 0x2000000b;
    public const uint RIM_TYPEMOUSE = 0, RIM_TYPEKEYBOARD = 1, RIM_TYPEHID = 2;
    public const ushort RI_KEY_BREAK = 1, RI_KEY_E0 = 2;
    public const int WH_KEYBOARD = 2, WH_KEYBOARD_LL = 13;
    public const uint LLKHF_INJECTED = 0x10;
    public const uint INPUT_MOUSE = 0, INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_EXTENDEDKEY = 1, KEYEVENTF_KEYUP = 2, KEYEVENTF_UNICODE = 4, KEYEVENTF_SCANCODE = 8;
    public const uint MOUSEEVENTF_LEFTDOWN = 2, MOUSEEVENTF_LEFTUP = 4, MOUSEEVENTF_RIGHTDOWN = 8, MOUSEEVENTF_RIGHTUP = 0x10,
        MOUSEEVENTF_MIDDLEDOWN = 0x20, MOUSEEVENTF_MIDDLEUP = 0x40, MOUSEEVENTF_WHEEL = 0x800;
    public const uint PM_REMOVE = 1, QS_RAWINPUT = 0x400;
    public static readonly IntPtr HWND_MESSAGE = new(-3);

    /// <summary>Marca das teclas que o próprio Batuta manda (o motor e o gancho deixam passar).</summary>
    public const uint MarcaBatuta = 0xBA7A0000;
    /// <summary>Marca do passador simulado n: 0xBA7A5100 + n (prova do motor sem aparelho).</summary>
    public const uint MarcaSimulado = 0xBA7A5100;

    [StructLayout(LayoutKind.Sequential)] public struct RAWINPUTDEVICE { public ushort UsagePage, Usage; public uint Flags; public IntPtr Target; }
    [StructLayout(LayoutKind.Sequential)] public struct RAWINPUTHEADER { public uint Type, Size; public IntPtr Device, WParam; }
    [StructLayout(LayoutKind.Sequential)] public struct RAWKEYBOARD { public ushort MakeCode, Flags, Reserved, VKey; public uint Message, ExtraInformation; }
    [StructLayout(LayoutKind.Sequential)] public struct RAWINPUTDEVICELIST { public IntPtr Device; public uint Type; }
    [StructLayout(LayoutKind.Sequential)] public struct RID_DEVICE_INFO_HID { public uint VendorId, ProductId, VersionNumber; public ushort UsagePage, Usage; }
    [StructLayout(LayoutKind.Explicit)] public struct RID_DEVICE_INFO { [FieldOffset(0)] public uint Size; [FieldOffset(4)] public uint Type; [FieldOffset(8)] public RID_DEVICE_INFO_HID Hid; [FieldOffset(8)] public fixed byte Bruto[24]; }
    [StructLayout(LayoutKind.Sequential)] public struct KBDLLHOOKSTRUCT { public uint Vk, Scan, Flags, Time; public IntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT { public ushort Vk, Scan; public uint Flags, Time; public IntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT { public int Dx, Dy; public int MouseData; public uint Flags, Time; public IntPtr Extra; }
    [StructLayout(LayoutKind.Explicit, Size = 40)] public struct INPUT { [FieldOffset(0)] public uint Type; [FieldOffset(8)] public KEYBDINPUT Ki; [FieldOffset(8)] public MOUSEINPUT Mi; }
    [StructLayout(LayoutKind.Sequential)] public struct MSG { public IntPtr Hwnd; public uint Message; public IntPtr WParam, LParam; public uint Time; public int X, Y; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASSEX { public uint Size, Style; public IntPtr WndProc; public int ClsExtra, WndExtra; public IntPtr Instance, Icon, Cursor, Background; public string? MenuName; public string ClassName; public IntPtr IconSm; }

    public delegate IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr w, IntPtr l);
    public delegate IntPtr HookProc(int code, IntPtr w, IntPtr l);

    [DllImport("user32", SetLastError = true)] public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] d, uint n, uint size);
    [DllImport("user32")] public static extern uint GetRawInputData(IntPtr h, uint cmd, void* data, ref uint size, uint headerSize);
    [DllImport("user32")] public static extern uint GetRawInputDeviceList([Out] RAWINPUTDEVICELIST[]? list, ref uint n, uint size);
    [DllImport("user32", CharSet = CharSet.Unicode)] public static extern uint GetRawInputDeviceInfoW(IntPtr h, uint cmd, void* data, ref uint size);
    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)] public static extern ushort RegisterClassExW(ref WNDCLASSEX c);
    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateWindowExW(uint ex, string cls, string? name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
    [DllImport("user32")] public static extern bool DestroyWindow(IntPtr h);
    [DllImport("user32")] public static extern IntPtr DefWindowProcW(IntPtr h, uint m, IntPtr w, IntPtr l);
    [DllImport("user32")] public static extern int GetMessageW(out MSG m, IntPtr h, uint min, uint max);
    [DllImport("user32")] public static extern bool PeekMessageW(out MSG m, IntPtr h, uint min, uint max, uint remove);
    [DllImport("user32")] public static extern bool TranslateMessage(ref MSG m);
    [DllImport("user32")] public static extern IntPtr DispatchMessageW(ref MSG m);
    [DllImport("user32")] public static extern bool PostMessageW(IntPtr h, uint m, IntPtr w, IntPtr l);
    [DllImport("user32")] public static extern IntPtr SetTimer(IntPtr h, IntPtr id, uint ms, IntPtr proc);
    [DllImport("user32")] public static extern bool KillTimer(IntPtr h, IntPtr id);
    [DllImport("user32")] public static extern uint MsgWaitForMultipleObjects(uint n, IntPtr handles, bool all, uint ms, uint mask);
    [DllImport("user32", SetLastError = true)] public static extern IntPtr SetWindowsHookExW(int id, IntPtr proc, IntPtr mod, uint thread);
    [DllImport("user32", SetLastError = true)] public static extern IntPtr SetWindowsHookExW(int id, HookProc proc, IntPtr mod, uint thread);
    [DllImport("user32")] public static extern bool UnhookWindowsHookEx(IntPtr h);
    [DllImport("user32")] public static extern IntPtr CallNextHookEx(IntPtr h, int code, IntPtr w, IntPtr l);
    [DllImport("user32", SetLastError = true)] public static extern uint SendInput(uint n, INPUT[] i, int size);
    [DllImport("user32")] public static extern uint MapVirtualKeyW(uint code, uint type);
    [DllImport("user32")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32", CharSet = CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr h, char[] s, int max);
    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr LoadLibraryW(string path);
    [DllImport("kernel32", CharSet = CharSet.Ansi)] public static extern IntPtr GetProcAddress(IntPtr mod, string name);
    [DllImport("kernel32")] public static extern IntPtr GetModuleHandleW(IntPtr name);
    [DllImport("kernel32")] public static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32")] public static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32", CharSet = CharSet.Unicode)] public static extern bool QueryFullProcessImageNameW(IntPtr p, uint flags, char[] name, ref uint size);
    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr tpl);
    [DllImport("hid")] public static extern bool HidD_GetProductString(IntPtr h, char[] buf, uint len);
    [DllImport("hid")] public static extern bool HidD_GetManufacturerString(IntPtr h, char[] buf, uint len);

    public static readonly int TamanhoInput = Marshal.SizeOf<INPUT>();
}
