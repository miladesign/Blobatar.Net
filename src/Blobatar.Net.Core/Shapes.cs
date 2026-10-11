namespace Blobatar.Net.Core;

public sealed class Body
{
    public double Cx, Cy, Rx, Ry, N, Rot;
    public double[] Radii = Array.Empty<double>();
    /// <summary>Polygon-only, set by the shapes that draw one.</summary>
    public int Sides;
    public double Round;
}

public readonly record struct Ellipse(double Cx, double Cy, double Rx, double Ry);
public readonly record struct Petal(double Cx, double Cy, double R);

public sealed class Deco
{
    public List<Petal> Petals { get; } = new();
    /// <summary>Extra outlines unioned with the core body, already traced.</summary>
    public List<string> Extra { get; } = new();
}

/// <summary>One silhouette: its size, patches, face region, decoration and outline.</summary>
public sealed class Shape
{
    public required string Name { get; init; }
    public required double Core { get; init; }
    public Action<Traits, Body>? Patch { get; init; }
    public Func<Body, Ellipse>? Face { get; init; }
    public Action<Traits, Body, Deco>? Decorate { get; init; }
    /// <summary>Null means a superellipse of the body.</summary>
    public Func<Body, string>? Path { get; init; }
}

/// <summary>Blobatar 2's ten silhouettes and their frozen seed-to-look band table.</summary>
public static class Shapes
{
    static string Poly(Body b) => Geo.Polygon(b.Cx, b.Cy, b.Rx, b.Ry, b.Sides, b.Round, b.Rot);
    static string Spline(Body b) => Geo.BlobPath(b.Cx, b.Cy, b.Rx, b.Ry, b.Radii, b.Rot);
    static Func<Body, Ellipse> Shrunk(double k) => b => new(b.Cx, b.Cy, b.Rx * k, b.Ry * k);
    static Ellipse SplineFace(Body b) => Shrunk(b.Radii.Min() * 0.95)(b);
    static Ellipse PolyFace(Body b) => Shrunk(0.84)(b);

    public static readonly Shape Round = new() { Name = "round", Core = 1 };

    public static readonly Shape Organic = new() { Name = "organic", Core = 0.98, Path = Spline, Face = SplineFace };

    public static readonly Shape Boxy = new()
    {
        Name = "boxy", Core = 0.86,
        Patch = (t, b) => { b.N = t.Num("body.n", 3.4, 6); b.Rot = t.Num("body.rot", -20, 20); },
    };

    public static readonly Shape Capsule = new()
    {
        Name = "capsule", Core = 1.02,
        Patch = (t, b) => b.Ry *= t.Num("capsule.squat", 0.55, 0.68),
        Face = Shrunk(0.94),
        Decorate = (_, b, o) =>
        {
            foreach (var s in new[] { -1, 1 }) o.Petals.Add(new(b.Cx + s * (b.Rx - b.Ry), b.Cy, b.Ry));
        },
        Path = b => Geo.Box(b.Cx, b.Cy, b.Rx - b.Ry, b.Ry),
    };

    public static readonly Shape Nub = new()
    {
        Name = "nub", Core = 0.88,
        Decorate = (t, b, o) =>
        {
            var count = t.Int("nub.n", 1, 2);
            for (var i = 0; i < count; i++)
            {
                var a = t.Num($"nub.a{i}", 0, 2 * Math.PI);
                o.Petals.Add(new(b.Cx + Math.Cos(a) * b.Rx * 0.88, b.Cy + Math.Sin(a) * b.Rx * 0.88,
                                 b.Rx * t.Num($"nub.r{i}", 0.24, 0.4)));
            }
        },
    };

    public static readonly Shape Cloud = new()
    {
        Name = "cloud", Core = 0.78, Face = SplineFace, Path = Spline,
        Decorate = (t, b, o) =>
        {
            var count = t.Int("cloud.n", 4, 6);
            for (var i = 0; i < count; i++)
            {
                var a = Math.PI + Math.PI * (i + 0.5) / count;
                o.Petals.Add(new(b.Cx + Math.Cos(a) * b.Rx * 0.8, b.Cy + Math.Sin(a) * b.Rx * 0.5,
                                 b.Rx * t.Num($"cloud.r{i}", 0.44, 0.62)));
            }
        },
    };

    public static readonly Shape Droplet = new()
    {
        Name = "droplet", Core = 0.78,
        Patch = (_, b) => { b.Cy += 0.22 * b.Ry; b.N = 2; },
        Face = b => new(b.Cx, b.Cy + b.Ry * 0.05, b.Rx * 0.88, b.Ry * 0.88),
        Decorate = (t, b, o) => o.Extra.Add(Geo.Taper(b.Cx, b.Cy, b.Rx, b.Ry, t.Num("droplet.tip", 1.4, 1.65))),
    };

    public static readonly Shape Hexagon = new()
    {
        Name = "hexagon", Core = 1.05, Path = Poly, Face = PolyFace,
        Patch = (t, b) =>
        {
            b.Sides = 6;
            b.Rot = t.Num("body.rot", -12, 12);
            b.Round = t.Num("poly.round", 0.24, 0.5);
        },
    };

    public static readonly Shape Sun = new()
    {
        Name = "sun", Core = 0.7,
        Decorate = (t, b, o) =>
        {
            var count = t.Int("sun.n", 6, 9);
            var dist = b.Rx * t.Num("sun.dist", 1.0, 1.08);
            var pr = b.Rx * t.Num("sun.r", 0.2, 0.26);
            var off = t.Num("sun.rot", 0, 2 * Math.PI);
            for (var i = 0; i < count; i++)
            {
                var a = off + 2 * Math.PI * i / count;
                o.Petals.Add(new(b.Cx + Math.Cos(a) * dist, b.Cy + Math.Sin(a) * dist, pr));
            }
        },
    };

    public static readonly Shape Triangle = new()
    {
        Name = "triangle", Core = 1.15, Path = Poly,
        Patch = (t, b) =>
        {
            b.Sides = 3;
            b.Rot = t.Num("body.rot", -5, 5);
            b.Round = t.Num("poly.round", 0.24, 0.5);
        },
        Face = b => new(b.Cx, b.Cy + b.Ry * 0.1, b.Rx * 0.54, b.Ry * 0.36),
    };

    /// <summary>[shape, upper edge of its band in [0, 1)], in order.</summary>
    public static readonly (Shape Shape, double UpTo)[] Bands =
    {
        (Round, 0.22), (Organic, 0.48), (Boxy, 0.6), (Capsule, 0.7), (Nub, 0.79),
        (Cloud, 0.86), (Droplet, 0.915), (Hexagon, 0.95), (Sun, 0.98), (Triangle, 1),
    };
}
