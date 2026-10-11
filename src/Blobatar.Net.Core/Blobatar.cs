namespace Blobatar.Net.Core;

public enum BackdropStyle { None, Square, Circle, Squircle }

public sealed record BlobatarOptions
{
    /// <summary>Emits width/height in the SVG output only.</summary>
    public int? Size { get; init; }
    public BackdropStyle Background { get; init; } = BackdropStyle.None;
    public PaletteOverride? Palette { get; init; }
    /// <summary>Locks the hue in degrees.</summary>
    public double? Hue { get; init; }
    /// <summary>Locks the tone as a 0-1 position (pale to ink). Use 0.999 for ink.</summary>
    public double? Tone { get; init; }
    /// <summary>Pins traits to 0-1 positions; a list means "any of these".</summary>
    public IReadOnlyDictionary<string, double[]>? Traits { get; init; }
    public bool Normalize { get; init; } = true;
    public bool Contrast { get; init; } = true;
    public string? Title { get; init; }
    /// <summary>Static pose, baked into the geometry. Null or idle draws the neutral face.</summary>
    public Expression? Expression { get; init; }
}

/// <summary>One drawn primitive with its fill.</summary>
public sealed record Mark(string? D, double Cx, double Cy, double R, string Fill)
{
    public bool IsCircle => D is null;
}

public sealed record Backdrop(string D, string Fill);

/// <summary>A fully resolved blobatar: palette, backdrop and drawable marks.</summary>
/// <param name="Wrap">Body-level SVG transform from a baked pose (empty when none).</param>
public sealed record BlobatarModel(BlobLayout Layout, Palette Palette, Backdrop? Backdrop, IReadOnlyList<Mark> Marks, string Wrap = "");

public static class BlobatarFactory
{
    public static BlobatarModel Create(string seed, BlobatarOptions? options = null)
    {
        var o = options ?? new BlobatarOptions();
        var t = new Traits(seed, o.Normalize, o.Traits);
        var p = Colors.Create(o.Hue ?? t.Num("hue", 0, 360), o.Contrast, o.Tone ?? t["tone"]);
        if (o.Palette is { } ov)
            p = new Palette(ov.Bg ?? p.Bg, ov.Head ?? p.Head, ov.Eye ?? p.Eye);

        var layout = Layouts.Compute(t);
        var wrap = "";
        if (o.Expression is { } e)
        {
            p = e.Apply(p);
            layout = layout.WithEyes(e.P.BakeEyes(layout.Eyes));
            wrap = e.P.BakeWrap();
        }
        return new BlobatarModel(layout, p, MakeBackdrop(o.Background, p), MakeMarks(layout, p), wrap);
    }

    static Backdrop? MakeBackdrop(BackdropStyle s, Palette p) => s switch
    {
        BackdropStyle.None => null,
        BackdropStyle.Square => new("M0 0H100V100H0Z", p.Bg),
        BackdropStyle.Circle => new(Geo.Superellipse(50, 50, 50, 50, 2), p.Bg),
        _ => new(Geo.Superellipse(50, 50, 50, 50, 6), p.Bg),
    };

    /// <summary>Petals, extras, body, then eyes, in paint order.</summary>
    static List<Mark> MakeMarks(BlobLayout l, Palette p)
    {
        var marks = new List<Mark>();
        foreach (var d in l.Petals) marks.Add(new(null, Geo.R2(d.Cx), Geo.R2(d.Cy), Geo.R2(d.R), p.Head));
        foreach (var d in l.Extra) marks.Add(new(d, 0, 0, 0, p.Head));
        marks.Add(new(l.BodyPath(), 0, 0, 0, p.Head));
        foreach (var e in l.Eyes) marks.Add(new(Geo.Superellipse(e), 0, 0, 0, p.Eye));
        return marks;
    }
}
