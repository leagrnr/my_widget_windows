using System;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MesWidgets;

public class SoleilLuneWidget : WidgetWindow
{
    const double Largeur = 250;
    const double MoisLunaire = 29.530588853;
    static readonly DateTime NouvelleLuneReference = new(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);

    readonly TextBlock _ville, _lever, _coucher, _duree, _phase, _detailLune;
    readonly Border _rempli;
    readonly ContentControl _lune = new() { Margin = new Thickness(0, 0, 14, 0) };
    DateTime? _heureLever, _heureCoucher;

    public override string Resume => Ville;

    public SoleilLuneWidget(WidgetConfig c) : base(c)
    {
        _ville = Texte(Ville, 13, Pale);
        _lever = Texte("--:--", 22);
        _coucher = Texte("--:--", 22);
        _coucher.HorizontalAlignment = HorizontalAlignment.Right;

        var soleil = new Grid { Margin = new Thickness(0, 6, 0, 0) };
        soleil.Children.Add(Bloc("Lever", _lever, HorizontalAlignment.Left));
        soleil.Children.Add(Bloc("Coucher", _coucher, HorizontalAlignment.Right));
        var iconeSoleil = IconeMeteo.Creer(0, true, 30);
        iconeSoleil.HorizontalAlignment = HorizontalAlignment.Center;
        soleil.Children.Add(iconeSoleil);

        Grid barre;
        (barre, _rempli) = Barre(4);
        barre.Margin = new Thickness(0, 8, 0, 4);
        _rempli.Background = Theme.B(0xFF, 0xFF, 0xC8, 0x3D);
        _duree = Texte("", 12, Pale);
        _duree.HorizontalAlignment = HorizontalAlignment.Center;

        _phase = Texte("", 15);
        _phase.FontWeight = FontWeights.SemiBold;
        _detailLune = Texte("", 12, Pale);
        _detailLune.TextWrapping = TextWrapping.Wrap;
        var infosLune = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Width = Largeur - 60 };
        infosLune.Children.Add(_phase);
        infosLune.Children.Add(_detailLune);
        var lune = new StackPanel { Orientation = Orientation.Horizontal };
        lune.Children.Add(_lune);
        lune.Children.Add(infosLune);

        var pile = new StackPanel { Width = Largeur };
        pile.Children.Add(_ville);
        pile.Children.Add(soleil);
        pile.Children.Add(barre);
        pile.Children.Add(_duree);
        pile.Children.Add(Separateur());
        pile.Children.Add(lune);
        Content = Carte(pile);

        CalculerLune();
        Loaded += (_, _) => ChargerSoleil();
        Minuteur(TimeSpan.FromMinutes(1), () => { AvancerJournee(); CalculerLune(); });
        Minuteur(TimeSpan.FromHours(3), ChargerSoleil);
    }

    static StackPanel Bloc(string titre, TextBlock valeur, HorizontalAlignment alignement)
    {
        var pile = new StackPanel { HorizontalAlignment = alignement };
        var t = Texte(titre, 12, Pale);
        t.HorizontalAlignment = alignement;
        pile.Children.Add(t);
        pile.Children.Add(valeur);
        return pile;
    }

    async void ChargerSoleil()
    {
        try
        {
            var url = string.Format(Inv,
                "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&daily=sunrise,sunset,daylight_duration&timezone=auto&forecast_days=1",
                Latitude, Longitude);
            using var doc = JsonDocument.Parse(await Web.Http.GetStringAsync(url));
            var jour = doc.RootElement.GetProperty("daily");
            _heureLever = DateTime.Parse(jour.GetProperty("sunrise")[0].GetString(), Inv);
            _heureCoucher = DateTime.Parse(jour.GetProperty("sunset")[0].GetString(), Inv);
            var duree = TimeSpan.FromSeconds(jour.GetProperty("daylight_duration")[0].GetDouble());

            _ville.Text = Ville;
            _lever.Text = _heureLever.Value.ToString("HH:mm");
            _coucher.Text = _heureCoucher.Value.ToString("HH:mm");
            _duree.Text = $"{(int)duree.TotalHours} h {duree.Minutes:00} de jour";
            AvancerJournee();
        }
        catch
        {
            _duree.Text = "Horaires indisponibles (connexion ?)";
        }
    }

    void AvancerJournee()
    {
        if (_heureLever is not { } lever || _heureCoucher is not { } coucher) return;
        double ratio = (DateTime.Now - lever).TotalMinutes / (coucher - lever).TotalMinutes;
        _rempli.Width = Largeur * Math.Clamp(ratio, 0, 1);
    }

    void CalculerLune()
    {
        double age = ((DateTime.UtcNow - NouvelleLuneReference).TotalDays % MoisLunaire + MoisLunaire) % MoisLunaire;
        double p = age / MoisLunaire;
        double eclairee = (1 - Math.Cos(2 * Math.PI * p)) / 2;

        _lune.Content = DessinerLune(p, 46);
        _phase.Text = p switch
        {
            < 0.0339 or > 0.9661 => "Nouvelle lune",
            < 0.216 => "Premier croissant",
            < 0.284 => "Premier quartier",
            < 0.466 => "Lune gibbeuse croissante",
            < 0.534 => "Pleine lune",
            < 0.716 => "Lune gibbeuse décroissante",
            < 0.784 => "Dernier quartier",
            _ => "Dernier croissant",
        };
        var pleine = DateTime.Now.AddDays((0.5 - p + 1) % 1 * MoisLunaire);
        _detailLune.Text = $"Éclairée à {eclairee * 100:0} % · pleine lune le {pleine.ToString("d MMM", Fr)}";
    }

    static FrameworkElement DessinerLune(double p, double taille)
    {
        const double r = 46, cx = 50, cy = 50;
        var c = new Canvas { Width = 100, Height = 100 };
        c.Children.Add(new Ellipse { Width = 2 * r, Height = 2 * r, Fill = Theme.Clair ? Theme.B(0xFF, 0xC9, 0xCF, 0xD8) : Theme.B(0xFF, 0x3A, 0x3F, 0x4B), Margin = new Thickness(cx - r, cy - r, 0, 0) });

        bool croissante = p < 0.5;
        var demi = new PathFigure { StartPoint = new Point(cx, cy - r), IsClosed = true };
        demi.Segments.Add(new ArcSegment(new Point(cx, cy + r), new Size(r, r), 0, false,
            croissante ? SweepDirection.Clockwise : SweepDirection.Counterclockwise, true));
        var demiDisque = new PathGeometry(new[] { demi });
        var ellipse = new EllipseGeometry(new Point(cx, cy), r * Math.Abs(Math.Cos(2 * Math.PI * p)), r);

        bool gibbeuse = p is > 0.25 and < 0.75;
        var eclairee = new CombinedGeometry(gibbeuse ? GeometryCombineMode.Union : GeometryCombineMode.Exclude, demiDisque, ellipse);
        c.Children.Add(new Path { Data = eclairee, Fill = Theme.B(0xFF, 0xF4, 0xE3, 0xA1) });
        return new Viewbox { Width = taille, Height = taille, Child = c };
    }

    protected override void RemplirMenu() =>
        Item("Changer de ville…", async () => { if (await ChoisirVille()) ChargerSoleil(); });
}
