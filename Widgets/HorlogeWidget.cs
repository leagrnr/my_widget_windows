using System;
using System.Windows;
using System.Windows.Controls;

namespace MesWidgets;

public class HorlogeWidget : WidgetWindow
{
    readonly TextBlock _heure, _date;

    bool Secondes => Option("secondes", "non") == "oui";

    public HorlogeWidget(WidgetConfig c) : base(c)
    {
        _heure = Texte("", 58, Blanc, "Segoe UI Light");
        _heure.HorizontalAlignment = HorizontalAlignment.Center;
        _date = Texte("", 15, Pale);
        _date.HorizontalAlignment = HorizontalAlignment.Center;

        var pile = new StackPanel { MinWidth = 230 };
        pile.Children.Add(_heure);
        pile.Children.Add(_date);
        Content = Carte(pile);

        MettreAJour();
        Minuteur(TimeSpan.FromSeconds(1), MettreAJour);
    }

    void MettreAJour()
    {
        var maintenant = DateTime.Now;
        _heure.Text = maintenant.ToString(Secondes ? "HH:mm:ss" : "HH:mm");
        _date.Text = Majuscule(maintenant.ToString("dddd d MMMM yyyy", Fr));
    }

    protected override void RemplirMenu()
    {
        Coche("Afficher les secondes", Secondes, oui =>
        {
            SetOption("secondes", oui ? "oui" : "non");
            MettreAJour();
        });
    }
}
