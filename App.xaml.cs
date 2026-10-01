using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace MesWidgets;

public partial class App : Application
{
    public static App Instance => (App)Current;

    public AppConfig Config { get; private set; }
    public IReadOnlyList<WidgetWindow> Widgets => _widgets;
    public event Action WidgetsChanged;

    readonly List<WidgetWindow> _widgets = new();
    Mutex _mutex;
    EventWaitHandle _signal;
    Forms.NotifyIcon _icone;
    GestionnaireWindow _gestionnaire;
    bool _quitte, _lectureSeule;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (Installation.Traiter(e.Args)) { Shutdown(); return; }

        _mutex = new Mutex(true, "MesWidgets.Instance", out bool seule);
        _signal = new EventWaitHandle(false, EventResetMode.AutoReset, "MesWidgets.Ouvrir");
        if (!seule) { _signal.Set(); Shutdown(); return; }
        new Thread(() => { while (_signal.WaitOne()) Dispatcher.InvokeAsync(OuvrirGestionnaire); }) { IsBackground = true }.Start();

        DispatcherUnhandledException += (_, ex) => { Journal(ex.Exception); ex.Handled = true; };
        SessionEnding += (_, _) => Sauver();

        Config = ConfigStore.Charger(out var etat);
        bool premierLancement = etat == ConfigStore.Etat.Nouveau;
        _lectureSeule = etat == ConfigStore.Etat.Indisponible;
        MettreAJourMenus();
        foreach (var c in Config.Widgets.ToList()) Afficher(c);
        CreerIcone();
        RaccourcisClavier.Enregistrer();
        MiseAJour.Demarrer();
        var regrandir = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        regrandir.Tick += (_, _) => Regrandir();
        regrandir.Start();
        _dernierClair = Theme.Clair;
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += (_, _) => Dispatcher.InvokeAsync(VerifierTheme);
        var horlogeTheme = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        horlogeTheme.Tick += (_, _) => VerifierTheme();
        horlogeTheme.Start();
        if (_lectureSeule) AttendreReglages();

