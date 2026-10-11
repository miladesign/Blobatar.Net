namespace Blobatar.Net.Core;

/// <summary>What the eyes are pointed at.</summary>
public enum GazeAim
{
    /// <summary>Nothing. The eyes ease home and the idle glances come back.</summary>
    None,
    /// <summary>The eyes ease to centre and stay there; idle glances stay stood down (a face deliberately not looking).</summary>
    Rest,
    /// <summary>Look toward the aim point supplied each frame.</summary>
    Look,
}

/// <summary>
/// The gaze driver's state: eased direction (<see cref="X"/>, <see cref="Y"/>) and the idle stand-down
/// (<see cref="Hold"/>, 0 to 1). One call per frame; no clock of its own.
/// </summary>
public sealed class GazeState
{
    public double X { get; private set; }
    public double Y { get; private set; }
    /// <summary>How far the idle glances have stood down, 0 (full idle) to 1 (silent).</summary>
    public double Hold { get; private set; }
    double _holdTarget;
    bool _resting = true;

    public double Settle { get; set; } = Gaze.Settle;
    public double Snap { get; set; } = Gaze.Snap;

    public void Aim(GazeAim aim)
    {
        _resting = aim != GazeAim.Look;
        _holdTarget = aim == GazeAim.None ? 0 : 1;
    }

    /// <param name="dtMs">Frame time in ms, capped at 64 by the caller.</param>
    /// <param name="dx">Aim minus face centre (x), or anything when resting.</param>
    /// <param name="width">Drawn width, for the write threshold.</param>
    /// <param name="travel">Full excursion in viewBox units.</param>
    public void Frame(double dtMs, double dx, double dy, double radius, double width, double travel)
    {
        var k = Gaze.Pursuit(dtMs, Settle);
        var s = Gaze.Step(X, Y, _resting ? 0 : dx, _resting ? 0 : dy, radius, k, 1, Snap);
        X = s.X;
        Y = s.Y;

        var eps = Gaze.Threshold(width, travel);
        if (Math.Abs(s.Tx - X) <= eps && Math.Abs(s.Ty - Y) <= eps) { X = s.Tx; Y = s.Ty; }

        Hold += (_holdTarget - Hold) * k;
        if (Math.Abs(_holdTarget - Hold) <= Gaze.HoldEps) Hold = _holdTarget;
    }

    /// <summary>Per-eye offsets for the current direction. <paramref name="face"/> null (no survey) means neutral eyes.</summary>
    public GazeEye[] Eyes(GazeFace? face, double travel, int count)
    {
        var r = new GazeEye[count];
        for (var i = 0; i < count; i++)
        {
            if (face is null || i >= face.Marks.Length) { r[i] = GazeEye.None; continue; }
            var p = Gaze.Project(face.Marks[i], X * (travel / face.Rx), Y * (travel / face.Ry));
            r[i] = p with { Dx = p.Dx * face.Rx, Dy = p.Dy * face.Ry };
        }
        return r;
    }
}
