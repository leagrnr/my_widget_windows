using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesWidgets;

public class VolumeWidget : WidgetWindow
{
    const double Largeur = 220;

    readonly TextBlock _pourcent;
    readonly Border _muet, _rempli, _poignee;
    readonly Grid _piste;
    bool _glisse;

    public VolumeWidget(WidgetConfig c) : base(c)
    {
        _muet = Bouton(null, () => { try { Audio.Muet = !Audio.Muet; } catch { } Lire(); }, 36);
        _muet.ToolTip = "Couper / remettre le son";
        _muet.Margin = new Thickness(-8, 0, 6, 0);
        _pourcent = Texte("", 15, Pale);
        _pourcent.VerticalAlignment = VerticalAlignment.Center;

        var entete = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(_muet, Dock.Left);
        DockPanel.SetDock(_pourcent, Dock.Right);
        entete.Children.Add(_muet);
        entete.Children.Add(_pourcent);
        var titre = Titre("Volume");
        titre.VerticalAlignment = VerticalAlignment.Center;
        entete.Children.Add(titre);

        _rempli = new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = Accent, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
        _poignee = new Border { Width = 16, Height = 16, CornerRadius = new CornerRadius(8), Background = Brushes.White, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Effect = Ombre() };
        _piste = new Grid { Width = Largeur, Height = 22, Background = Brushes.Transparent, Cursor = System.Windows.Input.Cursors.Hand };
        _piste.Children.Add(new Border { Height = 6, CornerRadius = new CornerRadius(3), Background = Theme.Piste, VerticalAlignment = VerticalAlignment.Center });
        _piste.Children.Add(_rempli);
        _piste.Children.Add(_poignee);

        _piste.MouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            _glisse = true;
            _piste.CaptureMouse();
            Regler(e.GetPosition(_piste).X);
        };
        _piste.MouseMove += (_, e) => { if (_glisse) Regler(e.GetPosition(_piste).X); };
        _piste.MouseLeftButtonUp += (_, _) => { _glisse = false; _piste.ReleaseMouseCapture(); };

        MouseWheel += (_, e) =>
        {
            try { Audio.Volume = Math.Clamp(Audio.Volume + (e.Delta > 0 ? 0.02f : -0.02f), 0, 1); } catch { }
            Lire();
        };

        var pile = new StackPanel { Width = Largeur };
        pile.Children.Add(entete);
        pile.Children.Add(_piste);
        Content = Carte(pile);

        Lire();
        Minuteur(TimeSpan.FromMilliseconds(500), () => { if (!_glisse) Lire(); });
    }

    void Regler(double x)
    {
        try { Audio.Volume = (float)Math.Clamp(x / Largeur, 0, 1); } catch { }
        Lire();
    }

    void Lire()
    {
        try { Afficher(Audio.Volume, Audio.Muet); }
        catch
        {
            _pourcent.Text = "";
            _muet.Child = Glyphe("", 18, Pale);
            _rempli.Width = 0;
            _poignee.Visibility = Visibility.Collapsed;
            ToolTip = "Aucune sortie audio";
        }
    }

    void Afficher(float volume, bool muet)
    {
        _poignee.Visibility = Visibility.Visible;
        _rempli.Width = Largeur * volume;
        _rempli.Background = muet ? Theme.TexteDoux : Accent;
        _poignee.Margin = new Thickness(Math.Clamp(Largeur * volume - 8, 0, Largeur - 16), 0, 0, 0);
        _pourcent.Text = muet ? "Muet" : $"{volume * 100:0} %";
        string glyphe = muet ? "" : volume < 0.01 ? "" : volume < 0.34 ? "" : volume < 0.67 ? "" : "";
        _muet.Child = Glyphe(glyphe, 18, Blanc);
    }
}

static class Audio
{
    static IAudioEndpointVolume Appareil(int flux)
    {
        var enumerateur = (IMMDeviceEnumerator)new MMDeviceEnumerator();
        Marshal.ThrowExceptionForHR(enumerateur.GetDefaultAudioEndpoint(flux, 1, out var appareil));
        var iid = typeof(IAudioEndpointVolume).GUID;
        Marshal.ThrowExceptionForHR(appareil.Activate(ref iid, 23, IntPtr.Zero, out object volume));
        return (IAudioEndpointVolume)volume;
    }

    static IAudioEndpointVolume Sortie() => Appareil(0);
    static IAudioEndpointVolume Micro() => Appareil(1);

    public static float Volume
    {
        get { Marshal.ThrowExceptionForHR(Sortie().GetMasterVolumeLevelScalar(out float v)); return v; }
        set { var g = Guid.Empty; Sortie().SetMasterVolumeLevelScalar(value, ref g); }
    }

    public static bool Muet
    {
        get { Marshal.ThrowExceptionForHR(Sortie().GetMute(out bool m)); return m; }
        set { var g = Guid.Empty; Sortie().SetMute(value, ref g); }
    }

    public static bool MicroMuet
    {
        get { Marshal.ThrowExceptionForHR(Micro().GetMute(out bool m)); return m; }
        set { var g = Guid.Empty; Marshal.ThrowExceptionForHR(Micro().SetMute(value, ref g)); }
    }

    public static float VolumeMicro
    {
        get { Marshal.ThrowExceptionForHR(Micro().GetMasterVolumeLevelScalar(out float v)); return v; }
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumerator { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int flux, int etat, out IntPtr appareils);
        [PreserveSig] int GetDefaultAudioEndpoint(int flux, int role, out IMMDevice appareil);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, int contexte, IntPtr parametres, [MarshalAs(UnmanagedType.IUnknown)] out object interfaceObtenue);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr notif);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr notif);
        [PreserveSig] int GetChannelCount(out uint canaux);
        [PreserveSig] int SetMasterVolumeLevel(float niveauDb, ref Guid contexte);
        [PreserveSig] int SetMasterVolumeLevelScalar(float niveau, ref Guid contexte);
        [PreserveSig] int GetMasterVolumeLevel(out float niveauDb);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float niveau);
        [PreserveSig] int SetChannelVolumeLevel(uint canal, float niveauDb, ref Guid contexte);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint canal, float niveau, ref Guid contexte);
        [PreserveSig] int GetChannelVolumeLevel(uint canal, out float niveauDb);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint canal, out float niveau);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool muet, ref Guid contexte);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool muet);
    }
}
