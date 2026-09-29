using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MesWidgets;

class ConnexionForge : Dialogue
{
    public record Resultat(string Serveur, string Jeton);

    readonly TextBox _serveur;
    readonly PasswordBox _jeton;

    ConnexionForge(string service, Func<string, string> pageJeton, string droits, string serveur) : base($"Connecter {service}", 480)
    {
        Grand($"Connecter ton compte {service}");
        Aide("Le widget a besoin d'un jeton d'accès personnel : une sorte de mot de passe limité, que tu peux révoquer à tout moment.", 0);

        if (serveur != null)
        {
            Etiquette("Adresse du serveur");
            _serveur = Ajouter(new TextBox { Text = serveur });
        }

        Etiquette("1. Crée un jeton");
        var ouvrir = Ajouter(new Button { Content = "Ouvrir la page de création du jeton  ↗", HorizontalAlignment = HorizontalAlignment.Left });
        ouvrir.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(pageJeton(Serveur)) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, service); }
        };
        Aide(droits, 8);

        Etiquette("2. Colle-le ici");
        _jeton = Ajouter(new PasswordBox());

        Encart("", "Le jeton reste sur ce PC, chiffré par Windows (Gestionnaire d'identification). Il n'est jamais écrit dans les réglages ni dans les sauvegardes.");

        Boutons(Secondaire("Annuler"), Principal("Connecter", () =>
        {
            if (_jeton.Password.Trim().Length == 0) { MessageBox.Show(this, "Colle d'abord ton jeton.", service); return; }
            Valider();
        }));
        Loaded += (_, _) => (_serveur != null && string.IsNullOrWhiteSpace(_serveur.Text) ? (Control)_serveur : _jeton).Focus();
    }

    string Serveur
    {
        get
        {
            var s = (_serveur?.Text ?? "").Trim().TrimEnd('/');
            if (s.Length > 0 && !s.Contains("://")) s = "https://" + s;
            return s;
        }
    }

    public static Resultat Demander(string service, Func<string, string> pageJeton, string droits, string serveur = null)
    {
        var f = new ConnexionForge(service, pageJeton, droits, serveur);
        return f.ShowDialog() == true ? new Resultat(f.Serveur, f._jeton.Password.Trim()) : null;
    }
}
