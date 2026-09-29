using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MesWidgets;

public class ReseauWidget : WidgetWindow
{
    const double Largeur = 230, Hauteur = 46;
    const int Points = 60;

    readonly List<double> _recus = new(), _envoyes = new();
    readonly TextBlock _nom, _recu, _envoye;
    readonly Polyline _ligneRecue, _ligneEnvoyee;
    long _ancienRecu, _ancienEnvoye;
    DateTime _ancienneMesure;

    public ReseauWidget(WidgetConfig c) : base(c)
    {
        var entete = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        entete.Children.Add(Titre("Réseau"));
        _nom = Texte("", 12, Pale);
        _nom.HorizontalAlignment = HorizontalAlignment.Right;
        _nom.VerticalAlignment = VerticalAlignment.Center;
        entete.Children.Add(_nom);

        _recu = Texte("", 19);
        _envoye = Texte("", 19);
        var debits = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        debits.ColumnDefinitions.Add(new ColumnDefinition());
        debits.ColumnDefinitions.Add(new ColumnDefinition());
        var bas = Debit("", Accent, "Réception", _recu);
        var haut = Debit("", Theme.Orange, "Envoi", _envoye);
        Grid.SetColumn(haut, 1);
        debits.Children.Add(bas);
        debits.Children.Add(haut);

        _ligneRecue = new Polyline { Stroke = Accent, StrokeThickness = 1.6, StrokeLineJoin = PenLineJoin.Round };
        _ligneEnvoyee = new Polyline { Stroke = Theme.Orange, StrokeThickness = 1.6, StrokeLineJoin = PenLineJoin.Round };
        var graphique = new Canvas { Width = Largeur, Height = Hauteur, ClipToBounds = true };
        graphique.Children.Add(_ligneRecue);
        graphique.Children.Add(_ligneEnvoyee);

        var pile = new StackPanel { Width = Largeur };
        pile.Children.Add(entete);
        pile.Children.Add(debits);
        pile.Children.Add(graphique);
        Content = Carte(pile);

        Mesurer();
        Minuteur(TimeSpan.FromSeconds(1), Mesurer);
    }

    static StackPanel Debit(string glyphe, Brush couleur, string libelle, TextBlock valeur)
    {
        var ligne = new StackPanel { Orientation = Orientation.Horizontal };
        var icone = Glyphe(glyphe, 14, couleur);
        icone.Margin = new Thickness(0, 0, 6, 0);
        ligne.Children.Add(icone);
        ligne.Children.Add(Texte(libelle, 12, Pale));
        var pile = new StackPanel();
        pile.Children.Add(ligne);
        pile.Children.Add(valeur);
        return pile;
    }

    void Mesurer()
    {
        var cartes = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                        && n.NetworkInterfaceType is not NetworkInterfaceType.Loopback and not NetworkInterfaceType.Tunnel)
            .ToList();

        long recu = 0, envoye = 0;
        foreach (var n in cartes)
        {
            try
            {
                var s = n.GetIPStatistics();
                recu += s.BytesReceived;
                envoye += s.BytesSent;
            }
            catch { }
        }

        var principale = cartes.FirstOrDefault(n => n.GetIPProperties().GatewayAddresses.Count > 0);
        _nom.Text = principale?.Name ?? "Hors ligne";

        var maintenant = DateTime.Now;
        if (_ancienneMesure != default)
        {
            double secondes = (maintenant - _ancienneMesure).TotalSeconds;
            Ajouter(_recus, Math.Max(0, (recu - _ancienRecu) / secondes));
            Ajouter(_envoyes, Math.Max(0, (envoye - _ancienEnvoye) / secondes));
            _recu.Text = Format(_recus[^1]);
            _envoye.Text = Format(_envoyes[^1]);
            Dessiner();
        }
        _ancienRecu = recu;
        _ancienEnvoye = envoye;
        _ancienneMesure = maintenant;
    }

    static void Ajouter(List<double> liste, double valeur)
    {
        liste.Add(valeur);
        if (liste.Count > Points) liste.RemoveAt(0);
    }

    void Dessiner()
    {
        double max = Math.Max(50 * 1024, Math.Max(_recus.Max(), _envoyes.Max()));
        _ligneRecue.Points = Courbe(_recus, max);
        _ligneEnvoyee.Points = Courbe(_envoyes, max);
    }

    static PointCollection Courbe(List<double> valeurs, double max)
    {
        var points = new PointCollection();
        double pas = Largeur / (Points - 1), debut = Largeur - (valeurs.Count - 1) * pas;
        for (int i = 0; i < valeurs.Count; i++)
            points.Add(new Point(debut + i * pas, Hauteur - 1 - valeurs[i] / max * (Hauteur - 3)));
        return points;
    }

    static string Format(double octetsParSeconde) =>
        octetsParSeconde < 1024 * 1024
            ? string.Format(Fr, "{0:0} Ko/s", octetsParSeconde / 1024)
            : string.Format(Fr, "{0:0.0} Mo/s", octetsParSeconde / 1024 / 1024);
}
