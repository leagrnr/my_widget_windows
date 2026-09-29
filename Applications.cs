using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MesWidgets;

static class Applications
{
    public record Appli(string Nom, string Cible, ImageSource Icone);

    public static Task<List<Appli>> Lister()
    {
        var resultat = new TaskCompletionSource<List<Appli>>();
        var thread = new Thread(() =>
        {
            try
            {
                var liste = new List<Appli>();
                dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                dynamic elements = shell.NameSpace("shell:AppsFolder").Items();
                int nombre = elements.Count;
                for (int i = 0; i < nombre; i++)
                {
                    dynamic e = elements.Item(i);
                    string nom = e.Name, id = e.Path;
                    if (string.IsNullOrWhiteSpace(nom) || string.IsNullOrWhiteSpace(id)) continue;
                    if (nom.Contains("Uninstall", StringComparison.OrdinalIgnoreCase) ||
                        nom.Contains("Désinstaller", StringComparison.OrdinalIgnoreCase)) continue;

                    var cible = File.Exists(id) ? id : @"shell:AppsFolder\" + id;
                    liste.Add(new Appli(nom, cible, Icone(cible, 32)));
                }
                resultat.SetResult(liste
                    .GroupBy(a => a.Nom, StringComparer.CurrentCultureIgnoreCase).Select(g => g.First())
                    .OrderBy(a => a.Nom, StringComparer.CurrentCultureIgnoreCase)
                    .ToList());
            }
            catch (Exception ex) { resultat.SetException(ex); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        return resultat.Task;
    }

    public static ImageSource Icone(string cible, int taille)
    {
        IntPtr hbitmap = IntPtr.Zero;
        try
        {
            var iid = typeof(IShellItemImageFactory).GUID;
            SHCreateItemFromParsingName(cible, IntPtr.Zero, ref iid, out var fabrique);
            if (fabrique.GetImage(new TAILLE { cx = taille, cy = taille }, 0x5, out hbitmap) != 0)
                return null;
            return Convertir(hbitmap);
        }
        catch { return null; }
        finally { if (hbitmap != IntPtr.Zero) DeleteObject(hbitmap); }
    }

    static BitmapSource Convertir(IntPtr hbitmap)
    {
        GetObject(hbitmap, Marshal.SizeOf<BITMAP>(), out BITMAP bm);
        if (bm.bmBits == IntPtr.Zero || bm.bmBitsPixel != 32) return null;
        int pas = bm.bmWidthBytes;
        var pixels = new byte[pas * bm.bmHeight];
        Marshal.Copy(bm.bmBits, pixels, 0, pixels.Length);

        var ordonnes = new byte[pixels.Length];
        for (int y = 0; y < bm.bmHeight; y++)
            Buffer.BlockCopy(pixels, y * pas, ordonnes, (bm.bmHeight - 1 - y) * pas, pas);

        var image = BitmapSource.Create(bm.bmWidth, bm.bmHeight, 96, 96, PixelFormats.Pbgra32, null, ordonnes, pas);
        image.Freeze();
        return image;
    }

    [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItemImageFactory
    {
        [PreserveSig] int GetImage(TAILLE taille, int options, out IntPtr hbitmap);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TAILLE { public int cx, cy; }

    [StructLayout(LayoutKind.Sequential)]
    struct BITMAP
    {
        public int bmType, bmWidth, bmHeight, bmWidthBytes;
        public ushort bmPlanes, bmBitsPixel;
        public IntPtr bmBits;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    static extern void SHCreateItemFromParsingName(string chemin, IntPtr contexte, ref Guid iid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory element);

    [DllImport("gdi32.dll")] static extern int GetObject(IntPtr objet, int taille, out BITMAP bitmap);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr objet);
}
