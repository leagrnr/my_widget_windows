using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Media;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesWidgets;

public class AlarmesWidget : WidgetWindow
{
    public class Alarme
    {
        public string Heure { get; set; } = "07:30";
        public string Libelle { get; set; } = "";
        public string Repetition { get; set; } = "une";
        public bool Active { get; set; } = true;
        public string DerniereSonnerie { get; set; } = "";
    }

    static readonly (string Code, string Nom)[] Repetitions =
        { ("une", "Une seule fois"), ("jours", "Tous les jours"), ("semaine", "Du lundi au vendredi"), ("weekend", "Le week-end") };

    readonly List<Alarme> _alarmes;
    readonly StackPanel _liste = new();
    readonly TextBlock _prochaine;

    public override string Resume => _prochaine.Text;

    public AlarmesWidget(WidgetConfig c) : base(c)
    {
        try { _alarmes = JsonSerializer.Deserialize<List<Alarme>>(Option("alarmes", "[]")) ?? new(); }
        catch { _alarmes = new(); }

        _prochaine = Texte("", 12, Pale);
        _prochaine.Margin = new Thickness(0, 0, 0, 8);
        var entete = new DockPanel();
        var ajouter = Bouton(Glyphe("", 14), Ajouter, 30);
        ajouter.ToolTip = "Nouvelle alarme";
        DockPanel.SetDock(ajouter, Dock.Right);
        entete.Children.Add(ajouter);
        entete.Children.Add(Titre("Alarmes"));

        var pile = new StackPanel { Width = 260 };
        pile.Children.Add(entete);
        pile.Children.Add(_prochaine);
        pile.Children.Add(_liste);
        Content = Carte(pile);

        Construire();
        Minuteur(TimeSpan.FromSeconds(10), Verifier);
        Minuteur(TimeSpan.FromMinutes(1), MettreAJourProchaine);
    }

    static bool SonneCeJour(Alarme a, DateTime jour) => a.Repetition switch
    {
        "semaine" => jour.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
        "weekend" => jour.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
        _ => true,
    };

    static DateTime? Prochaine(Alarme a, DateTime maintenant)
    {
        if (!a.Active || !TimeSpan.TryParseExact(a.Heure, @"hh\:mm", CultureInfo.InvariantCulture, out var heure)) return null;
        for (int j = 0; j < 8; j++)
        {
            var jour = maintenant.Date.AddDays(j);
            var moment = jour + heure;
            if (moment > maintenant && SonneCeJour(a, jour)) return moment;
        }
        return null;
    }

    void Verifier()
    {
        var maintenant = DateTime.Now;
        var cle = maintenant.ToString("yyyy-MM-dd HH:mm");
        bool change = false;
        foreach (var a in _alarmes.Where(a => a.Active && a.Heure == maintenant.ToString("HH:mm") && a.DerniereSonnerie != cle && SonneCeJour(a, maintenant)))
        {
            a.DerniereSonnerie = cle;
            if (a.Repetition == "une") a.Active = false;
            change = true;
            SystemSounds.Exclamation.Play();
            App.Instance.Notification("⏰ " + a.Heure, string.IsNullOrWhiteSpace(a.Libelle) ? "C'est l'heure !" : a.Libelle);
        }
        if (change) Enregistrer();
    }

    void MettreAJourProchaine()
    {
        var maintenant = DateTime.Now;
        var suivante = _alarmes.Select(a => Prochaine(a, maintenant)).Where(d => d != null).Min();
        if (suivante is not { } d) { _prochaine.Text = _alarmes.Count == 0 ? "Aucune alarme" : "Aucune alarme active"; return; }
        var ecart = d - maintenant;
        var quand = d.Date == maintenant.Date ? "aujourd'hui" : d.Date == maintenant.Date.AddDays(1) ? "demain" : d.ToString("dddd", Fr);
        _prochaine.Text = $"Prochaine : {quand} à {d:HH:mm} (dans {(ecart.TotalHours >= 1 ? $"{(int)ecart.TotalHours} h {ecart.Minutes:00}" : $"{Math.Max(1, (int)Math.Ceiling(ecart.TotalMinutes))} min")})";
    }

