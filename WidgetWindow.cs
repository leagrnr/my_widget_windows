using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace MesWidgets;

public abstract class WidgetWindow : Window
{
    public WidgetConfig Config { get; }

    public virtual string Resume => "";

    protected static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    protected static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    protected static Brush Blanc => Theme.Texte;
    protected static Brush Pale => Theme.TexteDoux;
    Brush _accentPropre, _fondPropre;
    protected Brush Accent => _accentPropre ??= Option("_accent", "") is { Length: > 0 } hex ? Theme.Hex(hex) : Theme.Accent;
    protected Brush FondCarte => _fondPropre ??= Option("_fond", "") is { Length: > 0 } code ? Theme.FondPour(code) : Theme.Fond;
    protected static Brush Alerte => Theme.Alerte;
    protected const string Icones = "Segoe Fluent Icons, Segoe MDL2 Assets";

    static readonly (string Nom, double Valeur)[] Tailles = { ("Petite", 0.8), ("Normale", 1), ("Grande", 1.25), ("Très grande", 1.5), ("Énorme", 2) };
    public const double EchelleMin = 0.5, EchelleMax = 3;
    static readonly (string Nom, double Valeur)[] Opacites = { ("100 %", 1), ("85 %", 0.85), ("70 %", 0.7), ("50 %", 0.5) };

    readonly ContextMenu _menu = new();
    readonly List<DispatcherTimer> _minuteurs = new();
    IntPtr _hwnd;

    bool Verrouille => Option("_verrou", "non") == "oui";

    protected WidgetWindow(WidgetConfig config)
    {
        Config = config;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;
        UseLayoutRounding = true;
        ShowActivated = false;
        Left = config.X;
        Top = config.Y;

        var ecrans = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                              SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        if (!ecrans.Contains(new Point(Left + 40, Top + 20)))
        {
            Left = SystemParameters.WorkArea.Left + 60;
            Top = SystemParameters.WorkArea.Top + 60;
        }

        ContextMenu = _menu;
        ContextMenuOpening += (_, _) => ConstruireMenu();

        MouseLeftButtonDown += (_, _) =>
        {
            if (Verrouille) { Clic(); return; }
            if (!Deplacer()) Clic();
        };

        Loaded += (_, _) => App.Instance.Ranger(this);
        var attente = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        attente.Tick += (_, _) => { attente.Stop(); Bureau.RamenerDansEcran(_hwnd); App.Instance.Ranger(this); };
        SizeChanged += (_, _) => { if (_hwnd != IntPtr.Zero) Bureau.RamenerDansEcran(_hwnd); attente.Stop(); attente.Start(); };
        Closed += (_, _) => attente.Stop();

        Activated += (_, _) => Dispatcher.InvokeAsync(() => Bureau.AuFond(_hwnd));
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;
        Bureau.Coller(_hwnd);
        HwndSource.FromHwnd(_hwnd).AddHook(Bureau.GarderAuFond);
        Loaded += (_, _) => Bureau.RamenerDansEcran(_hwnd);
        ContentRendered += (_, _) => Bureau.RamenerDansEcran(_hwnd);
        AjouterPoignee();
        AppliquerApparence();
    }

    protected override void OnClosed(EventArgs e)
    {
        foreach (var t in _minuteurs) t.Stop();
        base.OnClosed(e);
    }

    public void AuFond() => Bureau.AuFond(_hwnd);

    protected IntPtr Hwnd => _hwnd;

    public Rect Zone => new(Left + 6, Top + 6, Math.Max(0, ActualWidth - 12), Math.Max(0, ActualHeight - 12));

    protected virtual void Clic() { }

    public void DemanderSuppression()
    {
        if (!ConfirmerSuppression()) return;
        Supprime();
        App.Instance.Supprimer(this);
    }

    protected virtual bool ConfirmerSuppression() => true;

    protected virtual void Supprime() { }

    void ConstruireMenu()
    {
        _menu.Items.Clear();
        RemplirMenu();
        if (_menu.Items.Count > 0) _menu.Items.Add(new Separator());
        MenuApparence();
        _menu.Items.Add(new Separator());
        var supprimer = Item("Supprimer ce widget", DemanderSuppression);
        supprimer.Icon = Glyphe("\uE74D", 13, Alerte);
        supprimer.Foreground = Alerte;
    }

    void OuvrirMenu()
    {
        ConstruireMenu();
        _menu.PlacementTarget = _modifier;
        _menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        _menu.IsOpen = true;
    }

    protected virtual void RemplirMenu() { }

    FrameworkElement _racine;
    Border _poignee;
    Border _barre;
    Border _modifier;
    double _echelle = 1;

