namespace Blobatar.Net.Core;

/// <summary>Per-seed idle timings, in ms. Same traits as the stylesheet reads on the web.</summary>
public sealed record IdleSeeds(
    double Phase, double Bob, double Blink, double BlinkPhase, double Saccade, double SaccadePhase,
    double LookX, double LookY, double LookMX, double LookMY)
{
    public static IdleSeeds FromTraits(Traits t)
    {
        var blink = Geo.JsRound(t.Num("motion.blink", 3500, 6500));
        var saccade = Geo.JsRound(t.Num("motion.saccade", 4200, 7600));
        var lookX = t.Num("motion.lookX", 1, 2.2);
        var lookY = t.Num("motion.lookY", 0.8, 1.7);
        return new IdleSeeds(
            Geo.JsRound(t.Num("motion.phase", 0, 2800)),
            Geo.JsRound(t.Num("motion.bob", 0, 3400)),
            blink,
            Geo.JsRound(t.Num("motion.blinkPhase", 0, blink)),
            saccade,
            Geo.JsRound(t.Num("motion.saccadePhase", 0, saccade)),
            Geo.R2(lookX) * (t.Bool("motion.lookXFlip") ? -1 : 1),
            Geo.R2(lookY) * (t.Bool("motion.lookYFlip") ? -1 : 1),
            Geo.R2(lookX), Geo.R2(lookY));
    }

    /// <summary>The glances scaled down by the gaze stand-down (0 = untouched, 1 = silent).</summary>
    public IdleSeeds Stood(double hold) => hold <= 0 ? this : this with
    {
        LookX = LookX * (1 - hold), LookY = LookY * (1 - hold), LookMX = LookMX * (1 - hold), LookMY = LookMY * (1 - hold),
    };

    public static IdleSeeds FromSeed(string seed, bool normalize = true, IReadOnlyDictionary<string, double[]>? overrides = null) =>
        FromTraits(new Traits(seed, normalize, overrides));
}

/// <summary>One moment of the idle layer, one field per nesting level.</summary>
public sealed record IdleFrame(
    (double X, double Y) Shake, (double X, double Y) Breathe, double Bob, (double X, double Y) Saccade,
    double Rockp, double Blink, (double Mx, double Side, double Sy, double Rot) Wrap);

/// <summary>A transform per nesting level: root, breathe, bob, eyes, then per eye a pose and a glance transform.</summary>
public sealed record IdleTransforms(
    SvgTransform Root, SvgTransform Breathe, SvgTransform Bob, SvgTransform Eyes,
    SvgTransform[] Eye, SvgTransform[] Glance);

/// <summary>The seven idle loops as a pure function of time, so no state is kept and nothing drifts.</summary>
public static class Idle
{
    const double BreatheMs = 2800, BobMs = 3400, RockMs = 900, ShakeMs = 112;

    static readonly double[][] Saccade =
    {
        new double[] { 0, 0, 0 }, new double[] { 0.15, 0, 0 }, new double[] { 0.165, -0.8, -0.9 }, new double[] { 0.31, -0.8, -0.9 },
        new double[] { 0.325, 1, 0.1 }, new double[] { 0.47, 1, 0.1 }, new double[] { 0.485, -0.15, 0.85 }, new double[] { 0.63, -0.15, 0.85 },
        new double[] { 0.645, 0.75, -0.8 }, new double[] { 0.79, 0.75, -0.8 }, new double[] { 0.805, -1, -0.15 }, new double[] { 0.985, -1, -0.15 },
        new double[] { 1, 0, 0 },
    };

    static readonly double[][] Wrap =
    {
        new double[] { 0, 0, 0, 0, 0 }, new double[] { 0.15, 0, 0, 0, 0 },
        new double[] { 0.165, -0.0176, 0.008, -0.027, 0.648 }, new double[] { 0.31, -0.0176, 0.008, -0.027, 0.648 },
        new double[] { 0.325, -0.022, -0.01, -0.003, 0.09 }, new double[] { 0.47, -0.022, -0.01, -0.003, 0.09 },
        new double[] { 0.485, -0.0033, 0.0015, -0.0255, -0.115 }, new double[] { 0.63, -0.0033, 0.0015, -0.0255, -0.115 },
        new double[] { 0.645, -0.0165, -0.0075, -0.024, -0.54 }, new double[] { 0.79, -0.0165, -0.0075, -0.024, -0.54 },
        new double[] { 0.805, -0.022, 0.01, -0.0045, 0.135 }, new double[] { 0.985, -0.022, 0.01, -0.0045, 0.135 },
        new double[] { 1, 0, 0, 0, 0 },
    };

