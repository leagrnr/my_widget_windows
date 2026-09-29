using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MesWidgets;

public class AnimalWidget : WidgetWindow
{
    enum Etat { Marche, Assis, Dort, Saute }

    static readonly (string Nom, string Pelage, string Yeux)[] Pelages =
    {
        ("Roux", "#E8944A", "#2B2B2B"), ("Noir", "#2E2E33", "#FFD54A"), ("Gris", "#8A8F98", "#2B2B2B"),
        ("Blanc", "#F4F4F4", "#3C7BD6"), ("Crème", "#EBCB9C", "#2B2B2B"),
    };

    const double L = 90, H = 70;

    readonly Canvas _dessin = new() { Width = L, Height = H };
    readonly ScaleTransform _miroir = new(1, 1, L / 2, 0);
    readonly TranslateTransform _saut = new();
    readonly ScaleTransform _ecrase = new(1, 1, L / 2, 62);
    readonly RotateTransform[] _pattes = new RotateTransform[4];
    readonly Rectangle[] _rectPattes = new Rectangle[4];
    readonly RotateTransform _queue = new(0, 18, 40);
    readonly UIElement _yeuxOuverts, _yeuxFermes;
    readonly TextBlock _zzz;
    readonly Path _coeur;
    readonly DispatcherTimer _animation = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };

    Etat _etat = Etat.Assis;
    DateTime _debutEtat = DateTime.Now, _finEtat = DateTime.Now.AddSeconds(3);
    int _direction = 1;
    double _phase;

    string Pelage => Option("pelage", "Roux");

    public override string Resume => Pelage;

    public AnimalWidget(WidgetConfig c) : base(c)
    {
        var (_, hexPelage, hexYeux) = Array.Find(Pelages, p => p.Nom == Pelage);
        if (hexPelage == null) (_, hexPelage, hexYeux) = Pelages[0];
        var pelage = Theme.Hex(hexPelage);
        var yeux = Theme.Hex(hexYeux);
        var rose = Theme.Hex("#F28BA8");

        var queue = new Path
        {
            Data = Geometry.Parse("M 18,42 C 8,40 2,30 8,16"),
            Stroke = pelage, StrokeThickness = 6,
            StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
            RenderTransform = _queue,
        };
        _dessin.Children.Add(queue);

        double[] xPattes = { 20, 29, 48, 57 };
        for (int i = 0; i < 4; i++)
        {
            _pattes[i] = new RotateTransform(0, 3, 0);
            var patte = new Rectangle { Width = 7, Height = 15, RadiusX = 3.5, RadiusY = 3.5, Fill = pelage, RenderTransform = _pattes[i] };
            _rectPattes[i] = patte;
            Canvas.SetLeft(patte, xPattes[i]);
            Canvas.SetTop(patte, 47);
            _dessin.Children.Add(patte);
        }

        Ajouter(new Ellipse { Width = 50, Height = 26, Fill = pelage }, 13, 29);
        Ajouter(new Ellipse { Width = 26, Height = 24, Fill = pelage }, 52, 16);
        _dessin.Children.Add(new Polygon { Fill = pelage, Points = new PointCollection { new(53, 25), new(55, 8), new(64, 18) } });
        _dessin.Children.Add(new Polygon { Fill = pelage, Points = new PointCollection { new(66, 18), new(74, 8), new(77, 25) } });
        _dessin.Children.Add(new Polygon { Fill = rose, Points = new PointCollection { new(56, 20), new(57, 12), new(61, 18) } });
        _dessin.Children.Add(new Polygon { Fill = rose, Points = new PointCollection { new(69, 18), new(73, 12), new(74, 21) } });

        var ouverts = new Canvas();
        foreach (var x in new[] { 61.0, 69.0 })
        {
            var oeil = new Ellipse { Width = 4, Height = 6, Fill = yeux };
            Canvas.SetLeft(oeil, x); Canvas.SetTop(oeil, 24);
            ouverts.Children.Add(oeil);
        }
        _yeuxOuverts = ouverts;
        var fermes = new Path { Data = Geometry.Parse("M 60,28 Q 63,30 66,28 M 68,28 Q 71,30 74,28"), Stroke = Theme.Hex("#2B2B2B"), StrokeThickness = 1.4 };
        _yeuxFermes = fermes;
        _dessin.Children.Add(_yeuxOuverts);
        _dessin.Children.Add(_yeuxFermes);

        _dessin.Children.Add(new Polygon { Fill = rose, Points = new PointCollection { new(73, 31), new(77, 31), new(75, 33.5) } });
        _dessin.Children.Add(new Path { Data = Geometry.Parse("M 76,34 L 88,32 M 76,35 L 88,37"), Stroke = Theme.B(0x90, 0x60, 0x60, 0x60), StrokeThickness = 0.8 });

        _zzz = new TextBlock { Text = "z Z", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Theme.Hex("#9FB4FF") };
        Canvas.SetLeft(_zzz, 66); Canvas.SetTop(_zzz, -4);
        _dessin.Children.Add(_zzz);

        _coeur = new Path { Data = Geometry.Parse("M 10,6 C 10,2 4,2 4,6 C 4,9 10,13 10,13 C 10,13 16,9 16,6 C 16,2 10,2 10,6 Z"), Fill = Theme.Hex("#FF5A7A") };
        Canvas.SetLeft(_coeur, 56); Canvas.SetTop(_coeur, -10);
        _dessin.Children.Add(_coeur);

        var transformations = new TransformGroup();
        transformations.Children.Add(_ecrase);
        transformations.Children.Add(_miroir);
        transformations.Children.Add(_saut);
        _dessin.RenderTransform = transformations;
        Content = new Border { Child = _dessin, Padding = new Thickness(4, 16, 4, 2), Background = Brushes.Transparent, ToolTip = "Clique pour le caresser 🐾" };

        ChangerEtat(Etat.Assis);
        _animation.Tick += (_, _) => Animer();
        _animation.Start();
        Closed += (_, _) => _animation.Stop();
    }

    void Ajouter(UIElement e, double x, double y)
    {
        Canvas.SetLeft(e, x);
        Canvas.SetTop(e, y);
        _dessin.Children.Add(e);
    }

    void ChangerEtat(Etat etat)
    {
        _etat = etat;
        _debutEtat = DateTime.Now;
        double secondes = etat switch
        {
            Etat.Marche => Random.Shared.Next(4, 11),
            Etat.Assis => Random.Shared.Next(3, 9),
            Etat.Dort => Random.Shared.Next(12, 30),
            _ => 0.7,
        };
        _finEtat = _debutEtat.AddSeconds(secondes);
        if (etat == Etat.Marche && Random.Shared.Next(3) == 0) _direction = -_direction;
    }

    void Animer()
    {
        var maintenant = DateTime.Now;
        if (maintenant >= _finEtat)
        {
            int de = Random.Shared.Next(100);
            ChangerEtat(_etat == Etat.Saute ? Etat.Assis : de < 55 ? Etat.Marche : de < 85 ? Etat.Assis : Etat.Dort);
        }

        double t = (maintenant - _debutEtat).TotalSeconds;
        _phase += 0.35;
        bool dort = _etat == Etat.Dort;

        double angle = _etat == Etat.Marche ? Math.Sin(_phase) * 25 : 0;
        _pattes[0].Angle = angle; _pattes[3].Angle = angle;
        _pattes[1].Angle = -angle; _pattes[2].Angle = -angle;
        foreach (var p in _rectPattes) p.Visibility = dort ? Visibility.Hidden : Visibility.Visible;

        _queue.Angle = dort ? 20 : Math.Sin(t * (_etat == Etat.Marche ? 6 : 2.5)) * 12;
        _ecrase.ScaleY = dort ? 0.82 : 1;

        bool cligne = !dort && (maintenant.Millisecond / 100 == 0) && maintenant.Second % 4 == 0;
        _yeuxOuverts.Visibility = dort || cligne ? Visibility.Hidden : Visibility.Visible;
        _yeuxFermes.Visibility = dort || cligne ? Visibility.Visible : Visibility.Hidden;
        _zzz.Visibility = dort ? Visibility.Visible : Visibility.Hidden;
        _zzz.Opacity = 0.5 + 0.5 * Math.Sin(t * 2);

        _saut.Y = _etat == Etat.Saute ? -Math.Sin(Math.Min(t / 0.7, 1) * Math.PI) * 14 : _etat == Etat.Marche ? -Math.Abs(Math.Sin(_phase)) * 1.5 : 0;
        _coeur.Visibility = _etat == Etat.Saute ? Visibility.Visible : Visibility.Hidden;
        _coeur.Opacity = 1 - Math.Min(t / 0.7, 1) * 0.6;

        _miroir.ScaleX = _direction;

        if (_etat == Etat.Marche)
        {
            double gauche = SystemParameters.VirtualScreenLeft, droite = gauche + SystemParameters.VirtualScreenWidth - ActualWidth;
            double x = Left + _direction * 1.6;
            if (x <= gauche || x >= droite) { _direction = -_direction; x = Math.Clamp(x, gauche, droite); }
            else if (App.Instance.Config.SansChevauchement)
            {
                var suivante = Zone;
                suivante.Offset(x - Left, 0);
                if (App.Instance.Obstacles(this).Any(o => Placement.Chevauche(o, suivante))) { _direction = -_direction; return; }
            }
            Left = x;
            if (Math.Abs(Left - x) > 0.5) _direction = -_direction;
        }
    }

    protected override void Clic() => ChangerEtat(Etat.Saute);

    protected override void RemplirMenu()
    {
        var menu = SousMenu("Pelage");
        foreach (var (nom, _, _) in Pelages)
            Coche(nom, nom == Pelage, _ => { SetOption("pelage", nom); Config.X = Left; App.Instance.AppliquerStyle(); }, menu);
        Item("Réveiller / faire marcher", () => ChangerEtat(Etat.Marche));
        Item("Faire la sieste", () => ChangerEtat(Etat.Dort));
    }
}
