using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace MesWidgets;

public class SteamWidget : WidgetWindow
{
    record Jeu(string Id, string Nom, DateTime? DernierePartie, string Image);

    readonly StackPanel _liste = new();

    public SteamWidget(WidgetConfig c) : base(c)
    {
        var pile = new StackPanel { Width = 280 };
        var titre = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        titre.Children.Add(Glyphe("\uE7FC", 16));
        var t = Titre("  Mes jeux");
        titre.Children.Add(t);
        pile.Children.Add(titre);
        pile.Children.Add(_liste);
        Content = Carte(pile);

        Loaded += (_, _) => Charger();
        Minuteur(TimeSpan.FromMinutes(5), Charger);
    }

    async void Charger()
    {
        var jeux = await Task.Run(Lire);
        _liste.Children.Clear();
        if (jeux == null)
        {
            var t = Texte("Steam n'est pas installé sur ce PC.", 13, Pale);
            t.TextWrapping = TextWrapping.Wrap;
            _liste.Children.Add(t);
            return;
        }
        if (jeux.Count == 0) { _liste.Children.Add(Texte("Aucun jeu installé.", 13, Pale)); return; }

        foreach (var j in jeux)
        {
            var image = new Border
            {
                Width = 92, Height = 43,
                CornerRadius = new CornerRadius(4),
                Background = Theme.Piste,
                Margin = new Thickness(0, 0, 10, 0),
            };
            if (j.Image != null)
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(j.Image);
                    bmp.DecodePixelWidth = 184;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    image.Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
                }
                catch { }
            }
            else image.Child = Glyphe("\uE7FC", 18, Pale);

            var infos = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var nom = Texte(j.Nom, 13);
            nom.FontWeight = FontWeights.SemiBold;
            nom.TextTrimming = TextTrimming.CharacterEllipsis;
            infos.Children.Add(nom);
            infos.Children.Add(Texte(j.DernierePartie is { } d ? "Joué " + Relatif(d) : "Jamais lancé", 11, Pale));

            var ligne = new DockPanel();
            DockPanel.SetDock(image, Dock.Left);
            ligne.Children.Add(image);
            ligne.Children.Add(infos);

            var id = j.Id;
            _liste.Children.Add(Cliquable(new Border
            {
                Child = ligne,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6),
                Margin = new Thickness(-6, 0, -6, 0),
                Background = Brushes.Transparent,
                ToolTip = "Lancer le jeu",
            }, () => Ouvrir($"steam://rungameid/{id}")));
        }
    }

    static List<Jeu> Lire()
    {
        using var cle = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
        if (cle?.GetValue("SteamPath") is not string chemin) return null;
        var steam = Path.GetFullPath(chemin.Replace('/', '\\'));
        if (!Directory.Exists(steam)) return null;

        var bibliotheques = new List<string> { steam };
        var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
            foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"(.+?)\""))
                bibliotheques.Add(m.Groups[1].Value.Replace("\\\\", "\\"));

        var jeux = new List<Jeu>();
        foreach (var biblio in bibliotheques.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var dossier = Path.Combine(biblio, "steamapps");
            if (!Directory.Exists(dossier)) continue;
            foreach (var acf in Directory.EnumerateFiles(dossier, "appmanifest_*.acf"))
            {
                try
                {
                    var texte = File.ReadAllText(acf);
                    string Valeur(string nom) => Regex.Match(texte, $"\"{nom}\"\\s+\"(.*?)\"", RegexOptions.IgnoreCase).Groups[1].Value;
                    var id = Valeur("appid");
                    if (id == "228980") continue;
                    long.TryParse(Valeur("LastPlayed"), out var derniere);
                    jeux.Add(new Jeu(id, Valeur("name"),
                        derniere > 0 ? DateTimeOffset.FromUnixTimeSeconds(derniere).LocalDateTime : null,
                        Image(steam, id)));
                }
                catch { }
            }
        }
        return jeux.OrderByDescending(j => j.DernierePartie ?? DateTime.MinValue).Take(5).ToList();
    }

    static string Image(string steam, string id)
    {
        var cache = Path.Combine(steam, "appcache", "librarycache");
        var ancien = Path.Combine(cache, $"{id}_header.jpg");
        if (File.Exists(ancien)) return ancien;
        var dossier = Path.Combine(cache, id);
        if (!Directory.Exists(dossier)) return null;
        return Directory.EnumerateFiles(dossier, "header*.jpg", SearchOption.AllDirectories).FirstOrDefault();
    }
}
