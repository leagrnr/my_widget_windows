using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MesWidgets;

static class MiseAJour
{
    public static Version Actuelle => Normaliser(typeof(App).Assembly.GetName().Version);
    public static (Version Version, string Url)? Disponible { get; private set; }
    public static event Action Changement;

    static DispatcherTimer _minuteur;
    static Version _dejaSignalee;

    public static void Demarrer()
    {
        _ = Verifier();
        _minuteur = new DispatcherTimer { Interval = TimeSpan.FromHours(12) };
        _minuteur.Tick += (_, _) => _ = Verifier();
        _minuteur.Start();
    }

    public static async Task<bool?> Verifier()
    {
        var depot = App.Instance.Config.Depot?.Trim().Trim('/');
        if (string.IsNullOrEmpty(depot)) return null;
        try
        {
            using var doc = JsonDocument.Parse(await Web.Http.GetStringAsync($"https://api.github.com/repos/{depot}/releases/latest"));
            var tag = doc.RootElement.GetProperty("tag_name").GetString()?.TrimStart('v', 'V');
            if (!System.Version.TryParse(tag, out var version)) return null;
            version = Normaliser(version);

            var url = doc.RootElement.GetProperty("assets").EnumerateArray()
                .Select(a => a.GetProperty("browser_download_url").GetString())
                .FirstOrDefault(u => u != null && u.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

            if (version > Actuelle && url != null)
            {
                Disponible = (version, url);
                if (_dejaSignalee != version)
                {
                    _dejaSignalee = version;
                    App.Instance.Notification("Mise à jour disponible",
                        $"Mes Widgets {version} est disponible. Ouvre le gestionnaire pour l'installer.");
                }
            }
            else Disponible = null;
            Changement?.Invoke();
            return Disponible != null;
        }
        catch { return null; }
    }

    public static async Task Installer()
    {
        if (Disponible is not { } maj) return;
        var fichier = Path.Combine(Path.GetTempPath(), "MesWidgets-Setup.exe");
        using (var flux = await Web.Http.GetStreamAsync(maj.Url))
        using (var sortie = File.Create(fichier))
            await flux.CopyToAsync(sortie);

        Process.Start(new ProcessStartInfo(fichier, "--silencieux") { UseShellExecute = true });
        App.Instance.Quitter();
    }

    static Version Normaliser(Version v) =>
        v == null ? new Version(0, 0, 0) : new Version(v.Major, v.Minor, Math.Max(0, v.Build));
}
