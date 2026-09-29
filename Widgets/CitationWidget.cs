using System;
using System.Windows;
using System.Windows.Controls;

namespace MesWidgets;

public class CitationWidget : WidgetWindow
{
    static readonly (string Texte, string Auteur)[] Citations =
    {
        ("Il faut cultiver notre jardin.", "Voltaire"),
        ("Le cœur a ses raisons que la raison ne connaît point.", "Blaise Pascal"),
        ("Je pense, donc je suis.", "René Descartes"),
        ("Rien ne sert de courir ; il faut partir à point.", "Jean de La Fontaine"),
        ("On a souvent besoin d'un plus petit que soi.", "Jean de La Fontaine"),
        ("Patience et longueur de temps font plus que force ni que rage.", "Jean de La Fontaine"),
        ("Ce n'est pas parce que les choses sont difficiles que nous n'osons pas, c'est parce que nous n'osons pas qu'elles sont difficiles.", "Sénèque"),
        ("Il n'y a pas de vent favorable pour celui qui ne sait pas où il va.", "Sénèque"),
        ("Un voyage de mille lieues commence toujours par un premier pas.", "Lao Tseu"),
        ("L'homme n'est qu'un roseau, le plus faible de la nature ; mais c'est un roseau pensant.", "Blaise Pascal"),
        ("Le travail éloigne de nous trois grands maux : l'ennui, le vice et le besoin.", "Voltaire"),
        ("Le mieux est l'ennemi du bien.", "Voltaire"),
        ("Le secret d'ennuyer est celui de tout dire.", "Voltaire"),
        ("Deviens ce que tu es.", "Friedrich Nietzsche"),
        ("Sans la musique, la vie serait une erreur.", "Friedrich Nietzsche"),
        ("La plus perdue de toutes les journées est celle où l'on n'a pas ri.", "Chamfort"),
        ("Connais-toi toi-même.", "Socrate"),
        ("Il faut rire avant d'être heureux, de peur de mourir sans avoir ri.", "Jean de La Bruyère"),
        ("Ce que l'on conçoit bien s'énonce clairement.", "Nicolas Boileau"),
        ("Hâtez-vous lentement.", "Nicolas Boileau"),
        ("Vingt fois sur le métier remettez votre ouvrage.", "Nicolas Boileau"),
        ("À vaincre sans péril, on triomphe sans gloire.", "Pierre Corneille"),
        ("Le bonheur est une idée neuve en Europe.", "Saint-Just"),
        ("Les grandes pensées viennent du cœur.", "Vauvenargues"),
        ("Le génie n'est qu'une plus grande aptitude à la patience.", "Buffon"),
        ("Le hasard ne favorise que les esprits préparés.", "Louis Pasteur"),
        ("La lecture est à l'esprit ce que l'exercice est au corps.", "Joseph Addison"),
        ("L'Art est long et le Temps est court.", "Charles Baudelaire"),
        ("La patience est amère, mais son fruit est doux.", "Jean-Jacques Rousseau"),
        ("Rien n'est permanent, sauf le changement.", "Héraclite"),
        ("La musique, c'est du bruit qui pense.", "Victor Hugo"),
        ("Rire est le propre de l'homme.", "François Rabelais"),
        ("Qui veut voyager loin ménage sa monture.", "Jean Racine"),
        ("Le temps est un grand maître, dit-on ; le malheur est qu'il tue ses élèves.", "Hector Berlioz"),
        ("Impossible n'est pas français.", "Napoléon Bonaparte"),
    };

    readonly TextBlock _texte, _auteur;
    DateTime _jour;
    int _index;

    public override string Resume => Citations[_index].Auteur;

    public CitationWidget(WidgetConfig c) : base(c)
    {
        var guillemet = Texte("“", 48, Accent, "Georgia");
        guillemet.Margin = new Thickness(-2, -8, 0, -26);

        _texte = Texte("", 16, Blanc, "Georgia");
        _texte.FontStyle = FontStyles.Italic;
        _texte.TextWrapping = TextWrapping.Wrap;
        _texte.LineHeight = 23;

        _auteur = Texte("", 13, Pale);
        _auteur.HorizontalAlignment = HorizontalAlignment.Right;
        _auteur.Margin = new Thickness(0, 10, 0, 0);

        var pile = new StackPanel { Width = 270 };
        pile.Children.Add(guillemet);
        pile.Children.Add(_texte);
        pile.Children.Add(_auteur);
        Content = Carte(pile);

        CitationDuJour();
        Minuteur(TimeSpan.FromMinutes(1), () => { if (DateTime.Today != _jour) CitationDuJour(); });
    }

    void CitationDuJour()
    {
        _jour = DateTime.Today;
        Afficher((int)((_jour - new DateTime(2000, 1, 1)).TotalDays % Citations.Length));
    }

    void Afficher(int index)
    {
        _index = index;
        _texte.Text = Citations[index].Texte;
        _auteur.Text = "— " + Citations[index].Auteur;
    }

    protected override void RemplirMenu()
    {
        Item("Autre citation", () =>
        {
            int i;
            do i = Random.Shared.Next(Citations.Length); while (i == _index);
            Afficher(i);
        });
        Item("Citation du jour", CitationDuJour);
    }
}
