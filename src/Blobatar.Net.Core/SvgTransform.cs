namespace Blobatar.Net.Core;

/// <summary>A 2D affine matrix in SVG order: x' = A x + C y + E, y' = B x + D y + F.</summary>
public readonly record struct Affine(double A, double B, double C, double D, double E, double F)
{
    public static readonly Affine Identity = new(1, 0, 0, 1, 0, 0);

    /// <summary>this × t: <paramref name="t"/> is applied first.</summary>
    public static Affine operator *(Affine m, Affine t) => new(
        m.A * t.A + m.C * t.B, m.B * t.A + m.D * t.B,
        m.A * t.C + m.C * t.D, m.B * t.C + m.D * t.D,
        m.A * t.E + m.C * t.F + m.E, m.B * t.E + m.D * t.F + m.F);
}

/// <summary>
/// An SVG transform list. Values are rounded to 3 decimals as the JS does, so
/// <see cref="ToString"/> matches the web output and <see cref="ToAffine"/> is what that string draws.
/// </summary>
public sealed class SvgTransform
{
    readonly List<(char Op, double X, double Y)> _ops = new();

    public static double R3(double v) => Geo.JsRound(v * 1000) / 1000;

    public SvgTransform Translate(double x, double y) { _ops.Add(('t', R3(x), R3(y))); return this; }
    public SvgTransform Rotate(double deg) { _ops.Add(('r', R3(deg), 0)); return this; }
    public SvgTransform Scale(double x, double y) { _ops.Add(('s', R3(x), R3(y))); return this; }

    public static string N(double v) =>
        v == 0 ? "0" : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    public override string ToString() => string.Join(" ", _ops.Select(o => o.Op switch
    {
        't' => $"translate({N(o.X)} {N(o.Y)})",
        'r' => $"rotate({N(o.X)})",
        _ => $"scale({N(o.X)} {N(o.Y)})",
    }));

    public Affine ToAffine()
    {
        var m = Affine.Identity;
        foreach (var (op, x, y) in _ops)
        {
            switch (op)
            {
                case 't': m *= new Affine(1, 0, 0, 1, x, y); break;
                case 'r':
                    var r = x * Math.PI / 180;
                    m *= new Affine(Math.Cos(r), Math.Sin(r), -Math.Sin(r), Math.Cos(r), 0, 0);
                    break;
                default: m *= new Affine(x, 0, 0, y, 0, 0); break;
            }
        }
        return m;
    }
}
