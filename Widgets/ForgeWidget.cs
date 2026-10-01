using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MesWidgets;

public abstract class ForgeWidget : WidgetWindow
{
    protected record Element(string Genre, string Titre, string Projet, DateTime? Date, string Url);

    protected record Donnees(
        string Nom, string Pseudo, string Avatar, string Profil,
        int ARelire, int Assignes, int Notifications,
        List<Element> Elements, Dictionary<DateTime, int> Contributions);

    const int Semaines = 18;
    const double Case = 11, EcartCase = 3;

    readonly StackPanel _contenu = new();
    readonly TextBlock _etat;
    Donnees _donnees;
    bool _occupe;

    protected abstract string NomService { get; }
    protected abstract string PageJeton(string serveur);
    protected abstract string DroitsJeton { get; }
    protected abstract (string ARelire, string Assignes, string Notifications) Libelles { get; }
    protected abstract (string ARelire, string Assignes, string Notifications) Pages(Donnees d);
    protected abstract Task<Donnees> Charger(string jeton);
    protected virtual string ServeurParDefaut => null;

    protected string Serveur => Option("serveur", ServeurParDefaut ?? "").TrimEnd('/');
    string CleCoffre => $"{Config.Type}/{Config.Id}";
    bool Connecte => !string.IsNullOrEmpty(Coffre.Lire(CleCoffre));

    public override string Resume => _donnees != null ? "@" + _donnees.Pseudo : Connecte ? "Connecté" : "Non connecté";

    protected ForgeWidget(WidgetConfig c) : base(c)
    {
        _etat = Texte("", 11, Pale);
        _etat.Margin = new Thickness(0, 10, 0, 0);
        _etat.TextWrapping = TextWrapping.Wrap;

        var pile = new StackPanel { Width = 290 };
        pile.Children.Add(_contenu);
        pile.Children.Add(_etat);
        Content = Carte(pile);

        Loaded += (_, _) => Actualiser();
        Minuteur(TimeSpan.FromMinutes(5), Actualiser);
    }

    async void Actualiser()
    {
        if (_occupe) return;
        var jeton = Coffre.Lire(CleCoffre);
        if (string.IsNullOrEmpty(jeton)) { AfficherDeconnecte(); return; }

        _occupe = true;
        _etat.Text = "Actualisation…";
        try
        {
            _donnees = await Charger(jeton);
            Afficher(_donnees);
            _etat.Text = $"{NomService} · mis à jour à {DateTime.Now:HH:mm}";
            App.Instance.Notifier();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _etat.Text = "Jeton refusé ou expiré : ✏ Modifier › Changer de compte…";
        }
        catch
        {
            _etat.Text = $"Impossible de joindre {NomService} (connexion ?)";
        }
        finally { _occupe = false; }
    }

