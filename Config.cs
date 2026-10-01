using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;
using Microsoft.Win32;

namespace MesWidgets;

public class WidgetConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public Dictionary<string, string> Options { get; set; } = new();
}

public class AppConfig
{
    public string Theme { get; set; } = "sombre";
    public string Accent { get; set; } = "#4F8CFF";
    public string Police { get; set; } = "Segoe UI";
    public double Arrondi { get; set; } = 16;
    public string Fond { get; set; } = "normal";
    public bool SansChevauchement { get; set; } = true;
    public bool Magnetisme { get; set; } = true;
    public bool Retrecir { get; set; } = true;
    public string Depot { get; set; } = "leagrnr/my_widget_windows";
    public string ProfilActif { get; set; } = "Principal";
    public Dictionary<string, List<WidgetConfig>> Profils { get; set; } = new();
    public List<WidgetConfig> Widgets { get; set; } = new();
}

public static class ConfigStore
{
    public enum Etat { Ok, Nouveau, Indisponible }

    const string CleRegistre = @"Software\MesWidgets";
    const int SauvegardesGardees = 10;
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string Dossier => Path.Combine(DossierLocal(), "MesWidgets");
    static string Fichier => Path.Combine(Dossier, "config.json");
    static string Precedente => Fichier + ".bak";
    static string DossierSauvegardes => Path.Combine(Dossier, "sauvegardes");

    public static string DossierLocal()
    {
        var chemin = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrWhiteSpace(chemin) || !Path.IsPathRooted(chemin))
            chemin = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (string.IsNullOrWhiteSpace(chemin) || !Path.IsPathRooted(chemin))
            chemin = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? @"C:\Users\" + Environment.UserName, "AppData", "Local");
        return chemin;
    }

    static bool DejaUtilise
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(CleRegistre);
            return k?.GetValue("Utilise") is int v && v == 1;
        }
    }

    static void MarquerUtilise()
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(CleRegistre);
            k.SetValue("Utilise", 1, RegistryValueKind.DWord);
        }
        catch { }
    }

    public static AppConfig Charger(out Etat etat)
    {
        for (int essai = 0; essai < 20; essai++)
        {
            var config = EssayerCharger(journaliser: essai == 0);
            if (config != null) { etat = Etat.Ok; return config; }

            bool jamaisUtilise = !DejaUtilise && !Directory.Exists(Dossier);
            if (jamaisUtilise) { etat = Etat.Nouveau; return ParDefaut(); }

            if (essai == 0) App.Journal(new Exception(
                $"Réglages illisibles au démarrage : fichier={Fichier} · existe={File.Exists(Fichier)} · dossier existe={Directory.Exists(Dossier)} · " +
                $"LocalApplicationData='{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}' · dossier courant='{Environment.CurrentDirectory}'"));
            Thread.Sleep(1000);
        }
        etat = Etat.Indisponible;
        return new AppConfig();
    }

    public static AppConfig EssayerCharger(bool journaliser = false)
    {
        var candidats = new List<string> { Fichier, Precedente };
        try
        {
            if (Directory.Exists(DossierSauvegardes))
                candidats.AddRange(Directory.GetFiles(DossierSauvegardes, "config-*.json").OrderByDescending(f => f));
        }
        catch { }

        foreach (var chemin in candidats)
        {
            try
            {
                if (!File.Exists(chemin)) continue;
                var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(chemin), Options);
                if (config?.Widgets == null) continue;
                if (chemin != Fichier)
                    App.Journal(new Exception($"config.json illisible : réglages restaurés depuis {chemin}"));
                MarquerUtilise();
                return config;
            }
            catch (Exception ex)
            {
                if (chemin == Fichier && journaliser) App.Journal(ex);
            }
        }
        return null;
    }

    public static void Sauver(AppConfig config)
    {
        Directory.CreateDirectory(Dossier);
        var tmp = Fichier + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(config, Options));
        if (File.Exists(Fichier)) File.Copy(Fichier, Precedente, true);
        File.Move(tmp, Fichier, true);
        MarquerUtilise();
        SauvegardeDuJour();
    }

    static void SauvegardeDuJour()
    {
        try
        {
            Directory.CreateDirectory(DossierSauvegardes);
            var aujourdhui = Path.Combine(DossierSauvegardes, $"config-{DateTime.Today:yyyy-MM-dd}.json");
            if (!File.Exists(aujourdhui)) File.Copy(Fichier, aujourdhui);
            foreach (var ancienne in Directory.GetFiles(DossierSauvegardes, "config-*.json").OrderByDescending(f => f).Skip(SauvegardesGardees))
                File.Delete(ancienne);
        }
        catch { }
    }

    static AppConfig ParDefaut()
    {
        var zone = SystemParameters.WorkArea;
        var horloge = new WidgetConfig { Type = "horloge", X = zone.Right - 310, Y = zone.Top + 20 };
        var postit = new WidgetConfig { Type = "postit", X = zone.Right - 290, Y = zone.Top + 200 };

        var ancien = Path.Combine(DossierLocal(), "WidgetBureau", "donnees.json");
        try
        {
            if (File.Exists(ancien))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(ancien));
                postit.Options["texte"] = doc.RootElement.GetProperty("note").GetString();
            }
        }
        catch { }

        return new AppConfig { Widgets = { horloge, postit } };
    }
}
