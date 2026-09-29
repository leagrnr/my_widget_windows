using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace MesWidgets;

public class RaccourcisWidget : WidgetWindow
{
    public class Raccourci
    {
        public string Nom { get; set; }
        public string Cible { get; set; }
    }

    readonly List<Raccourci> _liste;
    readonly WrapPanel _tuiles = new();

    public override string Resume => $"{_liste.Count} raccourci{(_liste.Count > 1 ? "s" : "")}";

    public RaccourcisWidget(WidgetConfig c) : base(c)
    {
        try { _liste = JsonSerializer.Deserialize<List<Raccourci>>(Option("liste", "")); }
        catch { _liste = null; }
        _liste ??= ParDefaut();

        var pile = new StackPanel { Width = 288 };
        pile.Children.Add(_tuiles);
        Content = Carte(pile);
        Construire();
    }

    static List<Raccourci> ParDefaut()
    {
        var profil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return new()
        {
            new() { Nom = "Documents", Cible = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
            new() { Nom = "Images", Cible = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
            new() { Nom = "Téléchargements", Cible = Path.Combine(profil, "Downloads") },
        };
    }

    void Construire()
    {
        _tuiles.Children.Clear();
        foreach (var r in _liste)
        {
            var nom = Texte(r.Nom, 11);
            nom.TextAlignment = TextAlignment.Center;
            nom.TextTrimming = TextTrimming.CharacterEllipsis;
            nom.Margin = new Thickness(0, 6, 0, 0);

            var pile = new StackPanel { Width = 60 };
            pile.Children.Add(Icone(r.Cible));
            pile.Children.Add(nom);

            var tuile = Cliquable(new Border
            {
                Child = pile,
                Width = 72, Height = 76,
                CornerRadius = new CornerRadius(10),
                Background = Brushes.Transparent,
                Padding = new Thickness(6, 8, 6, 4),
                ToolTip = r.Cible,
            }, () => Ouvrir(r.Cible));

            var menu = new ContextMenu();
            var ouvrir = new MenuItem { Header = "Ouvrir" };
            ouvrir.Click += (_, _) => Ouvrir(r.Cible);
            var renommer = new MenuItem { Header = "Renommer…" };
            renommer.Click += (_, _) =>
            {
                var n = Saisie.Demander("Raccourci", "Nouveau nom :", r.Nom);
                if (n != null) { r.Nom = n; Enregistrer(); }
            };
            var retirer = new MenuItem { Header = "Retirer" };
            retirer.Click += (_, _) => { _liste.Remove(r); Enregistrer(); };
            menu.Items.Add(ouvrir);
            menu.Items.Add(renommer);
            menu.Items.Add(retirer);
            tuile.ContextMenu = menu;

            _tuiles.Children.Add(tuile);
        }

        if (_liste.Count == 0)
            _tuiles.Children.Add(Texte("✏ Modifier › Ajouter…", 13, Pale));
    }

    static FrameworkElement Icone(string cible)
    {
        if (cible.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return Glyphe("", 28, Accent);

        if (cible.StartsWith(@"shell:AppsFolder\", StringComparison.OrdinalIgnoreCase))
        {
            var icone = Applications.Icone(cible, 64);
            return icone != null ? new Image { Source = icone, Width = 32, Height = 32 } : Glyphe("\uE8FC", 28, Pale);
        }
        var info = new SHFILEINFO();
        if (SHGetFileInfo(cible, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), 0x100) != IntPtr.Zero
            && info.hIcon != IntPtr.Zero)
        {
            var image = Imaging.CreateBitmapSourceFromHIcon(info.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DestroyIcon(info.hIcon);
            return new Image { Source = image, Width = 32, Height = 32 };
        }
        return Glyphe("", 28, Pale);
    }

    void Enregistrer()
    {
        SetOption("liste", JsonSerializer.Serialize(_liste));
        Construire();
        App.Instance.Notifier();
    }

    protected override void RemplirMenu()
    {
        Item("Ajouter une application…", () =>
        {
            var ajoutees = ChoixApplications.Demander()
                .Where(a => !_liste.Any(r => string.Equals(r.Cible, a.Cible, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (ajoutees.Count == 0) return;
            foreach (var a in ajoutees) _liste.Add(new Raccourci { Nom = a.Nom, Cible = a.Cible });
            Enregistrer();
        });
        Item("Ajouter un fichier…", () =>
        {
            var d = new OpenFileDialog
            {
                Title = "Choisir un fichier",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs",
                DereferenceLinks = false,
            };
            if (d.ShowDialog() != true) return;
            _liste.Add(new Raccourci { Nom = Path.GetFileNameWithoutExtension(d.FileName), Cible = d.FileName });
            Enregistrer();
        });
        Item("Ajouter un dossier…", () =>
        {
            var d = new OpenFolderDialog { Title = "Choisir un dossier" };
            if (d.ShowDialog() != true) return;
            _liste.Add(new Raccourci { Nom = Path.GetFileName(d.FolderName.TrimEnd('\\')) is { Length: > 0 } n ? n : d.FolderName, Cible = d.FolderName });
            Enregistrer();
        });
        Item("Ajouter un site web…", () =>
        {
            var adresse = Saisie.Demander("Site web", "Adresse du site (ex. youtube.com) :");
            if (adresse == null) return;
            if (!adresse.Contains("://")) adresse = "https://" + adresse;
            if (!Uri.TryCreate(adresse, UriKind.Absolute, out var uri)) { MessageBox.Show("Adresse invalide.", "Site web"); return; }
            var hote = uri.Host.StartsWith("www.") ? uri.Host[4..] : uri.Host;
            var nom = Saisie.Demander("Site web", "Nom affiché :", Majuscule(hote.Split('.')[0])) ?? hote;
            _liste.Add(new Raccourci { Nom = nom, Cible = uri.ToString() });
            Enregistrer();
        });
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SHGetFileInfo(string path, uint attributs, ref SHFILEINFO info, uint taille, uint flags);

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr h);
}
