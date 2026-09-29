using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace MesWidgets;

public partial class GestionnaireWindow : Window
{
    Brush Gris => (Brush)FindResource("Gris");
    bool _chargement = true;

    public GestionnaireWindow()
    {
        InitializeComponent();
        var app = App.Instance;
        var config = app.Config;

        foreach (var categorie in Catalogue.Categories)
        {
            ListeTypes.Children.Add(new TextBlock { Text = categorie, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 2) });
            var tuiles = new WrapPanel { Margin = new Thickness(-5, 0, -5, 0) };
            foreach (var t in Catalogue.Types.Where(t => t.Categorie == categorie)) tuiles.Children.Add(Tuile(t));
            ListeTypes.Children.Add(tuiles);
        }

        (config.Theme == "clair" ? ThemeClair : ThemeSombre).IsChecked = true;
        ThemeSombre.Checked += (_, _) => app.ChangerTheme("sombre");
        ThemeClair.Checked += (_, _) => app.ChangerTheme("clair");
        ConstruireAccents();

        foreach (var p in Theme.Polices) ChoixPolice.Items.Add(new ComboBoxItem { Content = p, FontFamily = new FontFamily(p) });
        ChoixPolice.SelectedIndex = Math.Max(0, Array.IndexOf(Theme.Polices, config.Police));
        ChoixPolice.SelectionChanged += (_, _) => Appliquer(() => config.Police = Theme.Polices[ChoixPolice.SelectedIndex]);

        foreach (var (nom, _) in Theme.Arrondis) ChoixArrondi.Items.Add(nom);
        ChoixArrondi.SelectedIndex = Math.Max(0, Array.FindIndex(Theme.Arrondis, a => a.Rayon == config.Arrondi));
        ChoixArrondi.SelectionChanged += (_, _) => Appliquer(() => config.Arrondi = Theme.Arrondis[ChoixArrondi.SelectedIndex].Rayon);

        foreach (var (nom, _) in Theme.Fonds) ChoixFond.Items.Add(nom);
        ChoixFond.SelectedIndex = Math.Max(0, Array.FindIndex(Theme.Fonds, f => f.Code == config.Fond));
        ChoixFond.SelectionChanged += (_, _) => Appliquer(() => config.Fond = Theme.Fonds[ChoixFond.SelectedIndex].Code);

        if (RaccourcisClavier.Refuses.Count > 0)
        {
            RaccourcisRefuses.Text = $"⚠ Déjà utilisé par une autre application : {string.Join(", ", RaccourcisClavier.Refuses)}";
            RaccourcisRefuses.Visibility = Visibility.Visible;
        }

        BoutonExporter.Click += (_, _) =>
        {
            var d = new SaveFileDialog { Title = "Exporter ma disposition", Filter = "Sauvegarde Mes Widgets|*.json", FileName = $"MesWidgets-{DateTime.Today:yyyy-MM-dd}.json" };
            if (d.ShowDialog(this) != true) return;
            try { app.Exporter(d.FileName); MessageBox.Show(this, "Disposition exportée.", "Mes Widgets"); }
            catch (Exception ex) { MessageBox.Show(this, "Échec de l'export :\n" + ex.Message, "Mes Widgets"); }
        };
        BoutonImporter.Click += (_, _) =>
        {
            var d = new OpenFileDialog { Title = "Restaurer une sauvegarde", Filter = "Sauvegarde Mes Widgets|*.json" };
            if (d.ShowDialog(this) != true) return;
            if (MessageBox.Show(this, "Remplacer tes widgets actuels par ceux de la sauvegarde ?", "Mes Widgets",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try { app.Importer(d.FileName); Close(); app.OuvrirGestionnaire(); }
            catch (Exception ex) { MessageBox.Show(this, "Impossible de restaurer :\n" + ex.Message, "Mes Widgets"); }
        };
        BoutonRamener.Click += (_, _) => app.RamenerWidgets();

        CaseDemarrage.IsChecked = Demarrage.EstActive;
        CaseDemarrage.Checked += (_, _) => Demarrage.Activer(true);
        CaseDemarrage.Unchecked += (_, _) => Demarrage.Activer(false);

        CaseChevauchement.IsChecked = config.SansChevauchement;
        CaseChevauchement.Checked += (_, _) => { config.SansChevauchement = true; app.Sauver(); app.RangerTout(); };
        CaseChevauchement.Unchecked += (_, _) => { config.SansChevauchement = false; app.Sauver(); };

        ChampDepot.Text = config.Depot;
        BoutonVerifier.Click += async (_, _) =>
        {
            config.Depot = ChampDepot.Text.Trim();
            app.Sauver();
            EtatMaj.Text = "Vérification…";
            var resultat = await MiseAJour.Verifier();
            EtatMaj.Text = resultat switch
            {
                true => "",
                false => $"Tu as la dernière version ({Installation.Version}).",
                null => string.IsNullOrEmpty(config.Depot) ? "Indique d'abord ton dépôt GitHub (ex. pseudo/MesWidgets)." : "Impossible de trouver une version publiée sur ce dépôt.",
            };
        };
        BoutonInstallerMaj.Click += async (_, _) =>
        {
            BoutonInstallerMaj.IsEnabled = false;
            TexteMaj.Text = "Téléchargement de la mise à jour…";
            try { await MiseAJour.Installer(); }
            catch (Exception ex) { TexteMaj.Text = "Échec : " + ex.Message; BoutonInstallerMaj.IsEnabled = true; }
        };
        MiseAJour.Changement += AfficherMaj;
        AfficherMaj();

        TexteVersion.Text = $"Version {Installation.Version} · Fermer cette fenêtre garde tes widgets.";
        BoutonQuitter.Click += (_, _) => app.Quitter();

        app.WidgetsChanged += Rafraichir;
        Closed += (_, _) => { app.WidgetsChanged -= Rafraichir; MiseAJour.Changement -= AfficherMaj; };
        Rafraichir();
        _chargement = false;
    }

    void Appliquer(Action changement)
    {
        if (_chargement) return;
        changement();
        App.Instance.AppliquerStyle();
    }

    void ConstruireAccents()
    {
        ListeAccents.Children.Clear();
        foreach (var (nom, hex) in Theme.Accents)
        {
            bool choisi = string.Equals(hex, App.Instance.Config.Accent, StringComparison.OrdinalIgnoreCase);
            var rond = new Grid { Width = 26, Height = 26 };
            rond.Children.Add(new Ellipse { Fill = Theme.Hex(hex) });
            if (choisi) rond.Children.Add(new Ellipse { Stroke = Brushes.White, StrokeThickness = 2.5, Margin = new Thickness(3) });
            var b = new Button
            {
                Content = rond,
                ToolTip = nom,
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Template = new ControlTemplate(typeof(Button)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) },
            };
            b.Click += (_, _) =>
            {
                App.Instance.Config.Accent = hex;
                App.Instance.AppliquerStyle();
                ConstruireAccents();
            };
            ListeAccents.Children.Add(b);
        }
    }

