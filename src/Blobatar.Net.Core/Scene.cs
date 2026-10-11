namespace Blobatar.Net.Core;

/// <summary>Everything a renderer draws for one animated frame, as finished transforms and colours.</summary>
/// <param name="Body">Applies to every head part: root, breathe, bob.</param>
/// <param name="Eyes">One per eye, already including the body. Draw each neutral eye shape under its own.</param>
public readonly record struct SceneFrame(Affine Body, Affine[] Eyes, string Head, string Eye);

/// <summary>
/// The animated blobatar without a UI framework: idle loops, expression morph and gaze, as a pure
/// function of the clock plus a little state. A control supplies geometry, a clock and a pointer.
/// </summary>
public sealed class BlobatarScene
{
    readonly Eye[] _eyes;
    readonly IdleSeeds _seeds;
    readonly Palette _palette;
    readonly Morph _morph = new();
    readonly GazeState _gaze = new();
    double _lastTick;

    /// <summary>The face disc, surveyed from the neutral geometry on construction. Null means gaze leaves the eyes where they are.</summary>
    public GazeFace? Face { get; set; }

    /// <param name="neutral">A model built <b>without</b> an expression: the pose arrives as transforms.</param>
    public BlobatarScene(BlobatarModel neutral, string seed, bool normalize, Expression expression)
    {
        _eyes = neutral.Layout.Eyes;
        _palette = neutral.Palette;
        _seeds = IdleSeeds.FromSeed(seed, normalize);
        _morph.Snap(expression, _palette);
        Face = Gaze.Survey(neutral);
    }

    /// <summary>Morphs from wherever the face is now: 300 ms in, 400 ms back to idle.</summary>
    public void SetExpression(Expression e, double now) => _morph.Retarget(e, _palette, now);

    /// <summary>Call when frames stop arriving (hidden), so the next one is not read as a long gap.</summary>
    public void Pause() => _lastTick = 0;

    /// <summary>Advances the gaze one frame. <paramref name="dx"/>, <paramref name="dy"/> are the aim minus the face centre.</summary>
    /// <param name="side">Drawn side length, for the near-field ease and write threshold.</param>
    public void Tick(double now, GazeAim aim, double dx, double dy, double side, double travel)
    {
        var dt = _lastTick > 0 ? Math.Min(now - _lastTick, 64) : 16;
        _lastTick = now;
        _gaze.Aim(aim);
        _gaze.Frame(dt, dx, dy, Math.Max(1, side / 2), Math.Max(1, side), travel);
    }

    public SceneFrame Frame(double now, double travel)
    {
        var (pose, head, eye) = _morph.Sample(now);
        // The gaze stands the idle glances down; its projection rides on top of each eye's pose.
        var f = Idle.At(_seeds.Stood(_gaze.Hold), now, 1, pose.Shake);
        var tr = Idle.Transforms(_eyes, pose, f, _gaze.Eyes(Face, travel, _eyes.Length));

        var body = tr.Root.ToAffine() * tr.Breathe.ToAffine() * tr.Bob.ToAffine();
        var eyes = body * tr.Eyes.ToAffine();
        var each = new Affine[_eyes.Length];
        for (var i = 0; i < each.Length; i++) each[i] = eyes * tr.Eye[i].ToAffine() * tr.Glance[i].ToAffine();
        return new SceneFrame(body, each, head, eye);
    }
}
