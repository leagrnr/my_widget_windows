using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesWidgets;

public class PomodoroWidget : WidgetWindow
{
    const double Largeur = 200;
    static readonly Brush Vert = Theme.B(0xFF, 0x4C, 0xC3, 0x8A);

    bool _pause, _enCours;
    TimeSpan _reste;
    DateTime _fin;

    readonly TextBlock _libelle, _temps, _compteur;
    readonly Border _rempli, _lecture;

    int MinutesTravail => int.Parse(Option("travail", "25"));
    int MinutesPause => int.Parse(Option("pause", "5"));
    TimeSpan Duree => TimeSpan.FromMinutes(_pause ? MinutesPause : MinutesTravail);

    int Faites
    {
        get
        {
            var v = Option("faites", "").Split(':');
            return v.Length == 2 && v[0] == DateTime.Today.ToString("yyyy-MM-dd") ? int.Parse(v[1]) : 0;
        }
        set => SetOption("faites", $"{DateTime.Today:yyyy-MM-dd}:{value}");
    }

    public PomodoroWidget(WidgetConfig c) : base(c)
    {
        _libelle = Texte("", 14, Pale);
        _libelle.HorizontalAlignment = HorizontalAlignment.Center;
        _temps = Texte("", 54, Blanc, "Segoe UI Light");
        _temps.HorizontalAlignment = HorizontalAlignment.Center;

        var (barre, rempli) = Barre(5);
        _rempli = rempli;
        barre.Margin = new Thickness(0, 2, 0, 12);

        _lecture = Cliquable(new Border
        {
            Width = 46, Height = 46,
            CornerRadius = new CornerRadius(23),
            Background = Accent,
            Margin = new Thickness(14, 0, 14, 0),
        }, Basculer, survol: false);

        var reset = Bouton(Glyphe("", 16), Reinitialiser, 36);
        reset.ToolTip = "Recommencer";
        var passer = Bouton(Glyphe("", 16), Passer, 36);
        passer.ToolTip = "Passer à la suite";

        var boutons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        boutons.Children.Add(reset);
        boutons.Children.Add(_lecture);
        boutons.Children.Add(passer);

        _compteur = Texte("", 12, Pale);
        _compteur.HorizontalAlignment = HorizontalAlignment.Center;
        _compteur.Margin = new Thickness(0, 10, 0, 0);

        var pile = new StackPanel { Width = Largeur };
        pile.Children.Add(_libelle);
        pile.Children.Add(_temps);
        pile.Children.Add(barre);
        pile.Children.Add(boutons);
        pile.Children.Add(_compteur);
        Content = Carte(pile);

        _reste = Duree;
        Afficher();
        Minuteur(TimeSpan.FromMilliseconds(250), Tic);
    }

    void Tic()
    {
        if (!_enCours) return;
        _reste = _fin - DateTime.Now;
        if (_reste <= TimeSpan.Zero) Terminer();
        Afficher();
    }

    void Basculer()
    {
        if (_enCours) { _reste = _fin - DateTime.Now; _enCours = false; }
        else { _fin = DateTime.Now + _reste; _enCours = true; }
        Afficher();
    }

    void Terminer()
    {
        SystemSounds.Asterisk.Play();
        if (!_pause)
        {
            Faites++;
            App.Instance.Notification("Pomodoro", $"Bravo ! Place à {MinutesPause} min de pause.");
        }
        else
        {
            App.Instance.Notification("Pomodoro", "La pause est finie, au travail !");
        }
        _pause = !_pause;
        _reste = Duree;
        _fin = DateTime.Now + _reste;
    }

    void Passer()
    {
        _pause = !_pause;
        _enCours = false;
        _reste = Duree;
        Afficher();
    }

    void Reinitialiser()
    {
        _enCours = false;
        _reste = Duree;
        Afficher();
    }

    void Afficher()
    {
        var s = (int)Math.Ceiling(Math.Max(0, _reste.TotalSeconds));
        _temps.Text = $"{s / 60:00}:{s % 60:00}";
        _libelle.Text = _pause ? "Pause" : "Concentration";
        _rempli.Background = _pause ? Vert : Accent;
        _rempli.Width = Largeur * Math.Clamp(1 - _reste.TotalSeconds / Duree.TotalSeconds, 0, 1);
        _lecture.Background = _pause ? Vert : Accent;
        _lecture.Child = Glyphe(_enCours ? "" : "", 18, Brushes.White);
        _lecture.ToolTip = _enCours ? "Pause" : "Démarrer";
        int n = Faites;
        _compteur.Text = n == 0 ? "Aucune session aujourd'hui" : $"{n} session{(n > 1 ? "s" : "")} aujourd'hui";
    }

    protected override void RemplirMenu()
    {
        var travail = SousMenu("Durée de concentration");
        foreach (var m in new[] { 15, 20, 25, 30, 45, 50 })
            Coche($"{m} min", m == MinutesTravail, _ => { SetOption("travail", m.ToString()); if (!_enCours && !_pause) Reinitialiser(); }, travail);

        var pause = SousMenu("Durée de la pause");
        foreach (var m in new[] { 5, 10, 15, 20 })
            Coche($"{m} min", m == MinutesPause, _ => { SetOption("pause", m.ToString()); if (!_enCours && _pause) Reinitialiser(); }, pause);
    }
}
