using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MesWidgets;

public class TachesWidget : WidgetWindow
{
    public class Tache
    {
        public string Texte { get; set; }
        public bool Fait { get; set; }
    }

    readonly List<Tache> _taches;
    readonly StackPanel _liste = new();
    readonly TextBlock _titre, _compte;
    readonly TextBox _saisie;

    public override string Resume =>
        _taches.Count == 0 ? "(vide)" : $"{_taches.Count(t => t.Fait)} / {_taches.Count} faites";

    public TachesWidget(WidgetConfig c) : base(c)
    {
        try { _taches = JsonSerializer.Deserialize<List<Tache>>(Option("taches", "[]")) ?? new(); }
        catch { _taches = new(); }

        var entete = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        _titre = Titre(Option("titre", "Mes tâches"));
        _compte = Texte("", 13, Pale);
        _compte.HorizontalAlignment = HorizontalAlignment.Right;
        _compte.VerticalAlignment = VerticalAlignment.Center;
        entete.Children.Add(_titre);
        entete.Children.Add(_compte);

        _saisie = new TextBox
        {
            Background = Brushes.Transparent,
            Foreground = Blanc,
            CaretBrush = Blanc,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 6, 6, 6),
            FontSize = 13,
        };
        var indice = Texte("+ Ajouter une tâche (Entrée)", 13, Pale);
        indice.Margin = new Thickness(9, 6, 0, 0);
        indice.IsHitTestVisible = false;
        _saisie.TextChanged += (_, _) => indice.Visibility = _saisie.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        _saisie.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter || string.IsNullOrWhiteSpace(_saisie.Text)) return;
            _taches.Add(new Tache { Texte = _saisie.Text.Trim() });
            _saisie.Clear();
            Enregistrer();
        };
        var zone = new Grid();
        zone.Children.Add(_saisie);
        zone.Children.Add(indice);

        var pile = new StackPanel { Width = 250 };
        pile.Children.Add(entete);
        pile.Children.Add(_liste);
        pile.Children.Add(new Border { Child = zone, CornerRadius = new CornerRadius(8), Background = Theme.Piste, Margin = new Thickness(0, 8, 0, 0) });
        Content = Carte(pile);

        Construire();
    }

    void Construire()
    {
        _liste.Children.Clear();
        foreach (var t in _taches.ToList())
        {
            var ligne = new DockPanel { Background = Brushes.Transparent, Margin = new Thickness(0, 1, 0, 1) };

            var suppr = Bouton(Glyphe("", 10, Pale), () => { _taches.Remove(t); Enregistrer(); }, 22);
            suppr.Opacity = 0;
            suppr.ToolTip = "Supprimer";
            ligne.MouseEnter += (_, _) => suppr.Opacity = 1;
            ligne.MouseLeave += (_, _) => suppr.Opacity = 0;
            DockPanel.SetDock(suppr, Dock.Right);
            ligne.Children.Add(suppr);

            var caseCoche = new Border
            {
                Width = 18, Height = 18,
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(t.Fait ? 0 : 1.5),
                BorderBrush = Pale,
                Background = t.Fait ? Accent : Brushes.Transparent,
                Margin = new Thickness(0, 1, 10, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = t.Fait ? Glyphe("", 11, Brushes.White) : null,
            };
            var texte = Texte(t.Texte, 14, t.Fait ? Pale : Blanc);
            texte.TextWrapping = TextWrapping.Wrap;
            if (t.Fait) texte.TextDecorations = TextDecorations.Strikethrough;

            var contenu = new DockPanel();
            contenu.Children.Add(caseCoche);
            contenu.Children.Add(texte);
            var zone = Cliquable(new Border { Child = contenu, Background = Brushes.Transparent, Padding = new Thickness(2, 4, 2, 4), CornerRadius = new CornerRadius(6) },
                                 () => { t.Fait = !t.Fait; Enregistrer(); });
            ligne.Children.Add(zone);
            _liste.Children.Add(ligne);
        }
        _compte.Text = _taches.Count == 0 ? "" : $"{_taches.Count(t => t.Fait)}/{_taches.Count}";
    }

    void Enregistrer()
    {
        SetOption("taches", JsonSerializer.Serialize(_taches));
        Construire();
        App.Instance.Notifier();
    }

    protected override void RemplirMenu()
    {
        Item("Retirer les tâches terminées", () => { _taches.RemoveAll(t => t.Fait); Enregistrer(); });
        Item("Renommer la liste…", () =>
        {
            var nom = Saisie.Demander("Liste de tâches", "Nom de la liste :", _titre.Text);
            if (nom == null) return;
            _titre.Text = nom;
            SetOption("titre", nom);
        });
        Item("Nouvelle liste", () => App.Instance.Ajouter("taches"));
    }

    protected override bool ConfirmerSuppression() =>
        _taches.Count == 0 ||
        MessageBox.Show("Supprimer cette liste et ses tâches ?", "Mes Widgets",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
