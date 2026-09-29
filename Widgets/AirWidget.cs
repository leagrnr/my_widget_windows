using System;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesWidgets;

public class AirWidget : WidgetWindow
{
    static readonly (int Max, string Nom, string Couleur)[] Niveaux =
    {
        (20, "Bon", "#50F0E6"), (40, "Correct", "#50CCAA"), (60, "Moyen", "#F0E641"),
        (80, "Médiocre", "#FF5050"), (100, "Très médiocre", "#960032"), (int.MaxValue, "Extrêmement médiocre", "#7D2181"),
    };

    static readonly (string Cle, string Nom)[] Pollens =
    {
        ("alder_pollen", "Aulne"), ("birch_pollen", "Bouleau"), ("grass_pollen", "Graminées"),
        ("mugwort_pollen", "Armoise"), ("olive_pollen", "Olivier"), ("ragweed_pollen", "Ambroisie"),
    };

    readonly TextBlock _ville, _indice, _particules;
    readonly Border _pastille;
    readonly StackPanel _pollens = new();

    public override string Resume => Ville;

    public AirWidget(WidgetConfig c) : base(c)
    {
        _ville = Texte(Ville, 13, Pale);
        _indice = Texte("--", 44, Blanc, "Segoe UI Light");
        _pastille = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 3, 10, 4),
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Background = Theme.Piste,
            Child = Texte("…", 13, Brushes.Black),
        };
        var ligne = new StackPanel { Orientation = Orientation.Horizontal };
        ligne.Children.Add(_indice);
        ligne.Children.Add(_pastille);
        _particules = Texte("", 12, Pale);

        var pile = new StackPanel { Width = 250 };
        pile.Children.Add(_ville);
        pile.Children.Add(Texte("Qualité de l'air", 15));
        pile.Children.Add(ligne);
        pile.Children.Add(_particules);
        pile.Children.Add(Separateur());
        var titrePollen = Texte("POLLENS", 11, Pale);
        titrePollen.FontWeight = FontWeights.SemiBold;
        titrePollen.Margin = new Thickness(0, 0, 0, 4);
        pile.Children.Add(titrePollen);
        pile.Children.Add(_pollens);
        Content = Carte(pile);

        Loaded += (_, _) => Charger();
        Minuteur(TimeSpan.FromMinutes(30), Charger);
    }

    async void Charger()
    {
        try
        {
            var url = string.Format(Inv,
                "https://air-quality-api.open-meteo.com/v1/air-quality?latitude={0}&longitude={1}&current=european_aqi,pm10,pm2_5,{2}",
                Latitude, Longitude, string.Join(",", Pollens.Select(p => p.Cle)));
            using var doc = JsonDocument.Parse(await Web.Http.GetStringAsync(url));
            var actuel = doc.RootElement.GetProperty("current");

            _ville.Text = Ville;
            int indice = (int)Math.Round(actuel.GetProperty("european_aqi").GetDouble());
            var niveau = Niveaux.First(n => indice <= n.Max);
            _indice.Text = indice.ToString();
            _pastille.Background = Theme.Hex(niveau.Couleur);
            _pastille.Child = Texte(niveau.Nom, 13, indice > 80 ? Brushes.White : Brushes.Black);
            _particules.Text = string.Format(Fr, "PM2,5 : {0:0} µg/m³ · PM10 : {1:0} µg/m³",
                actuel.GetProperty("pm2_5").GetDouble(), actuel.GetProperty("pm10").GetDouble());

            _pollens.Children.Clear();
            foreach (var (cle, nom) in Pollens)
            {
                if (!actuel.TryGetProperty(cle, out var v) || v.ValueKind != JsonValueKind.Number) continue;
                double grains = v.GetDouble();
                if (grains < 1) continue;
                var (texte, couleur) = grains switch
                {
                    < 10 => ("Faible", Theme.Hex("#50CCAA")),
                    < 50 => ("Modéré", Theme.Hex("#F0E641")),
                    < 200 => ("Élevé", Theme.Hex("#FF9050")),
                    _ => ("Très élevé", Theme.Hex("#FF5050")),
                };
                var ligne = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
                var niveauPollen = Texte(texte, 13, couleur);
                niveauPollen.FontWeight = FontWeights.SemiBold;
                DockPanel.SetDock(niveauPollen, Dock.Right);
                ligne.Children.Add(niveauPollen);
                ligne.Children.Add(Texte(nom, 13));
                _pollens.Children.Add(ligne);
            }
            if (_pollens.Children.Count == 0)
                _pollens.Children.Add(Texte("Pas de pollen en ce moment 🌿", 13, Pale));
        }
        catch
        {
            _particules.Text = "Données indisponibles (connexion ?)";
        }
    }

    protected override void RemplirMenu()
    {
        Item("Changer de ville…", async () => { if (await ChoisirVille()) Charger(); });
        Item("Actualiser", Charger);
    }
}
