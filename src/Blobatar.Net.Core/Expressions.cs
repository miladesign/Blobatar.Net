namespace Blobatar.Net.Core;

/// <summary>A named pose, with an optional colour tint that scales with <see cref="Pose.Heat"/>.</summary>
public sealed record Expression(string Name, Pose P, Tint? Tint = null)
{
    public bool IsIdle => P == Pose.Ident && Tint is null;

    /// <summary>The palette this pose wears at its own heat, or the input when it has no tint.</summary>
    public Palette Apply(Palette pal) => Tint is null ? pal : TintWith(pal, P, Tint);

    public static Palette TintWith(Palette pal, Pose p, Tint t)
    {
        var (head, eye) = Colors.Tinted(pal.Head, pal.Eye, t);
        return pal with { Head = Colors.MixHex(pal.Head, head, p.Heat), Eye = Colors.MixHex(pal.Eye, eye, p.Heat) };
    }
}

/// <summary>The thirteen poses plus idle. Values are the original's frozen numbers.</summary>
public static class Expressions
{
    public static readonly Expression Idle = new("idle", Pose.Ident);

    public static readonly Expression Happy = new("happy", new Pose(
        Esx: 1.72, Esy: 0.3, Tilt: 8, Edy: -1.5, Edx: 1.5, Esx2: 0.08, Esy2: 0.05, Tilt2: -16, Lock: 1, Bdy: -2.2));

    public static readonly Expression Sad = new("sad", new Pose(
        Esx: 0.6, Esy: 0.56, Tilt: 26, Edy: 3.6, Edx: 1.9, Esx2: -0.05, Esy2: -0.07, Tilt2: -7, Lock: 1, Bdy: 2.6));

    public static readonly Expression Mad = new("mad", new Pose(
        Esx: 1.85, Esy: 0.26, Tilt: -33, Edy: 0.4, Edx: 0.6, Esy2: -0.03, Tilt2: 5,
        Lock: 1, Heat: 0.62, Shake: 0.55, Bdy: 0.8), Colors.Hot);

    public static readonly Expression Surprised = new("surprised", new Pose(
        Esx: 1.34, Esy: 1.2, Tilt: -6, Edy: -1.05, Edx: 0.5, Esx2: 0.05, Esy2: 0.07, Tilt2: 3, Lock: 1, Bdy: -1.4));

    public static readonly Expression Wink = new("wink", new Pose(
        Esx: 1.32, Esy: 0.76, Tilt: 5, Edy: -0.6, Edx: 0.8, Esx2: 0.26, Esy2: -0.56, Tilt2: -11, Lock: 1, Bdy: -1.1));

    public static readonly Expression Sleepy = new("sleepy", new Pose(
        Esx: 1.14, Esy: 0.22, Edy: 2.4, Edx: 0.3, Esx2: -0.04, Esy2: 0.03, Tilt2: 4, Lock: 1, Bdy: 1.2));

    public static readonly Expression Smug = new("smug", new Pose(
        Esx: 1.3, Esy: 0.42, Tilt: 18, Edy: -0.5, Edx: 0.5, Esx2: 0.06, Esy2: -0.06, Tilt2: -36, Lock: 1, Bdy: -1));

    public static readonly Expression Unsure = new("unsure", new Pose(
        Esx: 0.95, Esy: 1.02, Tilt: 4, Edy: -0.2, Edx: 0.3, Esx2: 0.24, Esy2: -0.44, Tilt2: -18, Lock: 1));

    public static readonly Expression Scared = new("scared", new Pose(
        Esx: 0.78, Esy: 0.96, Tilt: -12, Edy: -1.5, Edx: -0.8, Esx2: -0.04, Esy2: 0.05, Tilt2: 4,
        Lock: 1, Shake: 0.35, Bdy: -0.6));

    public static readonly Expression Love = new("love", new Pose(
        Esx: 0.86, Esy: 1.28, Tilt: -14, Edy: -0.5, Edx: -0.35, Esx2: 0.05, Esy2: 0.06, Tilt2: 6,
        Lock: 1, Heat: 0.6, Bdy: -1.6), Colors.Rose);

    public static readonly Expression Shy = new("shy", new Pose(
        Esx: 0.62, Esy: 0.5, Tilt: 10, Edy: 1.4, Edx: -0.2, Esx2: -0.05, Esy2: -0.04, Tilt2: -8,
        Lock: 1, Heat: 0.55, Bdy: 0.9), Colors.Blush);

    public static readonly Expression Sick = new("sick", new Pose(
        Esx: 1.25, Esy: 0.34, Tilt: 20, Edy: 1.8, Edx: 0.8, Esx2: 0.05, Esy2: -0.05, Tilt2: -6,
        Lock: 1, Heat: 0.6, Shake: 0.18, Bdy: 1.4), Colors.Bile);

    public static readonly Expression Thinking = new("thinking", new Pose(
        Esx: 1.15, Esy: 0.62, Edy: 4.2, Edx: 0.4, Esx2: 0.02, Esy2: 0.06, Edy2: -8.4,
        Lock: 1, Rock: 0.8, Bdy: -0.4));

    public static Expression Of(ExpressionKind kind) => All[(int)kind];

    public static readonly IReadOnlyList<Expression> All = new[]
    {
        Idle, Happy, Sad, Mad, Surprised, Wink, Sleepy, Smug, Unsure, Scared, Love, Shy, Sick, Thinking,
    };
}
