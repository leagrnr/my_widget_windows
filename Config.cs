using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

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
    public string Depot { get; set; } = "";
    public List<WidgetConfig> Widgets { get; set; } = new();
}

public static class ConfigStore
{
    public static readonly string Dossier =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MesWidgets");

    static string Fichier => Path.Combine(Dossier, "config.json");
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppConfig Charger(out bool nouveau)
    {
        nouveau = !File.Exists(Fichier);
        if (nouveau) return ParDefaut();
        try
        {
            return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(Fichier), Options) ?? new AppConfig();
        }
        catch
        {
            File.Copy(Fichier, Fichier + ".abime", true);
            return new AppConfig();
        }
    }

    public static void Sauver(AppConfig config)
    {
        Directory.CreateDirectory(Dossier);
        var tmp = Fichier + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(config, Options));
        File.Move(tmp, Fichier, true);
    }

    static AppConfig ParDefaut()
    {
        var zone = SystemParameters.WorkArea;
        var horloge = new WidgetConfig { Type = "horloge", X = zone.Right - 310, Y = zone.Top + 20 };
        var postit = new WidgetConfig { Type = "postit", X = zone.Right - 290, Y = zone.Top + 200 };

        var ancien = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                  "WidgetBureau", "donnees.json");
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
