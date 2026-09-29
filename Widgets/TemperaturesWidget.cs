using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesWidgets;

public class TemperaturesWidget : WidgetWindow
{
    readonly StackPanel _lignes = new();
    IntPtr _requete, _compteur;

    public TemperaturesWidget(WidgetConfig c) : base(c)
    {
        var pile = new StackPanel { Width = 220 };
        var titre = Titre("Températures");
        titre.Margin = new Thickness(0, 0, 0, 6);
        pile.Children.Add(titre);
        pile.Children.Add(_lignes);
        var note = Texte("Capteurs de la carte mère (ACPI)", 11, Pale);
        note.Margin = new Thickness(0, 6, 0, 0);
        pile.Children.Add(note);
        Content = Carte(pile);

        if (PdhOpenQuery(null, IntPtr.Zero, out _requete) == 0)
            PdhAddEnglishCounter(_requete, @"\Thermal Zone Information(*)\Temperature", IntPtr.Zero, out _compteur);

        MettreAJour();
        Minuteur(TimeSpan.FromSeconds(3), MettreAJour);
        Closed += (_, _) => { if (_requete != IntPtr.Zero) PdhCloseQuery(_requete); };
    }

    void MettreAJour()
    {
        var mesures = Lire();
        _lignes.Children.Clear();
        if (mesures.Count == 0)
        {
            var t = Texte("Aucun capteur de température accessible sur ce PC.", 13, Pale);
            t.TextWrapping = TextWrapping.Wrap;
            _lignes.Children.Add(t);
            return;
        }

        int n = 1;
        foreach (var (_, kelvin) in mesures)
        {
            double celsius = kelvin - 273.15;
            var ligne = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
            var valeur = Texte($"{celsius:0} °C", 26, celsius >= 85 ? Alerte : celsius >= 70 ? Theme.Orange : Blanc, "Segoe UI Light");
            DockPanel.SetDock(valeur, Dock.Right);
            ligne.Children.Add(valeur);
            var nom = Texte(mesures.Count == 1 ? "Capteur principal" : $"Capteur {n++}", 13, Pale);
            nom.VerticalAlignment = VerticalAlignment.Center;
            ligne.Children.Add(nom);
            _lignes.Children.Add(ligne);
        }
    }

    List<(string Nom, double Kelvin)> Lire()
    {
        var resultat = new List<(string, double)>();
        if (_compteur == IntPtr.Zero || PdhCollectQueryData(_requete) != 0) return resultat;

        uint taille = 0, nombre = 0;
        PdhGetFormattedCounterArray(_compteur, PDH_FMT_DOUBLE, ref taille, ref nombre, IntPtr.Zero);
        if (taille == 0) return resultat;
        var tampon = Marshal.AllocHGlobal((int)taille);
        try
        {
            if (PdhGetFormattedCounterArray(_compteur, PDH_FMT_DOUBLE, ref taille, ref nombre, tampon) != 0) return resultat;
            for (int i = 0; i < nombre; i++)
            {
                var element = tampon + i * 24;
                var nom = Marshal.PtrToStringUni(Marshal.ReadIntPtr(element));
                double valeur = BitConverter.Int64BitsToDouble(Marshal.ReadInt64(element + 16));
                if (valeur > 200 && valeur < 400) resultat.Add((nom, valeur));
            }
        }
        finally { Marshal.FreeHGlobal(tampon); }
        return resultat;
    }

    const uint PDH_FMT_DOUBLE = 0x00000200;

    [DllImport("pdh.dll", EntryPoint = "PdhOpenQueryW", CharSet = CharSet.Unicode)]
    static extern int PdhOpenQuery(string source, IntPtr donnees, out IntPtr requete);

    [DllImport("pdh.dll", EntryPoint = "PdhAddEnglishCounterW", CharSet = CharSet.Unicode)]
    static extern int PdhAddEnglishCounter(IntPtr requete, string chemin, IntPtr donnees, out IntPtr compteur);

    [DllImport("pdh.dll")]
    static extern int PdhCollectQueryData(IntPtr requete);

    [DllImport("pdh.dll", EntryPoint = "PdhGetFormattedCounterArrayW", CharSet = CharSet.Unicode)]
    static extern int PdhGetFormattedCounterArray(IntPtr compteur, uint format, ref uint taille, ref uint nombre, IntPtr tampon);

    [DllImport("pdh.dll")]
    static extern int PdhCloseQuery(IntPtr requete);
}
