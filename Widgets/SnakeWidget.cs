using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MesWidgets;

public class SnakeWidget : WidgetWindow
{
    const int N = 18;
    const double T = 14;

    readonly Canvas _terrain = new() { Width = N * T, Height = N * T, ClipToBounds = true };
    readonly TextBlock _scoreTexte, _message;
    readonly DispatcherTimer _boucle = new() { Interval = TimeSpan.FromMilliseconds(120) };
    readonly List<(int X, int Y)> _serpent = new();
    (int X, int Y) _direction = (1, 0), _prochaine = (1, 0), _pomme;
    int _score;
    bool _perdu = true;

    int Record => int.Parse(Option("record", "0"));

    public SnakeWidget(WidgetConfig c) : base(c)
    {
        _scoreTexte = Texte("", 13, Pale);
        _scoreTexte.HorizontalAlignment = HorizontalAlignment.Right;
        _scoreTexte.VerticalAlignment = VerticalAlignment.Center;
        var entete = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        entete.Children.Add(Titre("Snake"));
        entete.Children.Add(_scoreTexte);

        _message = Texte("", 14, Blanc);
        _message.TextAlignment = TextAlignment.Center;
        _message.HorizontalAlignment = HorizontalAlignment.Center;
        _message.VerticalAlignment = VerticalAlignment.Center;
        _message.IsHitTestVisible = false;

        var zone = new Grid { Background = Brushes.Transparent };
        zone.Children.Add(new Border { Background = Theme.Piste, CornerRadius = new CornerRadius(8), Child = _terrain });
        zone.Children.Add(_message);
        zone.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            Activate();
            Keyboard.Focus(this);
            if (_perdu) Demarrer(); else Pause();
        };

        PreviewKeyDown += (_, e) =>
        {
            (int, int)? d = e.Key switch
            {
                Key.Left => (-1, 0), Key.Right => (1, 0), Key.Up => (0, -1), Key.Down => (0, 1), _ => null,
            };
            if (d is { } nouvelle)
            {
                e.Handled = true;
                if (nouvelle.Item1 != -_direction.X || nouvelle.Item2 != -_direction.Y) _prochaine = nouvelle;
                if (!_boucle.IsEnabled && !_perdu) Pause();
            }
            else if (e.Key == Key.Space)
            {
                e.Handled = true;
                if (_perdu) Demarrer(); else Pause();
            }
        };

        _boucle.Tick += (_, _) => Avancer();
        Closed += (_, _) => _boucle.Stop();

        var pile = new StackPanel();
        pile.Children.Add(entete);
        pile.Children.Add(zone);
        var aide = Texte("Flèches pour tourner · Espace = pause", 11, Pale);
        aide.Margin = new Thickness(0, 6, 0, 0);
        pile.Children.Add(aide);
        Content = Carte(pile);

        _message.Text = "Clique ou appuie sur Espace\npour jouer 🐍";
        Dessiner();
    }

    void Demarrer()
    {
        _serpent.Clear();
        for (int i = 0; i < 4; i++) _serpent.Add((N / 2 - i, N / 2));
        _direction = _prochaine = (1, 0);
        _score = 0;
        _perdu = false;
        _boucle.Interval = TimeSpan.FromMilliseconds(120);
        NouvellePomme();
        _message.Text = "";
        _boucle.Start();
        Dessiner();
    }

    void Pause()
    {
        if (_boucle.IsEnabled) { _boucle.Stop(); _message.Text = "Pause"; }
        else { _message.Text = ""; _boucle.Start(); }
    }

    void Avancer()
    {
        _direction = _prochaine;
        var tete = (X: _serpent[0].X + _direction.X, Y: _serpent[0].Y + _direction.Y);
        if (tete.X < 0 || tete.Y < 0 || tete.X >= N || tete.Y >= N || _serpent.Contains(tete))
        {
            _boucle.Stop();
            _perdu = true;
            if (_score > Record) SetOption("record", _score.ToString());
            _message.Text = $"Perdu ! Score : {_score}\nClique pour rejouer";
            Dessiner();
            return;
        }

        _serpent.Insert(0, tete);
        if (tete == _pomme)
        {
            _score++;
            NouvellePomme();
            _boucle.Interval = TimeSpan.FromMilliseconds(Math.Max(60, 120 - _score * 3));
        }
        else _serpent.RemoveAt(_serpent.Count - 1);
        Dessiner();
    }

    void NouvellePomme()
    {
        var libres = (from x in Enumerable.Range(0, N) from y in Enumerable.Range(0, N) select (x, y)).Except(_serpent).ToList();
        _pomme = libres[Random.Shared.Next(libres.Count)];
    }

    void Dessiner()
    {
        _terrain.Children.Clear();
        if (_serpent.Count > 0)
        {
            var pomme = new Ellipse { Width = T - 2, Height = T - 2, Fill = Theme.Hex("#FF5A5A") };
            Canvas.SetLeft(pomme, _pomme.X * T + 1);
            Canvas.SetTop(pomme, _pomme.Y * T + 1);
            _terrain.Children.Add(pomme);
        }
        for (int i = 0; i < _serpent.Count; i++)
        {
            var bout = new Rectangle
            {
                Width = T - 1.5, Height = T - 1.5,
                RadiusX = 3, RadiusY = 3,
                Fill = Accent,
                Opacity = i == 0 ? 1 : 0.75,
            };
            Canvas.SetLeft(bout, _serpent[i].X * T + 0.75);
            Canvas.SetTop(bout, _serpent[i].Y * T + 0.75);
            _terrain.Children.Add(bout);
        }
        _scoreTexte.Text = $"Score : {_score} · Record : {Math.Max(Record, _score)}";
    }

    protected override void RemplirMenu() => Item("Nouvelle partie", Demarrer);
}
