using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace MesWidgets;

static class Web
{
    public static readonly HttpClient Http = Creer();

    static HttpClient Creer()
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        h.DefaultRequestHeaders.UserAgent.ParseAdd("MesWidgets/1.0 (Windows)");
        return h;
    }
}

public class ActualitesWidget : WidgetWindow
{
    static readonly (string Nom, string Url)[] Sources =
    {
        ("Le Monde", "https://www.lemonde.fr/rss/une.xml"),
        ("franceinfo", "https://www.francetvinfo.fr/titres.rss"),
        ("20 Minutes", "https://www.20minutes.fr/feeds/rss-une.xml"),
        ("Le Figaro", "https://www.lefigaro.fr/rss/figaro_actualites.xml"),
        ("Numerama (tech)", "https://www.numerama.com/feed/"),
        ("Futura (sciences)", "https://www.futura-sciences.com/rss/actualites.xml"),
    };

    readonly TextBlock _source, _etat;
    readonly StackPanel _articles = new();

    string NomSource => Option("source", Sources[0].Nom);
    string Adresse => Option("url", Sources[0].Url);
    public override string Resume => NomSource;

    public ActualitesWidget(WidgetConfig c) : base(c)
    {
        var entete = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        _etat = Texte("", 11, Pale);
        _etat.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(_etat, Dock.Right);
        entete.Children.Add(_etat);
        _source = Titre(NomSource);
        entete.Children.Add(_source);

        var pile = new StackPanel { Width = 300 };
        pile.Children.Add(entete);
        pile.Children.Add(_articles);
        Content = Carte(pile);

        Loaded += (_, _) => Charger();
        Minuteur(TimeSpan.FromMinutes(20), Charger);
    }

    async void Charger()
    {
        _source.Text = NomSource;
        try
        {
            var xml = XDocument.Parse(await Web.Http.GetStringAsync(Adresse));
            XNamespace atom = "http://www.w3.org/2005/Atom";

            var articles = xml.Descendants("item").Select(i => (
                    Titre: (string)i.Element("title"),
                    Lien: (string)i.Element("link"),
                    Date: (string)i.Element("pubDate")))
                .Concat(xml.Descendants(atom + "entry").Select(e => (
                    Titre: (string)e.Element(atom + "title"),
                    Lien: (string)e.Element(atom + "link")?.Attribute("href"),
                    Date: (string)e.Element(atom + "updated"))))
                .Where(a => !string.IsNullOrWhiteSpace(a.Titre))
                .Take(5)
                .ToList();

            _articles.Children.Clear();
            foreach (var a in articles)
            {
                var titre = Texte(WebUtility.HtmlDecode(a.Titre.Trim()), 13);
                titre.TextWrapping = TextWrapping.Wrap;
                titre.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                titre.LineHeight = 18;
                titre.MaxHeight = 36;
                titre.TextTrimming = TextTrimming.CharacterEllipsis;

                var bloc = new StackPanel();
                bloc.Children.Add(titre);
                if (DateTimeOffset.TryParse(a.Date?.Replace("GMT", "+0000"), out var date))
                    bloc.Children.Add(Texte(Relatif(date.LocalDateTime), 11, Pale));

                var lien = a.Lien;
                var ligne = Cliquable(new Border
                {
                    Child = bloc,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 5, 8, 6),
                    Margin = new Thickness(-8, 0, -8, 0),
                    Background = System.Windows.Media.Brushes.Transparent,
                    ToolTip = "Ouvrir l'article",
                }, () => { if (!string.IsNullOrEmpty(lien)) Ouvrir(lien); });
                _articles.Children.Add(ligne);
            }
            _etat.Text = DateTime.Now.ToString("HH:mm");
            if (articles.Count == 0) _articles.Children.Add(Texte("Aucun article dans ce flux.", 13, Pale));
        }
        catch
        {
            _articles.Children.Clear();
            _articles.Children.Add(Texte("Actualités indisponibles.\nVérifie ta connexion Internet.", 13, Pale));
        }
    }

    protected override void RemplirMenu()
    {
        var menu = SousMenu("Source");
        foreach (var (nom, url) in Sources)
            Coche(nom, url == Adresse, _ => Choisir(nom, url), menu);
        Item("Autre flux RSS…", () =>
        {
            var url = Saisie.Demander("Actualités", "Adresse du flux RSS :", "https://");
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                Choisir(new Uri(url).Host.Replace("www.", ""), url);
        }, menu);
        Item("Actualiser", Charger);
    }

    void Choisir(string nom, string url)
    {
        Config.Options["source"] = nom;
        SetOption("url", url);
        App.Instance.Notifier();
        Charger();
    }
}
