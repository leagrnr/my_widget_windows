using System;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace MesWidgets;

public class DevisesWidget : WidgetWindow
{
    static readonly (string Code, string Nom, string Symbole)[] Monnaies =
    {
        ("EUR", "Euro", "€"),
        ("USD", "Dollar américain", "$"),
        ("GBP", "Livre sterling", "£"),
        ("CHF", "Franc suisse", "CHF"),
        ("CAD", "Dollar canadien", "$ CA"),
        ("JPY", "Yen japonais", "¥"),
        ("CNY", "Yuan chinois", "¥"),
        ("AUD", "Dollar australien", "$ AU"),
        ("BRL", "Réal brésilien", "R$"),
        ("INR", "Roupie indienne", "₹"),
        ("MXN", "Peso mexicain", "$ MX"),
        ("TRY", "Livre turque", "₺"),
        ("SEK", "Couronne suédoise", "kr"),
        ("NOK", "Couronne norvégienne", "kr"),
    };

    readonly TextBlock _base, _date;
    readonly StackPanel _lignes = new();

    string Base => Option("base", "EUR");
    string[] Cibles => Option("cibles", "USD,GBP,CHF,CAD,JPY").Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                          .Where(c => c != Base).ToArray();
    public override string Resume => $"{Base} → {string.Join(", ", Cibles)}";

    public DevisesWidget(WidgetConfig c) : base(c)
    {
        _base = Texte("", 13, Pale);
        _base.Margin = new Thickness(0, 0, 0, 6);
        _date = Texte("", 11, Pale);
        _date.Margin = new Thickness(0, 6, 0, 0);

        var pile = new StackPanel { Width = 250 };
        pile.Children.Add(Titre("Taux de change"));
        pile.Children.Add(_base);
        pile.Children.Add(_lignes);
        pile.Children.Add(_date);
        Content = Carte(pile);

        Loaded += (_, _) => Charger();
        Minuteur(TimeSpan.FromHours(1), Charger);
    }

    static (string Code, string Nom, string Symbole) Monnaie(string code) => Monnaies.First(m => m.Code == code);

    async void Charger()
    {
        var b = Monnaie(Base);
        _base.Text = $"1 {b.Symbole} ({b.Nom.ToLower(Fr)}) =";
        if (Cibles.Length == 0) { _lignes.Children.Clear(); _date.Text = "✏ Modifier › Devises affichées"; return; }
        try
        {
            var url = $"https://api.frankfurter.dev/v1/latest?base={Base}&symbols={string.Join(",", Cibles)}";
            using var doc = JsonDocument.Parse(await Web.Http.GetStringAsync(url));
            var taux = doc.RootElement.GetProperty("rates");

            _lignes.Children.Clear();
            foreach (var code in Cibles)
            {
                if (!taux.TryGetProperty(code, out var v)) continue;
                var m = Monnaie(code);
                double valeur = v.GetDouble();

                var ligne = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
                var montant = Texte(valeur.ToString(valeur < 1 ? "N4" : "N2", Fr) + " " + m.Symbole, 16);
                montant.VerticalAlignment = VerticalAlignment.Center;
                DockPanel.SetDock(montant, Dock.Right);
                ligne.Children.Add(montant);

                var nom = new StackPanel();
                var codeTexte = Texte(code, 13);
                codeTexte.FontWeight = FontWeights.SemiBold;
                nom.Children.Add(codeTexte);
                nom.Children.Add(Texte(m.Nom, 11, Pale));
                ligne.Children.Add(nom);
                _lignes.Children.Add(ligne);
            }
            var date = DateTime.ParseExact(doc.RootElement.GetProperty("date").GetString(), "yyyy-MM-dd", Inv);
            _date.Text = $"Taux BCE du {date:dd/MM/yyyy}";
        }
        catch
        {
            _lignes.Children.Clear();
            _date.Text = "Taux indisponibles (connexion ?)";
        }
    }

    protected override void RemplirMenu()
    {
        var bases = SousMenu("Devise de départ");
        foreach (var m in Monnaies.Take(5))
            Coche($"{m.Code} – {m.Nom}", m.Code == Base, _ => { SetOption("base", m.Code); Charger(); App.Instance.Notifier(); }, bases);

        var cibles = SousMenu("Devises affichées");
        var actuelles = Option("cibles", "USD,GBP,CHF,CAD,JPY").Split(',').ToList();
        foreach (var m in Monnaies)
            Coche($"{m.Code} – {m.Nom}", actuelles.Contains(m.Code), oui =>
            {
                if (oui) actuelles.Add(m.Code); else actuelles.Remove(m.Code);
                SetOption("cibles", string.Join(",", Monnaies.Select(x => x.Code).Where(actuelles.Contains)));
                Charger();
                App.Instance.Notifier();
            }, cibles);
        Item("Actualiser", Charger);
    }
}