    void AfficherDeconnecte()
    {
        _donnees = null;
        _contenu.Children.Clear();
        _contenu.Children.Add(Titre(NomService));
        var aide = Texte($"Connecte ton compte pour voir ce qu'on te demande de relire, ce qui t'est assigné et tes contributions.", 13, Pale);
        aide.TextWrapping = TextWrapping.Wrap;
        aide.Margin = new Thickness(0, 6, 0, 12);
        _contenu.Children.Add(aide);

        var bouton = Cliquable(new Border
        {
            Background = Accent,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 7, 14, 8),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = Texte("Se connecter", 13, Brushes.White),
        }, Connecter, survol: false);
        _contenu.Children.Add(bouton);
        _etat.Text = "";
    }

    void Afficher(Donnees d)
    {
        _contenu.Children.Clear();

        var avatar = new Border { Width = 40, Height = 40, CornerRadius = new CornerRadius(20), Background = Theme.Piste, Margin = new Thickness(0, 0, 12, 0) };
        if (!string.IsNullOrEmpty(d.Avatar))
        {
            try { avatar.Background = new ImageBrush(new BitmapImage(new Uri(d.Avatar))) { Stretch = Stretch.UniformToFill }; }
            catch { }
        }
        var nom = Texte(d.Nom, 15);
        nom.FontWeight = FontWeights.SemiBold;
        nom.TextTrimming = TextTrimming.CharacterEllipsis;
        var identite = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        identite.Children.Add(nom);
        identite.Children.Add(Texte($"@{d.Pseudo} · {NomService}", 12, Pale));
        var entete = new DockPanel();
        DockPanel.SetDock(avatar, Dock.Left);
        entete.Children.Add(avatar);
        entete.Children.Add(identite);
        _contenu.Children.Add(Cliquable(new Border { Child = entete, Background = Brushes.Transparent, CornerRadius = new CornerRadius(8), ToolTip = "Ouvrir mon profil" }, () => Ouvrir(d.Profil), survol: false));

        var (lRelire, lAssignes, lNotifs) = Libelles;
        var (pRelire, pAssignes, pNotifs) = Pages(d);
        var compteurs = new Grid { Margin = new Thickness(0, 12, 0, 8) };
        for (int i = 0; i < 3; i++) compteurs.ColumnDefinitions.Add(new ColumnDefinition());
        var tuiles = new[] { (d.ARelire, lRelire, pRelire), (d.Assignes, lAssignes, pAssignes), (d.Notifications, lNotifs, pNotifs) };
        for (int i = 0; i < 3; i++)
        {
            var (nombre, libelle, page) = tuiles[i];
            var chiffre = Texte(nombre >= 50 && i == 2 ? "50+" : nombre.ToString(), 22, nombre > 0 ? Blanc : Pale);
            chiffre.FontWeight = FontWeights.SemiBold;
            var bloc = new StackPanel();
            bloc.Children.Add(chiffre);
            bloc.Children.Add(Texte(libelle, 11, Pale));
            var tuile = Cliquable(new Border
            {
                Child = bloc,
                Background = Theme.Piste,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 6, 10, 8),
                Margin = new Thickness(i == 0 ? 0 : 4, 0, i == 2 ? 0 : 4, 0),
            }, () => Ouvrir(page));
            Grid.SetColumn(tuile, i);
            compteurs.Children.Add(tuile);
        }
        _contenu.Children.Add(compteurs);

        if (d.Elements.Count == 0)
        {
            _contenu.Children.Add(Texte("Rien à traiter pour le moment 🎉", 13, Pale));
        }
        foreach (var e in d.Elements)
        {
            var badge = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(5, 1, 5, 2),
                Margin = new Thickness(0, 2, 8, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Background = e.Genre == "Issue" ? Theme.Piste : Accent,
                Child = Texte(e.Genre, 10, e.Genre == "Issue" ? Blanc : Brushes.White),
            };
            ((TextBlock)badge.Child).FontWeight = FontWeights.Bold;
            var titre = Texte(e.Titre, 13);
            titre.TextTrimming = TextTrimming.CharacterEllipsis;
            var detail = Texte(e.Projet + (e.Date is { } date ? " · " + Relatif(date) : ""), 11, Pale);
            detail.TextTrimming = TextTrimming.CharacterEllipsis;
            var textes = new StackPanel();
            textes.Children.Add(titre);
            textes.Children.Add(detail);
            var ligne = new DockPanel();
            DockPanel.SetDock(badge, Dock.Left);
            ligne.Children.Add(badge);
            ligne.Children.Add(textes);
            var url = e.Url;
            _contenu.Children.Add(Cliquable(new Border
            {
                Child = ligne,
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 4, 6, 5),
                Margin = new Thickness(-6, 0, -6, 0),
                ToolTip = e.Titre,
            }, () => Ouvrir(url)));
        }

        if (d.Contributions != null) _contenu.Children.Add(Graphique(d.Contributions));
    }

    FrameworkElement Graphique(Dictionary<DateTime, int> contributions)
    {
        var aujourdhui = DateTime.Today;
        var debut = aujourdhui.AddDays(-(((int)aujourdhui.DayOfWeek + 6) % 7)).AddDays(-7 * (Semaines - 1));
        int total = contributions.Where(c => c.Key >= debut && c.Key <= aujourdhui).Sum(c => c.Value);
        int max = Math.Max(1, contributions.Where(c => c.Key >= debut).Select(c => c.Value).DefaultIfEmpty(0).Max());

        var grille = new Canvas { Width = Semaines * (Case + EcartCase), Height = 7 * (Case + EcartCase), HorizontalAlignment = HorizontalAlignment.Left };
        for (int s = 0; s < Semaines; s++)
            for (int j = 0; j < 7; j++)
            {
                var date = debut.AddDays(7 * s + j);
                if (date > aujourdhui) continue;
                contributions.TryGetValue(date, out int n);
                double niveau = n == 0 ? 0 : 0.3 + 0.7 * Math.Min(1, (double)n / max);
                var carre = new Rectangle
                {
                    Width = Case, Height = Case,
                    RadiusX = 2.5, RadiusY = 2.5,
                    Fill = n == 0 ? Theme.Piste : Accent,
                    Opacity = n == 0 ? 1 : niveau,
                    ToolTip = $"{n} contribution{(n > 1 ? "s" : "")} le {date.ToString("d MMM", Fr)}",
                };
                Canvas.SetLeft(carre, s * (Case + EcartCase));
                Canvas.SetTop(carre, j * (Case + EcartCase));
                grille.Children.Add(carre);
            }

        var titre = Texte($"{total} contribution{(total > 1 ? "s" : "")} ces 4 derniers mois", 11, Pale);
        titre.Margin = new Thickness(0, 0, 0, 6);
        var pile = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        pile.Children.Add(Separateur());
        pile.Children.Add(titre);
        pile.Children.Add(grille);
        return pile;
    }

    async void Connecter()
    {
        var r = ConnexionForge.Demander(NomService, PageJeton, DroitsJeton, ServeurParDefaut != null ? Serveur : null);
        if (r == null) return;
        if (ServeurParDefaut != null) SetOption("serveur", string.IsNullOrEmpty(r.Serveur) ? ServeurParDefaut : r.Serveur);
        _etat.Text = "Vérification du jeton…";
        try
        {
            var d = await Charger(r.Jeton);
            Coffre.Ecrire(CleCoffre, r.Jeton);
            _donnees = d;
            Afficher(d);
            _etat.Text = $"{NomService} · connecté";
            App.Instance.Notifier();
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _etat.Text = "";
            MessageBox.Show($"{NomService} refuse ce jeton. Vérifie qu'il est complet, pas expiré et qu'il a bien les droits demandés.", NomService, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            _etat.Text = "";
            MessageBox.Show($"Impossible de joindre {NomService} :\n{ex.Message}", NomService, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    void Deconnecter()
    {
        Coffre.Effacer(CleCoffre);
        AfficherDeconnecte();
        App.Instance.Notifier();
    }

    protected override void Supprime()
    {
        if (!App.Instance.IdPartage(Config)) Coffre.Effacer(CleCoffre);
    }

    protected override void RemplirMenu()
    {
        if (Connecte)
        {
            Item("Actualiser", Actualiser);
            Item("Changer de compte…", Connecter);
            Item("Se déconnecter", Deconnecter);
        }
        else Item("Se connecter…", Connecter);
    }

    protected static async Task<(JsonElement Json, int? Total)> Lire(string url, Action<HttpRequestHeaders> entetes, HttpContent corps = null)
    {
        using var requete = new HttpRequestMessage(corps == null ? HttpMethod.Get : HttpMethod.Post, url) { Content = corps };
        entetes(requete.Headers);
        using var reponse = await Web.Http.SendAsync(requete);
        reponse.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync());
        int? total = reponse.Headers.TryGetValues("X-Total", out var v) && int.TryParse(v.FirstOrDefault(), out var t) ? t : null;
        return (doc.RootElement.Clone(), total);
    }

    protected static string Chaine(JsonElement e, string propriete) =>
        e.TryGetProperty(propriete, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    protected static DateTime? Date(JsonElement e, string propriete) =>
        DateTimeOffset.TryParse(Chaine(e, propriete), out var d) ? d.LocalDateTime : null;
}
