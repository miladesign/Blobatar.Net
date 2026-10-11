using System.Text.Json;
using Blobatar.Net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blobatar.Net.Tests;

/// <summary>golden3.json is the original JS gaze maths. Numbers must agree to 1e-9.</summary>
[TestClass]
public class GazeTests
{
    static readonly JsonElement Golden = JsonDocument.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "golden3.json"))).RootElement;

    static bool Near(double a, double b) => Math.Abs(a - b) < 1e-9;
    static double D(JsonElement e, string p) => e.GetProperty(p).GetDouble();

    [TestMethod]
    public void Projection_matches_js()
    {
        foreach (var c in Golden.GetProperty("project").EnumerateArray())
        {
            var m = c.GetProperty("m");
            var p = Gaze.Project(new GazeMark(D(m, "x"), D(m, "y")), D(c, "yaw"), D(c, "pitch"));
            var j = c.GetProperty("p");
            Assert.IsTrue(Near(D(j, "dx"), p.Dx) && Near(D(j, "dy"), p.Dy) && Near(D(j, "sx"), p.Sx)
                          && Near(D(j, "sy"), p.Sy) && Near(D(j, "t"), p.T), $"project {c}");
        }
    }

    [TestMethod]
    public void Step_matches_js()
    {
        foreach (var c in Golden.GetProperty("step").EnumerateArray())
        {
            var i = c.GetProperty("i");
            var gain = i.TryGetProperty("gain", out var g) ? g.GetDouble() : 1;
            var snap = i.TryGetProperty("snap", out var sn) ? sn.GetDouble() : Gaze.Snap;
            var r = Gaze.Step(D(i, "x"), D(i, "y"), D(i, "dx"), D(i, "dy"), D(i, "radius"), D(i, "k"), gain, snap);
            var j = c.GetProperty("r");
            Assert.IsTrue(Near(D(j, "x"), r.X) && Near(D(j, "y"), r.Y) && Near(D(j, "tx"), r.Tx)
                          && Near(D(j, "ty"), r.Ty) && Near(D(j, "f"), r.F), $"step {c}");
        }
    }

    [TestMethod]
    public void Pursuit_and_threshold_match_js()
    {
        foreach (var c in Golden.GetProperty("pursuit").EnumerateArray())
        {
            var settle = c.TryGetProperty("settle", out var s) ? s.GetDouble() : Gaze.Settle;
            Assert.IsTrue(Near(D(c, "v"), Gaze.Pursuit(D(c, "dt"), settle)));
        }
        foreach (var c in Golden.GetProperty("threshold").EnumerateArray())
            Assert.IsTrue(Near(D(c, "v"), Gaze.Threshold(D(c, "w"), D(c, "t"))));
    }

    [TestMethod]
    public void Survey_of_a_round_head_finds_the_face_disc()
    {
        // 60-unit circle at (50, 50) with two 6x12 eyes at (42, 48) and (58, 48).
        bool Inside(double x, double y) => (x - 50) * (x - 50) + (y - 50) * (y - 50) <= 900;
        var face = Gaze.Survey((20, 20, 60, 60), new[] { (39.0, 42.0, 6.0, 12.0), (55.0, 42.0, 6.0, 12.0) }, Inside)!;
        Assert.AreEqual(2, face.Marks.Length);
        Assert.IsTrue(Math.Abs(face.Rx - 27) < 0.05 && Math.Abs(face.Ry - 24) < 0.05, $"{face.Rx} {face.Ry}");
        Assert.IsTrue(Math.Abs(face.Marks[0].X - -8 / 27.0) < 0.01 && Math.Abs(face.Marks[0].Y - -2 / 24.0) < 0.01);
        Assert.IsTrue(Math.Abs(face.Marks[1].X - 8 / 27.0) < 0.01);
    }

    [TestMethod]
    public void Survey_shrinks_the_disc_for_a_head_that_is_not_an_ellipse()
    {
        // A triangle fills far less of its bounding ellipse than a circle does.
        bool Tri(double x, double y) => y >= 20 && y <= 80 && Math.Abs(x - 50) <= (y - 20) / 2;
        var face = Gaze.Survey((20, 20, 60, 60), new[] { (45.0, 60.0, 6.0, 12.0) }, Tri)!;
        Assert.IsTrue(face.Rx < 27 && face.Ry < 24);
    }

    [TestMethod]
    public void Looking_far_right_settles_exactly_on_the_unit_direction()
    {
        var g = new GazeState();
        g.Aim(GazeAim.Look);
        for (var i = 0; i < 120; i++) g.Frame(16, 500, 0, 50, 100, 3);
        Assert.AreEqual(1.0, g.X);
        Assert.AreEqual(0.0, g.Y);
        Assert.AreEqual(1.0, g.Hold);
    }

    [TestMethod]
    public void Aim_on_top_of_the_face_eases_to_centre()
    {
        var g = new GazeState();
        g.Aim(GazeAim.Look);
        for (var i = 0; i < 120; i++) g.Frame(16, 2, 1, 50, 100, 3);
        Assert.IsTrue(Math.Sqrt(g.X * g.X + g.Y * g.Y) < 0.2);
    }

    [TestMethod]
    public void Rest_holds_the_stand_down_and_none_releases_it()
    {
        var g = new GazeState();
        g.Aim(GazeAim.Rest);
        for (var i = 0; i < 120; i++) g.Frame(16, 500, 0, 50, 100, 3);
        Assert.AreEqual(0.0, g.X);
        Assert.AreEqual(1.0, g.Hold);
        g.Aim(GazeAim.None);
        for (var i = 0; i < 120; i++) g.Frame(16, 500, 0, 50, 100, 3);
        Assert.AreEqual(0.0, g.Hold);
    }

    [TestMethod]
    public void A_far_jump_is_a_saccade_not_a_glide()
    {
        var g = new GazeState();
        g.Aim(GazeAim.Look);
        for (var i = 0; i < 120; i++) g.Frame(16, -500, 0, 50, 100, 3); // settle at -1
        g.Frame(16, 500, 0, 50, 100, 3);                                // target now +1: 2 units away > snap
        Assert.AreEqual(1.0, g.X);
    }

    [TestMethod]
    public void No_gaze_leaves_the_idle_transforms_untouched()
    {
        var eyes = BlobatarFactory.Create("alain").Layout.Eyes;
        var f = Idle.At(IdleSeeds.FromSeed("alain"), 1500, 1);
        var plain = Idle.Transforms(eyes, Pose.Ident, f);
        var neutral = Idle.Transforms(eyes, Pose.Ident, f, new[] { GazeEye.None, GazeEye.None });
        for (var i = 0; i < 2; i++) Assert.AreEqual(plain.Eye[i].ToString(), neutral.Eye[i].ToString());
        Assert.AreEqual(IdleSeeds.FromSeed("alain"), IdleSeeds.FromSeed("alain").Stood(0));
    }
}
