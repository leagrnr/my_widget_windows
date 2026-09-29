using System.Windows;
using System.Windows.Controls;

namespace MesWidgets;

class Saisie : Dialogue
{
    readonly TextBox _champ;

    Saisie(string titre, string question, string valeur, double largeur) : base(titre, largeur)
    {
        Grand(titre);
        Etiquette(question).Margin = new Thickness(0, 8, 0, 8);
        _champ = Ajouter(new TextBox { Text = valeur ?? "" });
        Boutons(Secondaire("Annuler"), Principal("OK", () =>
        {
            if (!string.IsNullOrWhiteSpace(_champ.Text)) Valider();
        }));
        Loaded += (_, _) => { _champ.Focus(); _champ.SelectAll(); };
    }

    public static string Demander(string titre, string question, string valeur = "", double largeur = 420)
    {
        var f = new Saisie(titre, question, valeur, largeur);
        return f.ShowDialog() == true ? f._champ.Text.Trim() : null;
    }
}
