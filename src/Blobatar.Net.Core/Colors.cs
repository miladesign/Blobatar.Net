using System.Globalization;

namespace Blobatar.Net.Core;

public readonly record struct Oklch(double L, double C, double H);

/// <summary>Resolved colors as #rrggbb.</summary>
public sealed record Palette(string Bg, string Head, string Eye);

/// <summary>Per-slot overrides; they bypass the contrast guarantee.</summary>
public sealed record PaletteOverride(string? Bg = null, string? Head = null, string? Eye = null);

/// <summary>OKLCH ramp with gamut clamping and WCAG contrast floors.</summary>
public static partial class Colors
{
    static readonly Oklch DarkSurface = new(0.145, 0, 0);
    const double SurfaceFloor = 1.5;

    // [upper edge, tone]: pastel, pale neutral, mid, deep, bright, ink
    static readonly (double Edge, double L, double C)[] Tones =
    {
        (0.2, 0.86, 0.085), (0.36, 0.9, 0.028), (0.62, 0.73, 0.135),
        (0.8, 0.62, 0.165), (0.93, 0.87, 0.16), (1.0, 0.34, 0.035),
    };

    static (double R, double G, double B) ToLinear(Oklch c)
    {
        var r = c.H * Math.PI / 180;
        var a = c.C * Math.Cos(r);
        var b = c.C * Math.Sin(r);
        var l_ = c.L + 0.3963377774 * a + 0.2158037573 * b;
        var m_ = c.L - 0.1055613458 * a - 0.0638541728 * b;
        var s_ = c.L - 0.0894841775 * a - 1.291485548 * b;
        double L = l_ * l_ * l_, M = m_ * m_ * m_, S = s_ * s_ * s_;
        return (4.0767416621 * L - 3.3077115913 * M + 0.2309699292 * S,
                -1.2684380046 * L + 2.6097574011 * M - 0.3413193965 * S,
                -0.0041960863 * L - 0.7034186147 * M + 1.707614701 * S);
    }

    static bool InGamut((double R, double G, double B) v) =>
        Ok(v.R) && Ok(v.G) && Ok(v.B);
    static bool Ok(double v) => v >= -1e-4 && v <= 1 + 1e-4;

    static (double R, double G, double B) Resolve(Oklch color)
    {
        var rgb = ToLinear(color);
        if (!InGamut(rgb))
        {
            double lo = 0, hi = color.C;
            for (var i = 0; i < 12; i++)
            {
                var mid = (lo + hi) / 2;
                if (InGamut(ToLinear(color with { C = mid }))) lo = mid; else hi = mid;
            }
            rgb = ToLinear(color with { C = lo });
        }
        static double Clamp(double v) => Math.Min(1, Math.Max(0, v));
        return (Clamp(rgb.R), Clamp(rgb.G), Clamp(rgb.B));
    }

    static double Luminance(Oklch c)
    {
        var (r, g, b) = Resolve(c);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    public static double Contrast(Oklch a, Oklch b)
    {
        double x = Luminance(a), y = Luminance(b);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    public static Oklch EnsureContrast(Oklch fg, Oklch bg, double min)
    {
        if (Contrast(fg, bg) >= min) return fg;
        var lean = fg.L >= bg.L ? 1 : -1;
        foreach (var dir in new[] { lean, -lean })
        {
            var probe = fg;
            for (var i = 0; i < 60; i++)
            {
                probe = probe with { L = Math.Min(1, Math.Max(0, probe.L + dir * 0.02)) };
                if (Contrast(probe, bg) >= min) return probe;
                if (probe.L == 0 || probe.L == 1) break;
            }
        }
        var black = fg with { L = 0, C = 0 };
        var white = fg with { L = 1, C = 0 };
        return Contrast(black, bg) >= Contrast(white, bg) ? black : white;
    }

    public static string ToHex(Oklch color)
    {
        var (r, g, b) = Resolve(color);
        static string Ch(double v)
        {
            var s = v <= 0.0031308 ? 12.92 * v : 1.055 * Math.Pow(v, 1 / 2.4) - 0.055;
            return ((int)Geo.JsRound(s * 255)).ToString("x2", CultureInfo.InvariantCulture);
        }
        return "#" + Ch(r) + Ch(g) + Ch(b);
    }

    static Oklch ToneAt(double v, out double c)
    {
        foreach (var t in Tones)
            if (v < t.Edge) { c = t.C; return new Oklch(t.L, t.C, 0); }
        c = Tones[0].C;
        return new Oklch(Tones[0].L, Tones[0].C, 0);
    }

    /// <summary>The three-slot palette for a hue and a 0-1 tone position.</summary>
    public static Palette Create(double hue, bool enforce = true, double tone = 0)
    {
        var t = ToneAt(tone, out _);
        var head = EnsureContrast(new Oklch(t.L, t.C, hue), DarkSurface, SurfaceFloor);
        var bg = new Oklch(0.965, 0.01, hue);
        var eye = head.L >= 0.5 ? new Oklch(0.17, 0.02, hue) : new Oklch(0.97, 0.012, hue);
        if (enforce)
        {
            head = EnsureContrast(head, bg, 1.25);
            eye = EnsureContrast(eye, head, 4.5);
        }
        return new Palette(ToHex(bg), ToHex(head), ToHex(eye));
    }
}
