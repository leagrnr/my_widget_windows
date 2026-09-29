using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesWidgets;

public class AgendaWidget : WidgetWindow
{
    readonly StackPanel _liste = new();
    readonly TextBlock _etat;

    string Adresse => Option("url", "");
    public override string Resume => Adresse == "" ? "Non connecté" : "Connecté";

    public AgendaWidget(WidgetConfig c) : base(c)
    {
        var entete = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        _etat = Texte("", 11, Pale);
        _etat.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(_etat, Dock.Right);
        entete.Children.Add(_etat);
        entete.Children.Add(Titre("Agenda"));

        var pile = new StackPanel { Width = 280 };
        pile.Children.Add(entete);
        pile.Children.Add(_liste);
        Content = Carte(pile);

        Loaded += (_, _) => Charger();
        Minuteur(TimeSpan.FromMinutes(15), Charger);
    }

    async void Charger()
    {
        _liste.Children.Clear();
        if (Adresse == "")
        {
            _etat.Text = "";
            var aide = Texte("Affiche les rendez-vous de ton agenda Google ou Outlook.\n\n✏ Modifier › Connecter un agenda…", 13, Pale);
            aide.TextWrapping = TextWrapping.Wrap;
            _liste.Children.Add(aide);
            return;
        }
        try
        {
            var ics = await Web.Http.GetStringAsync(Adresse);
            var debut = DateTime.Today;
            var evenements = Ics.Evenements(ics, debut, debut.AddDays(14));
            Afficher(evenements);
            _etat.Text = DateTime.Now.ToString("HH:mm");
        }
        catch
        {
            _etat.Text = "";
            var erreur = Texte("Impossible de lire l'agenda.\nVérifie l'adresse (bouton ✏) et ta connexion.", 13, Pale);
            erreur.TextWrapping = TextWrapping.Wrap;
            _liste.Children.Add(erreur);
        }
    }

    void Afficher(List<Ics.Evenement> evenements)
    {
        if (evenements.Count == 0)
        {
            _liste.Children.Add(Texte("Rien de prévu ces 2 prochaines semaines.", 13, Pale));
            return;
        }

        foreach (var jour in evenements.Take(8).GroupBy(e => e.Debut.Date))
        {
            int dans = (jour.Key - DateTime.Today).Days;
            var titreJour = Texte(dans switch { 0 => "Aujourd'hui", 1 => "Demain", _ => Majuscule(jour.Key.ToString("dddd d MMMM", Fr)) }, 12, Accent);
            titreJour.FontWeight = FontWeights.SemiBold;
            titreJour.Margin = new Thickness(0, 6, 0, 2);
            _liste.Children.Add(titreJour);

            foreach (var e in jour)
            {
                var ligne = new DockPanel { Margin = new Thickness(0, 1, 0, 1) };
                var heure = Texte(e.JourneeEntiere ? "Journée" : e.Debut.ToString("HH:mm"), 12, Pale);
                heure.Width = 54;
                heure.VerticalAlignment = VerticalAlignment.Center;
                ligne.Children.Add(heure);
                var titre = Texte(e.Titre, 13);
                titre.TextTrimming = TextTrimming.CharacterEllipsis;
                ligne.Children.Add(titre);
                _liste.Children.Add(ligne);
            }
        }
    }

    protected override void RemplirMenu()
    {
        Item(Adresse == "" ? "Connecter un agenda…" : "Changer d'agenda…", () =>
        {
            var url = Saisie.Demander("Agenda",
                "Colle l'adresse iCal secrète de ton agenda :\n\n" +
                "• Google Agenda (sur le web) : Paramètres › clique sur ton agenda › « Adresse secrète au format iCal »\n" +
                "• Outlook.com : Paramètres › Calendrier › Calendriers partagés › Publier un calendrier › lien ICS\n\n" +
                "Garde cette adresse pour toi : elle donne accès à ton agenda.",
                Adresse, 480);
            if (url == null) return;
            if (url.StartsWith("webcal://", StringComparison.OrdinalIgnoreCase)) url = "https://" + url[9..];
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) { MessageBox.Show("Adresse invalide.", "Agenda"); return; }
            SetOption("url", url);
            App.Instance.Notifier();
            Charger();
        });
        if (Adresse == "") return;
        Item("Actualiser", Charger);
        Item("Déconnecter l'agenda", () => { SetOption("url", ""); App.Instance.Notifier(); Charger(); });
    }
}

static class Ics
{
    public record Evenement(DateTime Debut, bool JourneeEntiere, string Titre);

    static readonly string[] Jours = { "SU", "MO", "TU", "WE", "TH", "FR", "SA" };

