using System;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace MesWidgets;

public class MeteoWidget : WidgetWindow
{
    public static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    readonly TextBlock _ville, _temperature, _description, _details;
    readonly ContentControl _icone = new() { Margin = new Thickness(0, 4, 12, 4), VerticalAlignment = VerticalAlignment.Center };
    readonly Grid _jours = new();

    public override string Resume => Ville;

    public MeteoWidget(WidgetConfig c) : base(c)
    {
        _ville = Texte(Ville, 14, Pale);
        _temperature = Texte("--°", 46, Blanc, "Segoe UI Light");
        _temperature.VerticalAlignment = VerticalAlignment.Center;

        var ligne = new StackPanel { Orientation = Orientation.Horizontal };
        ligne.Children.Add(_icone);
        ligne.Children.Add(_temperature);

        _description = Texte("Chargement…", 15);
        _details = Texte("", 12, Pale);

        for (int i = 0; i < 3; i++) _jours.ColumnDefinitions.Add(new ColumnDefinition());

        var pile = new StackPanel { Width = 240 };
        pile.Children.Add(_ville);
        pile.Children.Add(ligne);
        pile.Children.Add(_description);
        pile.Children.Add(_details);
        pile.Children.Add(Separateur());
        pile.Children.Add(_jours);
        Content = Carte(pile);

        Loaded += (_, _) => Actualiser();
        Minuteur(TimeSpan.FromMinutes(30), Actualiser);
    }

    async void Actualiser()
    {
        try
        {
            var url = string.Format(Inv,
                "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}" +
                "&current=temperature_2m,apparent_temperature,weather_code,wind_speed_10m,is_day" +
                "&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=4",
                Latitude, Longitude);

            using var doc = JsonDocument.Parse(await Http.GetStringAsync(url));
            var actuel = doc.RootElement.GetProperty("current");
            int code = actuel.GetProperty("weather_code").GetInt32();
            bool jour = actuel.GetProperty("is_day").GetInt32() == 1;

            _ville.Text = Ville;
            _icone.Content = IconeMeteo.Creer(code, jour, 56);
            _temperature.Text = $"{actuel.GetProperty("temperature_2m").GetDouble():0}°";
            _description.Text = Description(code);
            _details.Text = string.Format(Fr, "Ressenti {0:0}° · Vent {1:0} km/h",
                actuel.GetProperty("apparent_temperature").GetDouble(), actuel.GetProperty("wind_speed_10m").GetDouble());

            var jours = doc.RootElement.GetProperty("daily");
            _jours.Children.Clear();
            for (int i = 1; i <= 3; i++)
            {
                var date = DateTime.ParseExact(jours.GetProperty("time")[i].GetString(), "yyyy-MM-dd", Inv);
                var bloc = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                var nom = Texte(Majuscule(date.ToString("ddd", Fr).TrimEnd('.')), 12, Pale);
                var icone = IconeMeteo.Creer(jours.GetProperty("weather_code")[i].GetInt32(), true, 30);
                icone.Margin = new Thickness(0, 2, 0, 2);
                var temp = Texte(string.Format(Fr, "{0:0}° / {1:0}°",
                    jours.GetProperty("temperature_2m_max")[i].GetDouble(),
                    jours.GetProperty("temperature_2m_min")[i].GetDouble()), 12);
                foreach (var e in new FrameworkElement[] { nom, icone, temp })
                {
                    e.HorizontalAlignment = HorizontalAlignment.Center;
                    bloc.Children.Add(e);
                }
                Grid.SetColumn(bloc, i - 1);
                _jours.Children.Add(bloc);
            }
        }
        catch
        {
            _icone.Content = Glyphe("", 40, Pale);
            _description.Text = "Météo indisponible";
            _details.Text = "Vérifie ta connexion Internet";
        }
    }

    protected override void RemplirMenu()
    {
        Item("Changer de ville…", async () => { if (await ChoisirVille()) Actualiser(); });
        Item("Actualiser", Actualiser);
    }

    static string Description(int code) => code switch
    {
        0 => "Ciel dégagé",
        1 => "Plutôt dégagé",
        2 => "Partiellement nuageux",
        3 => "Couvert",
        45 or 48 => "Brouillard",
        51 or 53 or 55 => "Bruine",
        56 or 57 => "Bruine verglaçante",
        61 => "Pluie faible",
        63 => "Pluie",
        65 => "Forte pluie",
        66 or 67 => "Pluie verglaçante",
        71 => "Neige faible",
        73 => "Neige",
        75 => "Forte neige",
        77 => "Grains de neige",
        80 or 81 => "Averses",
        82 => "Fortes averses",
        85 or 86 => "Averses de neige",
        95 => "Orage",
        96 or 99 => "Orage avec grêle",
        _ => "—",
    };
}
