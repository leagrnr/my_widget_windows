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
    bool _quitte;

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

        Config = ConfigStore.Charger(out bool premierLancement);
        MettreAJourMenus();
        foreach (var c in Config.Widgets.ToList()) Afficher(c);
        CreerIcone();
        RaccourcisClavier.Enregistrer();
        MiseAJour.Demarrer();

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
        WidgetsChanged?.Invoke();
    }

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
        try { ConfigStore.Sauver(Config); }
        catch (Exception ex) { Journal(ex); }
    }

    public void Notifier() => WidgetsChanged?.Invoke();

    public void Notification(string titre, string texte) =>
        _icone?.ShowBalloonTip(6000, titre, texte, Forms.ToolTipIcon.Info);

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
        try
        {
            Directory.CreateDirectory(ConfigStore.Dossier);
            File.AppendAllText(Path.Combine(ConfigStore.Dossier, "erreurs.log"), $"{DateTime.Now:G} {ex}\n\n");
        }
        catch { }
    }
}
