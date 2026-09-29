using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace MesWidgets;

public class CompteAReboursWidget : WidgetWindow
{
    readonly TextBlock _titre, _nombre, _unite, _date;

    string Nom => Option("titre", "Nouvel an");
    DateTime Cible => DateTime.TryParseExact(Option("date", ""), "yyyy-MM-dd", Inv, DateTimeStyles.None, out var d)
        ? d : new DateTime(DateTime.Today.Year + 1, 1, 1);

    public override string Resume => $"{Nom} · {Cible:dd/MM/yyyy}";

    public CompteAReboursWidget(WidgetConfig c) : base(c)
    {
        _titre = Titre("");
        _titre.HorizontalAlignment = HorizontalAlignment.Center;
        _titre.TextTrimming = TextTrimming.CharacterEllipsis;

        _nombre = Texte("", 56, Blanc, "Segoe UI Light");
        _unite = Texte("", 16, Pale);
        _unite.Margin = new Thickness(8, 0, 0, 12);
        _unite.VerticalAlignment = VerticalAlignment.Bottom;
        var ligne = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        ligne.Children.Add(_nombre);
        ligne.Children.Add(_unite);

        _date = Texte("", 13, Pale);
        _date.HorizontalAlignment = HorizontalAlignment.Center;

        var pile = new StackPanel { Width = 220 };
        pile.Children.Add(_titre);
        pile.Children.Add(ligne);
        pile.Children.Add(_date);
        Content = Carte(pile);

        MettreAJour();
        Minuteur(TimeSpan.FromMinutes(1), MettreAJour);
    }

    void MettreAJour()
    {
        _titre.Text = Nom;
        _date.Text = Majuscule(Cible.ToString("dddd d MMMM yyyy", Fr));
        int jours = (Cible - DateTime.Today).Days;
        _nombre.FontSize = 56;
        switch (jours)
        {
            case 0:
                _nombre.Text = "Aujourd'hui !";
                _nombre.FontSize = 34;
                _unite.Text = "";
                break;
            case > 0:
                _nombre.Text = jours.ToString("N0", Fr);
                _unite.Text = jours > 1 ? "jours" : "jour";
                break;
            default:
                _nombre.Text = (-jours).ToString("N0", Fr);
                _unite.Text = -jours > 1 ? "jours passés" : "jour passé";
                break;
        }
    }

    void Modifier()
    {
        var nom = Saisie.Demander("Compte à rebours", "Nom de l'événement :", Nom);
        if (nom == null) return;
        var texte = Saisie.Demander("Compte à rebours", "Date (jj/mm/aaaa) :", Cible.ToString("dd/MM/yyyy"));
        if (texte == null) return;
        if (!DateTime.TryParseExact(texte, new[] { "d/M/yyyy", "d/M/yy", "d-M-yyyy", "d.M.yyyy" }, Fr, DateTimeStyles.None, out var date))
        {
            MessageBox.Show("Je n'ai pas compris la date. Exemple : 24/12/2026", "Compte à rebours");
            return;
        }
        Config.Options["titre"] = nom;
        SetOption("date", date.ToString("yyyy-MM-dd", Inv));
        MettreAJour();
        App.Instance.Notifier();
    }

    protected override void RemplirMenu()
    {
        Item("Modifier l'événement…", Modifier);
        Item("Nouveau compte à rebours", () => App.Instance.Ajouter("rebours"));
    }
}
