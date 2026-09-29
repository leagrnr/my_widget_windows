using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MesWidgets;

public class RechercheWidget : WidgetWindow
{
    static readonly (string Nom, string Url)[] Moteurs =
    {
        ("Google", "https://www.google.com/search?q="),
        ("DuckDuckGo", "https://duckduckgo.com/?q="),
        ("Bing", "https://www.bing.com/search?q="),
        ("Qwant", "https://www.qwant.com/?q="),
        ("Ecosia", "https://www.ecosia.org/search?q="),
        ("YouTube", "https://www.youtube.com/results?search_query="),
        ("Wikipédia", "https://fr.wikipedia.org/w/index.php?search="),
    };

    readonly TextBox _champ;
    readonly TextBlock _indice;

    string Moteur => Option("moteur", "Google");
    public override string Resume => Moteur;

    public RechercheWidget(WidgetConfig c) : base(c)
    {
        _champ = new TextBox
        {
            Background = Brushes.Transparent,
            Foreground = Blanc,
            CaretBrush = Blanc,
            BorderThickness = new Thickness(0),
            FontSize = 15,
            FontFamily = new FontFamily(Theme.Police),
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        _indice = Texte("", 15, Pale);
        _indice.IsHitTestVisible = false;
        _indice.VerticalAlignment = VerticalAlignment.Center;
        _indice.Margin = new Thickness(2, 0, 0, 0);
        _champ.TextChanged += (_, _) => _indice.Visibility = _champ.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        _champ.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(_champ.Text))
            {
                var url = Moteurs.First(m => m.Nom == Moteur).Url;
                Ouvrir(url + Uri.EscapeDataString(_champ.Text.Trim()));
                _champ.Clear();
            }
            else if (e.Key == Key.Escape) _champ.Clear();
        };

        var zone = new Grid();
        zone.Children.Add(_champ);
        zone.Children.Add(_indice);

        var loupe = Glyphe("", 16, Pale);
        loupe.Margin = new Thickness(0, 0, 12, 0);
        var ligne = new DockPanel { Width = 300 };
        DockPanel.SetDock(loupe, Dock.Left);
        ligne.Children.Add(loupe);
        ligne.Children.Add(zone);

        var carte = Carte(ligne);
        carte.Padding = new Thickness(18, 12, 18, 12);
        carte.CornerRadius = new CornerRadius(Math.Max(Theme.Arrondi, 12) * 1.5);
        Content = carte;
        MettreAJourIndice();
    }

    void MettreAJourIndice() => _indice.Text = $"Rechercher sur {Moteur}…";

    protected override void Clic() { Activate(); _champ.Focus(); }

    protected override void RemplirMenu()
    {
        var menu = SousMenu("Moteur de recherche");
        foreach (var (nom, _) in Moteurs)
            Coche(nom, nom == Moteur, _ => { SetOption("moteur", nom); MettreAJourIndice(); App.Instance.Notifier(); }, menu);
    }
}
