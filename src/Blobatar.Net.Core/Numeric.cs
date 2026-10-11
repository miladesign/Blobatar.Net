namespace Blobatar.Net.Core;

internal static class Numeric
{
    public static double Hypot(double x, double y)
    {
#if NET7_0_OR_GREATER
        return double.Hypot(x, y);
#else
        // Scale before squaring to avoid overflow and underflow on .NET 6.
        x = Math.Abs(x);
        y = Math.Abs(y);
        if (double.IsInfinity(x) || double.IsInfinity(y)) return double.PositiveInfinity;
        var max = Math.Max(x, y);
        if (max == 0) return 0;
        var ratio = Math.Min(x, y) / max;
        return max * Math.Sqrt(1 + ratio * ratio);
#endif
    }
}
