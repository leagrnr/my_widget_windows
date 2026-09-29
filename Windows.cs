using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MesWidgets;

static class Bureau
{
    [DllImport("user32.dll")] static extern IntPtr FindWindow(string classe, string titre);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr apres, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr h, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr h, int index, IntPtr valeur);

    const int GWL_EXSTYLE = -20, GWLP_HWNDPARENT = -8;
    const long WS_EX_TOOLWINDOW = 0x80, WS_EX_APPWINDOW = 0x40000;

    public static void Coller(IntPtr h)
    {
        long ex = GetWindowLongPtr(h, GWL_EXSTYLE).ToInt64();
        SetWindowLongPtr(h, GWL_EXSTYLE, new IntPtr((ex | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW));
        SetWindowLongPtr(h, GWLP_HWNDPARENT, FindWindow("Progman", null));
        AuFond(h);
    }

    public static void AuFond(IntPtr h) => SetWindowPos(h, new IntPtr(1), 0, 0, 0, 0, 0x13);

    [StructLayout(LayoutKind.Sequential)]
    struct WINDOWPOS
    {
        public IntPtr hwnd, hwndInsertAfter;
        public int x, y, cx, cy;
        public uint flags;
    }

    const int WM_WINDOWPOSCHANGING = 0x0046;
    const uint SWP_NOZORDER = 0x0004;

    public static IntPtr GarderAuFond(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool traite)
    {
        if (message == WM_WINDOWPOSCHANGING)
        {
            var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
            if ((pos.flags & SWP_NOZORDER) == 0 && pos.hwndInsertAfter != new IntPtr(1))
            {
                pos.hwndInsertAfter = new IntPtr(1);
                Marshal.StructureToPtr(pos, lParam, false);
            }
        }
        return IntPtr.Zero;
    }
}

static class Demarrage
{
    const string Cle = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Nom = "MesWidgets";

    public static bool EstActive
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(Cle);
            return k?.GetValue(Nom) is string s && s.Contains(Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void Activer(bool oui, string exe = null)
    {
        using var k = Registry.CurrentUser.CreateSubKey(Cle);
        if (oui) k.SetValue(Nom, $"\"{exe ?? Environment.ProcessPath}\"");
        else k.DeleteValue(Nom, false);
    }
}
