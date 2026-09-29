using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Foundation;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace MesWidgets;

public class MusiqueWidget : WidgetWindow
{
    const double Largeur = 270;

    GlobalSystemMediaTransportControlsSessionManager _gestion;
    GlobalSystemMediaTransportControlsSession _session;
    bool _occupe;
    string _morceau;

    readonly Border _pochette, _lecture, _rempli;
    readonly Grid _barre;
    readonly TextBlock _source, _titre, _artiste;

    public MusiqueWidget(WidgetConfig c) : base(c)
    {
        _pochette = new Border
        {
            Width = 72, Height = 72,
            CornerRadius = new CornerRadius(8),
            Background = Theme.Piste,
            Margin = new Thickness(0, 0, 14, 0),
        };

        _source = Texte("", 11, Pale);
        _titre = Texte("", 15);
        _titre.FontWeight = FontWeights.SemiBold;
        _titre.TextTrimming = TextTrimming.CharacterEllipsis;
        _artiste = Texte("", 13, Pale);
        _artiste.TextTrimming = TextTrimming.CharacterEllipsis;

        var infos = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        infos.Children.Add(_source);
        infos.Children.Add(_titre);
        infos.Children.Add(_artiste);

        var haut = new DockPanel();
        DockPanel.SetDock(_pochette, Dock.Left);
        haut.Children.Add(_pochette);
        haut.Children.Add(infos);

        (_barre, _rempli) = Barre(4);
        _barre.Margin = new Thickness(0, 12, 0, 8);

        _lecture = Cliquable(new Border
        {
            Width = 44, Height = 44,
            CornerRadius = new CornerRadius(22),
            Background = Accent,
            Margin = new Thickness(16, 0, 16, 0),
        }, () => Commande(s => s.TryTogglePlayPauseAsync()), survol: false);

        var boutons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        boutons.Children.Add(Bouton(Glyphe("", 16), () => Commande(s => s.TrySkipPreviousAsync()), 36));
        boutons.Children.Add(_lecture);
        boutons.Children.Add(Bouton(Glyphe("", 16), () => Commande(s => s.TrySkipNextAsync()), 36));

        var pile = new StackPanel { Width = Largeur };
        pile.Children.Add(haut);
        pile.Children.Add(_barre);
        pile.Children.Add(boutons);
        Content = Carte(pile);

        Vide();
        Loaded += async (_, _) =>
        {
            try { _gestion = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync(); } catch { }
            Actualiser();
        };
        Minuteur(TimeSpan.FromSeconds(1), Actualiser);
    }

    void Vide()
    {
        _source.Text = "MUSIQUE";
        _titre.Text = "Rien en lecture";
        _artiste.Text = "Lance Spotify ou une vidéo";
        _pochette.Background = Theme.Piste;
        _pochette.Child = Glyphe("", 28, Pale);
        _barre.Visibility = Visibility.Hidden;
        _lecture.Child = Glyphe("", 18, Brushes.White);
        _morceau = null;
    }

    async void Actualiser()
    {
        if (_gestion == null || _occupe) return;
        _occupe = true;
        try
        {
            _session = _gestion.GetCurrentSession();
            if (_session == null) { Vide(); return; }

            var infos = await _session.TryGetMediaPropertiesAsync();
            var etat = _session.GetPlaybackInfo();
            bool joue = etat.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            _source.Text = NomApp(_session.SourceAppUserModelId).ToUpperInvariant();
            _titre.Text = string.IsNullOrWhiteSpace(infos.Title) ? "Titre inconnu" : infos.Title;
            _artiste.Text = infos.Artist ?? "";
            _lecture.Child = Glyphe(joue ? "" : "", 18, Brushes.White);

            var t = _session.GetTimelineProperties();
            var duree = t.EndTime - t.StartTime;
            if (duree > TimeSpan.Zero)
            {
                var position = t.Position + (joue ? DateTimeOffset.Now - t.LastUpdatedTime : TimeSpan.Zero);
                _rempli.Width = Largeur * Math.Clamp(position / duree, 0, 1);
                _barre.Visibility = Visibility.Visible;
            }
            else _barre.Visibility = Visibility.Hidden;

            var cle = infos.Title + "|" + infos.Artist;
            if (cle != _morceau)
            {
                _morceau = cle;
                await ChargerPochette(infos.Thumbnail);
            }
        }
        catch { }
        finally { _occupe = false; }
    }

    async Task ChargerPochette(IRandomAccessStreamReference reference)
    {
        _pochette.Background = Theme.Piste;
        _pochette.Child = Glyphe("", 28, Pale);
        if (reference == null) return;
        try
        {
            using var flux = await reference.OpenReadAsync();
            var memoire = new MemoryStream();
            await flux.AsStreamForRead().CopyToAsync(memoire);
            memoire.Position = 0;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = memoire;
            image.DecodePixelWidth = 144;
            image.EndInit();
            image.Freeze();

            _pochette.Child = null;
            _pochette.Background = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
        }
        catch { }
    }

    async void Commande(Func<GlobalSystemMediaTransportControlsSession, IAsyncOperation<bool>> action)
    {
        if (_session == null) return;
        try { await action(_session); } catch { }
        await Task.Delay(300);
        Actualiser();
    }

    static string NomApp(string id)
    {
        if (string.IsNullOrEmpty(id)) return "Musique";
        if (id.Contains('!')) id = id[(id.LastIndexOf('!') + 1)..];
        if (id.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) id = id[..^4];
        return id.ToLowerInvariant() switch
        {
            "msedge" => "Edge",
            "chrome" => "Chrome",
            "firefox" => "Firefox",
            "spotify" => "Spotify",
            "vlc" => "VLC",
            "app" => "Musique",
            _ => Majuscule(id),
        };
    }
}
