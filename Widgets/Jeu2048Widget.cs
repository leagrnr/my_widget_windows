using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MesWidgets;

public class Jeu2048Widget : WidgetWindow
{
    const double Case = 56, Ecart = 8;

    readonly int[,] _g = new int[4, 4];
    int _score;
    bool _gagneAffiche;
    readonly Grid _plateau = new();
    readonly TextBlock _scoreTexte, _recordTexte, _message;
    readonly Border _voile;
    Point? _depart;

    int Record => int.Parse(Option("record", "0"));

    public Jeu2048Widget(WidgetConfig c) : base(c)
    {
        for (int i = 0; i < 4; i++)
        {
            _plateau.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Case + Ecart) });
            _plateau.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Case + Ecart) });
        }

        _scoreTexte = Texte("0", 16);
        _scoreTexte.FontWeight = FontWeights.SemiBold;
        _recordTexte = Texte("", 12, Pale);
        var scores = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
        _scoreTexte.HorizontalAlignment = HorizontalAlignment.Right;
        _recordTexte.HorizontalAlignment = HorizontalAlignment.Right;
        scores.Children.Add(_scoreTexte);
        scores.Children.Add(_recordTexte);
        var titre = Texte("2048", 26);
        titre.FontWeight = FontWeights.Bold;
        var entete = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        entete.Children.Add(titre);
        entete.Children.Add(scores);

        _message = Texte("", 20, Brushes.White);
        _message.FontWeight = FontWeights.Bold;
        _message.HorizontalAlignment = HorizontalAlignment.Center;
        var rejouer = Bouton(Texte("Nouvelle partie", 13, Brushes.White), Nouvelle);
        rejouer.Background = Theme.B(0x40, 0xFF, 0xFF, 0xFF);
        rejouer.HorizontalAlignment = HorizontalAlignment.Center;
        rejouer.Margin = new Thickness(0, 10, 0, 0);
        var pileVoile = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pileVoile.Children.Add(_message);
        pileVoile.Children.Add(rejouer);
        _voile = new Border { Background = Theme.B(0xB0, 0x20, 0x20, 0x28), CornerRadius = new CornerRadius(8), Child = pileVoile, Visibility = Visibility.Collapsed };

        var zone = new Grid();
        zone.Children.Add(new Border { Background = Theme.Hex("#BBADA0"), CornerRadius = new CornerRadius(8), Padding = new Thickness(Ecart / 2), Child = _plateau });
        zone.Children.Add(_voile);

        _plateau.Background = Brushes.Transparent;
        _plateau.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            Activate();
            Keyboard.Focus(this);
            _depart = e.GetPosition(_plateau);
            _plateau.CaptureMouse();
        };
        _plateau.MouseLeftButtonUp += (_, e) =>
        {
            _plateau.ReleaseMouseCapture();
            if (_depart is not { } d) return;
            _depart = null;
            var v = e.GetPosition(_plateau) - d;
            if (Math.Max(Math.Abs(v.X), Math.Abs(v.Y)) < 25) return;
            Jouer(Math.Abs(v.X) > Math.Abs(v.Y) ? (v.X < 0 ? 0 : 1) : (v.Y < 0 ? 2 : 3));
        };
        PreviewKeyDown += (_, e) =>
        {
            int dir = e.Key switch { Key.Left => 0, Key.Right => 1, Key.Up => 2, Key.Down => 3, _ => -1 };
            if (dir < 0) return;
            e.Handled = true;
            Jouer(dir);
        };

        var pile = new StackPanel();
        pile.Children.Add(entete);
        pile.Children.Add(zone);
        var aide = Texte("Flèches du clavier ou glisser avec la souris", 11, Pale);
        aide.Margin = new Thickness(0, 6, 0, 0);
        pile.Children.Add(aide);
        Content = Carte(pile);

        if (!Charger()) Nouvelle();
        Dessiner();
    }

    void Nouvelle()
    {
        Array.Clear(_g);
        _score = 0;
        _gagneAffiche = false;
        AjouterTuile();
        AjouterTuile();
        _voile.Visibility = Visibility.Collapsed;
        Enregistrer();
        Dessiner();
    }

    void Jouer(int dir)
    {
        if (_voile.Visibility == Visibility.Visible) return;
        bool bouge = false;
        for (int i = 0; i < 4; i++)
        {
            var cases = Enumerable.Range(0, 4).Select(k => dir switch
            {
                0 => (i, k), 1 => (i, 3 - k), 2 => (k, i), _ => (3 - k, i),
            }).ToArray();

            var valeurs = cases.Select(p => _g[p.Item1, p.Item2]).Where(v => v != 0).ToList();
            var fusion = new List<int>();
            for (int k = 0; k < valeurs.Count; k++)
            {
                if (k + 1 < valeurs.Count && valeurs[k] == valeurs[k + 1])
                {
                    fusion.Add(valeurs[k] * 2);
                    _score += valeurs[k] * 2;
                    k++;
                }
                else fusion.Add(valeurs[k]);
            }
            for (int k = 0; k < 4; k++)
            {
                int v = k < fusion.Count ? fusion[k] : 0;
                var (r, c) = cases[k];
                if (_g[r, c] != v) bouge = true;
                _g[r, c] = v;
            }
        }
        if (!bouge) return;

        AjouterTuile();
        if (_score > Record) Config.Options["record"] = _score.ToString();
        Enregistrer();
        Dessiner();

        if (!_gagneAffiche && _g.Cast<int>().Any(v => v >= 2048))
        {
            _gagneAffiche = true;
            Voile("Bravo, 2048 ! 🎉");
        }
        else if (!Possible()) Voile("Perdu !");
    }

    void Voile(string texte)
    {
        _message.Text = texte;
        _voile.Visibility = Visibility.Visible;
        if (texte.StartsWith("Bravo"))
        {
            _voile.MouseLeftButtonDown += Continuer;
        }
    }

    void Continuer(object sender, MouseButtonEventArgs e)
    {
        _voile.MouseLeftButtonDown -= Continuer;
        _voile.Visibility = Visibility.Collapsed;
        e.Handled = true;
    }

    bool Possible()
    {
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
            {
                if (_g[r, c] == 0) return true;
                if (c < 3 && _g[r, c] == _g[r, c + 1]) return true;
                if (r < 3 && _g[r, c] == _g[r + 1, c]) return true;
            }
        return false;
    }

    void AjouterTuile()
    {
        var vides = new List<(int, int)>();
        for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) if (_g[r, c] == 0) vides.Add((r, c));
        if (vides.Count == 0) return;
        var (rr, cc) = vides[Random.Shared.Next(vides.Count)];
        _g[rr, cc] = Random.Shared.Next(10) == 0 ? 4 : 2;
    }

    void Dessiner()
    {
        _plateau.Children.Clear();
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
            {
                int v = _g[r, c];
                var (fond, texte) = Couleurs(v);
                var tuile = new Border
                {
                    Width = Case, Height = Case,
                    CornerRadius = new CornerRadius(6),
                    Background = Theme.Hex(fond),
                    Child = v == 0 ? null : new TextBlock
                    {
                        Text = v.ToString(),
                        FontSize = v < 100 ? 26 : v < 1000 ? 22 : 17,
                        FontWeight = FontWeights.Bold,
                        Foreground = Theme.Hex(texte),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                };
                Grid.SetRow(tuile, r);
                Grid.SetColumn(tuile, c);
                _plateau.Children.Add(tuile);
            }
        _scoreTexte.Text = $"Score : {_score}";
        _recordTexte.Text = $"Record : {Record}";
    }

    static (string Fond, string Texte) Couleurs(int v) => v switch
    {
        0 => ("#CDC1B4", "#776E65"),
        2 => ("#EEE4DA", "#776E65"),
        4 => ("#EDE0C8", "#776E65"),
        8 => ("#F2B179", "#FFFFFF"),
        16 => ("#F59563", "#FFFFFF"),
        32 => ("#F67C5F", "#FFFFFF"),
        64 => ("#F65E3B", "#FFFFFF"),
        128 => ("#EDCF72", "#FFFFFF"),
        256 => ("#EDCC61", "#FFFFFF"),
        512 => ("#EDC850", "#FFFFFF"),
        1024 => ("#EDC53F", "#FFFFFF"),
        2048 => ("#EDC22E", "#FFFFFF"),
        _ => ("#3C3A32", "#FFFFFF"),
    };

    void Enregistrer() => SetOption("partie", _score + ";" + string.Join(",", _g.Cast<int>()));

    bool Charger()
    {
        try
        {
            var parties = Option("partie", "").Split(';');
            if (parties.Length != 2) return false;
            var valeurs = parties[1].Split(',').Select(int.Parse).ToArray();
            if (valeurs.Length != 16) return false;
            _score = int.Parse(parties[0]);
            for (int i = 0; i < 16; i++) _g[i / 4, i % 4] = valeurs[i];
            _gagneAffiche = valeurs.Any(v => v >= 2048);
            if (!Possible()) Voile("Perdu !");
            return true;
        }
        catch { return false; }
    }

    protected override void RemplirMenu() => Item("Nouvelle partie", Nouvelle);
}
