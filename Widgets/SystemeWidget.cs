using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace MesWidgets;

public class SystemeWidget : WidgetWindow
{
    const double Largeur = 220;
    const double Go = 1024d * 1024 * 1024;

    class Jauge { public FrameworkElement Racine; public TextBlock Valeur; public Border Rempli; }

    readonly Jauge _cpu, _memoire, _disque, _batterie;
    readonly TextBlock _allume;
    long _ancienRepos, _ancienTotal;

    public SystemeWidget(WidgetConfig c) : base(c)
    {
        var pile = new StackPanel { Width = Largeur };
        var titre = Texte("Mon PC", 16);
        titre.FontWeight = FontWeights.SemiBold;
        titre.Margin = new Thickness(0, 0, 0, 10);
        pile.Children.Add(titre);

        _cpu = CreerJauge("Processeur", pile);
        _memoire = CreerJauge("Mémoire", pile);
        _disque = CreerJauge("Disque " + Path.GetPathRoot(Environment.SystemDirectory).TrimEnd('\\'), pile);
        _batterie = CreerJauge("Batterie", pile);

        _allume = Texte("", 12, Pale);
        pile.Children.Add(_allume);
        Content = Carte(pile);

        MettreAJour();
        Minuteur(TimeSpan.FromSeconds(2), MettreAJour);
    }

    Jauge CreerJauge(string nom, Panel parent)
    {
        var valeur = Texte("", 13);
        valeur.HorizontalAlignment = HorizontalAlignment.Right;
        var entete = new Grid();
        entete.Children.Add(Texte(nom, 13, Pale));
        entete.Children.Add(valeur);

        var (barre, rempli) = Barre();
        barre.Margin = new Thickness(0, 5, 0, 0);

        var bloc = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        bloc.Children.Add(entete);
        bloc.Children.Add(barre);
        parent.Children.Add(bloc);
        return new Jauge { Racine = bloc, Valeur = valeur, Rempli = rempli };
    }

    void Regler(Jauge j, double ratio, string texte, bool alerte)
    {
        j.Valeur.Text = texte;
        j.Rempli.Width = Largeur * Math.Clamp(ratio, 0, 1);
        j.Rempli.Background = alerte ? Alerte : Accent;
    }

    void MettreAJour()
    {
        if (GetSystemTimes(out long repos, out long noyau, out long utilisateur))
        {
            long total = noyau + utilisateur;
            long dTotal = total - _ancienTotal, dRepos = repos - _ancienRepos;
            if (_ancienTotal != 0 && dTotal > 0)
            {
                double cpu = 1 - (double)dRepos / dTotal;
                Regler(_cpu, cpu, $"{cpu * 100:0} %", cpu > 0.85);
            }
            _ancienTotal = total;
            _ancienRepos = repos;
        }

        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref mem))
        {
            double utilise = (mem.ullTotalPhys - mem.ullAvailPhys) / Go, total = mem.ullTotalPhys / Go;
            Regler(_memoire, utilise / total, string.Format(Fr, "{0:0.0} / {1:0} Go", utilise, total), utilise / total > 0.85);
        }

        try
        {
            var d = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
            double total = d.TotalSize / Go, utilise = (d.TotalSize - d.AvailableFreeSpace) / Go;
            Regler(_disque, utilise / total, string.Format(Fr, "{0:0} / {1:0} Go", utilise, total), utilise / total > 0.9);
        }
        catch { }

        var courant = Forms.SystemInformation.PowerStatus;
        if (courant.BatteryChargeStatus.HasFlag(Forms.BatteryChargeStatus.NoSystemBattery))
        {
            _batterie.Racine.Visibility = Visibility.Collapsed;
        }
        else
        {
            double niveau = courant.BatteryLifePercent;
            bool enCharge = courant.PowerLineStatus == Forms.PowerLineStatus.Online;
            Regler(_batterie, niveau, $"{niveau * 100:0} %" + (enCharge ? " ⚡" : ""), !enCharge && niveau < 0.2);
        }

        var duree = TimeSpan.FromMilliseconds(Environment.TickCount64);
        _allume.Text = "Allumé depuis " + (duree.TotalDays >= 1
            ? $"{(int)duree.TotalDays} j {duree.Hours} h"
            : $"{duree.Hours} h {duree.Minutes:00} min");
    }

    [DllImport("kernel32.dll")]
    static extern bool GetSystemTimes(out long repos, out long noyau, out long utilisateur);

    [StructLayout(LayoutKind.Sequential)]
    struct MEMORYSTATUSEX
    {
        public uint dwLength, dwMemoryLoad;
        public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile,
                     ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")]
    static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX m);
}
