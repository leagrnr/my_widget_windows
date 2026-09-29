using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MesWidgets;

public class GitHubWidget : ForgeWidget
{
    const string Api = "https://api.github.com";

    public GitHubWidget(WidgetConfig c) : base(c) { }

    protected override string NomService => "GitHub";

    protected override string PageJeton(string serveur) =>
        "https://github.com/settings/tokens/new?scopes=repo,notifications,read:user&description=Mes%20Widgets";

    protected override string DroitsJeton =>
        "Droits nécessaires : repo, notifications et read:user (déjà cochés par le bouton). Choisis une date d'expiration, clique sur « Generate token » et copie le jeton (il commence par ghp_).";

    protected override (string ARelire, string Assignes, string Notifications) Libelles => ("À relire", "Assignées", "Notifications");

    protected override (string ARelire, string Assignes, string Notifications) Pages(Donnees d) =>
        ("https://github.com/pulls/review-requested", "https://github.com/issues/assigned", "https://github.com/notifications");

    protected override async Task<Donnees> Charger(string jeton)
    {
        void Entetes(HttpRequestHeaders h)
        {
            h.Authorization = new AuthenticationHeaderValue("Bearer", jeton);
            h.Accept.ParseAdd("application/vnd.github+json");
        }

        var (moi, _) = await Lire($"{Api}/user", Entetes);
        var login = Chaine(moi, "login");

        async Task<(int Total, List<Element> Elements)> Chercher(string requete, string genreParDefaut)
        {
            var (r, _) = await Lire($"{Api}/search/issues?q={Uri.EscapeDataString(requete)}&sort=updated&per_page=5", Entetes);
            var elements = r.GetProperty("items").EnumerateArray().Select(i => new Element(
                i.TryGetProperty("pull_request", out _) ? "PR" : genreParDefaut,
                Chaine(i, "title"),
                string.Join("/", (Chaine(i, "repository_url") ?? "").Split('/').TakeLast(2)),
                Date(i, "updated_at"),
                Chaine(i, "html_url"))).ToList();
            return (r.GetProperty("total_count").GetInt32(), elements);
        }

        var relire = await Chercher($"is:open is:pr review-requested:{login} archived:false", "PR");
        var assignes = await Chercher($"is:open assignee:{login} archived:false", "Issue");
        var (notifs, _) = await Lire($"{Api}/notifications?per_page=50", Entetes);

        var elements = relire.Elements.Concat(assignes.Elements)
            .GroupBy(e => e.Url).Select(g => g.First())
            .Take(4).ToList();

        return new Donnees(
            Chaine(moi, "name") ?? login, login, Chaine(moi, "avatar_url"), Chaine(moi, "html_url"),
            relire.Total, assignes.Total, notifs.GetArrayLength(),
            elements, await Contributions(Entetes));
    }

    static async Task<Dictionary<DateTime, int>> Contributions(Action<HttpRequestHeaders> entetes)
    {
        try
        {
            var requete = JsonSerializer.Serialize(new
            {
                query = "query { viewer { contributionsCollection { contributionCalendar { weeks { contributionDays { date contributionCount } } } } } }",
            });
            var (r, _) = await Lire($"{Api}/graphql", entetes, new StringContent(requete, Encoding.UTF8, "application/json"));
            return r.GetProperty("data").GetProperty("viewer").GetProperty("contributionsCollection")
                .GetProperty("contributionCalendar").GetProperty("weeks").EnumerateArray()
                .SelectMany(s => s.GetProperty("contributionDays").EnumerateArray())
                .ToDictionary(j => DateTime.Parse(j.GetProperty("date").GetString()), j => j.GetProperty("contributionCount").GetInt32());
        }
        catch { return null; }
    }
}
