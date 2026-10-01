using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace MesWidgets;

static class RaccourcisClavier
{
    const int WM_HOTKEY = 0x0312;
    const uint MOD_ALT = 0x1, MOD_WIN = 0x8, MOD_NOREPEAT = 0x4000;

    static readonly Dictionary<int, Action> _actions = new();

    public static readonly List<string> Refuses = new();

    public static void Enregistrer()
    {
        Ajouter(2, 'M', "Win+Alt+M", App.Instance.BasculerMicro);
        Ajouter(3, 'H', "Win+Alt+H", App.Instance.BasculerMasquage);

        ComponentDispatcher.ThreadPreprocessMessage += (ref MSG msg, ref bool traite) =>
        {
            if (msg.message == WM_HOTKEY && _actions.TryGetValue((int)msg.wParam, out var action))
            {
                action();
                traite = true;
            }
        };
    }

    static void Ajouter(int id, char touche, string nom, Action action)
    {
        if (RegisterHotKey(IntPtr.Zero, id, MOD_WIN | MOD_ALT | MOD_NOREPEAT, touche)) _actions[id] = action;
        else Refuses.Add(nom);
    }

    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(IntPtr fenetre, int id, uint modificateurs, uint touche);
}
