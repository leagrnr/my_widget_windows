using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MesWidgets;

public class CalendrierWidget : WidgetWindow
{
    DateTime _aujourdhui = DateTime.Today;
    DateTime _mois;
    readonly TextBlock _titre;
    readonly UniformGrid _grille = new() { Columns = 7 };

    public CalendrierWidget(WidgetConfig c) : base(c)
    {
        _mois = new DateTime(_aujourdhui.Year, _aujourdhui.Month, 1);

        _titre = Texte("", 16);
        _titre.FontWeight = FontWeights.SemiBold;
        _titre.HorizontalAlignment = HorizontalAlignment.Center;
        _titre.VerticalAlignment = VerticalAlignment.Center;
        _titre.Cursor = Cursors.Hand;
        _titre.ToolTip = "Revenir au mois en cours";
        _titre.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            _mois = new DateTime(_aujourdhui.Year, _aujourdhui.Month, 1);
            Construire();
        };

        var entete = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        var precedent = Fleche("‹", -1);
        var suivant = Fleche("›", +1);
        DockPanel.SetDock(precedent, Dock.Left);
        DockPanel.SetDock(suivant, Dock.Right);
        entete.Children.Add(precedent);
        entete.Children.Add(suivant);
        entete.Children.Add(_titre);

        var pile = new StackPanel { Width = 238 };
        pile.Children.Add(entete);
        pile.Children.Add(_grille);
        Content = Carte(pile);

        Construire();
        Minuteur(TimeSpan.FromMinutes(1), () =>
        {
            if (DateTime.Today == _aujourdhui) return;
            _aujourdhui = DateTime.Today;
            _mois = new DateTime(_aujourdhui.Year, _aujourdhui.Month, 1);
            Construire();
        });
    }

    TextBlock Fleche(string symbole, int delta)
    {
        var t = Texte(symbole, 22);
        t.Cursor = Cursors.Hand;
        t.Padding = new Thickness(8, 0, 8, 0);
        t.VerticalAlignment = VerticalAlignment.Center;
        t.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            _mois = _mois.AddMonths(delta);
            Construire();
        };
        return t;
    }

    void Construire()
    {
        _titre.Text = Majuscule(_mois.ToString("MMMM yyyy", Fr));
        _grille.Children.Clear();

        foreach (var j in new[] { "L", "M", "M", "J", "V", "S", "D" })
        {
            var t = Texte(j, 12, Pale);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.Margin = new Thickness(0, 0, 0, 4);
            _grille.Children.Add(t);
        }

        int decalage = ((int)_mois.DayOfWeek + 6) % 7;
        for (int i = 0; i < decalage; i++) _grille.Children.Add(new Border());

        int nbJours = DateTime.DaysInMonth(_mois.Year, _mois.Month);
        for (int d = 1; d <= nbJours; d++)
        {
            var date = _mois.AddDays(d - 1);
            bool weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var t = Texte(d.ToString(), 13, weekend ? Pale : Blanc);
            t.HorizontalAlignment = HorizontalAlignment.Center;
            t.VerticalAlignment = VerticalAlignment.Center;

            var cellule = new Border { Width = 30, Height = 28, Margin = new Thickness(2), CornerRadius = new CornerRadius(14), Child = t };
            if (date == _aujourdhui)
            {
                cellule.Background = Accent;
                t.Foreground = System.Windows.Media.Brushes.White;
                t.FontWeight = FontWeights.SemiBold;
            }
            _grille.Children.Add(cellule);
        }
    }
}
