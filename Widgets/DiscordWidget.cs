using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MesWidgets;

public class DiscordWidget : WidgetWindow
{
    static readonly Brush EnLigne = Theme.B(0xFF, 0x23, 0xA5, 0x5A);
    static readonly Brush Absent = Theme.B(0xFF, 0xF0, 0xB2, 0x32);
    static readonly Brush Occupe = Theme.B(0xFF, 0xF2, 0x3F, 0x43);
    static readonly Brush Blurple = Theme.B(0xFF, 0x58, 0x65, 0xF2);

    readonly StackPanel _contenu = new();
    readonly TextBlock _etat;
    string _invitation;

    string Serveur => Option("serveur", "");
    public override string Resume => Option("nom", Serveur.Length > 0 ? "Serveur " + Serveur : "Non configuré");

    public DiscordWidget(WidgetConfig c) : base(c)
    {
        _etat = Texte("", 11, Pale);
        _etat.Margin = new Thickness(0, 8, 0, 0);
        _etat.TextWrapping = TextWrapping.Wrap;
        var pile = new StackPanel { Width = 270 };
        pile.Children.Add(_contenu);
        pile.Children.Add(_etat);
        Content = Carte(pile);

        Loaded += (_, _) => Actualiser();
        Minuteur(TimeSpan.FromMinutes(2), Actualiser);
    }

    async void Actualiser()
    {
        _contenu.Children.Clear();
        if (Serveur.Length == 0)
        {
            _contenu.Children.Add(Titre("Discord"));
            var aide = Texte("Affiche qui est en ligne sur un serveur Discord et à quoi ils jouent.", 13, Pale);
            aide.TextWrapping = TextWrapping.Wrap;
            aide.Margin = new Thickness(0, 6, 0, 12);
            _contenu.Children.Add(aide);
            _contenu.Children.Add(Cliquable(new Border
            {
                Background = Blurple,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 7, 14, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = Texte("Choisir un serveur", 13, Brushes.White),
            }, Configurer, survol: false));
            _etat.Text = "";
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(await Web.Http.GetStringAsync($"https://discord.com/api/guilds/{Serveur}/widget.json"));
            var r = doc.RootElement;
            var nom = r.GetProperty("name").GetString();
            Config.Options["nom"] = nom;
            _invitation = r.TryGetProperty("instant_invite", out var inv) && inv.ValueKind == JsonValueKind.String ? inv.GetString() : null;
            int enLigne = r.GetProperty("presence_count").GetInt32();

            var titre = Titre(nom);
            titre.TextTrimming = TextTrimming.CharacterEllipsis;
            var compteur = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 10) };
            compteur.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = EnLigne, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
            compteur.Children.Add(Texte($"{enLigne} en ligne", 12, Pale));
            _contenu.Children.Add(titre);
            _contenu.Children.Add(compteur);

            var membres = r.GetProperty("members").EnumerateArray()
                .OrderByDescending(m => m.TryGetProperty("game", out _))
                .Take(8).ToList();
            foreach (var m in membres)
            {
                var avatar = new Grid { Width = 28, Height = 28, Margin = new Thickness(0, 0, 10, 0) };
                var rond = new Ellipse { Fill = Theme.Piste };
                if (m.TryGetProperty("avatar_url", out var url) && url.ValueKind == JsonValueKind.String)
                {
                    try { rond.Fill = new ImageBrush(new BitmapImage(new Uri(url.GetString()))); } catch { }
                }
                avatar.Children.Add(rond);
                var statut = m.GetProperty("status").GetString();
                avatar.Children.Add(new Ellipse
                {
                    Width = 10, Height = 10,
                    Fill = statut switch { "idle" => Absent, "dnd" => Occupe, _ => EnLigne },
                    Stroke = FondCarte, StrokeThickness = 2,
                    HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom,
                });

                var textes = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var pseudo = Texte(m.GetProperty("username").GetString(), 13);
                pseudo.TextTrimming = TextTrimming.CharacterEllipsis;
                textes.Children.Add(pseudo);
                if (m.TryGetProperty("game", out var jeu) && jeu.TryGetProperty("name", out var nomJeu))
                {
                    var t = Texte("Joue à " + nomJeu.GetString(), 11, Pale);
                    t.TextTrimming = TextTrimming.CharacterEllipsis;
                    textes.Children.Add(t);
                }

                var ligne = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
                DockPanel.SetDock(avatar, Dock.Left);
                ligne.Children.Add(avatar);
                ligne.Children.Add(textes);
                _contenu.Children.Add(ligne);
            }
            if (membres.Count == 0) _contenu.Children.Add(Texte("Personne en ligne.", 13, Pale));
            _etat.Text = $"Mis à jour à {DateTime.Now:HH:mm}";
            App.Instance.Notifier();
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
        {
            _contenu.Children.Add(Titre("Discord"));
            var t = Texte("Le widget de ce serveur est désactivé. Un administrateur doit l'activer : Paramètres du serveur › Widget › « Activer le widget du serveur ».", 13, Pale);
            t.TextWrapping = TextWrapping.Wrap;
            _contenu.Children.Add(t);
            _etat.Text = "";
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            _contenu.Children.Add(Titre("Discord"));
            _contenu.Children.Add(Texte("Serveur introuvable : vérifie son identifiant.", 13, Pale));
            _etat.Text = "";
        }
        catch
        {
            _contenu.Children.Add(Titre(Option("nom", "Discord")));
            _etat.Text = "Discord injoignable (connexion ?)";
        }
    }

    void Configurer()
    {
        var id = Saisie.Demander("Serveur Discord",
            "Identifiant du serveur (un nombre).\n\n" +
            "Pour l'obtenir : Paramètres utilisateur › Avancés › active le « Mode développeur », puis clic droit sur l'icône du serveur › « Copier l'identifiant du serveur ».\n\n" +
            "Le widget du serveur doit être activé (Paramètres du serveur › Widget).",
            Serveur, 480);
        if (id == null) return;
        id = new string(id.Where(char.IsDigit).ToArray());
        if (id.Length < 15) { MessageBox.Show("Cet identifiant ne ressemble pas à celui d'un serveur Discord (un long nombre).", "Discord"); return; }
        Config.Options.Remove("nom");
        SetOption("serveur", id);
        App.Instance.Notifier();
        Actualiser();
    }

    protected override void RemplirMenu()
    {
        Item(Serveur.Length == 0 ? "Choisir un serveur…" : "Changer de serveur…", Configurer);
        if (Serveur.Length == 0) return;
        Item("Actualiser", Actualiser);
        if (_invitation != null) Item("Rejoindre le serveur", () => Ouvrir(_invitation));
    }
}
