using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace MesWidgets;

public class CorbeilleWidget : WidgetWindow
{
    readonly TextBlock _details;
    readonly Border _vider;
    long _elements;

    public override string Resume => _details.Text;

    public CorbeilleWidget(WidgetConfig c) : base(c)
    {
        var icone = Cliquable(new Border
        {
            Width = 52, Height = 52,
            CornerRadius = new CornerRadius(12),
            Background = Theme.Piste,
            Margin = new Thickness(0, 0, 14, 0),
            Child = Glyphe("", 24),
            ToolTip = "Ouvrir la corbeille",
        }, () => Ouvrir("shell:RecycleBinFolder"));

        _details = Texte("", 13, Pale);
        var infos = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        infos.Children.Add(Titre("Corbeille"));
        infos.Children.Add(_details);

        var haut = new DockPanel();
        DockPanel.SetDock(icone, Dock.Left);
        haut.Children.Add(icone);
        haut.Children.Add(infos);

        var libelle = Texte("Vider la corbeille", 13);
        libelle.HorizontalAlignment = HorizontalAlignment.Center;
        _vider = Cliquable(new Border
        {
            Child = libelle,
            CornerRadius = new CornerRadius(8),
            Background = Theme.Piste,
            Padding = new Thickness(10, 6, 10, 7),
            Margin = new Thickness(0, 12, 0, 0),
        }, Vider);

        var pile = new StackPanel { Width = 220 };
        pile.Children.Add(haut);
        pile.Children.Add(_vider);
        Content = Carte(pile);

        MettreAJour();
        Minuteur(TimeSpan.FromSeconds(5), MettreAJour);
    }

    void MettreAJour()
    {
        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
        if (SHQueryRecycleBin(null, ref info) != 0) return;
        _elements = info.i64NumItems;
        _details.Text = _elements == 0 ? "Vide"
            : $"{_elements} élément{(_elements > 1 ? "s" : "")} · {Taille(info.i64Size)}";
        _vider.Opacity = _elements == 0 ? 0.4 : 1;
        _vider.IsHitTestVisible = _elements > 0;
    }

    void Vider()
    {
        SHEmptyRecycleBin(new WindowInteropHelper(this).Handle, null, 0);
        MettreAJour();
        App.Instance.Notifier();
    }

    static string Taille(long octets) => octets switch
    {
        < 1024 * 1024 => string.Format(Fr, "{0:0} Ko", octets / 1024.0),
        < 1024L * 1024 * 1024 => string.Format(Fr, "{0:0} Mo", octets / 1024.0 / 1024),
        _ => string.Format(Fr, "{0:0.0} Go", octets / 1024.0 / 1024 / 1024),
    };

    [StructLayout(LayoutKind.Sequential)]
    struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHQueryRecycleBin(string racine, ref SHQUERYRBINFO info);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHEmptyRecycleBin(IntPtr fenetre, string racine, uint options);
}