    void AfficherMaj()
    {
        if (MiseAJour.Disponible is { } maj)
        {
            TexteMaj.Text = $"🎉 Mes Widgets {maj.Version} est disponible (tu as la {Installation.Version}).";
            BandeauMaj.Visibility = Visibility.Visible;
        }
        else BandeauMaj.Visibility = Visibility.Collapsed;
    }

    Button Tuile(TypeWidget t)
    {
        var pile = new StackPanel();
        pile.Children.Add(new TextBlock { Text = t.Icone, FontSize = 26, FontFamily = new FontFamily("Segoe UI Emoji") });
        pile.Children.Add(new TextBlock { Text = t.Nom, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 2) });
        pile.Children.Add(new TextBlock { Text = t.Description, FontSize = 12, Foreground = Gris, TextWrapping = TextWrapping.Wrap });

        var b = new Button
        {
            Content = pile,
            Style = (Style)FindResource("Tuile"),
            Width = 164,
            Margin = new Thickness(5),
            Padding = new Thickness(12),
            ToolTip = "Ajouter sur le bureau",
        };
        b.Click += (_, _) => App.Instance.Ajouter(t.Type);
        return b;
    }

    void Rafraichir()
    {
        var widgets = App.Instance.Widgets;
        ListeWidgets.Children.Clear();
        CadreListe.Visibility = widgets.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        Vide.Visibility = widgets.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

        for (int i = 0; i < widgets.Count; i++)
        {
            var w = widgets[i];
            var info = Catalogue.Info(w.Config.Type);

            var ligne = new Grid { Margin = new Thickness(14, 8, 8, 8) };
            ligne.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ligne.ColumnDefinitions.Add(new ColumnDefinition());
            ligne.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ligne.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icone = new TextBlock { Text = info.Icone, FontSize = 20, FontFamily = new FontFamily("Segoe UI Emoji"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };

            var textes = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textes.Children.Add(new TextBlock { Text = info.Nom, FontWeight = FontWeights.SemiBold });
            string resume;
            try { resume = w.Resume; } catch { resume = ""; }
            if (!string.IsNullOrEmpty(resume))
                textes.Children.Add(new TextBlock { Text = resume, FontSize = 12, Foreground = Gris, TextTrimming = TextTrimming.CharacterEllipsis });

            var supprimer = new Button { Content = "Supprimer", Style = (Style)FindResource("LienRouge") };
            supprimer.Click += (_, _) => w.DemanderSuppression();

            Grid.SetColumn(textes, 1);
            Grid.SetColumn(supprimer, 3);
            foreach (UIElement e in new UIElement[] { icone, textes, supprimer }) ligne.Children.Add(e);

            ListeWidgets.Children.Add(new Border
            {
                Child = ligne,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xEF, 0xF2)),
                BorderThickness = new Thickness(0, i == 0 ? 0 : 1, 0, 0),
            });
        }
    }
}
