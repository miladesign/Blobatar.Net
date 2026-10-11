using System.Globalization;

namespace Blobatar.Net.Core;

/// <summary>Where an expression pulls the face colors toward, in OKLCH.</summary>
public sealed record Tint(double H, double L, double Pull, double C);

public static partial class Colors
{
    public static readonly Tint Hot = new(27, 0.58, 0.6, 0.18);
    public static readonly Tint Rose = new(358, 0.72, 0.55, 0.16);
    public static readonly Tint Blush = new(12, 0.84, 0.4, 0.1);
    public static readonly Tint Bile = new(142, 0.66, 0.6, 0.13);

    const double TintFloor = 4.55;

    public static Oklch FromHex(string hex)
    {
        var n = int.Parse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        static double Lin(int v)
        {
            var s = v / 255.0;
            return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
        double r = Lin((n >> 16) & 255), g = Lin((n >> 8) & 255), b = Lin(n & 255);

        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s2 = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

        var a = 1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s2;
        var bb = 0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s2;
        return new Oklch(
            0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s2,
            Numeric.Hypot(a, bb),
            Math.Atan2(bb, a) * 180 / Math.PI);
    }

    /// <summary>Mixes in OKLab (chroma and hue as a vector), so mid-points stay clean.</summary>
    public static Oklch Mix(Oklch a, Oklch b, double t)
    {
        static double Rad(double v) => v * Math.PI / 180;
        var ax = a.C * Math.Cos(Rad(a.H));
        var ay = a.C * Math.Sin(Rad(a.H));
        var bx = b.C * Math.Cos(Rad(b.H));
        var by = b.C * Math.Sin(Rad(b.H));
        var x = ax + (bx - ax) * t;
        var y = ay + (by - ay) * t;
        return new Oklch(a.L + (b.L - a.L) * t, Numeric.Hypot(x, y), Math.Atan2(y, x) * 180 / Math.PI);
    }

    public static string MixHex(string a, string b, double t) => ToHex(Mix(FromHex(a), FromHex(b), t));

    /// <summary>Per-channel sRGB lerp, which is what a CSS fill transition does.</summary>
    public static string FadeHex(string a, string b, double t)
    {
        var o = new System.Text.StringBuilder("#");
        for (var i = 1; i < 7; i += 2)
        {
            var from = int.Parse(a.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var to = int.Parse(b.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var v = (int)Geo.JsRound(from + (to - from) * t);
            o.Append(v.ToString("x2", CultureInfo.InvariantCulture));
        }
        return o.ToString();
    }

    /// <summary>The full-strength tinted head and eye colors, with the eye kept legible across the whole mix.</summary>
    public static (string Head, string Eye) Tinted(string head, string eye, Tint t)
    {
        var baseHead = FromHex(head);
        var baseEye = FromHex(eye);

        var hotHead = new Oklch(baseHead.L + (t.L - baseHead.L) * t.Pull, Math.Max(baseHead.C, t.C), t.H);
        hotHead = EnsureContrast(hotHead, DarkSurface, SurfaceFloor);
        var hotEye = EnsureContrast(baseEye, hotHead, TintFloor);

        var dir = hotEye.L >= hotHead.L ? 1 : -1;
        var headHex = ToHex(hotHead);
        for (var pass = 0; pass < 40; pass++)
        {
            var eyeHex = ToHex(hotEye);
            var worst = double.PositiveInfinity;
            for (var i = 0; i <= 10; i++)
            {
                var u = i / 10.0;
                worst = Math.Min(worst, Contrast(FromHex(MixHex(eye, eyeHex, u)), FromHex(MixHex(head, headHex, u))));
            }
            if (worst >= TintFloor) return (headHex, eyeHex);
            var l = Math.Min(1, Math.Max(0, hotEye.L + dir * 0.02));
            if (l == hotEye.L) return (headHex, eyeHex);
            hotEye = hotEye with { L = l };
        }
        return (headHex, ToHex(hotEye));
    }
}
