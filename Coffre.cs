using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace MesWidgets;

static class Coffre
{
    const uint GENERIQUE = 1;
    const uint PERSISTANCE_MACHINE = 2;
    const string Prefixe = "MesWidgets/";

    public static void Ecrire(string cle, string secret)
    {
        var octets = Encoding.Unicode.GetBytes(secret);
        var tampon = Marshal.AllocHGlobal(octets.Length);
        try
        {
            Marshal.Copy(octets, 0, tampon, octets.Length);
            var c = new CREDENTIAL
            {
                Type = GENERIQUE,
                TargetName = Prefixe + cle,
                CredentialBlobSize = (uint)octets.Length,
                CredentialBlob = tampon,
                Persist = PERSISTANCE_MACHINE,
                UserName = Environment.UserName,
            };
            if (!CredWrite(ref c, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            Marshal.FreeHGlobal(tampon);
        }
    }

    public static string Lire(string cle)
    {
        if (!CredRead(Prefixe + cle, GENERIQUE, 0, out var pointeur)) return null;
        try
        {
            var c = Marshal.PtrToStructure<CREDENTIAL>(pointeur);
            return c.CredentialBlobSize == 0 ? "" : Marshal.PtrToStringUni(c.CredentialBlob, (int)c.CredentialBlobSize / 2);
        }
        finally
        {
            CredFree(pointeur);
        }
    }

    public static void Effacer(string cle) => CredDelete(Prefixe + cle, GENERIQUE, 0);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredWrite(ref CREDENTIAL credential, uint options);

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredRead(string cible, uint type, uint options, out IntPtr credential);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode)]
    static extern bool CredDelete(string cible, uint type, uint options);

    [DllImport("advapi32.dll")]
    static extern void CredFree(IntPtr tampon);
}
