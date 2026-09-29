using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MesWidgets;

static class IconeMeteo
{
    static readonly Brush Soleil = Theme.B(0xFF, 0xFF, 0xC8, 0x3D);
    static readonly Brush Lune   = Theme.B(0xFF, 0xF4, 0xE3, 0xA1);
    static readonly Brush Pluie  = Theme.B(0xFF, 0x5A, 0xB0, 0xFF);
    static Brush Neige        => Theme.Clair ? Theme.B(0xFF, 0x8F, 0xB8, 0xE8) : Brushes.White;
    static Brush NuageClair   => Theme.Clair ? Theme.B(0xFF, 0xCF, 0xD7, 0xE3) : Theme.B(0xFF, 0xF2, 0xF4, 0xF8);
    static Brush NuageSombre  => Theme.Clair ? Theme.B(0xFF, 0xA6, 0xB0, 0xBF) : Theme.B(0xFF, 0xAA, 0xB2, 0xBF);
    static readonly Brush NuageOrage = Theme.B(0xFF, 0x7D, 0x85, 0x94);

    public static FrameworkElement Creer(int code, bool jour, double taille)
    {
        var c = new Canvas { Width = 100, Height = 100 };
        switch (code)
        {
            case 0:
                if (jour) DessinerSoleil(c, 50, 50, 22); else DessinerLune(c, 50, 50, 28);
                break;
            case 1 or 2:
                if (jour) DessinerSoleil(c, 36, 36, 17); else DessinerLune(c, 36, 36, 20);
                Nuage(c, 22, 40, 0.8, NuageClair);
                break;
            case 3:
                Nuage(c, 4, 14, 0.7, NuageSombre);
                Nuage(c, 16, 34, 0.85, NuageClair);
                break;
            case 45 or 48:
                Brouillard(c);
                break;
            case >= 51 and <= 57:
                Nuage(c, 8, 10, 0.9, NuageClair);
                Gouttes(c, 2);
                break;
            case >= 61 and <= 67:
                Nuage(c, 8, 10, 0.9, NuageSombre);
                Gouttes(c, 3);
                break;
            case >= 80 and <= 82:
                if (jour) DessinerSoleil(c, 30, 28, 14);
                Nuage(c, 14, 18, 0.85, NuageClair);
                Gouttes(c, 3);
                break;
            case (>= 71 and <= 77) or 85 or 86:
                Nuage(c, 8, 10, 0.9, NuageClair);
                Flocons(c);
                break;
            case >= 95:
                Nuage(c, 8, 10, 0.9, NuageOrage);
                Eclair(c);
                break;
            default:
                Nuage(c, 8, 22, 0.9, NuageClair);
                break;
        }
        return new Viewbox { Width = taille, Height = taille, Child = c };
    }

    static void DessinerSoleil(Canvas c, double cx, double cy, double r)
    {
        for (int i = 0; i < 8; i++)
        {
            double a = i * Math.PI / 4, cos = Math.Cos(a), sin = Math.Sin(a);
            c.Children.Add(Trait(cx + cos * (r + 6), cy + sin * (r + 6), cx + cos * (r + 13), cy + sin * (r + 13), Soleil, 5));
        }
        c.Children.Add(new Path { Fill = Soleil, Data = new EllipseGeometry(new Point(cx, cy), r, r) });
    }

    static void DessinerLune(Canvas c, double cx, double cy, double r)
    {
        var croissant = new CombinedGeometry(GeometryCombineMode.Exclude,
            new EllipseGeometry(new Point(cx, cy), r, r),
            new EllipseGeometry(new Point(cx + r * 0.45, cy - r * 0.35), r * 0.85, r * 0.85));
        c.Children.Add(new Path { Fill = Lune, Data = croissant });
    }

    static void Nuage(Canvas c, double x, double y, double echelle, Brush couleur)
    {
        var forme = new GeometryGroup { FillRule = FillRule.Nonzero };
        forme.Children.Add(new EllipseGeometry(new Point(30, 38), 20, 20));
        forme.Children.Add(new EllipseGeometry(new Point(55, 28), 26, 26));
        forme.Children.Add(new EllipseGeometry(new Point(78, 42), 16, 16));
        forme.Children.Add(new RectangleGeometry(new Rect(10, 38, 84, 20), 10, 10));

        var t = new TransformGroup();
        t.Children.Add(new ScaleTransform(echelle, echelle));
        t.Children.Add(new TranslateTransform(x, y));
        forme.Transform = t;
        c.Children.Add(new Path { Fill = couleur, Data = forme });
    }

    static void Gouttes(Canvas c, int nombre)
    {
        double depart = nombre == 3 ? 36 : 44;
        for (int i = 0; i < nombre; i++)
        {
            double x = depart + i * 16;
            c.Children.Add(Trait(x, 72, x - 5, 88, Pluie, 5));
        }
    }

    static void Flocons(Canvas c)
    {
        foreach (var (x, y) in new[] { (36.0, 76.0), (54.0, 82.0), (72.0, 76.0), (45.0, 93.0), (63.0, 93.0) })
            c.Children.Add(new Path { Fill = Neige, Data = new EllipseGeometry(new Point(x, y), 4.5, 4.5) });
    }

    static void Eclair(Canvas c)
    {
        var p = new Polygon { Fill = Soleil };
        foreach (var (x, y) in new[] { (54.0, 60.0), (40.0, 82.0), (50.0, 82.0), (44.0, 99.0), (66.0, 73.0), (55.0, 73.0), (61.0, 60.0) })
            p.Points.Add(new Point(x, y));
        c.Children.Add(p);
    }

    static void Brouillard(Canvas c)
    {
        c.Children.Add(Trait(18, 32, 72, 32, NuageSombre, 7));
        c.Children.Add(Trait(28, 48, 86, 48, NuageClair, 7));
        c.Children.Add(Trait(14, 64, 70, 64, NuageSombre, 7));
        c.Children.Add(Trait(32, 80, 82, 80, NuageClair, 7));
    }

    static Line Trait(double x1, double y1, double x2, double y2, Brush couleur, double epaisseur) => new()
    {
        X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
        Stroke = couleur,
        StrokeThickness = epaisseur,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
    };
}