        if (premierLancement)
        {
            Demarrage.Activer(true);
            Sauver();
            OuvrirGestionnaire();
        }
    }

    void Afficher(WidgetConfig c)
    {
        var w = Catalogue.Creer(c);
        if (w == null) return;
        w.Closed += (_, _) =>
        {
            _widgets.Remove(w);
            WidgetsChanged?.Invoke();
            if (!_quitte && Config.Widgets.Contains(c))
            {
                var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                t.Tick += (_, _) =>
                {
                    t.Stop();
                    if (!_quitte && Config.Widgets.Contains(c) && !_widgets.Any(x => x.Config == c)) Afficher(c);
                };
                t.Start();
            }
        };
        _widgets.Add(w);
        w.Show();
        if (_masques) w.Hide();
        WidgetsChanged?.Invoke();
    }

    bool _dernierClair, _masques;
    Forms.ToolStripMenuItem _itemMasquer;

    public bool Masques => _masques;

    void VerifierTheme()
    {
        bool clair = Theme.Clair;
        if (clair == _dernierClair || _quitte) return;
        _dernierClair = clair;
        AppliquerStyle();
    }

    public void BasculerMasquage()
    {
        _masques = !_masques;
        foreach (var w in _widgets.ToList())
        {
            if (_masques) w.Hide();
            else { w.Show(); w.AuFond(); }
        }
        if (_itemMasquer != null) _itemMasquer.Text = _masques ? "Afficher les widgets (Win+Alt+H)" : "Masquer les widgets (Win+Alt+H)";
        WidgetsChanged?.Invoke();
    }

    public IEnumerable<string> NomsProfils =>
        new[] { Config.ProfilActif }.Concat(Config.Profils.Keys).Distinct().OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase);

    public void ChangerProfil(string nom)
    {
        if (nom == Config.ProfilActif || !Config.Profils.TryGetValue(nom, out var liste)) return;
        Config.Profils[Config.ProfilActif] = Config.Widgets;
        Config.Widgets = liste;
        Config.Profils.Remove(nom);
        Config.ProfilActif = nom;
        foreach (var w in _widgets.ToList()) w.Close();
        Sauver();
        foreach (var c in Config.Widgets.ToList()) Afficher(c);
        Notification("Profil", $"Profil « {nom} » activé.");
    }

    public bool CreerProfil(string nom, bool copierActuel)
    {
        if (string.IsNullOrWhiteSpace(nom) || NomsProfils.Contains(nom, StringComparer.CurrentCultureIgnoreCase)) return false;
        Config.Profils[nom] = copierActuel
            ? JsonSerializer.Deserialize<List<WidgetConfig>>(JsonSerializer.Serialize(Config.Widgets))
            : new List<WidgetConfig>();
        Sauver();
        ChangerProfil(nom);
        return true;
    }

    public bool RenommerProfil(string ancien, string nouveau)
    {
        if (string.IsNullOrWhiteSpace(nouveau) || NomsProfils.Contains(nouveau, StringComparer.CurrentCultureIgnoreCase)) return false;
        if (ancien == Config.ProfilActif) Config.ProfilActif = nouveau;
        else if (Config.Profils.Remove(ancien, out var liste)) Config.Profils[nouveau] = liste;
        else return false;
        Sauver();
        Notifier();
        return true;
    }

    public void SupprimerProfil(string nom)
    {
        if (nom == Config.ProfilActif || !Config.Profils.Remove(nom, out var liste)) return;
        foreach (var c in liste)
            if (!IdPartage(c)) Coffre.Effacer($"{c.Type}/{c.Id}");
        Sauver();
        Notifier();
    }

    public bool IdPartage(WidgetConfig widget) =>
        Config.Widgets.Concat(Config.Profils.Values.SelectMany(l => l)).Any(c => c.Id == widget.Id && c != widget);

    public void Ajouter(string type)
    {
        var zone = SystemParameters.WorkArea;
        int n = Config.Widgets.Count % 6;
        var c = new WidgetConfig { Type = type, X = zone.Left + 60 + n * 36, Y = zone.Top + 60 + n * 36 };
        if (type == "animal") c.Y = zone.Bottom - 90;
        Config.Widgets.Add(c);
        Sauver();
        Afficher(c);
    }

    public void Supprimer(WidgetWindow w)
    {
        Config.Widgets.Remove(w.Config);
        Sauver();
        w.Close();
    }

    public List<Rect> Obstacles(WidgetWindow sauf) =>
        _widgets.Where(x => x != sauf && x.IsLoaded && x.ActualWidth > 0).Select(x => x.Zone).ToList();

    public void Ranger(WidgetWindow w)
    {
        if (!Config.SansChevauchement || _quitte || !w.IsLoaded || w.ActualWidth == 0) return;
        var zone = w.Zone;
        var autres = Obstacles(w);
        var ecrans = Placement.Ecrans(w);
        if (Placement.Libre(zone, autres, ecrans)) return;
        if (Config.Retrecir && w.Config.Type != "animal" && Retrecir(w, zone, autres, ecrans)) return;
        var place = Placement.Trouver(zone.Location, zone.Size, autres, ecrans);
        if (Math.Abs(place.X - zone.X) < 0.5 && Math.Abs(place.Y - zone.Y) < 0.5) return;
        w.Left += place.X - zone.X;
        w.Top += place.Y - zone.Y;
        w.Config.X = w.Left;
        w.Config.Y = w.Top;
        Sauver();
    }

    void Regrandir()
    {
        if (_quitte || _lectureSeule || !Config.SansChevauchement) return;
        if (System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed) return;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var w in _widgets.ToList())
        {
            if (!w.IsLoaded || w.ActualWidth == 0) continue;
            if (!w.Config.Options.TryGetValue("_voulue", out var texte) || !double.TryParse(texte, System.Globalization.NumberStyles.Float, inv, out var voulue)) continue;
            double actuelle = w.Echelle;
            if (voulue <= actuelle + 0.005) { w.Config.Options.Remove("_voulue"); Sauver(); continue; }

            var zone = w.Zone;
            var autres = Obstacles(w);
            var ecrans = Placement.Ecrans(w);
            double meilleure = actuelle;
            Rect choix = zone;
            for (int coin = 0; coin < 4; coin++)
            {
                double bas = actuelle, haut = voulue;
                if (Placement.Libre(Echelonnee(zone, actuelle, coin, voulue), autres, ecrans)) bas = voulue;
                else
                    for (int i = 0; i < 14; i++)
                    {
                        double milieu = (bas + haut) / 2;
                        if (Placement.Libre(Echelonnee(zone, actuelle, coin, milieu), autres, ecrans)) bas = milieu; else haut = milieu;
                    }
                if (bas > meilleure + 0.004) { meilleure = bas; choix = Echelonnee(zone, actuelle, coin, bas); }
            }
            if (meilleure <= actuelle + 0.01) continue;

            meilleure = Math.Floor(meilleure * 100) / 100;
            if (meilleure >= voulue - 0.005) w.Config.Options.Remove("_voulue");
            w.ChangerEchelle(meilleure);
            w.Left += choix.X - zone.X;
            w.Top += choix.Y - zone.Y;
            w.Config.X = w.Left;
            w.Config.Y = w.Top;
            Sauver();
        }
    }

    static Rect Echelonnee(Rect zone, double depart, int coin, double echelle)
    {
        const double MargesFixes = 8;
        double k = echelle / depart;
        double l = (zone.Width - MargesFixes) * k + MargesFixes;
        double h = (zone.Height - MargesFixes) * k + MargesFixes;
        double x = coin is 1 or 3 ? zone.Right - l : zone.Left;
        double y = coin >= 2 ? zone.Bottom - h : zone.Top;
        return new Rect(x, y, l, h);
    }

    bool Retrecir(WidgetWindow w, Rect zone, List<Rect> autres, List<Rect> ecrans)
    {
        const double MargesFixes = 8;
        double depart = w.Echelle;
        if (depart <= WidgetWindow.EchelleMin) return false;

        Rect Reduite(int coin, double echelle)
        {
            double k = echelle / depart;
            double l = (zone.Width - MargesFixes) * k + MargesFixes;
            double h = (zone.Height - MargesFixes) * k + MargesFixes;
            double x = coin is 1 or 3 ? zone.Right - l : zone.Left;
            double y = coin >= 2 ? zone.Bottom - h : zone.Top;
            return new Rect(x, y, l, h);
        }

        double meilleure = 0;
        Rect choix = default;
        for (int coin = 0; coin < 4; coin++)
        {
            if (!Placement.Libre(Reduite(coin, WidgetWindow.EchelleMin), autres, ecrans)) continue;
            double bas = WidgetWindow.EchelleMin, haut = depart;
            for (int i = 0; i < 14; i++)
            {
                double milieu = (bas + haut) / 2;
                if (Placement.Libre(Reduite(coin, milieu), autres, ecrans)) bas = milieu; else haut = milieu;
            }
            if (bas > meilleure) { meilleure = bas; choix = Reduite(coin, bas); }
        }
        if (meilleure < WidgetWindow.EchelleMin) return false;

        if (!w.Config.Options.ContainsKey("_voulue"))
            w.Config.Options["_voulue"] = depart.ToString(System.Globalization.CultureInfo.InvariantCulture);
        w.ChangerEchelle(Math.Floor(meilleure * 100) / 100);
        w.Left += choix.X - zone.X;
        w.Top += choix.Y - zone.Y;
        w.Config.X = w.Left;
        w.Config.Y = w.Top;
        Sauver();
        return true;
    }

    public void Aimanter(WidgetWindow w, double seuil = 20)
    {
        if (_quitte || !w.IsLoaded || w.ActualWidth == 0 || w.Config.Type == "animal") return;
        var reperes = _widgets
            .Where(x => x != w && x.IsLoaded && x.ActualWidth > 0 && x.Config.Type != "animal")
            .Select(x => x.Zone).ToList();
        var zone = w.Zone;
        var place = Placement.Aimanter(zone, reperes, Placement.Ecrans(w), seuil);
        if (Math.Abs(place.X - zone.X) < 0.5 && Math.Abs(place.Y - zone.Y) < 0.5) return;
        w.Left += place.X - zone.X;
        w.Top += place.Y - zone.Y;
        w.Config.X = w.Left;
        w.Config.Y = w.Top;
        Sauver();
    }

    public void AlignerTout()
    {
        for (int passe = 0; passe < 2; passe++)
            foreach (var w in _widgets.OrderBy(x => x.Top).ThenBy(x => x.Left).ToList())
                Aimanter(w, 60);
        RangerTout();
    }

    public void RangerTout()
    {
        foreach (var w in _widgets.ToList()) Ranger(w);
    }

    public void Sauver()
    {
        if (_lectureSeule) return;
        try { ConfigStore.Sauver(Config); }
        catch (Exception ex) { Journal(ex); }
    }

    void AttendreReglages()
    {
        Notification("Mes Widgets", "Tes réglages ne sont pas encore lisibles. Nouvel essai en cours, rien ne sera effacé.");
        var minuteur = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        minuteur.Tick += (_, _) =>
        {
            var config = ConfigStore.EssayerCharger();
            if (config == null) return;
            minuteur.Stop();
            foreach (var w in _widgets.ToList()) w.Close();
            Config = config;
            _lectureSeule = false;
            MettreAJourMenus();
            foreach (var c in Config.Widgets.ToList()) Afficher(c);
            Notification("Mes Widgets", "Tes widgets sont de retour.");
        };
        minuteur.Start();
    }

    public void Notifier() => WidgetsChanged?.Invoke();

    public void Notification(string titre, string texte) =>
        _icone?.ShowBalloonTip(6000, titre, texte, Forms.ToolTipIcon.Info);

    public void Recreer(WidgetWindow w)
    {
        var c = w.Config;
        Sauver();
        w.Close();
        if (Config.Widgets.Contains(c) && !_widgets.Any(x => x.Config == c)) Afficher(c);
    }

    public void AppliquerStyle()
    {
        Sauver();
        MettreAJourMenus();
        foreach (var w in _widgets.ToList()) w.Close();
        foreach (var c in Config.Widgets.ToList())
            if (!_widgets.Any(x => x.Config == c)) Afficher(c);
    }

    void MettreAJourMenus()
    {
        bool clair = Config.Theme == "clair";
        Resources["MenuFond"] = clair ? Theme.B(0xFA, 0xFF, 0xFF, 0xFF) : Theme.B(0xF2, 0x1E, 0x20, 0x28);
        Resources["MenuTexte"] = clair ? Theme.B(0xFF, 0x1F, 0x23, 0x28) : Theme.B(0xFF, 0xFF, 0xFF, 0xFF);
        Resources["MenuTexteDoux"] = clair ? Theme.B(0x99, 0x00, 0x00, 0x00) : Theme.B(0xB0, 0xFF, 0xFF, 0xFF);
        Resources["MenuSurvol"] = clair ? Theme.B(0x12, 0x00, 0x00, 0x00) : Theme.B(0x24, 0xFF, 0xFF, 0xFF);
        Resources["MenuBordure"] = clair ? Theme.B(0x1A, 0x00, 0x00, 0x00) : Theme.B(0x30, 0xFF, 0xFF, 0xFF);
        Resources["MenuAccent"] = Theme.Accent;
        Resources["DlgFond"] = clair ? Theme.B(0xFF, 0xF7, 0xF8, 0xFA) : Theme.B(0xFF, 0x1B, 0x1D, 0x24);
        Resources["DlgSurface"] = clair ? Theme.B(0xFF, 0xFF, 0xFF, 0xFF) : Theme.B(0xFF, 0x26, 0x29, 0x33);
        Resources["DlgTexte"] = clair ? Theme.B(0xFF, 0x1F, 0x23, 0x28) : Theme.B(0xFF, 0xF2, 0xF3, 0xF5);
        Resources["DlgTexteDoux"] = clair ? Theme.B(0xFF, 0x66, 0x70, 0x85) : Theme.B(0xFF, 0x9C, 0xA3, 0xAF);
        Resources["DlgBordure"] = clair ? Theme.B(0xFF, 0xD9, 0xDC, 0xE1) : Theme.B(0xFF, 0x3A, 0x3E, 0x4A);
        Resources["DlgSurvol"] = clair ? Theme.B(0x10, 0x00, 0x00, 0x00) : Theme.B(0x18, 0xFF, 0xFF, 0xFF);
        Resources["DlgAccent"] = Theme.Accent;
    }

    public void ChangerTheme(string theme)
    {
        Config.Theme = theme;
        _dernierClair = Theme.Clair;
        AppliquerStyle();
    }

    public void BasculerMicro()
    {
        try
        {
            Audio.MicroMuet = !Audio.MicroMuet;
            Notification("Micro", Audio.MicroMuet ? "Micro coupé 🔇" : "Micro activé 🎙");
        }
        catch { Notification("Micro", "Aucun micro trouvé."); }
    }

    public void Exporter(string fichier) =>
        File.WriteAllText(fichier, JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true }));

    public void Importer(string fichier)
    {
        var nouvelle = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(fichier));
        if (nouvelle?.Widgets == null) throw new InvalidDataException("Ce fichier n'est pas une sauvegarde de Mes Widgets.");
        var depot = Config.Depot;
        Config = nouvelle;
        if (string.IsNullOrEmpty(Config.Depot)) Config.Depot = depot;
        MettreAJourMenus();
        foreach (var w in _widgets.ToList()) w.Close();
        Sauver();
        foreach (var c in Config.Widgets.ToList()) Afficher(c);
    }

    public void RamenerWidgets()
    {
        var zone = SystemParameters.WorkArea;
        int i = 0;
        foreach (var c in Config.Widgets)
        {
            c.X = zone.Left + 40 + (i % 8) * 40;
            c.Y = zone.Top + 40 + (i % 8) * 40;
            i++;
        }
        AppliquerStyle();
    }

    void CreerIcone()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Gérer mes widgets…", null, (_, _) => OuvrirGestionnaire());
        var ajouter = new Forms.ToolStripMenuItem("Ajouter un widget");
        foreach (var categorie in Catalogue.Categories)
        {
            var sous = new Forms.ToolStripMenuItem(categorie);
            foreach (var t in Catalogue.Types.Where(t => t.Categorie == categorie))
                sous.DropDownItems.Add(t.Nom, null, (_, _) => Ajouter(t.Type));
            ajouter.DropDownItems.Add(sous);
        }
        menu.Items.Add(ajouter);
        var profils = new Forms.ToolStripMenuItem("Profil");
        profils.DropDownItems.Add("(chargement)");
        profils.DropDownOpening += (_, _) =>
        {
            profils.DropDownItems.Clear();
            foreach (var nom in NomsProfils)
            {
                var n = nom;
                profils.DropDownItems.Add(new Forms.ToolStripMenuItem(n, null, (_, _) => ChangerProfil(n)) { Checked = n == Config.ProfilActif });
            }
            profils.DropDownItems.Add(new Forms.ToolStripSeparator());
            profils.DropDownItems.Add("Gérer les profils…", null, (_, _) => OuvrirGestionnaire());
        };
        menu.Items.Add(profils);
        _itemMasquer = new Forms.ToolStripMenuItem("Masquer les widgets (Win+Alt+H)", null, (_, _) => BasculerMasquage());
        menu.Items.Add(_itemMasquer);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => Quitter());

        _icone = new Forms.NotifyIcon
        {
            Icon = ChargerIcone(),
            Text = "Mes Widgets",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icone.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) OuvrirGestionnaire(); };
    }

    static System.Drawing.Icon ChargerIcone()
    {
        try
        {
            using var flux = typeof(App).Assembly.GetManifestResourceStream("app.ico");
            return new System.Drawing.Icon(flux, Forms.SystemInformation.SmallIconSize);
        }
        catch
        {
            return System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);
        }
    }

    public void OuvrirGestionnaire()
    {
        if (_gestionnaire == null)
        {
            _gestionnaire = new GestionnaireWindow();
            _gestionnaire.Closed += (_, _) => _gestionnaire = null;
            _gestionnaire.Show();
        }
        else if (_gestionnaire.WindowState == WindowState.Minimized)
        {
            _gestionnaire.WindowState = WindowState.Normal;
        }
        _gestionnaire.Activate();
    }

    public void Quitter()
    {
        _quitte = true;
        Sauver();
        if (_icone != null) { _icone.Visible = false; _icone.Dispose(); }
        Shutdown();
    }

    public static void Journal(Exception ex)
    {
        var ligne = $"{DateTime.Now:G} {ex}\n\n";
        try
        {
            Directory.CreateDirectory(ConfigStore.Dossier);
            File.AppendAllText(Path.Combine(ConfigStore.Dossier, "erreurs.log"), ligne);
        }
        catch
        {
            try { File.AppendAllText(Path.Combine(Path.GetTempPath(), "MesWidgets-erreurs.log"), ligne); } catch { }
        }
    }
}
