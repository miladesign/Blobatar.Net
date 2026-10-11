namespace Blobatar.Net.Core;

/// <summary>A point on the face disc, each axis in -1..1 (the eye centre over the face radii).</summary>
public readonly record struct GazeMark(double X, double Y);

/// <summary>
/// One eye's gaze offsets: a shift in viewBox units, a foreshortening scale per axis and a roll in degrees.
/// <see cref="None"/> is the neutral eye.
/// </summary>
public readonly record struct GazeEye(double Dx, double Dy, double Sx, double Sy, double T)
{
    public static readonly GazeEye None = new(0, 0, 1, 1, 0);
}

/// <summary>The face disc the eyes slide over: where each eye sits and how far the disc reaches.</summary>
public sealed record GazeFace(GazeMark[] Marks, double Rx, double Ry);

/// <summary>Pointer-gaze maths: pursuit smoothing, the saccade snap and the sphere projection of each eye.</summary>
public static partial class Gaze
{
    /// <summary>Pursuit time constant in ms.</summary>
    public const double Settle = 110;
    /// <summary>Distance, in excursions, past which the eyes jump instead of gliding.</summary>
    public const double Snap = 1.6;
    /// <summary>Fraction of the face radius inside which the look eases to centre.</summary>
    public const double Deadzone = 0.55;
    public const double VisiblePx = 0.15;
    public const double HoldEps = 0.01;
    const double EpsMin = 0.002, EpsMax = 0.06;
    /// <summary>How far toward the rim a projected eye may travel before it flattens against it.</summary>
    public const double Limb = 0.97;
    /// <summary>Degrees of roll per unit of shear.</summary>
    public const double Tilt = 4;

    public static double Smoothstep(double t) => t * t * (3 - 2 * t);

    /// <summary>Fraction of the remaining distance covered in <paramref name="dt"/> ms.</summary>
    public static double Pursuit(double dt, double settle = Settle) => settle <= 0 ? 1 : 1 - Math.Exp(-dt / settle);

    /// <summary>Smallest eye movement, in direction units, worth drawing at this size and travel.</summary>
    public static double Threshold(double width, double travel)
    {
        var perUnit = travel * (width / 100);
        return perUnit > 0 ? Math.Clamp(VisiblePx / perUnit, EpsMin, EpsMax) : EpsMax;
    }

    static (double Sx, double Sy, double Sh) Patch(double x, double y, double z)
    {
        var r2 = x * x + y * y;
        if (r2 < 1e-9) return (1, 1, 0);
        var d = Math.Max(0, z);
        return ((d * x * x + y * y) / r2, (d * y * y + x * x) / r2, ((d - 1) * x * y) / r2);
    }

    /// <summary>Turns a point on a unit sphere by yaw then pitch (radians) and reports how its patch of surface changes.</summary>
    public static GazeEye Project(GazeMark m, double yaw, double pitch)
    {
        var r2 = m.X * m.X + m.Y * m.Y;
        var k = r2 > 1 ? 1 / Math.Sqrt(r2) : 1;
        var x0 = m.X * k;
        var y0 = m.Y * k;
        var z0 = Math.Sqrt(Math.Max(0, 1 - x0 * x0 - y0 * y0));

        double cy = Math.Cos(yaw), sy = Math.Sin(yaw);
        var x1 = x0 * cy + z0 * sy;
        var z1 = z0 * cy - x0 * sy;
        double cp = Math.Cos(pitch), sp = Math.Sin(pitch);
        var y1 = y0 * cp + z1 * sp;
        var z2 = z1 * cp - y0 * sp;

        var rho = Numeric.Hypot(x1, y1);
        var over = z2 <= 0 || rho > Limb;
        var back = over ? Limb / (rho == 0 ? 1 : rho) : 1;
        var px = x1 * back;
        var py = y1 * back;
        var pz = over ? Math.Sqrt(1 - Limb * Limb) : z2;

        var rest = Patch(x0, y0, z0);
        var now = Patch(px, py, pz);
        return new GazeEye(
            px - m.X, py - m.Y,
            Math.Min(1, now.Sx / (rest.Sx == 0 ? 1 : rest.Sx)),
            Math.Min(1, now.Sy / (rest.Sy == 0 ? 1 : rest.Sy)),
            Tilt * (now.Sh - rest.Sh));
    }

    public readonly record struct StepResult(double X, double Y, double Tx, double Ty, double F);

    /// <summary>One pursuit step toward the aim with the saccade branch. Pure.</summary>
    /// <param name="dx">Aim minus face centre, x.</param>
    /// <param name="radius">Drawn radius, the scale of the near-field ease.</param>
    /// <param name="k">Pursuit fraction for this frame.</param>
    public static StepResult Step(double x, double y, double dx, double dy, double radius, double k,
                                  double gain = 1, double snap = Snap)
    {
        var d = Numeric.Hypot(dx, dy);
        var near = radius > 0 ? Smoothstep(Math.Min(1, d / (radius * Deadzone))) : 1;
        var amp = gain * near;
        var tx = d > 0 ? dx / d * amp : 0;
        var ty = d > 0 ? dy / d * amp : 0;
        var f = Numeric.Hypot(tx - x, ty - y) > snap ? 1 : k;
        return new StepResult(x + (tx - x) * f, y + (ty - y) * f, tx, ty, f);
    }

    /// <summary>
    /// Finds where the eyes sit on the face disc. <paramref name="hit"/> says whether a point is inside the head
    /// silhouette; bounds are (x, y, width, height) in viewBox units of the neutral, unposed figure.
    /// </summary>
    public static GazeFace? Survey((double X, double Y, double W, double H) head,
                                   IReadOnlyList<(double X, double Y, double W, double H)> eyes,
                                   Func<double, double, bool> hit)
    {
        if (eyes.Count == 0) return null;
        var cx0 = head.X + head.W / 2;
        var cy0 = head.Y + head.H / 2;
        bool Hit(double t, double c, double s) => hit(cx0 + t * head.W / 2 * c, cy0 + t * head.H / 2 * s);

        // How much of the bounding ellipse the silhouette really fills, worst direction of sixteen.
        double fit = 1;
        for (var a = 0; a < 16; a++)
        {
            var r = a / 16.0 * Math.PI * 2;
            double c = Math.Cos(r), s = Math.Sin(r);
            if (Hit(1, c, s)) continue;
            double lo = 0, hi = 1;
            for (var i = 0; i < 12; i++)
            {
                var mid = (lo + hi) / 2;
                if (Hit(mid, c, s)) lo = mid; else hi = mid;
            }
            fit = Math.Min(fit, lo);
        }

        double ix = 0, iy = 0;
        var centres = new (double X, double Y)[eyes.Count];
        for (var i = 0; i < eyes.Count; i++)
        {
            var g = eyes[i];
            ix = Math.Max(ix, g.W / 2);
            iy = Math.Max(iy, g.H / 2);
            centres[i] = (g.X + g.W / 2 - cx0, g.Y + g.H / 2 - cy0);
        }

        var rx = Math.Max(1, head.W / 2 * fit - ix);
        var ry = Math.Max(1, head.H / 2 * fit - iy);
        double need = 0;
        foreach (var c in centres) need = Math.Max(need, Numeric.Hypot(c.X / rx, c.Y / ry));
        if (need > 0.85) { rx *= need / 0.85; ry *= need / 0.85; }
        return new GazeFace(centres.Select(c => new GazeMark(c.X / rx, c.Y / ry)).ToArray(), rx, ry);
    }
}
