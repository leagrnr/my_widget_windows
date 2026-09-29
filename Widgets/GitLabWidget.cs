using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace MesWidgets;

public class GitLabWidget : ForgeWidget
{
    public GitLabWidget(WidgetConfig c) : base(c) { }

    protected override string NomService => "GitLab";

    protected override string ServeurParDefaut => "https://gitlab.com";

    protected override string PageJeton(string serveur) =>
        $"{(string.IsNullOrEmpty(serveur) ? ServeurParDefaut : serveur)}/-/user_settings/personal_access_tokens?name=Mes%20Widgets&scopes=read_api,read_user";

    protected override string DroitsJeton =>
        "Droits nécessaires : read_api et read_user (déjà cochés par le bouton). Choisis une date d'expiration, clique sur « Create token » et copie le jeton (il commence par glpat-).";

    protected override (string ARelire, string Assignes, string Notifications) Libelles => ("À relire", "Assignées", "À faire");

    protected override (string ARelire, string Assignes, string Notifications) Pages(Donnees d) =>
        ($"{Serveur}/dashboard/merge_requests?reviewer_username={d.Pseudo}",
         $"{Serveur}/dashboard/issues?assignee_username={d.Pseudo}",
         $"{Serveur}/dashboard/todos");

    protected override async Task<Donnees> Charger(string jeton)
    {
        void Entetes(HttpRequestHeaders h) => h.Add("PRIVATE-TOKEN", jeton);
        var api = $"{Serveur}/api/v4";

        var (moi, _) = await Lire($"{api}/user", Entetes);
        var pseudo = Chaine(moi, "username");

        List<Element> Elements(JsonElement liste, string genre, char separateur) =>
            liste.EnumerateArray().Select(e =>
            {
                var reference = e.TryGetProperty("references", out var r) ? Chaine(r, "full") ?? "" : "";
                var projet = reference.Contains(separateur) ? reference[..reference.LastIndexOf(separateur)] : reference;
                return new Element(genre, Chaine(e, "title"), projet, Date(e, "updated_at"), Chaine(e, "web_url"));
            }).ToList();

        var (relire, totalRelire) = await Lire($"{api}/merge_requests?scope=all&state=opened&reviewer_username={Uri.EscapeDataString(pseudo)}&order_by=updated_at&per_page=5", Entetes);
        var (mrAssignees, totalMr) = await Lire($"{api}/merge_requests?scope=assigned_to_me&state=opened&order_by=updated_at&per_page=5", Entetes);
        var (issues, totalIssues) = await Lire($"{api}/issues?scope=assigned_to_me&state=opened&order_by=updated_at&per_page=5", Entetes);
        var (_, totalTodos) = await Lire($"{api}/todos?state=pending&per_page=1", Entetes);

        var elements = Elements(relire, "MR", '!')
            .Concat(Elements(mrAssignees, "MR", '!'))
            .Concat(Elements(issues, "Issue", '#'))
            .GroupBy(e => e.Url).Select(g => g.First())
            .OrderByDescending(e => e.Date)
            .Take(4).ToList();

        var avatar = Chaine(moi, "avatar_url");
        if (avatar != null && avatar.StartsWith('/')) avatar = Serveur + avatar;

        return new Donnees(
            Chaine(moi, "name") ?? pseudo, pseudo, avatar, Chaine(moi, "web_url"),
            totalRelire ?? relire.GetArrayLength(),
            (totalMr ?? mrAssignees.GetArrayLength()) + (totalIssues ?? issues.GetArrayLength()),
            totalTodos ?? 0,
            elements, await Contributions(pseudo, Entetes));
    }

    async Task<Dictionary<DateTime, int>> Contributions(string pseudo, Action<HttpRequestHeaders> entetes)
    {
        try
        {
            var (r, _) = await Lire($"{Serveur}/users/{Uri.EscapeDataString(pseudo)}/calendar.json", entetes);
            return r.EnumerateObject().ToDictionary(
                p => DateTime.ParseExact(p.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                p => p.Value.GetInt32());
        }
        catch { return null; }
    }
}
