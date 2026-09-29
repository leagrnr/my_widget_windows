using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MesWidgets;

public class NotesWidget : WidgetWindow
{
    public class Note
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Texte { get; set; } = "";
        public DateTime Modifiee { get; set; } = DateTime.Now;
    }

    readonly List<Note> _notes;
    readonly Grid _vue = new();
    readonly DispatcherTimer _delai = new() { Interval = TimeSpan.FromMilliseconds(800) };
    string _recherche = "";
    Note _ouverte;

    public override string Resume => $"{_notes.Count} note{(_notes.Count > 1 ? "s" : "")}";

    public NotesWidget(WidgetConfig c) : base(c)
    {
        try { _notes = JsonSerializer.Deserialize<List<Note>>(Option("notes", "[]")) ?? new(); }
        catch { _notes = new(); }

        _delai.Tick += (_, _) => Enregistrer();
        Closed += (_, _) => { if (_delai.IsEnabled) Enregistrer(); };

        var pile = new StackPanel { Width = 280 };
        pile.Children.Add(_vue);
        Content = Carte(pile);
        AfficherListe();
    }

    TextBox Champ(string texte, double taille) => new()
    {
        Text = texte,
        Background = Brushes.Transparent,
        Foreground = Blanc,
        CaretBrush = Blanc,
        BorderThickness = new Thickness(0),
        FontSize = taille,
        FontFamily = new FontFamily(Theme.Police),
    };

    void AfficherListe()
    {
        _ouverte = null;
        _vue.Children.Clear();
        var pile = new StackPanel();

        var recherche = Champ(_recherche, 13);
        recherche.Padding = new Thickness(4, 5, 4, 5);
        var indice = Texte("Rechercher…", 13, Pale);
        indice.IsHitTestVisible = false;
        indice.Margin = new Thickness(6, 5, 0, 0);
        indice.Visibility = _recherche.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        var liste = new StackPanel();
        recherche.TextChanged += (_, _) =>
        {
            _recherche = recherche.Text;
            indice.Visibility = _recherche.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            RemplirListe(liste);
        };
        var zone = new Grid();
        zone.Children.Add(recherche);
        zone.Children.Add(indice);
        var loupe = Glyphe("", 12, Pale);
        loupe.Margin = new Thickness(8, 0, 2, 0);
        var barre = new DockPanel();
        DockPanel.SetDock(loupe, Dock.Left);
        barre.Children.Add(loupe);
        barre.Children.Add(zone);

        var nouvelle = Bouton(Glyphe("", 14), Nouvelle, 32);
        nouvelle.ToolTip = "Nouvelle note";
        nouvelle.Margin = new Thickness(8, 0, 0, 0);
        var entete = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(nouvelle, Dock.Right);
        entete.Children.Add(nouvelle);
        entete.Children.Add(new Border { Child = barre, Background = Theme.Piste, CornerRadius = new CornerRadius(8) });

        pile.Children.Add(Titre("Notes"));
        pile.Children[0].SetValue(MarginProperty, new Thickness(0, 0, 0, 8));
        pile.Children.Add(entete);
        pile.Children.Add(liste);
        _vue.Children.Add(pile);
        RemplirListe(liste);
    }

    void RemplirListe(StackPanel liste)
    {
        liste.Children.Clear();
        var trouvees = _notes
            .Where(n => _recherche.Length == 0 || n.Texte.Contains(_recherche, StringComparison.CurrentCultureIgnoreCase))
            .OrderByDescending(n => n.Modifiee)
            .Take(8)
            .ToList();

        if (trouvees.Count == 0)
        {
            liste.Children.Add(Texte(_notes.Count == 0 ? "Aucune note. Clique sur + pour commencer." : "Aucune note trouvée.", 13, Pale));
            return;
        }

        foreach (var n in trouvees)
        {
            var lignes = n.Texte.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
            var titre = Texte(lignes.FirstOrDefault() ?? "(note vide)", 14);
            titre.FontWeight = FontWeights.SemiBold;
            titre.TextTrimming = TextTrimming.CharacterEllipsis;
            var apercu = Texte(lignes.Length > 1 ? lignes[1] : "", 12, Pale);
            apercu.TextTrimming = TextTrimming.CharacterEllipsis;
            var date = Texte(n.Modifiee.Date == DateTime.Today ? n.Modifiee.ToString("HH:mm") : n.Modifiee.ToString("d MMM", Fr), 11, Pale);

            var haut = new DockPanel();
            DockPanel.SetDock(date, Dock.Right);
            date.Margin = new Thickness(8, 0, 0, 0);
            haut.Children.Add(date);
            haut.Children.Add(titre);
            var bloc = new StackPanel();
            bloc.Children.Add(haut);
            if (apercu.Text.Length > 0) bloc.Children.Add(apercu);

            var note = n;
            liste.Children.Add(Cliquable(new Border
            {
                Child = bloc,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 5, 8, 6),
                Margin = new Thickness(-8, 0, -8, 0),
                Background = Brushes.Transparent,
            }, () => Ouvrir(note)));
        }
    }

    void Nouvelle()
    {
        var n = new Note();
        _notes.Add(n);
        Ouvrir(n);
    }

    void Ouvrir(Note n)
    {
        _ouverte = n;
        _vue.Children.Clear();

        var retour = Bouton(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { Glyphe("", 12), new TextBlock { Text = "  Notes", Foreground = Blanc, FontSize = 13 } },
        }, Fermer);
        retour.Margin = new Thickness(-10, 0, 0, 0);
        var supprimer = Bouton(Glyphe("", 14, Pale), () =>
        {
            if (n.Texte.Trim().Length > 0 && MessageBox.Show("Supprimer cette note ?", "Notes", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            _notes.Remove(n);
            Enregistrer();
            AfficherListe();
        }, 30);
        supprimer.ToolTip = "Supprimer la note";

        var entete = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        DockPanel.SetDock(supprimer, Dock.Right);
        entete.Children.Add(supprimer);
        entete.Children.Add(retour);

        var editeur = Champ(n.Texte, 14);
        editeur.AcceptsReturn = true;
        editeur.TextWrapping = TextWrapping.Wrap;
        editeur.Height = 220;
        editeur.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        editeur.TextChanged += (_, _) =>
        {
            n.Texte = editeur.Text;
            n.Modifiee = DateTime.Now;
            _delai.Stop();
            _delai.Start();
        };

        var pile = new StackPanel();
        pile.Children.Add(entete);
        pile.Children.Add(editeur);
        _vue.Children.Add(pile);
        editeur.Loaded += (_, _) => { Activate(); editeur.Focus(); editeur.CaretIndex = editeur.Text.Length; };
    }

    void Fermer()
    {
        if (_ouverte != null && string.IsNullOrWhiteSpace(_ouverte.Texte)) _notes.Remove(_ouverte);
        Enregistrer();
        AfficherListe();
    }

    void Enregistrer()
    {
        _delai.Stop();
        SetOption("notes", JsonSerializer.Serialize(_notes));
        App.Instance.Notifier();
    }

    protected override void RemplirMenu() => Item("Nouvelle note", Nouvelle);

    protected override bool ConfirmerSuppression() =>
        _notes.Count == 0 ||
        MessageBox.Show($"Supprimer ce carnet et ses {_notes.Count} note(s) ?", "Mes Widgets",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