    void Construire()
    {
        _liste.Children.Clear();
        foreach (var a in _alarmes.OrderBy(x => x.Heure))
        {
            var alarme = a;
            var heure = Texte(a.Heure, 24, a.Active ? Blanc : Pale, "Segoe UI Light");
            heure.Margin = new Thickness(0, 0, 12, 0);
            heure.VerticalAlignment = VerticalAlignment.Center;
            var textes = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var libelle = Texte(string.IsNullOrWhiteSpace(a.Libelle) ? "Alarme" : a.Libelle, 13, a.Active ? Blanc : Pale);
            libelle.TextTrimming = TextTrimming.CharacterEllipsis;
            textes.Children.Add(libelle);
            textes.Children.Add(Texte(Repetitions.FirstOrDefault(r => r.Code == a.Repetition).Nom ?? "", 11, Pale));

            var interrupteur = Interrupteur(a.Active, actif => { alarme.Active = actif; Enregistrer(); });
            var suppr = Bouton(Glyphe("", 10, Pale), () => { _alarmes.Remove(alarme); Enregistrer(); }, 22);
            suppr.Opacity = 0;
            suppr.ToolTip = "Supprimer";

            var ligne = new DockPanel { Margin = new Thickness(0, 3, 0, 3), Background = Brushes.Transparent };
            ligne.MouseEnter += (_, _) => suppr.Opacity = 1;
            ligne.MouseLeave += (_, _) => suppr.Opacity = 0;
            DockPanel.SetDock(heure, Dock.Left);
            DockPanel.SetDock(suppr, Dock.Right);
            DockPanel.SetDock(interrupteur, Dock.Right);
            ligne.Children.Add(heure);
            ligne.Children.Add(suppr);
            ligne.Children.Add(interrupteur);
            ligne.Children.Add(textes);
            _liste.Children.Add(ligne);
        }
        if (_alarmes.Count == 0)
            _liste.Children.Add(Texte("Clique sur + pour ajouter une alarme.", 13, Pale));
        MettreAJourProchaine();
    }

    FrameworkElement Interrupteur(bool actif, Action<bool> changement)
    {
        var bouton = new Border
        {
            Width = 16, Height = 16,
            CornerRadius = new CornerRadius(8),
            Background = Brushes.White,
            HorizontalAlignment = actif ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Margin = new Thickness(2),
        };
        var rail = new Border
        {
            Width = 38, Height = 20,
            CornerRadius = new CornerRadius(10),
            Background = actif ? Accent : Theme.Piste,
            Child = bouton,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 4, 0),
            ToolTip = actif ? "Désactiver" : "Activer",
        };
        return Cliquable(rail, () => changement(!actif), survol: false);
    }

    void Ajouter()
    {
        var f = new FormulaireAlarme();
        if (f.ShowDialog() != true) return;
        _alarmes.Add(new Alarme { Heure = f.Heure, Libelle = f.Libelle, Repetition = Repetitions[f.Repetition].Code });
        Enregistrer();
    }

    void Enregistrer()
    {
        SetOption("alarmes", JsonSerializer.Serialize(_alarmes));
        Construire();
        App.Instance.Notifier();
    }

    protected override void RemplirMenu() => Item("Nouvelle alarme…", Ajouter);

    class FormulaireAlarme : Dialogue
    {
        readonly TextBox _heure, _libelle;
        readonly ComboBox _repetition;

        public string Heure { get; private set; }
        public string Libelle => _libelle.Text.Trim();
        public int Repetition => Math.Max(0, _repetition.SelectedIndex);

        public FormulaireAlarme() : base("Nouvelle alarme", 420)
        {
            Grand("Nouvelle alarme");
            Etiquette("Heure");
            _heure = Ajouter(new TextBox { Text = DateTime.Now.AddHours(1).ToString("HH:00") });
            Aide("Par exemple : 07:30 ou 18h45");
            Etiquette("Nom (facultatif)");
            _libelle = Ajouter(new TextBox());
            Etiquette("Répétition");
            _repetition = Ajouter(new ComboBox());
            foreach (var (_, nom) in Repetitions) _repetition.Items.Add(nom);
            _repetition.SelectedIndex = 0;
            Boutons(Secondaire("Annuler"), Principal("Ajouter", () =>
            {
                var texte = _heure.Text.Trim().ToLowerInvariant().Replace('h', ':').Replace('.', ':');
                if (texte.EndsWith(':')) texte += "00";
                if (!TimeSpan.TryParse(texte, CultureInfo.InvariantCulture, out var t) || t < TimeSpan.Zero || t.TotalHours >= 24)
                {
                    MessageBox.Show(this, "Je n'ai pas compris l'heure. Exemple : 07:30", "Alarme");
                    return;
                }
                Heure = $"{t.Hours:00}:{t.Minutes:00}";
                Valider();
            }));
            Loaded += (_, _) => { _heure.Focus(); _heure.SelectAll(); };
        }
    }
}
