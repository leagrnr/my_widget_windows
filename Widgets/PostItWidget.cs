using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MesWidgets;

public class PostItWidget : WidgetWindow
{
    static readonly (string Nom, string Hex)[] Couleurs =
    {
        ("Jaune", "#FFF59D"), ("Rose", "#F8BBD0"), ("Vert", "#C5E1A5"),
        ("Bleu", "#B3E5FC"), ("Violet", "#D1C4E9"), ("Orange", "#FFCC80"),
    };

    readonly Border _fond;
    readonly TextBox _texte;
    readonly DispatcherTimer _delai;

    public override string Resume
    {
        get
        {
            var ligne = _texte.Text.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? "";
            return ligne.Length == 0 ? "(vide)" : ligne.Length > 40 ? ligne[..40] + "…" : ligne;
        }
    }

    public PostItWidget(WidgetConfig c) : base(c)
    {
        _texte = new TextBox
        {
            Text = Option("texte", ""),
            Width = 230,
            MinHeight = 170,
            MaxHeight = 420,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            FontFamily = new FontFamily("Segoe Print"),
            FontSize = 15,
            Foreground = new SolidColorBrush(Color.FromRgb(0x2B, 0x2B, 0x2B)),
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        var poignee = new Border
        {
            Height = 16,
            Background = new SolidColorBrush(Color.FromArgb(0x14, 0, 0, 0)),
            Cursor = Cursors.SizeAll,
            ToolTip = "Glisser pour déplacer · ✏ pour les options",
        };

        var pile = new StackPanel();
        pile.Children.Add(poignee);
        pile.Children.Add(new Border { Padding = new Thickness(12, 8, 12, 12), Child = _texte });

        _fond = new Border { Child = pile, CornerRadius = new CornerRadius(2), Margin = new Thickness(10), Effect = Ombre() };
        AppliquerCouleur();
        Content = _fond;

        _delai = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _delai.Tick += (_, _) => Enregistrer();
        _texte.TextChanged += (_, _) => { _delai.Stop(); _delai.Start(); };
        Closed += (_, _) => { if (_delai.IsEnabled) Enregistrer(); };
    }

    void Enregistrer()
    {
        _delai.Stop();
        SetOption("texte", _texte.Text);
        App.Instance.Notifier();
    }

    void AppliquerCouleur()
    {
        var hex = Option("couleur", Couleurs[0].Hex);
        _fond.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }

    protected override void RemplirMenu()
    {
        var menu = SousMenu("Couleur");
        var actuelle = Option("couleur", Couleurs[0].Hex);
        foreach (var (nom, hex) in Couleurs)
            Coche(nom, hex == actuelle, _ => { SetOption("couleur", hex); AppliquerCouleur(); }, menu);
        Item("Nouveau post-it", () => App.Instance.Ajouter("postit"));
    }

    protected override bool ConfirmerSuppression() =>
        string.IsNullOrWhiteSpace(_texte.Text) ||
        MessageBox.Show("Supprimer ce post-it et son contenu ?", "Mes Widgets",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
