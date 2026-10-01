using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesWidgets;

static class MessageBox
{
    public static MessageBoxResult Show(string texte) => Show(null, texte, "Mes Widgets", MessageBoxButton.OK, MessageBoxImage.None);
    public static MessageBoxResult Show(string texte, string titre) => Show(null, texte, titre, MessageBoxButton.OK, MessageBoxImage.None);
    public static MessageBoxResult Show(string texte, string titre, MessageBoxButton boutons, MessageBoxImage image) => Show(null, texte, titre, boutons, image);
    public static MessageBoxResult Show(Window proprietaire, string texte, string titre) => Show(proprietaire, texte, titre, MessageBoxButton.OK, MessageBoxImage.None);

    public static MessageBoxResult Show(Window proprietaire, string texte, string titre, MessageBoxButton boutons, MessageBoxImage image)
    {
        var alerte = new Alerte(texte, titre, boutons, image);
        if (proprietaire != null && proprietaire.IsVisible)
        {
            alerte.Owner = proprietaire;
            alerte.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        alerte.ShowDialog();
        return alerte.Resultat;
    }
}

class Alerte : Dialogue
{
    public MessageBoxResult Resultat { get; private set; }

    public Alerte(string texte, string titre, MessageBoxButton boutons, MessageBoxImage image) : base(titre, 430)
    {
        bool question = boutons is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel or MessageBoxButton.OKCancel;
        bool danger = question && (texte.StartsWith("Supprimer") || texte.StartsWith("Désinstaller") || texte.StartsWith("Remplacer"));
        Resultat = question ? (boutons == MessageBoxButton.OKCancel ? MessageBoxResult.Cancel : MessageBoxResult.No) : MessageBoxResult.OK;

        var (glyphe, couleur) = (image, danger) switch
        {
            (_, true) => ("", "#EF4444"),
            (MessageBoxImage.Error, _) => ("", "#EF4444"),
            (MessageBoxImage.Warning, _) => ("", "#F59E0B"),
            (MessageBoxImage.Question, _) => ("", null),
            _ => question ? ("", null) : ("", null),
        };
        var icone = new TextBlock
        {
            Text = glyphe,
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 26,
            Margin = new Thickness(0, 2, 16, 0),
            VerticalAlignment = VerticalAlignment.Top,
        };
        if (couleur != null) icone.Foreground = Theme.Hex(couleur);
        else icone.SetResourceReference(TextBlock.ForegroundProperty, "DlgAccent");

        var message = new TextBlock { Text = texte, FontSize = 14.5, TextWrapping = TextWrapping.Wrap, LineHeight = 21, VerticalAlignment = VerticalAlignment.Center };
        var ligne = new DockPanel { Margin = new Thickness(0, 4, 0, 6) };
        DockPanel.SetDock(icone, Dock.Left);
        ligne.Children.Add(icone);
        ligne.Children.Add(message);
        Ajouter(ligne);

        if (!question)
        {
            Boutons(Principal("OK", () => { Resultat = MessageBoxResult.OK; Valider(); }));
            return;
        }

        var oui = Principal(boutons == MessageBoxButton.OKCancel ? "OK" : "Oui", () =>
        {
            Resultat = boutons == MessageBoxButton.OKCancel ? MessageBoxResult.OK : MessageBoxResult.Yes;
            Valider();
        });
        if (danger)
        {
            oui.Background = Theme.Hex("#EF4444");
            oui.BorderBrush = Theme.Hex("#EF4444");
        }
        var non = Secondaire(boutons == MessageBoxButton.OKCancel ? "Annuler" : "Non");
        Boutons(non, oui);
        Loaded += (_, _) => (danger ? non : oui).Focus();
    }
}