    static readonly double[][] Shake =
    {
        new double[] { 0, 0.62, -0.34 }, new double[] { 0.25, -0.7, 0.22 }, new double[] { 0.5, 0.38, 0.66 },
        new double[] { 0.75, -0.44, -0.6 }, new double[] { 1, 0.62, -0.34 },
    };

    static double Cycle(double t, double phase, double period)
    {
        var u = (t + phase) / period;
        return u - Math.Floor(u);
    }

    /// <summary>Like a CSS <c>animation-direction: alternate</c> loop, eased by the caller.</summary>
    static double Alternate(double t, double phase, double period)
    {
        var u = (t + phase) / period;
        var n = Math.Floor(u);
        var f = u - n;
        return n % 2 != 0 ? 1 - f : f;
    }

    /// <summary>A value read linearly off a keyframe table, column <paramref name="col"/>.</summary>
    static double Stops(double u, double[][] table, int col)
    {
        for (var i = table.Length - 1; i >= 0; i--)
        {
            var row = table[i];
            if (u < row[0]) continue;
            if (i + 1 >= table.Length) return row[col];
            var next = table[i + 1];
            var span = next[0] - row[0];
            return span <= 0 ? row[col] : row[col] + (next[col] - row[col]) * ((u - row[0]) / span);
        }
        return table[0][col];
    }

    /// <param name="t">Clock in ms.</param>
    /// <param name="amp">Amplitude 0-1; 1 is fully animated.</param>
    /// <param name="shake">The pose's tremor amplitude.</param>
    public static IdleFrame At(IdleSeeds s, double t, double amp, double shake = 0)
    {
        var breathe = Ease.InOut(Alternate(t, s.Phase, BreatheMs));
        var bob = Ease.InOut(Alternate(t, s.Bob, BobMs));
        var sac = Cycle(t, s.SaccadePhase, s.Saccade);
        var sh = Cycle(t, 0, ShakeMs);

        var r = Cycle(t, 0, RockMs);
        var rockp = r < 0.5 ? 1 - 2 * Ease.InOut(r * 2) : -1 + 2 * Ease.InOut(r * 2 - 1);

        var b = Cycle(t, s.BlinkPhase, s.Blink);
        var blink = b < 0.972 ? 1
            : b < 0.986 ? 1 - 0.92 * amp * Ease.In((b - 0.972) / 0.014)
            : 1 - 0.92 * amp * (1 - Ease.Out((b - 0.986) / 0.014));

        return new IdleFrame(
            (Stops(sh, Shake, 1) * shake, Stops(sh, Shake, 2) * shake),
            (1 + 0.022 * amp * breathe, 1 - 0.018 * amp * breathe),
            -1.1 * amp * bob,
            (Stops(sac, Saccade, 1) * s.LookX * amp, Stops(sac, Saccade, 2) * s.LookY * amp),
            rockp, blink,
            (Stops(sac, Wrap, 1) * s.LookMX * amp, Stops(sac, Wrap, 2) * s.LookX * amp,
             Stops(sac, Wrap, 3) * s.LookMY * amp, Stops(sac, Wrap, 4) * s.LookX * s.LookY * amp));
    }

    /// <summary>
    /// Maps a frame onto the element tree. Draw the body under root, breathe, bob; each eye
    /// additionally under eyes, eye[i], and its own shape under glance[i] (which already includes eye[i]'s origin).
    /// </summary>
    public static IdleTransforms Transforms(IReadOnlyList<Eye> eyes, Pose p, IdleFrame f, IReadOnlyList<GazeEye>? gaze = null)
    {
        var eye = new SvgTransform[eyes.Count];
        var glance = new SvgTransform[eyes.Count];
        for (var i = 0; i < eyes.Count; i++)
        {
            var e = eyes[i];
            var side = i == 0 ? -1 : 1;
            eye[i] = p.EyeTransform(e, i, f.Rockp, gaze is null ? null : gaze[i]);
            glance[i] = new SvgTransform()
                .Translate(e.Cx, e.Cy)
                .Rotate(f.Wrap.Rot * side)
                .Scale(1 + f.Wrap.Mx + f.Wrap.Side * side, 1 + f.Wrap.Sy)
                .Rotate(e.Rot)
                .Scale(1, f.Blink)
                .Rotate(-e.Rot)
                .Translate(-e.Cx, -e.Cy);
        }
        return new IdleTransforms(
            new SvgTransform().Translate(f.Shake.X, f.Shake.Y),
            new SvgTransform().Translate(50, 50).Scale(f.Breathe.X, f.Breathe.Y).Translate(-50, -50),
            new SvgTransform().Translate(0, p.Bdy + f.Bob),
            new SvgTransform().Translate(f.Saccade.X, f.Saccade.Y),
            eye, glance);
    }
}
