using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesWidgets;

public class AnniversairesWidget : WidgetWindow
{
    public class Anniversaire
    {
        public string Nom { get; set; }
        public int Jour { get; set; }
        public int Mois { get; set; }
        public int? Annee { get; set; }
    }

    readonly List<Anniversaire> _liste;
    readonly StackPanel _lignes = new();

    public override string Resume => $"{_liste.Count} personne{(_liste.Count > 1 ? "s" : "")}";

    public AnniversairesWidget(WidgetConfig c) : base(c)
    {
        try { _liste = JsonSerializer.Deserialize<List<Anniversaire>>(Option("liste", "[]")) ?? new(); }
        catch { _liste = new(); }

        var titre = Titre("Anniversaires");
        titre.Margin = new Thickness(0, 0, 0, 8);
        var pile = new StackPanel { Width = 250 };
        pile.Children.Add(titre);
        pile.Children.Add(_lignes);
        Content = Carte(pile);

        Construire();
        Loaded += (_, _) => Rappeler();
        Minuteur(TimeSpan.FromMinutes(15), () => { Construire(); Rappeler(); });
    }

    static DateTime Prochain(Anniversaire a)
    {
        var aujourdhui = DateTime.Today;
        DateTime Le(int annee) => new(annee, a.Mois, Math.Min(a.Jour, DateTime.DaysInMonth(annee, a.Mois)));
        var d = Le(aujourdhui.Year);
        return d < aujourdhui ? Le(aujourdhui.Year + 1) : d;
    }

    void Construire()
    {
        _lignes.Children.Clear();
        if (_liste.Count == 0)
        {
            var vide = Texte("✏ Modifier › Ajouter un anniversaire", 13, Pale);
            vide.TextWrapping = TextWrapping.Wrap;
            _lignes.Children.Add(vide);
            return;
        }

        foreach (var a in _liste.OrderBy(Prochain).Take(6))
        {
            var date = Prochain(a);
            int dans = (date - DateTime.Today).Days;

            var ligne = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
            var badge = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Background = dans == 0 ? Accent : Theme.Piste,
                Child = Texte(dans switch { 0 => "Aujourd'hui 🎉", 1 => "Demain", _ => $"dans {dans} j" }, 12, dans == 0 ? Brushes.White : Blanc),
            };
            DockPanel.SetDock(badge, Dock.Right);
            ligne.Children.Add(badge);

            var infos = new StackPanel();
            var nom = Texte(a.Nom, 14);
            nom.FontWeight = FontWeights.SemiBold;
            nom.TextTrimming = TextTrimming.CharacterEllipsis;
            infos.Children.Add(nom);
            var detail = date.ToString("d MMMM", Fr) + (a.Annee is int an ? $" · {date.Year - an} ans" : "");
            infos.Children.Add(Texte(detail, 12, Pale));
            ligne.Children.Add(infos);

            _lignes.Children.Add(ligne);
        }
    }

    void Rappeler()
    {
        var cle = DateTime.Today.ToString("yyyy-MM-dd");
        if (Option("rappel", "") == cle) return;
        var fetes = _liste.Where(a => Prochain(a) == DateTime.Today).Select(a => a.Nom).ToList();
        if (fetes.Count == 0) return;
        App.Instance.Notification("Anniversaire 🎂", $"C'est l'anniversaire de {string.Join(" et ", fetes)} aujourd'hui !");
        SetOption("rappel", cle);
    }

    void Ajouter()
    {
        var nom = Saisie.Demander("Anniversaire", "Prénom et nom :");
        if (nom == null) return;
        var texte = Saisie.Demander("Anniversaire", $"Date d'anniversaire de {nom} (jj/mm, ou jj/mm/aaaa pour afficher l'âge) :");
        if (texte == null) return;

        var parties = texte.Split(new[] { '/', '-', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        int jour = 0, mois = 0, annee = 0;
        bool ok = parties.Length is 2 or 3
                  && int.TryParse(parties[0], out jour) && int.TryParse(parties[1], out mois)
                  && (parties.Length == 2 || int.TryParse(parties[2], out annee))
                  && mois is >= 1 and <= 12 && jour >= 1 && jour <= DateTime.DaysInMonth(2000, mois)
                  && (parties.Length == 2 || annee is > 1900 && annee <= DateTime.Today.Year);
        if (!ok)
        {
            MessageBox.Show("Je n'ai pas compris la date. Exemples : 14/07 ou 14/07/1995", "Anniversaire");
            return;
        }
        _liste.Add(new Anniversaire { Nom = nom, Jour = jour, Mois = mois, Annee = parties.Length == 3 ? annee : null });
        Enregistrer();
    }

    void Enregistrer()
    {
        SetOption("liste", JsonSerializer.Serialize(_liste));
        Construire();
        App.Instance.Notifier();
    }

    protected override void RemplirMenu()
    {
        Item("Ajouter un anniversaire…", Ajouter);
        if (_liste.Count == 0) return;
        var retirer = SousMenu("Retirer");
        foreach (var a in _liste.OrderBy(x => x.Nom))
            Item(a.Nom, () => { _liste.Remove(a); Enregistrer(); }, retirer);
    }
}
