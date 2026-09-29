using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MesWidgets;

public class MicroWidget : WidgetWindow
{
    readonly Border _bouton;
    readonly TextBlock _etat;
    bool? _muet;

    public override string Resume => _etat.Text;

    public MicroWidget(WidgetConfig c) : base(c)
    {
        _bouton = Cliquable(new Border
        {
            Width = 64, Height = 64,
            CornerRadius = new CornerRadius(32),
            Margin = new Thickness(0, 0, 14, 0),
            ToolTip = "Couper / activer le micro (Win+Alt+M)",
        }, Basculer, survol: false);

        _etat = Texte("", 16);
        _etat.FontWeight = FontWeights.SemiBold;
        var infos = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        infos.Children.Add(_etat);
        infos.Children.Add(Texte("Win+Alt+M", 12, Pale));

        var ligne = new StackPanel { Orientation = Orientation.Horizontal };
        ligne.Children.Add(_bouton);
        ligne.Children.Add(infos);
        Content = Carte(ligne);

        Lire();
        Minuteur(TimeSpan.FromMilliseconds(500), Lire);
    }

    void Basculer()
    {
        try { Audio.MicroMuet = !Audio.MicroMuet; } catch { }
        Lire();
    }

    void Lire()
    {
        bool? muet;
        try { muet = Audio.MicroMuet; } catch { muet = null; }
        if (muet == _muet && _bouton.Child != null) return;
        _muet = muet;

        var icone = new Grid { Width = 30, Height = 30 };
        icone.Children.Add(Glyphe("", 26, Brushes.White));
        if (muet != false)
        {
            icone.Children.Add(new Line { X1 = 3, Y1 = 3, X2 = 27, Y2 = 27, Stroke = Brushes.White, StrokeThickness = 3, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
        }
        _bouton.Child = icone;
        _bouton.Background = muet switch { false => Accent, true => Alerte, null => Theme.Piste };
        _etat.Text = muet switch { false => "Micro activé", true => "Micro coupé", null => "Aucun micro" };
    }
}
