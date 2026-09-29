using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MesWidgets;

class ChoixApplications : Window
{
    readonly TextBox _recherche;
    readonly StackPanel _liste = new();
    readonly TextBlock _etat;
    readonly Button _ajouter;
    readonly HashSet<Applications.Appli> _choisies = new();
    List<Applications.Appli> _toutes = new();

    public List<Applications.Appli> Choisies => _choisies.OrderBy(a => a.Nom).ToList();

    ChoixApplications()
    {
        Title = "Ajouter des applications";
        Width = 440;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Topmost = true;
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 14;
        Background = new SolidColorBrush(Color.FromRgb(0xF4, 0xF5, 0xF7));

        _recherche = new TextBox { Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 0, 10) };
        _recherche.TextChanged += (_, _) => Remplir();
        _etat = new TextBlock { Text = "Chargement des applications…", Foreground = Brushes.Gray, Margin = new Thickness(4) };

        _ajouter = new Button { Content = "Ajouter", IsDefault = true, IsEnabled = false, Width = 130, Padding = new Thickness(6, 5, 6, 5) };
        _ajouter.Click += (_, _) => DialogResult = true;
        var annuler = new Button { Content = "Annuler", IsCancel = true, Width = 90, Padding = new Thickness(6, 5, 6, 5), Margin = new Thickness(8, 0, 0, 0) };
        var boutons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        boutons.Children.Add(_ajouter);
        boutons.Children.Add(annuler);

        var defilement = new ScrollViewer
        {
            Content = new StackPanel { Children = { _etat, _liste } },
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        var cadre = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE3, 0xE5, 0xE8)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(4),
            Child = defilement,
        };

        var racine = new DockPanel { Margin = new Thickness(16) };
        var aide = new TextBlock { Text = "Coche une ou plusieurs applications :", Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(aide, Dock.Top);
        DockPanel.SetDock(_recherche, Dock.Top);
        DockPanel.SetDock(boutons, Dock.Bottom);
        racine.Children.Add(aide);
        racine.Children.Add(_recherche);
        racine.Children.Add(boutons);
        racine.Children.Add(cadre);
        Content = racine;

        Loaded += async (_, _) =>
        {
            _recherche.Focus();
            try { _toutes = await Applications.Lister(); }
            catch { _etat.Text = "Impossible de lire la liste des applications."; return; }
            Remplir();
        };
    }

    public static List<Applications.Appli> Demander()
    {
        var f = new ChoixApplications();
        return f.ShowDialog() == true ? f.Choisies : new();
    }

    void Remplir()
    {
        var texte = _recherche.Text.Trim();
        var trouvees = _toutes.Where(a => texte.Length == 0 || a.Nom.Contains(texte, StringComparison.CurrentCultureIgnoreCase)).ToList();
        _etat.Text = trouvees.Count == 0 ? "Aucune application trouvée." : "";
        _etat.Visibility = trouvees.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        _liste.Children.Clear();
        foreach (var a in trouvees)
        {
            var ligne = new StackPanel { Orientation = Orientation.Horizontal };
            ligne.Children.Add(new Image { Source = a.Icone, Width = 24, Height = 24, Margin = new Thickness(4, 0, 10, 0) });
            ligne.Children.Add(new TextBlock { Text = a.Nom, VerticalAlignment = VerticalAlignment.Center });

            var appli = a;
            var coche = new CheckBox
            {
                Content = ligne,
                IsChecked = _choisies.Contains(a),
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4, 5, 4, 5),
                Margin = new Thickness(4, 0, 4, 0),
                Cursor = Cursors.Hand,
            };
            coche.Checked += (_, _) => { _choisies.Add(appli); MettreAJourBouton(); };
            coche.Unchecked += (_, _) => { _choisies.Remove(appli); MettreAJourBouton(); };
            _liste.Children.Add(coche);
        }
    }

    void MettreAJourBouton()
    {
        _ajouter.IsEnabled = _choisies.Count > 0;
        _ajouter.Content = _choisies.Count <= 1 ? "Ajouter" : $"Ajouter ({_choisies.Count})";
    }
}
