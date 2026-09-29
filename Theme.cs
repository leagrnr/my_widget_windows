using System.Collections.Generic;
using System.Windows.Media;

namespace MesWidgets;

public static class Theme
{
    public static readonly (string Nom, string Hex)[] Accents =
    {
        ("Bleu", "#4F8CFF"), ("Violet", "#8B5CF6"), ("Rose", "#EC4899"), ("Rouge", "#EF4444"),
        ("Orange", "#F97316"), ("Jaune", "#EAB308"), ("Vert", "#22C55E"), ("Turquoise", "#14B8A6"),
    };

    public static readonly string[] Polices =
        { "Segoe UI", "Segoe UI Variable Display", "Bahnschrift", "Calibri", "Georgia", "Consolas", "Comic Sans MS" };

    public static readonly (string Nom, double Rayon)[] Arrondis =
        { ("Carrés", 0), ("Légers", 8), ("Arrondis", 16), ("Très arrondis", 26) };

    public static readonly (string Nom, string Code)[] Fonds =
        { ("Très transparent", "leger"), ("Normal", "normal"), ("Opaque", "opaque") };

    static AppConfig C => App.Instance?.Config;

    public static bool Clair => C?.Theme == "clair";
    public static string Police => C?.Police ?? "Segoe UI";
    public static double Arrondi => C?.Arrondi ?? 16;

    public static Brush Texte     => Clair ? B(0xFF, 0x1F, 0x23, 0x28) : Brushes.White;
    public static Brush TexteDoux => Clair ? B(0xA0, 0x00, 0x00, 0x00) : B(0xB0, 0xFF, 0xFF, 0xFF);
    public static Brush Piste     => Clair ? B(0x1A, 0x00, 0x00, 0x00) : B(0x33, 0xFF, 0xFF, 0xFF);
    public static Brush Survol    => Clair ? B(0x14, 0x00, 0x00, 0x00) : B(0x1F, 0xFF, 0xFF, 0xFF);
    public static Brush Alerte    { get; } = B(0xFF, 0xFF, 0x6B, 0x6B);
    public static Brush Orange    { get; } = B(0xFF, 0xFF, 0xA9, 0x4D);

    public static Brush Fond
    {
        get
        {
            byte alpha = (C?.Fond, Clair) switch
            {
                ("leger", false) => 0x80, ("leger", true) => 0xA8,
                ("opaque", false) => 0xF0, ("opaque", true) => 0xFA,
                (_, false) => 0xC0, (_, true) => 0xE6,
            };
            return Clair ? B(alpha, 0xFF, 0xFF, 0xFF) : B(alpha, 0x16, 0x18, 0x1D);
        }
    }

    static readonly Dictionary<string, Brush> _accents = new();
    public static Brush Accent
    {
        get
        {
            var hex = C?.Accent ?? "#4F8CFF";
            if (!_accents.TryGetValue(hex, out var b))
            {
                try { b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
                catch { b = new SolidColorBrush(Color.FromRgb(0x4F, 0x8C, 0xFF)); }
                b.Freeze();
                _accents[hex] = b;
            }
            return b;
        }
    }

    public static SolidColorBrush B(byte a, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }

    public static SolidColorBrush Hex(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
