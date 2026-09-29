using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesWidgets;

public class HorlogesMondeWidget : WidgetWindow
{
    static readonly (string Nom, string Zone)[] Villes =
    {
        ("Paris", "Romance Standard Time"),
        ("Londres", "GMT Standard Time"),
        ("New York", "Eastern Standard Time"),
        ("Montréal", "Eastern Standard Time"),
        ("Los Angeles", "Pacific Standard Time"),
        ("Mexico", "Central Standard Time (Mexico)"),
        ("São Paulo", "E. South America Standard Time"),
        ("Martinique", "SA Western Standard Time"),
        ("Dakar", "Greenwich Standard Time"),
        ("Johannesburg", "South Africa Standard Time"),
        ("Moscou", "Russian Standard Time"),
        ("Dubaï", "Arabian Standard Time"),
        ("La Réunion", "Mauritius Standard Time"),
        ("Bombay", "India Standard Time"),
        ("Bangkok", "SE Asia Standard Time"),
        ("Singapour", "Singapore Standard Time"),
        ("Pékin", "China Standard Time"),
        ("Tokyo", "Tokyo Standard Time"),
        ("Séoul", "Korea Standard Time"),
        ("Sydney", "AUS Eastern Standard Time"),
        ("Nouméa", "Central Pacific Standard Time"),
        ("Tahiti", "Hawaiian Standard Time"),
    };

    readonly StackPanel _lignes = new();

    string[] Choisies => Option("villes", "New York,Tokyo,Londres").Split(',', StringSplitOptions.RemoveEmptyEntries);
    public override string Resume => string.Join(", ", Choisies);

    public HorlogesMondeWidget(WidgetConfig c) : base(c)
    {
        var pile = new StackPanel { Width = 250 };
        pile.Children.Add(_lignes);
        Content = Carte(pile);
        Construire();
        Minuteur(TimeSpan.FromSeconds(5), Construire);
    }

    void Construire()
    {
        _lignes.Children.Clear();
        var ici = DateTime.Now;
        foreach (var nom in Choisies)
        {
            var ville = Villes.FirstOrDefault(v => v.Nom == nom);
            if (ville.Nom == null) continue;
            TimeZoneInfo zone;
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(ville.Zone); } catch { continue; }

            var la = TimeZoneInfo.ConvertTime(ici, zone);
            var ecart = la - ici;
            int jours = (la.Date - ici.Date).Days;
            string jour = jours switch { 0 => "Aujourd'hui", 1 => "Demain", -1 => "Hier", _ => la.ToString("ddd d", Fr) };
            string decalage = Math.Abs(ecart.TotalMinutes) < 1 ? "même heure"
                : (ecart < TimeSpan.Zero ? "−" : "+") + (ecart.Minutes != 0
                    ? $"{Math.Abs(ecart.Hours)} h {Math.Abs(ecart.Minutes):00}"
                    : $"{Math.Abs((int)Math.Round(ecart.TotalHours))} h");

            var ligne = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };

            var heure = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            bool nuit = la.Hour < 7 || la.Hour >= 20;
            var soleil = Glyphe(nuit ? "" : "", 13, Pale);
            soleil.Margin = new Thickness(0, 0, 8, 0);
            heure.Children.Add(soleil);
            heure.Children.Add(Texte(la.ToString("HH:mm"), 28, Blanc, "Segoe UI Light"));
            DockPanel.SetDock(heure, Dock.Right);
            ligne.Children.Add(heure);

            var infos = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var titre = Texte(ville.Nom, 14);
            titre.FontWeight = FontWeights.SemiBold;
            infos.Children.Add(titre);
            infos.Children.Add(Texte($"{jour} · {decalage}", 12, Pale));
            ligne.Children.Add(infos);

            _lignes.Children.Add(ligne);
        }

        if (_lignes.Children.Count == 0)
            _lignes.Children.Add(Texte("✏ Modifier › Villes", 13, Pale));
    }

    protected override void RemplirMenu()
    {
        var menu = SousMenu("Villes");
        var choisies = Choisies.ToList();
        foreach (var (nom, _) in Villes)
            Coche(nom, choisies.Contains(nom), oui =>
            {
                var liste = Choisies.ToList();
                if (oui) liste.Add(nom); else liste.Remove(nom);
                SetOption("villes", string.Join(",", Villes.Select(v => v.Nom).Where(liste.Contains)));
                Construire();
                App.Instance.Notifier();
            }, menu);
    }
}
