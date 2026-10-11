namespace Blobatar.Net.Core;

/// <summary>CSS cubic-bezier timing functions and the morph's two curves.</summary>
public static class Ease
{
    public static Func<double, double> Bezier(double x1, double y1, double x2, double y2)
    {
        double cx = 3 * x1, bx = 3 * (x2 - x1) - cx, ax = 1 - cx - bx;
        double cy = 3 * y1, by = 3 * (y2 - y1) - cy, ay = 1 - cy - by;
        return x =>
        {
            var t = x;
            for (var i = 0; i < 8; i++)
            {
                var err = ((ax * t + bx) * t + cx) * t - x;
                if (Math.Abs(err) < 1e-5) break;
                var d = (3 * ax * t + 2 * bx) * t + cx;
                if (Math.Abs(d) < 1e-6) break;
                t -= err / d;
            }
            return ((ay * t + by) * t + cy) * t;
        };
    }

    public static readonly Func<double, double> InOut = Bezier(0.42, 0, 0.58, 1);
    public static readonly Func<double, double> In = Bezier(0.42, 0, 1, 1);
    public static readonly Func<double, double> Out = Bezier(0, 0, 0.58, 1);

    /// <summary>Adopting an expression: 300 ms.</summary>
    public static readonly (double Ms, Func<double, double> Curve) MorphIn = (300, Bezier(0.45, 0.05, 0.5, 1));
    /// <summary>Returning to idle: 400 ms.</summary>
    public static readonly (double Ms, Func<double, double> Curve) MorphOut = (400, InOut);
}