    bool Deplacer()
    {
        double x = Left, y = Top;
        try { DragMove(); } catch (InvalidOperationException) { return false; }
        if (Math.Abs(Left - x) < 1 && Math.Abs(Top - y) < 1) return false;
        Config.X = Left;
        Config.Y = Top;
        App.Instance.Sauver();
        if (App.Instance.Config.Magnetisme) App.Instance.Aimanter(this);
        App.Instance.Ranger(this);
        Bureau.AuFond(_hwnd);
        return true;
    }

    void ChangerStyle(string cle, string valeur)
    {
        if (valeur == "") Config.Options.Remove(cle); else Config.Options[cle] = valeur;
        App.Instance.Recreer(this);
    }

    public double Echelle => _echelle;

    public void ChangerEchelle(double echelle) => Redimensionner(echelle, sauver: true, parUtilisateur: false);

    void AppliquerApparence()
    {
        Redimensionner(double.Parse(Option("_taille", "1"), Inv), sauver: false);
        Opacity = double.Parse(Option("_opacite", "1"), Inv);
    }

    void Redimensionner(double echelle, bool sauver, bool parUtilisateur = true)
    {
        _echelle = Math.Clamp(Math.Round(echelle, 2), EchelleMin, EchelleMax);
        if (_racine != null)
            _racine.LayoutTransform = _echelle == 1 ? Transform.Identity : new ScaleTransform(_echelle, _echelle);
        if (sauver && parUtilisateur) Config.Options.Remove("_voulue");
        if (sauver) SetOption("_taille", _echelle.ToString("0.##", Inv));
    }

