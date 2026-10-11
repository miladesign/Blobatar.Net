using System.Text.Json;
using Blobatar.Net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blobatar.Net.Tests;

/// <summary>
/// golden2.json is the original JS evaluated for expressions, idle frames, pose lerps,
/// colour fades and easing. The C# port must reproduce it exactly (strings) or to 1e-9 (numbers).
/// </summary>
[TestClass]
public class MotionTests
{
    static readonly JsonElement Golden = JsonDocument.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "golden2.json"))).RootElement;

    static Expression Named(string n) => Expressions.All.Single(e => e.Name == n);
    static bool Near(double a, double b) => Math.Abs(a - b) < 1e-9;

    static Pose ReadPose(JsonElement p) => new(
        p.GetProperty("esx").GetDouble(), p.GetProperty("esy").GetDouble(), p.GetProperty("tilt").GetDouble(),
        p.GetProperty("edy").GetDouble(), p.GetProperty("edx").GetDouble(), p.GetProperty("esx2").GetDouble(),
        p.GetProperty("esy2").GetDouble(), p.GetProperty("tilt2").GetDouble(), p.GetProperty("edy2").GetDouble(),
        p.GetProperty("lock").GetDouble(), p.GetProperty("heat").GetDouble(), p.GetProperty("shake").GetDouble(),
        p.GetProperty("rock").GetDouble(), p.GetProperty("bdy").GetDouble());

    [TestMethod]
    public void Expression_svg_matches_js()
    {
        var bad = new List<string>();
        foreach (var c in Golden.GetProperty("expr").EnumerateArray())
        {
            var seed = c.GetProperty("seed").GetString()!;
            var name = c.GetProperty("name").GetString()!;
            var actual = BlobatarSvg.Render(seed, new BlobatarOptions { Expression = Named(name) });
            var expected = c.GetProperty("svg").GetString()!;
            if (actual != expected) bad.Add($"{name}/{seed}\n js: {expected}\n c#: {actual}");
        }
        Assert.IsTrue(bad.Count == 0, $"{bad.Count} mismatches:\n" + string.Join("\n", bad.Take(2)));
    }

    [TestMethod]
    public void Idle_seeds_match_js()
    {
        foreach (var c in Golden.GetProperty("seeds").EnumerateArray())
        {
            var s = IdleSeeds.FromSeed(c.GetProperty("seed").GetString()!);
            var j = c.GetProperty("seeds");
            Assert.AreEqual(j.GetProperty("phase").GetDouble(), s.Phase);
            Assert.AreEqual(j.GetProperty("bob").GetDouble(), s.Bob);
            Assert.AreEqual(j.GetProperty("blink").GetDouble(), s.Blink);
            Assert.AreEqual(j.GetProperty("blinkPhase").GetDouble(), s.BlinkPhase);
            Assert.AreEqual(j.GetProperty("saccade").GetDouble(), s.Saccade);
            Assert.AreEqual(j.GetProperty("saccadePhase").GetDouble(), s.SaccadePhase);
            Assert.AreEqual(j.GetProperty("lookX").GetDouble(), s.LookX);
            Assert.AreEqual(j.GetProperty("lookY").GetDouble(), s.LookY);
            Assert.AreEqual(j.GetProperty("lookMX").GetDouble(), s.LookMX);
            Assert.AreEqual(j.GetProperty("lookMY").GetDouble(), s.LookMY);
        }
    }

    [TestMethod]
    public void Idle_transforms_match_js()
    {
        var bad = new List<string>();
        foreach (var c in Golden.GetProperty("idle").EnumerateArray())
        {
            var seed = c.GetProperty("seed").GetString()!;
            var pose = Named(c.GetProperty("name").GetString()!).P;
            var eyes = BlobatarFactory.Create(seed).Layout.Eyes; // unbaked: the pose rides in the transforms
            var f = Idle.At(IdleSeeds.FromSeed(seed), c.GetProperty("t").GetDouble(), c.GetProperty("amp").GetDouble(), pose.Shake);
            var tr = Idle.Transforms(eyes, pose, f);
            var j = c.GetProperty("tr");

            void Check(string label, string expected, SvgTransform actual)
            {
                if (expected != actual.ToString()) bad.Add($"{seed}/{c.GetProperty("name")}/t={c.GetProperty("t")} {label}\n js: {expected}\n c#: {actual}");
            }
            Check("root", j.GetProperty("root").GetString()!, tr.Root);
            Check("breathe", j.GetProperty("breathe").GetString()!, tr.Breathe);
            Check("bob", j.GetProperty("bob").GetString()!, tr.Bob);
            Check("eyes", j.GetProperty("eyes").GetString()!, tr.Eyes);
            for (var i = 0; i < 2; i++)
            {
                Check($"eye{i}", j.GetProperty("eye")[i].GetString()!, tr.Eye[i]);
                Check($"glance{i}", j.GetProperty("glance")[i].GetString()!, tr.Glance[i]);
            }
        }
        Assert.IsTrue(bad.Count == 0, $"{bad.Count} mismatches:\n" + string.Join("\n", bad.Take(3)));
    }

    [TestMethod]
    public void Pose_lerp_fade_and_easing_match_js()
    {
        foreach (var c in Golden.GetProperty("lerp").EnumerateArray())
        {
            var actual = Pose.Lerp(null, Named(c.GetProperty("name").GetString()!).P, c.GetProperty("u").GetDouble());
            Assert.AreEqual(ReadPose(c.GetProperty("pose")), actual);
        }
        foreach (var c in Golden.GetProperty("fade").EnumerateArray())
            Assert.AreEqual(c.GetProperty("hex").GetString(),
                Colors.FadeHex(c.GetProperty("a").GetString()!, c.GetProperty("b").GetString()!, c.GetProperty("u").GetDouble()));
        foreach (var c in Golden.GetProperty("ease").EnumerateArray())
        {
            var x = c.GetProperty("x").GetDouble();
            Assert.IsTrue(Near(c.GetProperty("io").GetDouble(), Ease.InOut(x)));
            Assert.IsTrue(Near(c.GetProperty("i").GetDouble(), Ease.In(x)));
            Assert.IsTrue(Near(c.GetProperty("o").GetDouble(), Ease.Out(x)));
            Assert.IsTrue(Near(c.GetProperty("min").GetDouble(), Ease.MorphIn.Curve(x)));
        }
    }

    [TestMethod]
    public void Affine_matches_the_transform_string()
    {
        // translate then scale: scale is applied first, then the move.
        var m = new SvgTransform().Translate(10, 20).Scale(2, 3).ToAffine();
        Assert.AreEqual(new Affine(2, 0, 0, 3, 10, 20), m);
        // rotate(90) sends +x to +y (clockwise on a y-down screen).
        var r = new SvgTransform().Rotate(90).ToAffine();
        Assert.IsTrue(Near(r.A, 0) && Near(r.B, 1) && Near(r.C, -1) && Near(r.D, 0));
    }

    [TestMethod]
    public void Idle_is_baked_identically_to_no_expression()
    {
        for (var i = 0; i < 20; i++)
            Assert.AreEqual(BlobatarSvg.Render("seed" + i),
                BlobatarSvg.Render("seed" + i, new BlobatarOptions { Expression = Expressions.Idle }));
    }
}

