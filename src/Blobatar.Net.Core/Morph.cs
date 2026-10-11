namespace Blobatar.Net.Core;

/// <summary>
/// The pose and colour travel between expressions, for substrates with no CSS transitions.
/// Pose channels lerp; fills fade per sRGB channel over the same eased progress.
/// Retargeting mid-flight continues from wherever the face currently is.
/// </summary>
public sealed class Morph
{
    Pose _fromPose = Pose.Ident, _toPose = Pose.Ident;
    string _fromHead = "", _fromEye = "", _toHead = "", _toEye = "";
    double _start = double.NegativeInfinity, _ms = 1;
    Func<double, double> _curve = Ease.InOut;

    /// <summary>Jumps to <paramref name="e"/> with no travel.</summary>
    public void Snap(Expression e, Palette basePalette)
    {
        var c = e.Apply(basePalette);
        _fromPose = _toPose = e.P;
        _fromHead = _toHead = c.Head;
        _fromEye = _toEye = c.Eye;
        _start = double.NegativeInfinity;
    }

    /// <summary>Starts travelling to <paramref name="e"/> from the current look: 300 ms in, 400 ms back to idle.</summary>
    public void Retarget(Expression e, Palette basePalette, double now)
    {
        var cur = Sample(now);
        var c = e.Apply(basePalette);
        (_fromPose, _fromHead, _fromEye) = (cur.Pose, cur.Head, cur.Eye);
        (_toPose, _toHead, _toEye) = (e.P, c.Head, c.Eye);
        (_ms, _curve) = e.IsIdle ? Ease.MorphOut : Ease.MorphIn;
        _start = now;
    }

    public (Pose Pose, string Head, string Eye) Sample(double now)
    {
        var u = Math.Clamp((now - _start) / _ms, 0, 1);
        if (u >= 1) return (_toPose, _toHead, _toEye);
        var k = _curve(u);
        return (Pose.Lerp(_fromPose, _toPose, k), Colors.FadeHex(_fromHead, _toHead, k), Colors.FadeHex(_fromEye, _toEye, k));
    }
}
