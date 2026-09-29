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

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor, rcWork;
        public uint dwFlags;
    }

    const int WM_WINDOWPOSCHANGING = 0x0046, WM_MOVING = 0x0216;
    const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010;
    const uint MONITOR_DEFAULTTONEAREST = 2;
    const double MargeTransparente = 6;

    [DllImport("user32.dll")] static extern IntPtr MonitorFromRect(ref RECT r, uint flags);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(POINT p, uint flags);
    [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr moniteur, ref MONITORINFO infos);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr h);

    public static IntPtr GarderAuFond(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool traite)
    {
        if (message == WM_MOVING)
        {
            var r = Marshal.PtrToStructure<RECT>(lParam);
            GetCursorPos(out var souris);
            Marshal.StructureToPtr(Borner(hwnd, r, MonitorFromPoint(souris, MONITOR_DEFAULTTONEAREST)), lParam, false);
            traite = true;
            return new IntPtr(1);
        }

        if (message == WM_WINDOWPOSCHANGING)
        {
            var pos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
            bool modifie = false;

            if ((pos.flags & SWP_NOZORDER) == 0 && pos.hwndInsertAfter != new IntPtr(1))
            {
                pos.hwndInsertAfter = new IntPtr(1);
                modifie = true;
            }

            if ((pos.flags & SWP_NOMOVE) == 0 || (pos.flags & SWP_NOSIZE) == 0)
            {
                GetWindowRect(hwnd, out var actuel);
                int x = (pos.flags & SWP_NOMOVE) != 0 ? actuel.Left : pos.x;
                int y = (pos.flags & SWP_NOMOVE) != 0 ? actuel.Top : pos.y;
                int l = (pos.flags & SWP_NOSIZE) != 0 ? actuel.Right - actuel.Left : pos.cx;
                int h = (pos.flags & SWP_NOSIZE) != 0 ? actuel.Bottom - actuel.Top : pos.cy;
                if (l > 0 && h > 0)
                {
                    var r = new RECT { Left = x, Top = y, Right = x + l, Bottom = y + h };
                    var b = Borner(hwnd, r, MonitorFromRect(ref r, MONITOR_DEFAULTTONEAREST));
                    if (b.Left != x || b.Top != y)
                    {
                        pos.x = b.Left;
                        pos.y = b.Top;
                        if ((pos.flags & SWP_NOSIZE) != 0) { pos.cx = l; pos.cy = h; }
                        pos.flags &= ~SWP_NOMOVE;
                        modifie = true;
                    }
                }
            }

            if (modifie) Marshal.StructureToPtr(pos, lParam, false);
        }
        return IntPtr.Zero;
    }

    public static void RamenerDansEcran(IntPtr hwnd)
    {
        GetWindowRect(hwnd, out var r);
        var b = Borner(hwnd, r, MonitorFromRect(ref r, MONITOR_DEFAULTTONEAREST));
        if (b.Left != r.Left || b.Top != r.Top)
            SetWindowPos(hwnd, IntPtr.Zero, b.Left, b.Top, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    static RECT Borner(IntPtr hwnd, RECT r, IntPtr moniteur)
    {
        var infos = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(moniteur, ref infos)) return r;
        uint dpi = GetDpiForWindow(hwnd);
        int marge = (int)Math.Round(MargeTransparente * (dpi == 0 ? 96 : dpi) / 96.0);
        var zone = infos.rcWork;
        int largeur = r.Right - r.Left, hauteur = r.Bottom - r.Top;
        int x = Math.Clamp(r.Left, zone.Left - marge, Math.Max(zone.Left - marge, zone.Right + marge - largeur));
        int y = Math.Clamp(r.Top, zone.Top - marge, Math.Max(zone.Top - marge, zone.Bottom + marge - hauteur));
        return new RECT { Left = x, Top = y, Right = x + largeur, Bottom = y + hauteur };
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
