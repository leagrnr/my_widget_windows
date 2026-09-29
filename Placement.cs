using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace MesWidgets;

static class Placement
{
    public const double Ecart = 4;
    const double MargeEcran = 8;

    public static Point Aimanter(Rect z, IReadOnlyList<Rect> autres, IReadOnlyList<Rect> ecrans, double seuil)
    {
        var xs = new List<double>();
        var ys = new List<double>();
        var xsSecours = new List<double>();
        var ysSecours = new List<double>();
        foreach (var o in autres)
        {
            xs.Add(o.Left);
            ys.Add(o.Top);
            xsSecours.Add(o.Right - z.Width);
            ysSecours.Add(o.Bottom - z.Height);

            bool memeLigne = z.Top < o.Bottom + seuil && o.Top < z.Bottom + seuil;
            bool memeColonne = z.Left < o.Right + seuil && o.Left < z.Right + seuil;
            if (memeLigne) { xs.Add(o.Right + Ecart); xs.Add(o.Left - Ecart - z.Width); }
            if (memeColonne) { ys.Add(o.Bottom + Ecart); ys.Add(o.Top - Ecart - z.Height); }
        }
        foreach (var e in ecrans)
        {
            xs.Add(e.Left + MargeEcran);
            xs.Add(e.Right - MargeEcran - z.Width);
            ys.Add(e.Top + MargeEcran);
            ys.Add(e.Bottom - MargeEcran - z.Height);
        }
        return new Point(PlusProche(z.X, xs, xsSecours, seuil), PlusProche(z.Y, ys, ysSecours, seuil));
    }

    static double PlusProche(double valeur, List<double> candidats, List<double> secours, double seuil)
    {
        foreach (var liste in new[] { candidats, secours })
        {
            double meilleur = valeur, distance = double.MaxValue;
            foreach (var c in liste)
            {
                double d = Math.Abs(c - valeur);
                if (d <= seuil && d < distance) { distance = d; meilleur = c; }
            }
            if (distance != double.MaxValue) return meilleur;
        }
        return valeur;
    }
    public static Point Trouver(Point souhait, Size taille, IReadOnlyList<Rect> autres, IReadOnlyList<Rect> ecrans)
    {
        if (Libre(new Rect(souhait, taille), autres, ecrans)) return souhait;

        var xs = new List<double> { souhait.X };
        var ys = new List<double> { souhait.Y };
        foreach (var r in autres.Concat(ecrans))
        {
            xs.Add(r.Right); xs.Add(r.Left - taille.Width); xs.Add(r.Left);
            ys.Add(r.Bottom); ys.Add(r.Top - taille.Height); ys.Add(r.Top);
            xs.Add(r.Right + Ecart); xs.Add(r.Left - Ecart - taille.Width);
            ys.Add(r.Bottom + Ecart); ys.Add(r.Top - Ecart - taille.Height);
        }
        foreach (var e in ecrans) { xs.Add(e.Right - taille.Width); ys.Add(e.Bottom - taille.Height); }

        Point? meilleur = null;
        double distance = double.MaxValue;
        foreach (var x in xs.Distinct())
            foreach (var y in ys.Distinct())
            {
                double d = (x - souhait.X) * (x - souhait.X) + (y - souhait.Y) * (y - souhait.Y);
                if (d < distance && Libre(new Rect(x, y, taille.Width, taille.Height), autres, ecrans))
                {
                    distance = d;
                    meilleur = new Point(x, y);
                }
            }
        if (meilleur != null) return meilleur.Value;

        foreach (var e in ecrans)
            for (double y = e.Top; y + taille.Height <= e.Bottom; y += 20)
                for (double x = e.Left; x + taille.Width <= e.Right; x += 20)
                    if (Libre(new Rect(x, y, taille.Width, taille.Height), autres, ecrans)) return new Point(x, y);

        return souhait;
    }

    public static bool Libre(Rect r, IEnumerable<Rect> autres, IEnumerable<Rect> ecrans) =>
        ecrans.Any(e => Contient(e, r)) && !autres.Any(a => Chevauche(a, r));

    public static bool Chevauche(Rect a, Rect b) =>
        a.Left < b.Right - 0.5 && b.Left < a.Right - 0.5 && a.Top < b.Bottom - 0.5 && b.Top < a.Bottom - 0.5;

    static bool Contient(Rect e, Rect r) =>
        r.Left >= e.Left - 0.5 && r.Top >= e.Top - 0.5 && r.Right <= e.Right + 0.5 && r.Bottom <= e.Bottom + 0.5;

    public static List<Rect> Ecrans(Visual reference)
    {
        var dpi = VisualTreeHelper.GetDpi(reference);
        return Forms.Screen.AllScreens.Select(s => new Rect(
            s.WorkingArea.X / dpi.DpiScaleX, s.WorkingArea.Y / dpi.DpiScaleY,
            s.WorkingArea.Width / dpi.DpiScaleX, s.WorkingArea.Height / dpi.DpiScaleY)).ToList();
    }
}
