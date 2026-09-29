using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Forms = System.Windows.Forms;

namespace MesWidgets;

public class BatterieWidget : WidgetWindow
{
    const double Diametre = 96, Epaisseur = 9;
    static readonly Brush Vert = Theme.B(0xFF, 0x22, 0xC5, 0x5E);

    readonly Path _arc;
    readonly TextBlock _pourcent, _etat, _detail;
    bool _alertePleine, _alerteFaible;

    public override string Resume => _pourcent.Text;

    public BatterieWidget(WidgetConfig c) : base(c)
    {
        var jauge = new Grid { Width = Diametre, Height = Diametre, Margin = new Thickness(0, 0, 16, 0) };
        jauge.Children.Add(new Ellipse { Stroke = Theme.Piste, StrokeThickness = Epaisseur });
        _arc = new Path { StrokeThickness = Epaisseur, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        jauge.Children.Add(_arc);
        _pourcent = Texte("", 22);
        _pourcent.FontWeight = FontWeights.SemiBold;
        _pourcent.HorizontalAlignment = HorizontalAlignment.Center;
        _pourcent.VerticalAlignment = VerticalAlignment.Center;
        jauge.Children.Add(_pourcent);

        _etat = Titre("");
        _detail = Texte("", 13, Pale);
        _detail.TextWrapping = TextWrapping.Wrap;
        var infos = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Width = 130 };
        infos.Children.Add(Texte("BATTERIE", 11, Pale));
        infos.Children.Add(_etat);
        infos.Children.Add(_detail);

        var ligne = new StackPanel { Orientation = Orientation.Horizontal };
        ligne.Children.Add(jauge);
        ligne.Children.Add(infos);
        Content = Carte(ligne);

        MettreAJour();
        Minuteur(TimeSpan.FromSeconds(5), MettreAJour);
    }

    void MettreAJour()
    {
        var s = Forms.SystemInformation.PowerStatus;
        if (s.BatteryChargeStatus.HasFlag(Forms.BatteryChargeStatus.NoSystemBattery))
        {
            _pourcent.Text = "—";
            _etat.Text = "Pas de batterie";
            _detail.Text = "PC branché sur secteur";
            _arc.Data = null;
            return;
        }

        double niveau = Math.Clamp(s.BatteryLifePercent, 0, 1);
        bool branche = s.PowerLineStatus == Forms.PowerLineStatus.Online;
        bool enCharge = s.BatteryChargeStatus.HasFlag(Forms.BatteryChargeStatus.Charging);

        _pourcent.Text = $"{niveau * 100:0} %";
        _arc.Stroke = niveau < 0.2 && !branche ? Alerte : branche ? Vert : Accent;
        _arc.Data = Arc(niveau);

        if (branche)
        {
            _etat.Text = niveau >= 0.995 ? "Chargée" : enCharge ? "En charge ⚡" : "Branchée";
            _detail.Text = niveau >= 0.995 ? "Tu peux débrancher le chargeur" : "Sur secteur";
        }
        else
        {
            _etat.Text = "Sur batterie";
            int reste = s.BatteryLifeRemaining;
            _detail.Text = reste > 0 ? $"Environ {Duree(reste)} restantes" : "Calcul du temps restant…";
        }

        if (branche && niveau >= 0.995 && !_alertePleine)
        {
            _alertePleine = true;
            App.Instance.Notification("Batterie chargée 🔋", "La batterie est pleine, tu peux débrancher le chargeur.");
        }
        if (!branche) _alertePleine = false;
        if (!branche && niveau <= 0.15 && !_alerteFaible)
        {
            _alerteFaible = true;
            App.Instance.Notification("Batterie faible 🪫", $"Plus que {niveau * 100:0} % : pense à brancher ton PC.");
        }
        if (branche) _alerteFaible = false;
    }

    static string Duree(int secondes)
    {
        var t = TimeSpan.FromSeconds(secondes);
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours} h {t.Minutes:00}" : $"{t.Minutes} min";
    }

    static Geometry Arc(double fraction)
    {
        double r = (Diametre - Epaisseur) / 2, cx = Diametre / 2, cy = Diametre / 2;
        if (fraction >= 0.999) return new EllipseGeometry(new Point(cx, cy), r, r);
        if (fraction <= 0.001) return null;
        double angle = fraction * 2 * Math.PI;
        var debut = new Point(cx, cy - r);
        var fin = new Point(cx + r * Math.Sin(angle), cy - r * Math.Cos(angle));
        var figure = new PathFigure { StartPoint = debut, IsClosed = false };
        figure.Segments.Add(new ArcSegment(fin, new Size(r, r), 0, fraction > 0.5, SweepDirection.Clockwise, true));
        return new PathGeometry(new[] { figure });
    }
}
