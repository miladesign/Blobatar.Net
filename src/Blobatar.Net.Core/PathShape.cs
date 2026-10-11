namespace Blobatar.Net.Core;

/// <summary>A filled shape you can measure and hit-test without any UI framework: bounds and a nonzero-winding point test.</summary>
public sealed class PathShape
{
    const int CubicSteps = 24, QuadSteps = 16;

    readonly List<(double X, double Y)[]> _polys = new();
    readonly bool _circle;
    readonly double _cx, _cy, _r;

    public (double X, double Y, double W, double H) Bounds { get; }

    public PathShape(string d)
    {
        List<(double X, double Y)>? cur = null;
        (double X, double Y) at = default;
        foreach (var s in PathData.Parse(d))
        {
            switch (s.Kind)
            {
                case SegmentKind.Move:
                    Flush(cur);
                    cur = new() { (s.X, s.Y) };
                    break;
                case SegmentKind.Line:
                    cur?.Add((s.X, s.Y));
                    break;
                case SegmentKind.Cubic:
                    for (var i = 1; i <= CubicSteps; i++)
                    {
                        var t = i / (double)CubicSteps;
                        var u = 1 - t;
                        double a = u * u * u, b = 3 * u * u * t, c = 3 * u * t * t, e = t * t * t;
                        cur?.Add((a * at.X + b * s.X1 + c * s.X2 + e * s.X, a * at.Y + b * s.Y1 + c * s.Y2 + e * s.Y));
                    }
                    break;
                case SegmentKind.Quad:
                    for (var i = 1; i <= QuadSteps; i++)
                    {
                        var t = i / (double)QuadSteps;
                        var u = 1 - t;
                        cur?.Add((u * u * at.X + 2 * u * t * s.X1 + t * t * s.X, u * u * at.Y + 2 * u * t * s.Y1 + t * t * s.Y));
                    }
                    break;
                case SegmentKind.Close:
                    Flush(cur);
                    cur = null;
                    break;
            }
            at = (s.X, s.Y);
        }
        Flush(cur);
        Bounds = Measure();
    }

    PathShape(double cx, double cy, double r)
    {
        (_circle, _cx, _cy, _r) = (true, cx, cy, r);
        Bounds = (cx - r, cy - r, 2 * r, 2 * r);
    }

    public static PathShape FromMark(Mark m) => m.IsCircle ? new PathShape(m.Cx, m.Cy, m.R) : new PathShape(m.D!);

    void Flush(List<(double X, double Y)>? poly)
    {
        if (poly is { Count: > 2 }) _polys.Add(poly.ToArray());
    }

    (double, double, double, double) Measure()
    {
        double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
        foreach (var p in _polys)
            foreach (var (x, y) in p) (x0, y0, x1, y1) = (Math.Min(x0, x), Math.Min(y0, y), Math.Max(x1, x), Math.Max(y1, y));
        return _polys.Count == 0 ? (0, 0, 0, 0) : (x0, y0, x1 - x0, y1 - y0);
    }

    /// <summary>Nonzero winding, like SVG's default fill rule.</summary>
    public bool Contains(double x, double y)
    {
        if (_circle) return (x - _cx) * (x - _cx) + (y - _cy) * (y - _cy) <= _r * _r;
        var wn = 0;
        foreach (var p in _polys)
        {
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
            {
                var (ax, ay) = p[j];
                var (bx, by) = p[i];
                var left = (bx - ax) * (y - ay) - (x - ax) * (by - ay);
                if (ay <= y) { if (by > y && left > 0) wn++; }
                else if (by <= y && left < 0) wn--;
            }
        }
        return wn != 0;
    }
}
