using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace MesWidgets;

public class PressePapiersWidget : WidgetWindow
{
    const int WM_CLIPBOARDUPDATE = 0x031D;
    const int Maximum = 10;

    readonly List<string> _historique = new();
    readonly StackPanel _liste = new();
    readonly TextBlock _titre;
    bool _pause;

    public override string Resume => _pause ? "En pause" : $"{_historique.Count} élément(s)";

    public PressePapiersWidget(WidgetConfig c) : base(c)
    {
        _titre = Titre("Presse-papiers");
        _titre.Margin = new Thickness(0, 0, 0, 6);
        var pile = new StackPanel { Width = 270 };
        pile.Children.Add(_titre);
        pile.Children.Add(_liste);
        Content = Carte(pile);
        Construire();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        AddClipboardFormatListener(Hwnd);
        HwndSource.FromHwnd(Hwnd).AddHook((IntPtr hwnd, int msg, IntPtr w, IntPtr l, ref bool traite) =>
        {
            if (msg == WM_CLIPBOARDUPDATE && !_pause) Dispatcher.InvokeAsync(() => Capturer(0), DispatcherPriority.Background);
            return IntPtr.Zero;
        });
        Closed += (_, _) => RemoveClipboardFormatListener(Hwnd);
    }

    void Capturer(int essai)
    {
        try
        {
            if (Clipboard.ContainsData("ExcludeClipboardContentFromMonitorProcessing")) return;
            if (Clipboard.GetData("CanIncludeInClipboardHistory") is System.IO.MemoryStream m && m.Length >= 4 && m.ToArray()[0] == 0) return;
            if (!Clipboard.ContainsText()) return;

            var texte = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(texte)) return;
            _historique.Remove(texte);
            _historique.Insert(0, texte);
            if (_historique.Count > Maximum) _historique.RemoveAt(Maximum);
            Construire();
        }
        catch (COMException) when (essai < 3)
        {
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            t.Tick += (_, _) => { t.Stop(); Capturer(essai + 1); };
            t.Start();
        }
        catch { }
    }

    void Construire()
    {
        _liste.Children.Clear();
        if (_historique.Count == 0)
        {
            var vide = Texte(_pause ? "En pause." : "Copie du texte (Ctrl+C) : il apparaîtra ici.", 13, Pale);
            vide.TextWrapping = TextWrapping.Wrap;
            _liste.Children.Add(vide);
            return;
        }

        foreach (var texte in _historique)
        {
            var apercu = texte.Replace("\r", "").Replace('\n', ' ').Replace('\t', ' ').Trim();
            var ligne = Texte(apercu, 13);
            ligne.TextTrimming = TextTrimming.CharacterEllipsis;

            var t = texte;
            var bloc = Cliquable(new Border
            {
                Child = ligne,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 5),
                Margin = new Thickness(-8, 0, -8, 0),
                Background = Brushes.Transparent,
                ToolTip = t.Length > 400 ? t[..400] + "…" : t,
            }, () => Recopier(t));
            _liste.Children.Add(bloc);
        }
    }

    void Recopier(string texte)
    {
        try { Clipboard.SetText(texte); } catch { return; }
        _titre.Text = "Copié ✓";
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2) };
        t.Tick += (_, _) => { t.Stop(); _titre.Text = "Presse-papiers"; };
        t.Start();
    }

    protected override void RemplirMenu()
    {
        Coche("Mettre en pause", _pause, oui => { _pause = oui; Construire(); App.Instance.Notifier(); });
        Item("Effacer l'historique", () => { _historique.Clear(); Construire(); App.Instance.Notifier(); });
    }

    [DllImport("user32.dll")] static extern bool AddClipboardFormatListener(IntPtr fenetre);
    [DllImport("user32.dll")] static extern bool RemoveClipboardFormatListener(IntPtr fenetre);
}
