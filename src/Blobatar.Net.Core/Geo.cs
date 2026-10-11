using System.Globalization;
using System.Text;

namespace Blobatar.Net.Core;

public readonly record struct Eye(double Cx, double Cy, double Rx, double Ry, double N, double Rot);

/// <summary>Path primitives. Output strings match the JS source character for character.</summary>
public static class Geo
{
    /// <summary>JS Math.round: halves round toward +infinity.</summary>
    public static double JsRound(double x)
    {
        var f = Math.Floor(x);
        return x - f >= 0.5 ? f + 1 : f;
    }

    /// <summary>Round to 2 decimals and print like JS (-0 becomes "0").</summary>
    public static string F(double v)
    {
        var s = JsRound(v * 100) / 100;
        return s == 0 ? "0" : s.ToString("R", CultureInfo.InvariantCulture);
    }

    public static double R2(double v) => JsRound(v * 100) / 100;

    /// <summary>|x/a|^n + |y/b|^n = 1 as four cubic Béziers, rotated in degrees.</summary>
    public static string Superellipse(double cx, double cy, double rx, double ry, double n = 4, double rot = 0)
    {
        var k = Math.Min(1, (8 * Math.Pow(2, -1 / n) - 4) / 3);
        double a = rx, b = ry, ak = a * k, bk = b * k;
        (double X, double Y)[] pts =
        {
            (a, 0), (a, bk), (ak, b), (0, b),
            (-ak, b), (-a, bk), (-a, 0),
            (-a, -bk), (-ak, -b), (0, -b),
            (ak, -b), (a, -bk), (a, 0),
        };
        var t = rot * Math.PI / 180;
        double cos = Math.Cos(t), sin = Math.Sin(t);
        string At(int i) => F(cx + pts[i].X * cos - pts[i].Y * sin) + " " + F(cy + pts[i].X * sin + pts[i].Y * cos);

        var d = new StringBuilder("M").Append(At(0));
        for (var i = 1; i < 13; i += 3) d.Append('C').Append(At(i)).Append(' ').Append(At(i + 1)).Append(' ').Append(At(i + 2));
        return d.Append('Z').ToString();
    }

    public static string Superellipse(Eye e) => Superellipse(e.Cx, e.Cy, e.Rx, e.Ry, e.N, e.Rot);

    /// <summary>Closed Catmull-Rom spline through radii sampled around an ellipse.</summary>
    public static string BlobPath(double cx, double cy, double rx, double ry, IReadOnlyList<double> radii, double rot = 0)
    {
        int n = radii.Count;
        var t0 = rot * Math.PI / 180;
        var p = new (double X, double Y)[n];
        for (var i = 0; i < n; i++)
        {
            var a = t0 + 2 * Math.PI * i / n;
            p[i] = (cx + rx * radii[i] * Math.Cos(a), cy + ry * radii[i] * Math.Sin(a));
        }
        (double X, double Y) At(int i) => p[((i % n) + n) % n];

        var d = new StringBuilder("M").Append(F(At(0).X)).Append(' ').Append(F(At(0).Y));
        for (var i = 0; i < n; i++)
        {
            var (x0, y0) = At(i - 1);
            var (x1, y1) = At(i);
            var (x2, y2) = At(i + 1);
            var (x3, y3) = At(i + 2);
            d.Append('C').Append(F(x1 + (x2 - x0) / 6)).Append(' ').Append(F(y1 + (y2 - y0) / 6))
             .Append(' ').Append(F(x2 - (x3 - x1) / 6)).Append(' ').Append(F(y2 - (y3 - y1) / 6))
             .Append(' ').Append(F(x2)).Append(' ').Append(F(y2));
        }
        return d.Append('Z').ToString();
    }

    /// <summary>Regular polygon with corners cut back and joined by quadratics.</summary>
    public static string Polygon(double cx, double cy, double rx, double ry, int sides, double round = 0.3, double rot = 0)
    {
        var k = round > 0 ? (round < 1 ? round / 2 : 0.5) : 0;
        var t0 = rot * Math.PI / 180 - Math.PI / 2;
        var v = new (double X, double Y)[sides];
        for (var i = 0; i < sides; i++)
        {
            var a = t0 + 2 * Math.PI * i / sides;
            v[i] = (cx + rx * Math.Cos(a), cy + ry * Math.Sin(a));
        }
        (double X, double Y) At(int i) => v[((i % sides) + sides) % sides];
        string Cut(int i, int j)
        {
            var (x0, y0) = At(i);
            var (x1, y1) = At(j);
            return F(x0 + (x1 - x0) * k) + " " + F(y0 + (y1 - y0) * k);
        }

        var d = new StringBuilder("M").Append(Cut(0, -1));
        for (var i = 0; i < sides; i++)
        {
            var (x, y) = At(i);
            d.Append('Q').Append(F(x)).Append(' ').Append(F(y)).Append(' ').Append(Cut(i, i + 1));
            if (k < 0.5) d.Append('L').Append(Cut(i + 1, i));
        }
        return d.Append('Z').ToString();
    }

    /// <summary>The straight run of a capsule.</summary>
    public static string Box(double cx, double cy, double rx, double ry)
    {
        var l = F(cx - rx);
        var r = F(cx + rx);
        return $"M{l} {F(cy - ry)}H{r}V{F(cy + ry)}H{l}Z";
    }

    /// <summary>The two tangents from an apex to the body ellipse (a droplet's point).</summary>
    public static string Taper(double cx, double cy, double rx, double ry, double tip)
    {
        var t = Math.Max(1.05, tip);
        var tx = rx * Math.Sqrt(1 - 1 / (t * t));
        var ty = cy - ry / t;
        var apex = cy - t * ry;
        var px = tx * 0.14;
        var py = ty + 0.86 * (apex - ty);
        return $"M{F(cx - tx)} {F(ty)}L{F(cx - px)} {F(py)}Q{F(cx)} {F(apex)} {F(cx + px)} {F(py)}L{F(cx + tx)} {F(ty)}Z";
    }
}
