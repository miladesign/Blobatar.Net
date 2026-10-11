namespace Blobatar.Net.Core;

/// <summary>
/// An expression as fourteen numbers. Every channel is linear, so a morph is a plain lerp.
/// The defaults are idle: the identity pose.
/// </summary>
public sealed record Pose(
    double Esx = 1, double Esy = 1, double Tilt = 0, double Edy = 0, double Edx = 0,
    double Esx2 = 0, double Esy2 = 0, double Tilt2 = 0, double Edy2 = 0,
    double Lock = 0, double Heat = 0, double Shake = 0, double Rock = 0, double Bdy = 0)
{
    public static readonly Pose Ident = new();

    /// <summary>
    /// Part-way between two poses. Written from*(1-t) + to*t so t = 1 returns the target exactly.
    /// </summary>
    public static Pose Lerp(Pose? a, Pose? b, double t)
    {
        var f = a ?? Ident;
        var o = b ?? Ident;
        double L(double x, double y) => x * (1 - t) + y * t;
        return new Pose(
            L(f.Esx, o.Esx), L(f.Esy, o.Esy), L(f.Tilt, o.Tilt), L(f.Edy, o.Edy), L(f.Edx, o.Edx),
            L(f.Esx2, o.Esx2), L(f.Esy2, o.Esy2), L(f.Tilt2, o.Tilt2), L(f.Edy2, o.Edy2),
            L(f.Lock, o.Lock), L(f.Heat, o.Heat), L(f.Shake, o.Shake), L(f.Rock, o.Rock), L(f.Bdy, o.Bdy));
    }

    /// <summary>Applies the pose to the eye geometry, for static rendering.</summary>
    public Eye[] BakeEyes(IReadOnlyList<Eye> eyes)
    {
        var r = new Eye[eyes.Count];
        for (var i = 0; i < r.Length; i++)
        {
            var e = eyes[i];
            double side = i == 0 ? -1 : 1, two = i == 0 ? 0 : 1;
            r[i] = e with
            {
                Cx = e.Cx + Edx * side,
                Cy = e.Cy + Edy + two * Edy2,
                Rx = e.Rx * (Esx + two * Esx2),
                Ry = e.Ry * (Esy + two * Esy2),
                Rot = e.Rot * (1 - Lock) + (Tilt + two * Tilt2) * side,
            };
        }
        return r;
    }

    /// <summary>The body-level transform a baked pose needs, or empty when it does not move the body.</summary>
    public string BakeWrap() => Bdy != 0 ? $"translate(0 {SvgTransform.N(SvgTransform.R3(Bdy))})" : "";

    /// <summary>
    /// One eye's pose transform for the animated path. <paramref name="rockp"/> is the
    /// rocking phase in [-1, 1]; 1 reproduces <see cref="BakeEyes"/>. <paramref name="gaze"/> adds the pointer gaze.
    /// </summary>
    public SvgTransform EyeTransform(Eye e, int i, double rockp = 1, GazeEye? gaze = null)
    {
        double wrap = i == 0 ? -1 : 1, sel = i == 0 ? 0 : 1;
        var g = gaze ?? GazeEye.None;
        var ph = sel * (1 - Rock) + Rock * ((1 + wrap * rockp) / 2);
        return new SvgTransform()
            .Translate(e.Cx + Edx * wrap + g.Dx, e.Cy + Edy + ph * Edy2 + g.Dy)
            .Rotate((Tilt + sel * Tilt2) * wrap + e.Rot * (1 - Lock) + g.T)
            .Scale((Esx + sel * Esx2) * g.Sx, (Esy + sel * Esy2) * g.Sy)
            .Rotate(-e.Rot)
            .Translate(-e.Cx, -e.Cy);
    }
}
