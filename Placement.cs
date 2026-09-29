using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace MesWidgets;

static class Placement
{
    public static Point Trouver(Point souhait, Size taille, IReadOnlyList<Rect> autres, IReadOnlyList<Rect> ecrans)
    {
        if (Libre(new Rect(souhait, taille), autres, ecrans)) return souhait;

        var xs = new List<double> { souhait.X };
        var ys = new List<double> { souhait.Y };
        foreach (var r in autres.Concat(ecrans))
        {
            xs.Add(r.Right); xs.Add(r.Left - taille.Width); xs.Add(r.Left);
            ys.Add(r.Bottom); ys.Add(r.Top - taille.Height); ys.Add(r.Top);
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
