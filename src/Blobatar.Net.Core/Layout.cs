namespace Blobatar.Net.Core;

/// <summary>Everything the renderer needs to draw one blobatar, as numbers.</summary>
public sealed class BlobLayout
{
    public required string Shape { get; init; }
    public required Func<Body, string>? Draw { get; init; }
    public required Body Body { get; init; }
    public required IReadOnlyList<Petal> Petals { get; init; }
    public required IReadOnlyList<string> Extra { get; init; }
    public required Eye[] Eyes { get; init; }

    public BlobLayout WithEyes(Eye[] eyes) => new()
    {
        Shape = Shape, Draw = Draw, Body = Body, Petals = Petals, Extra = Extra, Eyes = eyes,
    };

    public string BodyPath() => Draw is null ? Geo.Superellipse(Body.Cx, Body.Cy, Body.Rx, Body.Ry, Body.N, Body.Rot) : Draw(Body);
}

/// <summary>Seed traits to layout. The numeric ranges here are frozen per major version.</summary>
public static class Layouts
{
    public static BlobLayout Compute(Traits t)
    {
        var shape = PickShape(t["shape"]);
        var r = t.Num("body.r", 31, 38) * shape.Core;
        var radii = new double[t.Int("body.pts", 6, 8)];
        for (var i = 0; i < radii.Length; i++) radii[i] = 1 + t.Jitter($"body.r{i}", 0.16);

        var body = new Body
        {
            Cx = 50 + t.Jitter("body.x", 1.5),
            Cy = 50 + t.Jitter("body.y", 1.5),
            Rx = r,
            Ry = r * t.Num("body.ratio", 0.92, 1.08),
            N = t.Num("body.n", 1.9, 2.5),
            Rot = 0,
            Radii = radii,
        };
        shape.Patch?.Invoke(t, body);

        var face = shape.Face?.Invoke(body) ?? new Ellipse(body.Cx, body.Cy, body.Rx, body.Ry);
        var deco = new Deco();
        shape.Decorate?.Invoke(t, body, deco);

        return new BlobLayout
        {
            Shape = shape.Name, Draw = shape.Path, Body = body,
            Petals = deco.Petals, Extra = deco.Extra, Eyes = FaceFit(t, body, face),
        };
    }

    static Shape PickShape(double v)
    {
        foreach (var (shape, upTo) in Shapes.Bands) if (v < upTo) return shape;
        return Shapes.Bands[^1].Shape;
    }

    /// <summary>Fits the eye cluster inside the silhouette's face region on both axes.</summary>
    static Eye[] FaceFit(Traits t, Body b, Ellipse face)
    {
        var rx = b.Rx;
        var er0 = t.Num("eye.rx", 0.075, 0.105) * rx;
        var ratio = t.Num("eye.ratio", 1.9, 3.2);
        var scale = t.Num("eye.scale", 0.78, 1.24);
        var stretch = t.Num("eye.stretch", 0.85, 1.18);
        var clearance = t.Num("eye.gap", 0.1, 0.24) * rx;
        var wide = er0 * Math.Max(1, scale);
        var tall = er0 * ratio * Math.Max(1, scale * stretch);
        var gap0 = wide + rx * 0.03 + clearance;

        var gx = t.Jitter("gaze.x", 0.09) * face.Rx;
        var gy = t.Num("gaze.y", -0.2, 0.08) * face.Ry;
        var dy = t.Jitter("eye.dy", 0.04) * face.Ry;
        var reach = Numeric.Hypot(wide, tall);
        var need = Numeric.Hypot(
            (Math.Abs(gx) + gap0 + reach) / face.Rx,
            (Math.Abs(gy) + Math.Abs(dy) + reach) / face.Ry);
        var fit = need > 0.9 ? 0.9 / need : 1;

        var er = er0 * fit;
        var eyeRy = er * ratio;
        var gap = gap0 * fit;
        var room = Math.Max(0, Math.Min(1, clearance / tall));
        var bound = Math.Min(12, Math.Asin(room) * 180 / Math.PI);
        var lean = t.Num("eye.lean", -1, 1) * bound;
        var lean2 = Math.Max(-12, Math.Min(12, lean + t.Jitter("eye.lean2", 3.5)));

        var cx = face.Cx + gx * fit;
        var cy = face.Cy + gy * fit;
        return new[]
        {
            new Eye(cx - gap, cy, er, eyeRy, t.Num("eye.n", 3.5, 6), lean),
            new Eye(cx + gap, cy + dy * fit, er * scale, eyeRy * scale * stretch, t.Num("eye.n", 3.5, 6), lean2),
        };
    }
}