    void AjouterPoignee()
    {
        _racine = Content as FrameworkElement;
        if (_racine == null) return;

        _poignee = new Border
        {
            Width = 18, Height = 18,
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 8, 8),
            Cursor = Cursors.SizeNWSE,
            Opacity = 0,
            ToolTip = "Glisser pour redimensionner (ou Ctrl + molette)",
            Child = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M 13,4 L 4,13 M 13,9 L 9,13"),
                Stroke = Pale,
                StrokeThickness = 1.6,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
            },
        };
        Content = null;
        _modifier = new Border
        {
            Width = 28, Height = 28,
            CornerRadius = new CornerRadius(14),
            Background = FondCarte,
            BorderBrush = Theme.Piste,
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 4, 4, 0),
            Cursor = Cursors.Hand,
            Opacity = 0,
            ToolTip = "Modifier ce widget",
            Child = Glyphe("\uE70F", 12),
        };
        _modifier.MouseEnter += (_, _) => _modifier.Background = Accent;
        _modifier.MouseLeave += (_, _) => _modifier.Background = FondCarte;
        _modifier.MouseLeftButtonDown += (_, e) => e.Handled = true;
        _modifier.MouseLeftButtonUp += (_, e) => { e.Handled = true; OuvrirMenu(); };

        var trait = new Border
        {
            Width = 40, Height = 5,
            CornerRadius = new CornerRadius(2.5),
            Background = Pale,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _barre = new Border
        {
            Width = 90, Height = 16,
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 11, 0, 0),
            Cursor = Cursors.SizeAll,
            Opacity = 0,
            ToolTip = "Glisser pour déplacer",
            Child = trait,
        };
        _barre.MouseEnter += (_, _) => trait.Background = Accent;
        _barre.MouseLeave += (_, _) => trait.Background = Pale;
        _barre.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            if (!Verrouille) Deplacer();
        };

        var conteneur = new Grid();
        conteneur.Children.Add(_racine);
        conteneur.Children.Add(_barre);
        conteneur.Children.Add(_poignee);
        conteneur.Children.Add(_modifier);
        Content = conteneur;

        MouseEnter += (_, _) =>
        {
            _poignee.Opacity = Verrouille ? 0 : 1;
            _barre.Opacity = Verrouille ? 0 : 1;
            _barre.IsHitTestVisible = !Verrouille;
            _modifier.Opacity = 1;
        };
        MouseLeave += (_, _) =>
        {
            if (!_poignee.IsMouseCaptured) _poignee.Opacity = 0;
            _barre.Opacity = 0;
            if (!_menu.IsOpen) _modifier.Opacity = 0;
        };
        _menu.Closed += (_, _) => { if (!IsMouseOver) _modifier.Opacity = 0; };

        Point depart = default;
        double echelleDepart = 1;
        Size tailleDepart = default;
        _poignee.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            if (Verrouille) return;
            depart = PointToScreen(e.GetPosition(this));
            echelleDepart = _echelle;
            tailleDepart = new Size(Math.Max(1, _racine.ActualWidth * _echelle), Math.Max(1, _racine.ActualHeight * _echelle));
            _poignee.CaptureMouse();
        };
        _poignee.MouseMove += (_, e) =>
        {
            if (!_poignee.IsMouseCaptured) return;
            var p = PointToScreen(e.GetPosition(this));
            var dpi = VisualTreeHelper.GetDpi(this);
            double dx = (p.X - depart.X) / dpi.DpiScaleX, dy = (p.Y - depart.Y) / dpi.DpiScaleY;
            double facteur = ((tailleDepart.Width + dx) / tailleDepart.Width + (tailleDepart.Height + dy) / tailleDepart.Height) / 2;
            Redimensionner(echelleDepart * facteur, sauver: false);
        };
        _poignee.MouseLeftButtonUp += (_, _) =>
        {
            if (!_poignee.IsMouseCaptured) return;
            _poignee.ReleaseMouseCapture();
            if (!IsMouseOver) _poignee.Opacity = 0;
            Redimensionner(_echelle, sauver: true);
        };

        PreviewMouseWheel += (_, e) =>
        {
            if (Keyboard.Modifiers != ModifierKeys.Control || Verrouille) return;
            e.Handled = true;
            Redimensionner(_echelle * (e.Delta > 0 ? 1.08 : 1 / 1.08), sauver: true);
        };
    }

    void MenuApparence()
    {
        var taille = SousMenu($"Taille ({_echelle * 100:0} %)");
        taille.Icon = Glyphe("\uE740", 13);
        foreach (var (nom, v) in Tailles)
            Coche($"{nom} ({v * 100:0} %)", Math.Abs(v - _echelle) < 0.005, _ => Redimensionner(v, sauver: true), taille);
        taille.Items.Add(new Separator());
        taille.Items.Add(new MenuItem { Header = "Astuce : tire le coin en bas à droite, ou Ctrl + molette", IsEnabled = false });

        var opacite = SousMenu("Opacité");
        opacite.Icon = Glyphe("\uE706", 13);
        var op = Option("_opacite", "1");
        foreach (var (nom, v) in Opacites)
            Coche(nom, v.ToString(Inv) == op, _ => { SetOption("_opacite", v.ToString(Inv)); AppliquerApparence(); }, opacite);

        var couleur = SousMenu("Couleur de ce widget");
        couleur.Icon = Glyphe("", 13);
        var accent = Option("_accent", "");
        Coche("Comme les autres", accent == "", _ => ChangerStyle("_accent", ""), couleur);
        foreach (var (nom, hex) in Theme.Accents)
        {
            var m = Coche(nom, string.Equals(accent, hex, StringComparison.OrdinalIgnoreCase), _ => ChangerStyle("_accent", hex), couleur);
            m.Icon = new Border { Width = 12, Height = 12, CornerRadius = new CornerRadius(6), Background = Theme.Hex(hex) };
        }

        var fond = SousMenu("Fond de ce widget");
        var fondActuel = Option("_fond", "");
        Coche("Comme les autres", fondActuel == "", _ => ChangerStyle("_fond", ""), fond);
        foreach (var (nom, code) in Theme.Fonds)
            Coche(nom, fondActuel == code, _ => ChangerStyle("_fond", code), fond);

        Coche("Verrouiller la position et la taille", Verrouille, oui => SetOption("_verrou", oui ? "oui" : "non")).Icon = Glyphe("\uE72E", 13);
    }

    protected string Option(string cle, string defaut) =>
        Config.Options.TryGetValue(cle, out var v) ? v : defaut;

    protected void SetOption(string cle, string valeur)
    {
        Config.Options[cle] = valeur;
        App.Instance.Sauver();
    }

    protected void Minuteur(TimeSpan intervalle, Action action)
    {
        var t = new DispatcherTimer { Interval = intervalle };
        t.Tick += (_, _) => action();
        t.Start();
        _minuteurs.Add(t);
    }

    protected MenuItem Item(string texte, Action action, ItemsControl parent = null)
    {
        var m = new MenuItem { Header = texte };
        m.Click += (_, _) => action();
        (parent ?? _menu).Items.Add(m);
        return m;
    }

    protected MenuItem Coche(string texte, bool coche, Action<bool> action, ItemsControl parent = null)
    {
        var m = new MenuItem { Header = texte, IsCheckable = true, IsChecked = coche };
        m.Click += (_, _) => action(m.IsChecked);
        (parent ?? _menu).Items.Add(m);
        return m;
    }

    protected MenuItem SousMenu(string texte)
    {
        var m = new MenuItem { Header = texte };
        _menu.Items.Add(m);
        return m;
    }

    protected Border Carte(UIElement contenu) => new()
    {
        Child = contenu,
        CornerRadius = new CornerRadius(Theme.Arrondi),
        Background = FondCarte,
        Padding = new Thickness(18, 12, 18, 14),
        Margin = new Thickness(10),
        Effect = Ombre(),
    };

    protected static DropShadowEffect Ombre() => new() { BlurRadius = 14, ShadowDepth = 2, Opacity = 0.35 };

    protected static TextBlock Texte(string texte, double taille, Brush couleur = null, string police = null)
    {
        if (police == null || (police == "Segoe UI Light" && Theme.Police != "Segoe UI")) police = Theme.Police;
        return new TextBlock
        {
            Text = texte,
            FontSize = taille,
            Foreground = couleur ?? Blanc,
            FontFamily = new FontFamily(police),
        };
    }

    protected static TextBlock Titre(string texte)
    {
        var t = Texte(texte, 16);
        t.FontWeight = FontWeights.SemiBold;
        return t;
    }

    protected static TextBlock Glyphe(string code, double taille, Brush couleur = null)
    {
        var t = Texte(code, taille, couleur, Icones);
        t.VerticalAlignment = VerticalAlignment.Center;
        t.HorizontalAlignment = HorizontalAlignment.Center;
        return t;
    }

    protected static T Cliquable<T>(T element, Action action, bool survol = true) where T : FrameworkElement
    {
        element.Cursor = Cursors.Hand;
        element.MouseLeftButtonDown += (_, e) => { e.Handled = true; action(); };
        if (survol && element is Border b)
        {
            var fond = b.Background;
            b.MouseEnter += (_, _) => b.Background = Theme.Survol;
            b.MouseLeave += (_, _) => b.Background = fond;
        }
        return element;
    }

    protected static Border Bouton(UIElement contenu, Action action, double taille = 0)
    {
        var b = new Border
        {
            Child = contenu,
            CornerRadius = new CornerRadius(taille > 0 ? taille / 2 : 8),
            Background = Brushes.Transparent,
            Padding = taille > 0 ? new Thickness(0) : new Thickness(10, 4, 10, 5),
        };
        if (taille > 0) { b.Width = taille; b.Height = taille; }
        return Cliquable(b, action);
    }

    protected (Grid Barre, Border Rempli) Barre(double hauteur = 6)
    {
        var rempli = new Border { Height = hauteur, CornerRadius = new CornerRadius(hauteur / 2), HorizontalAlignment = HorizontalAlignment.Left, Background = Accent, Width = 0 };
        var barre = new Grid();
        barre.Children.Add(new Border { Height = hauteur, CornerRadius = new CornerRadius(hauteur / 2), Background = Theme.Piste });
        barre.Children.Add(rempli);
        return (barre, rempli);
    }

    protected static Border Separateur() =>
        new() { Height = 1, Margin = new Thickness(0, 10, 0, 8), Background = Theme.Piste };

    protected static void Ouvrir(string cible)
    {
        try
        {
            if (cible.StartsWith(@"shell:AppsFolder\", StringComparison.OrdinalIgnoreCase))
                Process.Start("explorer.exe", cible);
            else
                Process.Start(new ProcessStartInfo(cible) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show($"Impossible d'ouvrir :\n{cible}\n\n{ex.Message}", "Mes Widgets"); }
    }

    protected string Ville => Option("ville", "Paris");
    protected string Latitude => Option("lat", "48.8566");
    protected string Longitude => Option("lon", "2.3522");

    protected async System.Threading.Tasks.Task<bool> ChoisirVille()
    {
        var saisie = Saisie.Demander("Ville", "Nom de la ville :", Ville);
        if (saisie == null) return false;
        try
        {
            var lieu = await Lieux.Chercher(saisie);
            if (lieu == null)
            {
                MessageBox.Show($"Je ne trouve pas la ville « {saisie} ».", "Ville", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            Config.Options["lat"] = lieu.Latitude.ToString(Inv);
            Config.Options["lon"] = lieu.Longitude.ToString(Inv);
            SetOption("ville", lieu.Nom);
            App.Instance.Notifier();
            return true;
        }
        catch
        {
            MessageBox.Show("Impossible de joindre le service de recherche de villes.", "Ville", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    protected static string Majuscule(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], Fr) + s[1..];

    protected static string Relatif(DateTime date)
    {
        var d = DateTime.Now - date;
        if (d.TotalMinutes < 1) return "à l'instant";
        if (d.TotalMinutes < 60) return $"il y a {(int)d.TotalMinutes} min";
        if (d.TotalHours < 24) return $"il y a {(int)d.TotalHours} h";
        return $"il y a {(int)d.TotalDays} j";
    }
}
