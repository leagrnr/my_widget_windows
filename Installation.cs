using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.Win32;

namespace MesWidgets;

static class Installation
{
    public static readonly string Dossier =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "MesWidgets");
    public static readonly string Exe = Path.Combine(Dossier, "MesWidgets.exe");

    static string RaccourciMenu => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Mes Widgets.lnk");
    const string CleDesinstallation = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\MesWidgets";

    public static string Version => typeof(App).Assembly.GetName().Version?.ToString(3) ?? "?";

    public static bool Traiter(string[] args)
    {
        if (args.Contains("--desinstaller")) { Desinstaller(); return true; }

        var ici = Path.GetDirectoryName(Environment.ProcessPath) ?? "";
        bool dejaInstalle = string.Equals(Path.GetFullPath(ici).TrimEnd('\\'), Dossier, StringComparison.OrdinalIgnoreCase);
        bool enDeveloppement = ici.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase);
        if (dejaInstalle || enDeveloppement || args.Contains("--portable")) return false;

        Installer(silencieux: args.Contains("--silencieux"));
        return true;
    }

    static void Installer(bool silencieux)
    {
        if (!silencieux && MessageBox.Show(
                $"Installer Mes Widgets {Version} sur ce PC ?\n\n" +
                "Le programme sera copié dans ton dossier utilisateur (pas besoin d'être administrateur) " +
                "et se lancera à chaque démarrage de Windows.",
                "Installation de Mes Widgets", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            FermerLesAutres();
            Directory.CreateDirectory(Dossier);
            for (int essai = 0; ; essai++)
            {
                try { File.Copy(Environment.ProcessPath, Exe, true); break; }
                catch (IOException) when (essai < 10) { Thread.Sleep(500); }
            }

            CreerRaccourci(RaccourciMenu);
            Demarrage.Activer(true, Exe);

            using (var k = Registry.CurrentUser.CreateSubKey(CleDesinstallation))
            {
                k.SetValue("DisplayName", "Mes Widgets");
                k.SetValue("DisplayVersion", Version);
                k.SetValue("Publisher", "Mes Widgets");
                k.SetValue("DisplayIcon", Exe);
                k.SetValue("InstallLocation", Dossier);
                k.SetValue("UninstallString", $"\"{Exe}\" --desinstaller");
                k.SetValue("EstimatedSize", (int)(new FileInfo(Exe).Length / 1024), RegistryValueKind.DWord);
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }

            Process.Start(new ProcessStartInfo(Exe) { UseShellExecute = true, WorkingDirectory = Dossier });
        }
        catch (Exception ex)
        {
            MessageBox.Show("L'installation a échoué :\n\n" + ex.Message, "Mes Widgets", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    static void Desinstaller()
    {
        if (MessageBox.Show("Désinstaller Mes Widgets ?", "Mes Widgets",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        bool reglages = MessageBox.Show(
            "Supprimer aussi tes widgets et leurs réglages (post-its, tâches, notes…) ?\n\n" +
            "Réponds Non pour les retrouver si tu réinstalles plus tard.",
            "Mes Widgets", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

        FermerLesAutres();
        Demarrage.Activer(false, Exe);
        try { File.Delete(RaccourciMenu); } catch { }
        Registry.CurrentUser.DeleteSubKeyTree(CleDesinstallation, false);
        if (reglages)
        {
            try { Directory.Delete(ConfigStore.Dossier, true); } catch { }
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\MesWidgets", false);
        }

        Process.Start(new ProcessStartInfo("cmd.exe", $"/c timeout /t 2 /nobreak >nul & rmdir /s /q \"{Dossier}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        });
        MessageBox.Show("Mes Widgets a été désinstallé.", "Mes Widgets", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    static void FermerLesAutres()
    {
        foreach (var p in Process.GetProcessesByName("MesWidgets").Where(p => p.Id != Environment.ProcessId))
        {
            try { p.Kill(); p.WaitForExit(5000); } catch { }
        }
    }

    static void CreerRaccourci(string chemin)
    {
        var type = Type.GetTypeFromProgID("WScript.Shell");
        if (type == null) return;
        dynamic shell = Activator.CreateInstance(type);
        dynamic lnk = shell.CreateShortcut(chemin);
        lnk.TargetPath = Exe;
        lnk.WorkingDirectory = Dossier;
        lnk.Description = "Des widgets sur le bureau Windows";
        lnk.Save();
    }
}
