using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MesWidgets;

public class DockerWidget : WidgetWindow
{
    record Conteneur(string Id, string Nom, string Image, bool Actif, string Statut);

    static readonly HttpClient Moteur = new(new SocketsHttpHandler
    {
        ConnectCallback = async (_, jeton) =>
        {
            var tuyau = new NamedPipeClientStream(".", "docker_engine", PipeDirection.InOut, PipeOptions.Asynchronous);
            await tuyau.ConnectAsync(2000, jeton);
            return tuyau;
        },
    })
    { BaseAddress = new Uri("http://localhost"), Timeout = TimeSpan.FromSeconds(20) };

    static readonly Brush Vert = Theme.B(0xFF, 0x22, 0xC5, 0x5E);

    readonly StackPanel _liste = new();
    readonly TextBlock _resume;
    readonly HashSet<string> _enCours = new();
    bool _occupe;

    public override string Resume => _resume.Text;

    public DockerWidget(WidgetConfig c) : base(c)
    {
        _resume = Texte("", 12, Pale);
        _resume.VerticalAlignment = VerticalAlignment.Center;
        var entete = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(_resume, Dock.Right);
        entete.Children.Add(_resume);
        entete.Children.Add(Titre("Docker"));

        var pile = new StackPanel { Width = 290 };
        pile.Children.Add(entete);
        pile.Children.Add(_liste);
        Content = Carte(pile);

        Loaded += (_, _) => Actualiser();
        Minuteur(TimeSpan.FromSeconds(5), Actualiser);
    }

    async void Actualiser()
    {
        if (_occupe) return;
        _occupe = true;
        try
        {
            var json = await Moteur.GetStringAsync("/containers/json?all=true");
            using var doc = JsonDocument.Parse(json);
            var conteneurs = doc.RootElement.EnumerateArray().Select(e => new Conteneur(
                    e.GetProperty("Id").GetString(),
                    e.GetProperty("Names").EnumerateArray().Select(n => n.GetString()?.TrimStart('/')).FirstOrDefault() ?? "?",
                    e.GetProperty("Image").GetString(),
                    e.GetProperty("State").GetString() == "running",
                    e.GetProperty("Status").GetString()))
                .OrderByDescending(x => x.Actif).ThenBy(x => x.Nom)
                .ToList();
            Afficher(conteneurs);
        }
        catch
        {
            AfficherArrete();
        }
        finally { _occupe = false; }
    }

    void Afficher(List<Conteneur> conteneurs)
    {
        _liste.Children.Clear();
        int actifs = conteneurs.Count(x => x.Actif);
        _resume.Text = $"{actifs} / {conteneurs.Count} actif{(actifs > 1 ? "s" : "")}";
        if (conteneurs.Count == 0)
        {
            _liste.Children.Add(Texte("Aucun conteneur.", 13, Pale));
            return;
        }

        foreach (var x in conteneurs.Take(8))
        {
            var point = new Ellipse { Width = 9, Height = 9, Fill = x.Actif ? Vert : Theme.Piste, Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            var nom = Texte(x.Nom, 13);
            nom.FontWeight = FontWeights.SemiBold;
            nom.TextTrimming = TextTrimming.CharacterEllipsis;
            var detail = Texte($"{x.Image} · {Traduire(x.Statut)}", 11, Pale);
            detail.TextTrimming = TextTrimming.CharacterEllipsis;
            var textes = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textes.Children.Add(nom);
            textes.Children.Add(detail);

            FrameworkElement action;
            if (_enCours.Contains(x.Id)) action = Texte("…", 16, Pale);
            else
            {
                var id = x.Id;
                var b = Bouton(Glyphe(x.Actif ? "" : "", 13, x.Actif ? Alerte : Vert), () => Basculer(id, x.Actif), 30);
                b.ToolTip = x.Actif ? "Arrêter" : "Démarrer";
                action = b;
            }
            action.VerticalAlignment = VerticalAlignment.Center;
            action.Margin = new Thickness(8, 0, 0, 0);

            var ligne = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
            DockPanel.SetDock(point, Dock.Left);
            DockPanel.SetDock(action, Dock.Right);
            ligne.Children.Add(point);
            ligne.Children.Add(action);
            ligne.Children.Add(textes);
            _liste.Children.Add(ligne);
        }
        if (conteneurs.Count > 8)
            _liste.Children.Add(Texte($"… et {conteneurs.Count - 8} autre(s)", 11, Pale));
    }

    void AfficherArrete()
    {
        _liste.Children.Clear();
        _resume.Text = "arrêté";
        var t = Texte("Docker Desktop n'est pas lancé.", 13, Pale);
        t.Margin = new Thickness(0, 0, 0, 10);
        _liste.Children.Add(t);
        var chemin = CheminDockerDesktop();
        if (chemin == null) return;
        _liste.Children.Add(Cliquable(new Border
        {
            Background = Accent,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 7, 14, 8),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = Texte("Lancer Docker Desktop", 13, Brushes.White),
        }, () => Ouvrir(chemin), survol: false));
    }

    async void Basculer(string id, bool actif)
    {
        _enCours.Add(id);
        Actualiser();
        try
        {
            var reponse = await Moteur.PostAsync($"/containers/{id}/{(actif ? "stop?t=10" : "start")}", null);
            if (!reponse.IsSuccessStatusCode && (int)reponse.StatusCode != 304)
                MessageBox.Show($"Docker a refusé l'opération ({(int)reponse.StatusCode}).", "Docker", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Impossible de joindre Docker :\n{ex.Message}", "Docker", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _enCours.Remove(id);
            _occupe = false;
            Actualiser();
        }
    }

    static string Traduire(string statut) => (statut ?? "")
        .Replace("Up ", "actif depuis ").Replace("Exited", "arrêté").Replace(" ago", "")
        .Replace("About an hour", "1 heure").Replace("About a minute", "1 minute")
        .Replace("hours", "h").Replace("hour", "h").Replace("minutes", "min").Replace("minute", "min")
        .Replace("seconds", "s").Replace("days", "jours").Replace("day", "jour").Replace("weeks", "semaines").Replace("week", "semaine")
        .Replace("(healthy)", "(sain)").Replace("(unhealthy)", "(en difficulté)").Replace("Created", "créé").Replace("Paused", "en pause");

    static string CheminDockerDesktop()
    {
        var candidats = new[]
        {
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "DockerDesktop", "Docker Desktop.exe"),
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "Docker Desktop.exe"),
        };
        return candidats.FirstOrDefault(File.Exists);
    }

    protected override void RemplirMenu()
    {
        Item("Actualiser", Actualiser);
        var chemin = CheminDockerDesktop();
        if (chemin != null) Item("Ouvrir Docker Desktop", () => Ouvrir(chemin));
    }
}
