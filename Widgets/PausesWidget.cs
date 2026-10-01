using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesWidgets;

public class PausesWidget : WidgetWindow
{
    record Rappel(string Code, string Icone, string Nom, string Message, int ParDefaut, int[] Choix);

    static readonly Rappel[] Rappels =
    {
        new("eau", "💧", "Boire de l'eau", "Un verre d'eau ? Ton corps te dira merci.", 60, new[] { 30, 45, 60, 90, 120 }),
        new("bouger", "🚶", "Se lever, bouger", "Lève-toi et bouge un peu, étire-toi.", 50, new[] { 30, 45, 50, 60, 90 }),
        new("yeux", "👀", "Reposer tes yeux", "Règle 20-20-20 : regarde au loin (6 mètres) pendant 20 secondes.", 20, new[] { 20, 30, 45, 60 }),
    };

    readonly StackPanel _liste = new();
    readonly TextBlock _etat;
    readonly Border _boutonPause;
    DateTime? _pauseJusqua;

    public override string Resume => _pauseJusqua != null ? "En pause" : $"{Rappels.Count(Actif)} rappel(s) actif(s)";

    public PausesWidget(WidgetConfig c) : base(c)
    {
        _etat = Texte("", 11, Pale);
        _etat.Margin = new Thickness(0, 8, 0, 0);

        _boutonPause = Cliquable(new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = Theme.Piste,
            Padding = new Thickness(10, 4, 10, 5),
            VerticalAlignment = VerticalAlignment.Center,
        }, BasculerPause);
        var entete = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(_boutonPause, Dock.Right);
        entete.Children.Add(_boutonPause);
        entete.Children.Add(Titre("Pauses"));

        var pile = new StackPanel { Width = 260 };
        pile.Children.Add(entete);
        pile.Children.Add(_liste);
        pile.Children.Add(_etat);
        Content = Carte(pile);

        foreach (var r in Rappels)
            if (Option("prochain_" + r.Code, "") == "") Reporter(r);
        Construire();
        Minuteur(TimeSpan.FromSeconds(15), Verifier);
    }

    bool Actif(Rappel r) => Option("actif_" + r.Code, "oui") == "oui";
    int Intervalle(Rappel r) => int.TryParse(Option("minutes_" + r.Code, ""), out var m) ? m : r.ParDefaut;

    DateTime Prochain(Rappel r) =>
        DateTime.TryParse(Option("prochain_" + r.Code, ""), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : DateTime.Now.AddMinutes(Intervalle(r));

    void Reporter(Rappel r) =>
        Config.Options["prochain_" + r.Code] = DateTime.Now.AddMinutes(Intervalle(r)).ToString("s", CultureInfo.InvariantCulture);

    void Verifier()
    {
        var maintenant = DateTime.Now;
        if (_pauseJusqua is { } fin)
        {
            if (maintenant < fin) { Construire(); return; }
            _pauseJusqua = null;
            foreach (var r in Rappels) Reporter(r);
        }

        bool change = false;
        foreach (var r in Rappels.Where(Actif))
        {
            var prochain = Prochain(r);
            if (maintenant < prochain) continue;
            if (maintenant - prochain < TimeSpan.FromMinutes(10))
                App.Instance.Notification($"{r.Icone} {r.Nom}", r.Message);
            Reporter(r);
            change = true;
        }
        if (change) App.Instance.Sauver();
        Construire();
    }

    void Construire()
    {
        _liste.Children.Clear();
        var maintenant = DateTime.Now;
        foreach (var r in Rappels)
        {
            var rappel = r;
            bool actif = Actif(r);
            var icone = Texte(r.Icone, 18, Blanc, "Segoe UI Emoji");
            icone.Margin = new Thickness(0, 0, 10, 0);
            icone.VerticalAlignment = VerticalAlignment.Center;
            var textes = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textes.Children.Add(Texte(r.Nom, 13, actif ? Blanc : Pale));
            string detail;
            if (!actif) detail = "désactivé";
            else if (_pauseJusqua != null) detail = $"toutes les {Intervalle(r)} min · en pause";
            else
            {
                var reste = Prochain(r) - maintenant;
                detail = $"toutes les {Intervalle(r)} min · dans {Math.Max(1, (int)Math.Ceiling(reste.TotalMinutes))} min";
            }
            textes.Children.Add(Texte(detail, 11, Pale));

            var interrupteur = Interrupteur(actif, oui =>
            {
                Config.Options["actif_" + rappel.Code] = oui ? "oui" : "non";
                if (oui) Reporter(rappel);
                App.Instance.Sauver();
                Construire();
                App.Instance.Notifier();
            });

            var ligne = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
            DockPanel.SetDock(icone, Dock.Left);
            DockPanel.SetDock(interrupteur, Dock.Right);
            ligne.Children.Add(icone);
            ligne.Children.Add(interrupteur);
            ligne.Children.Add(textes);
            _liste.Children.Add(ligne);
        }

        _boutonPause.Child = Texte(_pauseJusqua != null ? "Reprendre" : "Pause 1 h", 12);
        _etat.Text = _pauseJusqua is { } fin ? $"Rappels en pause jusqu'à {fin:HH:mm}" : "";
    }

    FrameworkElement Interrupteur(bool actif, Action<bool> changement)
    {
        var bouton = new Border
        {
            Width = 16, Height = 16,
            CornerRadius = new CornerRadius(8),
            Background = Brushes.White,
            HorizontalAlignment = actif ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Margin = new Thickness(2),
        };
        var rail = new Border
        {
            Width = 38, Height = 20,
            CornerRadius = new CornerRadius(10),
            Background = actif ? Accent : Theme.Piste,
            Child = bouton,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
        };
        return Cliquable(rail, () => changement(!actif), survol: false);
    }

    void BasculerPause()
    {
        if (_pauseJusqua != null)
        {
            _pauseJusqua = null;
            foreach (var r in Rappels) Reporter(r);
            App.Instance.Sauver();
        }
        else _pauseJusqua = DateTime.Now.AddHours(1);
        Construire();
        App.Instance.Notifier();
    }

    protected override void RemplirMenu()
    {
        foreach (var r in Rappels)
        {
            var rappel = r;
            var menu = SousMenu($"{r.Icone} {r.Nom} : toutes les…");
            foreach (var m in r.Choix)
                Coche($"{m} minutes", m == Intervalle(r), _ =>
                {
                    Config.Options["minutes_" + rappel.Code] = m.ToString();
                    Reporter(rappel);
                    App.Instance.Sauver();
                    Construire();
                }, menu);
        }
        Item(_pauseJusqua != null ? "Reprendre les rappels" : "Mettre en pause 1 heure", BasculerPause);
    }
}
