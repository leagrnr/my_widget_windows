using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace MesWidgets;

public class CadrePhotoWidget : WidgetWindow
{
    const double L = 300, H = 200;
    static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tif", ".tiff" };

    readonly Border _avant = new() { CornerRadius = new CornerRadius(12) };
    readonly Border _arriere = new() { CornerRadius = new CornerRadius(12) };
    readonly TextBlock _message;
    readonly DispatcherTimer _diaporama = new();
    List<string> _images = new();
    int _index;
    string _actuelle;

    string Dossier => Option("dossier", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
    int Secondes => int.Parse(Option("intervalle", "30"));
    public override string Resume => Path.GetFileName(Dossier.TrimEnd('\\'));

    public CadrePhotoWidget(WidgetConfig c) : base(c)
    {
        _message = Texte("", 13, Pale);
        _message.TextWrapping = TextWrapping.Wrap;
        _message.TextAlignment = TextAlignment.Center;
        _message.VerticalAlignment = VerticalAlignment.Center;
        _message.Margin = new Thickness(24);

        var cadre = new Grid { Width = L, Height = H };
        cadre.Children.Add(new Border { CornerRadius = new CornerRadius(12), Background = Theme.Fond });
        cadre.Children.Add(_arriere);
        cadre.Children.Add(_avant);
        cadre.Children.Add(_message);
        Content = new Border { Child = cadre, Margin = new Thickness(10), Effect = Ombre() };

        PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2 && _actuelle != null) { e.Handled = true; Ouvrir(_actuelle); }
        };

        _diaporama.Tick += (_, _) => Suivante();
        Closed += (_, _) => _diaporama.Stop();
        Loaded += (_, _) => Charger();
    }

    async void Charger()
    {
        _diaporama.Stop();
        _message.Text = "Chargement des images…";
        _message.Visibility = Visibility.Visible;
        var dossier = Dossier;
        _images = await Task.Run(() => Lister(dossier));
        _index = 0;
        if (_images.Count == 0)
        {
            _avant.Background = _arriere.Background = null;
            _actuelle = null;
            _message.Text = $"Aucune image dans « {Path.GetFileName(dossier.TrimEnd('\\'))} ».\n✏ Modifier › Choisir le dossier";
            return;
        }
        Suivante();
        _diaporama.Interval = TimeSpan.FromSeconds(Secondes);
        _diaporama.Start();
    }

    static List<string> Lister(string dossier)
    {
        try
        {
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
            var liste = Directory.EnumerateFiles(dossier, "*", options)
                .Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Take(5000)
                .ToArray();
            Random.Shared.Shuffle(liste);
            return liste.ToList();
        }
        catch { return new List<string>(); }
    }

    async void Suivante()
    {
        for (int essai = 0; essai < 5 && _images.Count > 0; essai++)
        {
            var chemin = _images[_index++ % _images.Count];
            try
            {
                var image = await Task.Run(() => Decoder(chemin));
                _arriere.Background = _avant.Background;
                _avant.Background = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
                _avant.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.8)));
                _message.Visibility = Visibility.Collapsed;
                _actuelle = chemin;
                ToolTip = Path.GetFileName(chemin);
                return;
            }
            catch { }
        }
    }

    static BitmapImage Decoder(string chemin)
    {
        using var flux = File.OpenRead(chemin);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = 640;
        image.StreamSource = flux;
        image.EndInit();
        image.Freeze();
        return image;
    }

    protected override void RemplirMenu()
    {
        Item("Photo suivante", Suivante);
        if (_actuelle != null) Item("Ouvrir la photo", () => Ouvrir(_actuelle));
        Item("Choisir le dossier…", () =>
        {
            var d = new OpenFolderDialog { Title = "Dossier de photos", InitialDirectory = Dossier };
            if (d.ShowDialog() != true) return;
            SetOption("dossier", d.FolderName);
            App.Instance.Notifier();
            Charger();
        });
        var intervalle = SousMenu("Changer de photo toutes les");
        foreach (var (nom, s) in new[] { ("10 secondes", 10), ("30 secondes", 30), ("1 minute", 60), ("5 minutes", 300), ("15 minutes", 900) })
            Coche(nom, s == Secondes, _ =>
            {
                SetOption("intervalle", s.ToString());
                _diaporama.Interval = TimeSpan.FromSeconds(s);
            }, intervalle);
    }
}
