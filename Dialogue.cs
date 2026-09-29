using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;

namespace MesWidgets;

class Dialogue : Window
{
    static ResourceDictionary _styles;

    protected readonly StackPanel Corps = new();
    readonly StackPanel _pied = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

    protected Dialogue(string titre, double largeur, double hauteur = double.NaN)
    {
        Title = titre;
        Width = largeur;
        if (double.IsNaN(hauteur)) SizeToContent = SizeToContent.Height;
        else Height = hauteur;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = double.IsNaN(hauteur) ? ResizeMode.NoResize : ResizeMode.CanResize;
        MinHeight = 200;
        ShowInTaskbar = true;
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 14;
        UseLayoutRounding = true;

        _styles ??= new ResourceDictionary { Source = new Uri("pack://application:,,,/MesWidgets;component/Dialogues.xaml") };
        Resources.MergedDictionaries.Add(_styles);
        SetResourceReference(BackgroundProperty, "DlgFond");
        SetResourceReference(ForegroundProperty, "DlgTexte");

        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 44,
            GlassFrameThickness = new Thickness(0),
            ResizeBorderThickness = new Thickness(double.IsNaN(hauteur) ? 0 : 6),
            UseAeroCaptionButtons = false,
        });

        var nom = new TextBlock { Text = titre, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(24, 0, 0, 0) };
        nom.SetResourceReference(TextBlock.ForegroundProperty, "DlgTexteDoux");
        var fermer = new Button
        {
            Style = (Style)_styles["BoutonFermer"],
            Width = 46,
            Content = new TextBlock { Text = "", FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = 10 },
        };
        WindowChrome.SetIsHitTestVisibleInChrome(fermer, true);
        fermer.Click += (_, _) => Close();
        var barre = new DockPanel { Height = 44 };
        DockPanel.SetDock(fermer, Dock.Right);
        barre.Children.Add(fermer);
        barre.Children.Add(nom);

        _pied.Margin = new Thickness(24, 12, 24, 22);
        var racine = new DockPanel();
        DockPanel.SetDock(barre, Dock.Top);
        DockPanel.SetDock(_pied, Dock.Bottom);
        racine.Children.Add(barre);
        racine.Children.Add(_pied);
        racine.Children.Add(new Border { Child = Corps, Margin = new Thickness(24, 0, 24, 0) });
        Content = racine;
    }

    protected TextBlock Grand(string texte)
    {
        var t = new TextBlock { Text = texte, FontSize = 21, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) };
        Corps.Children.Add(t);
        return t;
    }

    protected TextBlock Etiquette(string texte)
    {
        var t = new TextBlock { Text = texte, FontSize = 13, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 16, 0, 6) };
        Corps.Children.Add(t);
        return t;
    }

    protected TextBlock Aide(string texte, double margeHaut = 6)
    {
        var t = new TextBlock { Text = texte, FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, margeHaut, 0, 0), LineHeight = 18 };
        t.SetResourceReference(TextBlock.ForegroundProperty, "DlgTexteDoux");
        Corps.Children.Add(t);
        return t;
    }

    protected T Ajouter<T>(T element) where T : UIElement
    {
        Corps.Children.Add(element);
        return element;
    }

    protected Border Encart(string glyphe, string texte)
    {
        var icone = new TextBlock
        {
            Text = glyphe,
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 16,
            Margin = new Thickness(0, 1, 12, 0),
            VerticalAlignment = VerticalAlignment.Top,
        };
        icone.SetResourceReference(TextBlock.ForegroundProperty, "DlgAccent");
        var message = new TextBlock { Text = texte, FontSize = 12.5, TextWrapping = TextWrapping.Wrap, LineHeight = 18 };
        message.SetResourceReference(TextBlock.ForegroundProperty, "DlgTexteDoux");
        var ligne = new DockPanel();
        DockPanel.SetDock(icone, Dock.Left);
        ligne.Children.Add(icone);
        ligne.Children.Add(message);
        var encart = new Border { Child = ligne, CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 18, 0, 0) };
        encart.SetResourceReference(Border.BackgroundProperty, "DlgSurface");
        Corps.Children.Add(encart);
        return encart;
    }

    protected Button Principal(string texte, Action action)
    {
        var b = new Button { Content = texte, IsDefault = true, Style = (Style)_styles["BoutonPrincipal"], Margin = new Thickness(10, 0, 0, 0) };
        b.Click += (_, _) => action();
        return b;
    }

    protected Button Secondaire(string texte, Action action = null)
    {
        var b = new Button { Content = texte, Margin = new Thickness(10, 0, 0, 0) };
        if (action == null) { b.IsCancel = true; b.Click += (_, _) => Close(); }
        else b.Click += (_, _) => action();
        return b;
    }

    protected void Boutons(params Button[] boutons)
    {
        _pied.Children.Clear();
        foreach (var b in boutons) _pied.Children.Add(b);
    }

    protected void Valider() => DialogResult = true;
}