    public static List<Evenement> Evenements(string texte, DateTime de, DateTime a)
    {
        var lignes = new List<string>();
        foreach (var brute in texte.Replace("\r\n", "\n").Split('\n'))
        {
            if (brute.Length > 0 && (brute[0] == ' ' || brute[0] == '\t') && lignes.Count > 0) lignes[^1] += brute[1..];
            else lignes.Add(brute);
        }

        var resultat = new List<Evenement>();
        Dictionary<string, (string Params, string Valeur)> props = null;
        List<string> exclues = null;
        foreach (var l in lignes)
        {
            if (l == "BEGIN:VEVENT") { props = new(); exclues = new(); continue; }
            if (l == "END:VEVENT" && props != null)
            {
                try { Traiter(props, exclues, de, a, resultat); } catch { }
                props = null;
                continue;
            }
            if (props == null) continue;
            int i = l.IndexOf(':');
            if (i < 0) continue;
            var tete = l[..i];
            var valeur = l[(i + 1)..];
            var nom = tete.Split(';')[0].ToUpperInvariant();
            if (nom == "EXDATE") exclues.AddRange(valeur.Split(','));
            else props[nom] = (tete, valeur);
        }
        return resultat.OrderBy(e => e.Debut).ToList();
    }

    static void Traiter(Dictionary<string, (string Params, string Valeur)> p, List<string> exclues,
                        DateTime de, DateTime a, List<Evenement> resultat)
    {
        if (!p.TryGetValue("DTSTART", out var dt)) return;
        if (p.TryGetValue("STATUS", out var statut) && statut.Valeur == "CANCELLED") return;

        var (debut, journee) = LireDate(dt.Params, dt.Valeur);
        var titre = p.TryGetValue("SUMMARY", out var s) ? Nettoyer(s.Valeur) : "(sans titre)";
        var sautees = exclues.Select(x => { try { return LireDate(dt.Params, x.Trim()).Quand; } catch { return DateTime.MinValue; } }).ToHashSet();

        void Ajouter(DateTime d)
        {
            bool dedans = journee ? d.Date >= de.Date && d.Date < a.Date : d >= de && d < a;
            if (dedans && !sautees.Contains(d)) resultat.Add(new Evenement(d, journee, titre));
        }

        if (!p.TryGetValue("RRULE", out var rrule)) { Ajouter(debut); return; }

        var regle = rrule.Valeur.Split(';').Select(x => x.Split('=')).Where(x => x.Length == 2)
                                .ToDictionary(x => x[0].ToUpperInvariant(), x => x[1]);
        var freq = regle.GetValueOrDefault("FREQ", "");
        int intervalle = int.TryParse(regle.GetValueOrDefault("INTERVAL", "1"), out var iv) && iv > 0 ? iv : 1;
        int? nombre = int.TryParse(regle.GetValueOrDefault("COUNT", ""), out var n) ? n : null;
        DateTime? jusque = regle.TryGetValue("UNTIL", out var u) ? LireDate("", u).Quand : null;
        var parJour = regle.GetValueOrDefault("BYDAY", "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(j => Array.IndexOf(Jours, j[^2..])).Where(j => j >= 0).ToList();

        int compte = 0;
        bool Continuer(DateTime d) => d < a && (jusque == null || d <= jusque) && (nombre == null || compte < nombre);

        if (freq == "WEEKLY" && parJour.Count > 0)
        {
            var lundi = debut.Date.AddDays(-(((int)debut.DayOfWeek + 6) % 7));
            var ordre = parJour.OrderBy(j => (j + 6) % 7).ToList();
            for (int semaine = 0; semaine < 3000; semaine++)
            {
                foreach (var j in ordre)
                {
                    var d = lundi.AddDays(7 * intervalle * semaine + (j + 6) % 7) + debut.TimeOfDay;
                    if (d < debut) continue;
                    if (!Continuer(d)) return;
                    compte++;
                    Ajouter(d);
                }
            }
            return;
        }

        for (int k = 0; k < 20000; k++)
        {
            var d = freq switch
            {
                "DAILY" => debut.AddDays(k * intervalle),
                "WEEKLY" => debut.AddDays(7 * k * intervalle),
                "MONTHLY" => debut.AddMonths(k * intervalle),
                "YEARLY" => debut.AddYears(k * intervalle),
                _ => k == 0 ? debut : DateTime.MaxValue,
            };
            if (!Continuer(d)) return;
            compte++;
            Ajouter(d);
        }
    }

    static (DateTime Quand, bool Journee) LireDate(string parametres, string valeur)
    {
        valeur = valeur.Trim();
        if (parametres.Contains("VALUE=DATE", StringComparison.OrdinalIgnoreCase) && !parametres.Contains("DATE-TIME") || valeur.Length == 8)
            return (DateTime.ParseExact(valeur[..8], "yyyyMMdd", CultureInfo.InvariantCulture), true);

        var d = DateTime.ParseExact(valeur[..15], "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
        if (valeur.EndsWith("Z"))
            return (DateTime.SpecifyKind(d, DateTimeKind.Utc).ToLocalTime(), false);

        var tzid = parametres.Split(';').FirstOrDefault(x => x.StartsWith("TZID=", StringComparison.OrdinalIgnoreCase));
        if (tzid != null)
        {
            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(tzid[5..].Trim('"'));
                return (TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(d, DateTimeKind.Unspecified), zone, TimeZoneInfo.Local), false);
            }
            catch { }
        }
        return (d, false);
    }

    static string Nettoyer(string s) =>
        s.Replace("\\n", " ").Replace("\\N", " ").Replace("\\,", ",").Replace("\\;", ";").Replace("\\\\", "\\").Trim();
}