[TestClass]
public class MorphTests
{
    static readonly Palette Base = Colors.Create(200);

    [TestMethod]
    public void Settles_exactly_on_the_target()
    {
        var m = new Morph();
        m.Snap(Expressions.Idle, Base);
        m.Retarget(Expressions.Mad, Base, 1000);
        var end = m.Sample(1000 + Ease.MorphIn.Ms);
        Assert.AreEqual(Expressions.Mad.P, end.Pose);
        Assert.AreEqual(Expressions.Mad.Apply(Base).Head, end.Head);
    }

    [TestMethod]
    public void Starts_at_the_origin_and_returning_takes_longer()
    {
        var m = new Morph();
        m.Snap(Expressions.Happy, Base);
        m.Retarget(Expressions.Idle, Base, 500);
        Assert.AreEqual(Expressions.Happy.P, m.Sample(500).Pose);
        Assert.AreNotEqual(Pose.Ident, m.Sample(500 + Ease.MorphIn.Ms).Pose); // 300 ms is not enough going back
        Assert.AreEqual(Pose.Ident, m.Sample(500 + Ease.MorphOut.Ms).Pose);
    }

    [TestMethod]
    public void Retargeting_mid_flight_is_continuous()
    {
        var m = new Morph();
        m.Snap(Expressions.Idle, Base);
        m.Retarget(Expressions.Sad, Base, 0);
        var before = m.Sample(150);
        m.Retarget(Expressions.Surprised, Base, 150);
        Assert.AreEqual(before.Pose, m.Sample(150).Pose);
        Assert.AreEqual(before.Head, m.Sample(150).Head);
    }
}
