using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesWidgets;

static class Saisie
{
    public static string Demander(string titre, string question, string valeur = "", double largeur = 360)
    {
        var fenetre = new Window
        {
            Title = titre,
            Width = largeur,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            Topmost = true,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
        };
        var champ = new TextBox { Text = valeur ?? "", Margin = new Thickness(0, 8, 0, 14), Padding = new Thickness(4) };
        var ok = new Button { Content = "OK", IsDefault = true, Width = 90, Padding = new Thickness(4) };
        var annuler = new Button { Content = "Annuler", IsCancel = true, Width = 90, Padding = new Thickness(4), Margin = new Thickness(8, 0, 0, 0) };
        ok.Click += (_, _) => fenetre.DialogResult = true;

        var boutons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        boutons.Children.Add(ok);
        boutons.Children.Add(annuler);

        var pile = new StackPanel { Margin = new Thickness(16) };
        pile.Children.Add(new TextBlock { Text = question, TextWrapping = TextWrapping.Wrap });
        pile.Children.Add(champ);
        pile.Children.Add(boutons);
        fenetre.Content = pile;
        fenetre.Loaded += (_, _) => { champ.Focus(); champ.SelectAll(); };

        return fenetre.ShowDialog() == true && !string.IsNullOrWhiteSpace(champ.Text) ? champ.Text.Trim() : null;
    }
}
