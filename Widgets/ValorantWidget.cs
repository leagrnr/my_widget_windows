using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace MesWidgets;

public class ValorantWidget : WidgetWindow
{
    const string Api = "https://api.henrikdev.xyz/valorant";
    const string PageCle = "https://docs.henrikdev.xyz";
    const string RangsParDefaut = "03621f52-342b-cf4e-4f86-9350a49c6d04";
    const double Largeur = 300;

    static readonly (string Code, string Nom)[] Regions =
    {
        ("eu", "Europe"), ("na", "Amérique du Nord"), ("latam", "Amérique latine"),
        ("br", "Brésil"), ("ap", "Asie-Pacifique"), ("kr", "Corée"),
    };

    static readonly Brush Victoire = Theme.B(0xFF, 0x3E, 0xD6, 0x9A);
    static readonly Brush Defaite = Theme.B(0xFF, 0xFF, 0x5A, 0x6E);
    static string _jeuDeRangs;

    record Partie(string Carte, string Agent, string IdAgent, int Kills, int Morts, int Assists, int Tete, int Tirs, int Nous, int Eux, DateTime? Date);

    readonly StackPanel _contenu = new();
    readonly TextBlock _etat;
    bool _occupe;

    string RiotId => Option("riotid", "");
    string Region => Option("region", "eu");
    string CleCoffre => $"valorant/{Config.Id}";
    public override string Resume => RiotId.Length > 0 ? RiotId : "Non configuré";

    public ValorantWidget(WidgetConfig c) : base(c)
    {
        _etat = Texte("", 11, Pale);
        _etat.Margin = new Thickness(0, 10, 0, 0);
        _etat.TextWrapping = TextWrapping.Wrap;
        var pile = new StackPanel { Width = Largeur };
        pile.Children.Add(_contenu);
        pile.Children.Add(_etat);
        Content = Carte(pile);

        Loaded += (_, _) => Actualiser();
        Minuteur(TimeSpan.FromMinutes(10), Actualiser);
    }

    async void Actualiser()
    {
        if (_occupe) return;
        var cle = Coffre.Lire(CleCoffre);
        if (RiotId.Length == 0 || string.IsNullOrEmpty(cle)) { AfficherNonConfigure(); return; }

        _occupe = true;
        _etat.Text = "Actualisation…";
        try
        {
            var (nom, tag) = Decouper(RiotId);
            void Entetes(HttpRequestHeaders h) => h.TryAddWithoutValidation("Authorization", cle);
            var mmr = await Lire($"{Api}/v3/mmr/{Region}/pc/{Uri.EscapeDataString(nom)}/{Uri.EscapeDataString(tag)}", Entetes);
            var parties = await Lire($"{Api}/v1/stored-matches/{Region}/{Uri.EscapeDataString(nom)}/{Uri.EscapeDataString(tag)}?mode=competitive&size=10", Entetes);
            _jeuDeRangs ??= await JeuDeRangs();
            Afficher(mmr, LireParties(parties));
            _etat.Text = $"HenrikDev API · mis à jour à {DateTime.Now:HH:mm}";
            App.Instance.Notifier();
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _etat.Text = "Clé API refusée : ✏ Modifier › Modifier le compte…";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _etat.Text = $"Joueur « {RiotId} » introuvable dans la région {Region.ToUpperInvariant()}.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == (HttpStatusCode)429)
        {
            _etat.Text = "Trop de demandes à l'API, nouvel essai dans 10 minutes.";
        }
        catch
        {
            _etat.Text = "Impossible de joindre l'API (connexion ?)";
        }
        finally { _occupe = false; }
    }

    void AfficherNonConfigure()
    {
        _contenu.Children.Clear();
        _contenu.Children.Add(Titre("Valorant"));
        var aide = Texte("Suis ton rang, tes RR et tes dernières parties classées.", 13, Pale);
        aide.TextWrapping = TextWrapping.Wrap;
        aide.Margin = new Thickness(0, 6, 0, 12);
        _contenu.Children.Add(aide);
        _contenu.Children.Add(Cliquable(new Border
        {
            Background = Accent,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 7, 14, 8),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = Texte("Configurer", 13, Brushes.White),
        }, Configurer, survol: false));
        _etat.Text = "";
    }

