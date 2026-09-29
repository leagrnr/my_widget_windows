using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesWidgets;

class ChoixApplications : Dialogue
{
    readonly TextBox _recherche;
    readonly StackPanel _liste = new();
    readonly TextBlock _etat;
    readonly Button _ajouter;
    readonly HashSet<Applications.Appli> _choisies = new();
    List<Applications.Appli> _toutes = new();

    public List<Applications.Appli> Choisies => _choisies.OrderBy(a => a.Nom).ToList();

    ChoixApplications() : base("Ajouter des applications", 480)
    {
        Grand("Ajouter des applications");
        Aide("Coche une ou plusieurs applications, elles seront ajoutées à tes raccourcis.", 0);

        _recherche = new TextBox { Padding = new Thickness(36, 8, 10, 8) };
        var loupe = new TextBlock
        {
            Text = "",
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 14,
            Margin = new Thickness(13, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };
        loupe.SetResourceReference(TextBlock.ForegroundProperty, "DlgTexteDoux");
        var indice = new TextBlock { Text = "Rechercher une application…", Margin = new Thickness(37, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        indice.SetResourceReference(TextBlock.ForegroundProperty, "DlgTexteDoux");
        _recherche.TextChanged += (_, _) =>
        {
            indice.Visibility = _recherche.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            Remplir();
        };
        var zoneRecherche = new Grid { Margin = new Thickness(0, 14, 0, 10) };
        zoneRecherche.Children.Add(_recherche);
        zoneRecherche.Children.Add(loupe);
        zoneRecherche.Children.Add(indice);
        Ajouter(zoneRecherche);

        _etat = new TextBlock { Text = "Chargement des applications…", Margin = new Thickness(12, 10, 12, 10) };
        _etat.SetResourceReference(TextBlock.ForegroundProperty, "DlgTexteDoux");
        var cadre = new Border
        {
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            Child = new ScrollViewer
            {
                Height = 360,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = new StackPanel { Children = { _etat, _liste } },
            },
        };
        cadre.SetResourceReference(Border.BackgroundProperty, "DlgSurface");
        cadre.SetResourceReference(Border.BorderBrushProperty, "DlgBordure");
        Ajouter(cadre);

        _ajouter = Principal("Ajouter", Valider);
        _ajouter.IsEnabled = false;
        Boutons(Secondaire("Annuler"), _ajouter);

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
            ligne.Children.Add(new Image { Source = a.Icone, Width = 26, Height = 26, Margin = new Thickness(0, 0, 12, 0) });
            ligne.Children.Add(new TextBlock { Text = a.Nom, VerticalAlignment = VerticalAlignment.Center });

            var appli = a;
            var coche = new CheckBox { Content = ligne, IsChecked = _choisies.Contains(a), Padding = new Thickness(10, 6, 10, 6) };
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
