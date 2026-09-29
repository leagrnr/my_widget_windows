using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace MesWidgets;

public class CongesWidget : WidgetWindow
{
    readonly StackPanel _vacances = new(), _feries = new();

    string ZoneScolaire => Option("zone", "C");
    public override string Resume => $"Zone {ZoneScolaire}";

    public CongesWidget(WidgetConfig c) : base(c)
    {
        var pile = new StackPanel { Width = 260 };
        pile.Children.Add(Sous("VACANCES SCOLAIRES · ZONE " + ZoneScolaire));
        pile.Children.Add(_vacances);
        pile.Children.Add(Separateur());
        pile.Children.Add(Sous("JOURS FÉRIÉS"));
        pile.Children.Add(_feries);
        Content = Carte(pile);

        AfficherFeries();
        Loaded += (_, _) => ChargerVacances();
        Minuteur(TimeSpan.FromHours(6), () => { AfficherFeries(); ChargerVacances(); });
    }

    static TextBlock Sous(string texte)
    {
        var t = Texte(texte, 11, Pale);
        t.FontWeight = FontWeights.SemiBold;
        t.Margin = new Thickness(0, 0, 0, 4);
        return t;
    }

    FrameworkElement Ligne(string titre, string detail, int dans, bool enCours = false)
    {
        var badge = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 2, 8, 3),
            VerticalAlignment = VerticalAlignment.Center,
            Background = enCours || dans == 0 ? Accent : Theme.Piste,
            Child = Texte(enCours ? "En cours" : dans switch { 0 => "Aujourd'hui", 1 => "Demain", _ => $"dans {dans} j" }, 12,
                          enCours || dans == 0 ? System.Windows.Media.Brushes.White : Blanc),
        };
        var ligne = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
        DockPanel.SetDock(badge, Dock.Right);
        ligne.Children.Add(badge);
        var infos = new StackPanel();
        var t = Texte(titre, 14);
        t.FontWeight = FontWeights.SemiBold;
        t.TextTrimming = TextTrimming.CharacterEllipsis;
        infos.Children.Add(t);
        infos.Children.Add(Texte(detail, 12, Pale));
        ligne.Children.Add(infos);
        return ligne;
    }

    async void ChargerVacances()
    {
        try
        {
            var aujourdhui = DateTime.Today.ToString("yyyy-MM-dd");
            var url = "https://data.education.gouv.fr/api/explore/v2.1/catalog/datasets/fr-en-calendrier-scolaire/records" +
                      $"?where={Uri.EscapeDataString($"zones=\"Zone {ZoneScolaire}\" and end_date>=\"{aujourdhui}\"")}" +
                      "&order_by=start_date&limit=30&select=description,start_date,end_date,population";
            using var doc = JsonDocument.Parse(await Web.Http.GetStringAsync(url));

            var periodes = doc.RootElement.GetProperty("results").EnumerateArray()
                .Where(r => r.GetProperty("population").GetString() is not "Enseignants")
                .Select(r => (
                    Nom: r.GetProperty("description").GetString(),
                    Debut: DateTimeOffset.Parse(r.GetProperty("start_date").GetString(), Inv).LocalDateTime.Date,
                    Fin: DateTimeOffset.Parse(r.GetProperty("end_date").GetString(), Inv).LocalDateTime.Date))
                .GroupBy(v => (v.Nom, v.Debut)).Select(g => g.First())
                .Take(2)
                .ToList();

            _vacances.Children.Clear();
            foreach (var v in periodes)
            {
                var nom = Majuscule(v.Nom.Replace("Vacances de la ", "").Replace("Vacances de ", "").Replace("Vacances d'", "").Replace("Vacances d’", ""));
                bool enCours = v.Debut <= DateTime.Today;
                var detail = $"du {v.Debut.ToString("d MMM", Fr)} · reprise le {v.Fin.ToString("ddd d MMM", Fr)}";
                _vacances.Children.Add(Ligne(nom, detail, (v.Debut - DateTime.Today).Days, enCours));
            }
            if (periodes.Count == 0) _vacances.Children.Add(Texte("Calendrier pas encore publié.", 13, Pale));
        }
        catch
        {
            _vacances.Children.Clear();
            _vacances.Children.Add(Texte("Indisponible (connexion ?)", 13, Pale));
        }
    }

    void AfficherFeries()
    {
        _feries.Children.Clear();
        var annee = DateTime.Today.Year;
        foreach (var (date, nom) in Feries(annee).Concat(Feries(annee + 1)).Where(f => f.Date >= DateTime.Today).Take(3))
            _feries.Children.Add(Ligne(nom, Majuscule(date.ToString("dddd d MMMM", Fr)), (date - DateTime.Today).Days));
    }

    static IEnumerable<(DateTime Date, string Nom)> Feries(int a)
    {
        var paques = Paques(a);
        return new List<(DateTime, string)>
        {
            (new(a, 1, 1), "Jour de l'an"),
            (paques.AddDays(1), "Lundi de Pâques"),
            (new(a, 5, 1), "Fête du Travail"),
            (new(a, 5, 8), "Victoire 1945"),
            (paques.AddDays(39), "Ascension"),
            (paques.AddDays(50), "Lundi de Pentecôte"),
            (new(a, 7, 14), "Fête nationale"),
            (new(a, 8, 15), "Assomption"),
            (new(a, 11, 1), "Toussaint"),
            (new(a, 11, 11), "Armistice 1918"),
            (new(a, 12, 25), "Noël"),
        }.OrderBy(f => f.Item1);
    }

    static DateTime Paques(int y)
    {
        int a = y % 19, b = y / 100, c = y % 100, d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30, i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int mois = (h + l - 7 * m + 114) / 31, jour = (h + l - 7 * m + 114) % 31 + 1;
        return new DateTime(y, mois, jour);
    }

    protected override void RemplirMenu()
    {
        var menu = SousMenu("Zone scolaire");
        foreach (var (z, villes) in new[] { ("A", "Lyon, Bordeaux, Grenoble…"), ("B", "Lille, Rennes, Marseille…"), ("C", "Paris, Toulouse, Montpellier…") })
            Coche($"Zone {z} ({villes})", z == ZoneScolaire, _ => { SetOption("zone", z); App.Instance.AppliquerStyle(); }, menu);
    }
}