    void Afficher(JsonElement mmr, List<Partie> parties)
    {
        _contenu.Children.Clear();
        var donnees = mmr.GetProperty("data");
        var actuel = donnees.GetProperty("current");
        int tier = actuel.GetProperty("tier").GetProperty("id").GetInt32();
        string rang = Traduire(actuel.GetProperty("tier").GetProperty("name").GetString());
        int rr = Entier(actuel, "rr");
        int variation = Entier(actuel, "last_change");

        var icone = new Image { Width = 56, Height = 56, Margin = new Thickness(0, 0, 14, 0), Source = Charger(IconeRang(tier)) };
        var nomRang = Texte(rang, 19);
        nomRang.FontWeight = FontWeights.SemiBold;
        var identite = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        identite.Children.Add(nomRang);
        identite.Children.Add(Texte(RiotId, 12, Pale));
        var entete = new DockPanel();
        DockPanel.SetDock(icone, Dock.Left);
        entete.Children.Add(icone);
        entete.Children.Add(identite);
        var (nom, tag) = Decouper(RiotId);
        _contenu.Children.Add(Cliquable(new Border { Child = entete, Background = Brushes.Transparent, ToolTip = "Ouvrir le profil sur tracker.gg" },
            () => Ouvrir($"https://tracker.gg/valorant/profile/riot/{Uri.EscapeDataString(nom + "#" + tag)}/overview"), survol: false));

        if (tier > 2)
        {
            var (barre, rempli) = Barre(6);
            rempli.Width = Largeur * Math.Clamp(rr / 100.0, 0, 1);
            barre.Margin = new Thickness(0, 10, 0, 4);
            var ligneRr = new DockPanel();
            var delta = Texte(variation == 0 ? "" : (variation > 0 ? "+" : "") + variation + " RR", 12, variation >= 0 ? Victoire : Defaite);
            delta.FontWeight = FontWeights.SemiBold;
            DockPanel.SetDock(delta, Dock.Right);
            ligneRr.Children.Add(delta);
            ligneRr.Children.Add(Texte($"{rr} / 100 RR", 12, Pale));
            _contenu.Children.Add(barre);
            _contenu.Children.Add(ligneRr);
        }

        if (donnees.TryGetProperty("peak", out var pic) && pic.ValueKind == JsonValueKind.Object && pic.TryGetProperty("tier", out var picTier))
        {
            var saison = pic.TryGetProperty("season", out var s) && s.ValueKind == JsonValueKind.Object ? $" ({s.GetProperty("short").GetString()?.ToUpperInvariant()})" : "";
            var meilleur = Texte($"Meilleur rang : {Traduire(picTier.GetProperty("name").GetString())}{saison}", 12, Pale);
            meilleur.Margin = new Thickness(0, 4, 0, 0);
            _contenu.Children.Add(meilleur);
        }

        if (parties.Count == 0)
        {
            _contenu.Children.Add(Separateur());
            _contenu.Children.Add(Texte("Aucune partie classée récente.", 13, Pale));
            return;
        }

        int victoires = parties.Count(p => p.Nous > p.Eux), defaites = parties.Count(p => p.Nous < p.Eux);
        double kd = (double)parties.Sum(p => p.Kills) / Math.Max(1, parties.Sum(p => p.Morts));
        int tirs = parties.Sum(p => p.Tirs);
        double hs = tirs == 0 ? 0 : 100.0 * parties.Sum(p => p.Tete) / tirs;

        _contenu.Children.Add(Separateur());
        var stats = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        for (int i = 0; i < 3; i++) stats.ColumnDefinitions.Add(new ColumnDefinition());
        var blocs = new[]
        {
            ($"{victoires}V · {defaites}D", $"{100.0 * victoires / parties.Count:0} % de victoires"),
            (kd.ToString("0.00", Fr), "K/D"),
            ($"{hs:0} %", "Headshots"),
        };
        for (int i = 0; i < 3; i++)
        {
            var bloc = new StackPanel();
            var valeur = Texte(blocs[i].Item1, 15);
            valeur.FontWeight = FontWeights.SemiBold;
            bloc.Children.Add(valeur);
            bloc.Children.Add(Texte(blocs[i].Item2, 11, Pale));
            Grid.SetColumn(bloc, i);
            stats.Children.Add(bloc);
        }
        _contenu.Children.Add(stats);
        var sous = Texte($"Sur les {parties.Count} dernières parties classées", 11, Pale);
        sous.Margin = new Thickness(0, 0, 0, 6);
        _contenu.Children.Add(sous);

        foreach (var p in parties.Take(5)) _contenu.Children.Add(LignePartie(p));
    }

    FrameworkElement LignePartie(Partie p)
    {
        var couleur = p.Nous > p.Eux ? Victoire : p.Nous < p.Eux ? Defaite : Pale;
        var trait = new Border { Width = 3, CornerRadius = new CornerRadius(1.5), Background = couleur, Margin = new Thickness(0, 2, 10, 2) };
        var agent = new Border
        {
            Width = 30, Height = 30,
            CornerRadius = new CornerRadius(15),
            Background = Theme.Piste,
            Margin = new Thickness(0, 0, 10, 0),
            ToolTip = p.Agent,
        };
        var image = Charger($"https://media.valorant-api.com/agents/{p.IdAgent}/displayicon.png");
        if (image != null) agent.Background = new ImageBrush(image) { Stretch = Stretch.UniformToFill };

        var score = Texte($"{p.Nous}–{p.Eux}", 14, couleur);
        score.FontWeight = FontWeights.SemiBold;
        score.VerticalAlignment = VerticalAlignment.Center;
        score.Margin = new Thickness(8, 0, 0, 0);

        var carte = Texte(p.Carte, 13);
        carte.FontWeight = FontWeights.SemiBold;
        var infos = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        infos.Children.Add(carte);
        infos.Children.Add(Texte($"{p.Kills} / {p.Morts} / {p.Assists}" + (p.Date is { } d ? " · " + Relatif(d) : ""), 11, Pale));

        var ligne = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
        DockPanel.SetDock(trait, Dock.Left);
        DockPanel.SetDock(agent, Dock.Left);
        DockPanel.SetDock(score, Dock.Right);
        ligne.Children.Add(trait);
        ligne.Children.Add(agent);
        ligne.Children.Add(score);
        ligne.Children.Add(infos);
        return ligne;
    }

    static List<Partie> LireParties(JsonElement reponse)
    {
        var liste = new List<Partie>();
        if (!reponse.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return liste;
        foreach (var m in data.EnumerateArray())
        {
            try
            {
                var meta = m.GetProperty("meta");
                var stats = m.GetProperty("stats");
                var equipes = m.GetProperty("teams");
                var equipe = (stats.TryGetProperty("team", out var t) ? t.GetString() : "")?.ToLowerInvariant() ?? "";
                int rouge = Entier(equipes, "red"), bleu = Entier(equipes, "blue");
                var tirs = stats.TryGetProperty("shots", out var s) ? s : default;
                int tete = tirs.ValueKind == JsonValueKind.Object ? Entier(tirs, "head") : 0;
                int total = tirs.ValueKind == JsonValueKind.Object ? tete + Entier(tirs, "body") + Entier(tirs, "leg") : 0;
                var perso = stats.GetProperty("character");
                liste.Add(new Partie(
                    meta.GetProperty("map").GetProperty("name").GetString(),
                    perso.TryGetProperty("name", out var n) ? n.GetString() : "",
                    perso.TryGetProperty("id", out var id) ? id.GetString() : "",
                    Entier(stats, "kills"), Entier(stats, "deaths"), Entier(stats, "assists"),
                    tete, total,
                    equipe == "red" ? rouge : bleu, equipe == "red" ? bleu : rouge,
                    DateTimeOffset.TryParse(meta.TryGetProperty("started_at", out var d) ? d.GetString() : null, out var date) ? date.LocalDateTime : null));
            }
            catch { }
        }
        return liste;
    }

    static int Entier(JsonElement e, string propriete) =>
        e.TryGetProperty(propriete, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    static string Traduire(string rang)
    {
        if (string.IsNullOrEmpty(rang)) return "Non classé";
        var mots = new Dictionary<string, string>
        {
            ["Unrated"] = "Non classé", ["Unranked"] = "Non classé", ["Iron"] = "Fer", ["Bronze"] = "Bronze",
            ["Silver"] = "Argent", ["Gold"] = "Or", ["Platinum"] = "Platine", ["Diamond"] = "Diamant",
            ["Ascendant"] = "Ascendant", ["Immortal"] = "Immortel", ["Radiant"] = "Radiant",
        };
        var parties = rang.Split(' ');
        if (mots.TryGetValue(parties[0], out var fr)) parties[0] = fr;
        return string.Join(" ", parties);
    }

    static string IconeRang(int tier) =>
        $"https://media.valorant-api.com/competitivetiers/{_jeuDeRangs ?? RangsParDefaut}/{tier}/largeicon.png";

    static async Task<string> JeuDeRangs()
    {
        try
        {
            using var doc = JsonDocument.Parse(await Web.Http.GetStringAsync("https://valorant-api.com/v1/competitivetiers"));
            return doc.RootElement.GetProperty("data").EnumerateArray().Last().GetProperty("uuid").GetString();
        }
        catch { return RangsParDefaut; }
    }

    static BitmapImage Charger(string url)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(url);
            image.DecodePixelWidth = 112;
            image.EndInit();
            return image;
        }
        catch { return null; }
    }

    static (string Nom, string Tag) Decouper(string riotId)
    {
        int i = riotId.LastIndexOf('#');
        return i < 0 ? (riotId, "") : (riotId[..i], riotId[(i + 1)..]);
    }

    static async Task<JsonElement> Lire(string url, Action<HttpRequestHeaders> entetes)
    {
        using var requete = new HttpRequestMessage(HttpMethod.Get, url);
        entetes(requete.Headers);
        using var reponse = await Web.Http.SendAsync(requete);
        reponse.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    void Configurer()
    {
        var f = new FormulaireValorant(RiotId, Array.FindIndex(Regions, r => r.Code == Region), !string.IsNullOrEmpty(Coffre.Lire(CleCoffre)));
        if (f.ShowDialog() != true) return;
        Config.Options["riotid"] = f.RiotId;
        SetOption("region", Regions[f.Region].Code);
        if (f.Cle.Length > 0) Coffre.Ecrire(CleCoffre, f.Cle);
        App.Instance.Notifier();
        Actualiser();
    }

    class FormulaireValorant : Dialogue
    {
        readonly TextBox _riotId;
        readonly ComboBox _region;
        readonly PasswordBox _cle;

        public string RiotId => _riotId.Text.Trim();
        public int Region => Math.Max(0, _region.SelectedIndex);
        public string Cle => _cle.Password.Trim();

        public FormulaireValorant(string riotId, int region, bool cleEnregistree) : base("Configurer Valorant", 480)
        {
            Grand("Suivre un compte Valorant");
            Aide("Ton rang, tes RR et tes dernières parties classées, directement sur ton bureau.", 0);

            Etiquette("Riot ID");
            _riotId = Ajouter(new TextBox { Text = riotId });
            Aide("Ton pseudo suivi de ton tag, par exemple : Pseudo#EUW");

            Etiquette("Région");
            _region = Ajouter(new ComboBox());
            foreach (var (_, nom) in Regions) _region.Items.Add(nom);
            _region.SelectedIndex = Math.Max(0, region);

            Etiquette("Clé API HenrikDev");
            _cle = Ajouter(new PasswordBox());
            if (cleEnregistree) Aide("Une clé est déjà enregistrée : laisse vide pour la garder.");
            var lien = Ajouter(new Button { Content = "Comment obtenir une clé gratuite  ↗", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 10, 0, 0) });
            lien.Click += (_, _) => { try { Process.Start(new ProcessStartInfo(PageCle) { UseShellExecute = true }); } catch { } };

            Encart("\uE72E", "Riot ne donne pas accès aux données de Valorant aux projets personnels : le widget passe par l'API communautaire HenrikDev (clé gratuite via leur Discord). La clé reste chiffrée sur ce PC, dans le Gestionnaire d'identification de Windows.");

            Boutons(Secondaire("Annuler"), Principal("Enregistrer", () =>
            {
                if (!RiotId.Contains('#')) { MessageBox.Show(this, "Écris ton Riot ID avec le #, par exemple : Pseudo#EUW", "Valorant"); return; }
                if (Cle.Length == 0 && !cleEnregistree) { MessageBox.Show(this, "Colle ta clé API HenrikDev.", "Valorant"); return; }
                Valider();
            }));
            Loaded += (_, _) => _riotId.Focus();
        }
    }
    protected override void Supprime()
    {
        if (!App.Instance.IdPartage(Config)) Coffre.Effacer(CleCoffre);
    }

    protected override void RemplirMenu()
    {
        Item("Actualiser", Actualiser);
        Item(RiotId.Length > 0 ? "Modifier le compte…" : "Configurer…", Configurer);
        if (RiotId.Length > 0)
            Item("Se déconnecter", () =>
            {
                Coffre.Effacer(CleCoffre);
                Config.Options.Remove("riotid");
                App.Instance.Sauver();
                App.Instance.Notifier();
                AfficherNonConfigure();
            });
    }
}
