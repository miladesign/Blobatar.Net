using Blobatar.Net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blobatar.Net.Tests;

[TestClass]
public class SceneTests
{
    static BlobatarScene Make(string seed, Expression e) =>
        new(BlobatarFactory.Create(seed), seed, true, e);

    [TestMethod]
    public void Frame_composes_the_same_levels_as_the_idle_transforms()
    {
        var model = BlobatarFactory.Create("alain");
        var scene = Make("alain", Expressions.Idle);
        var now = 1500.0;

        var f = Idle.At(IdleSeeds.FromSeed("alain"), now, 1, 0);
        var tr = Idle.Transforms(model.Layout.Eyes, Pose.Ident, f);
        var body = tr.Root.ToAffine() * tr.Breathe.ToAffine() * tr.Bob.ToAffine();

        var frame = scene.Frame(now, 3);
        Assert.AreEqual(body, frame.Body);
        Assert.AreEqual(2, frame.Eyes.Length);
        Assert.AreEqual(body * tr.Eyes.ToAffine() * tr.Eye[0].ToAffine() * tr.Glance[0].ToAffine(), frame.Eyes[0]);
        Assert.AreEqual(model.Palette.Head, frame.Head);
    }

    [TestMethod]
    public void Expression_morph_arrives_with_its_tint()
    {
        var scene = Make("alain", Expressions.Idle);
        scene.SetExpression(Expressions.Mad, 1000);
        var end = scene.Frame(1000 + Ease.MorphIn.Ms, 3);
        Assert.AreEqual(Expressions.Mad.Apply(BlobatarFactory.Create("alain").Palette).Head, end.Head);
    }

    [TestMethod]
    public void Looking_moves_the_eyes_and_resting_does_not()
    {
        var look = Make("alain", Expressions.Idle);
        var rest = Make("alain", Expressions.Idle);
        look.Face = rest.Face = Gaze.Survey((20, 20, 60, 60), new[] { (39.0, 42.0, 6.0, 12.0), (55.0, 42.0, 6.0, 12.0) },
            (x, y) => (x - 50) * (x - 50) + (y - 50) * (y - 50) <= 900);
        for (var t = 16.0; t < 2000; t += 16)
        {
            look.Tick(t, GazeAim.Look, 400, 0, 100, 3);
            rest.Tick(t, GazeAim.Rest, 400, 0, 100, 3);
        }
        var l = look.Frame(2000, 3).Eyes[0];
        var r = rest.Frame(2000, 3).Eyes[0];
        Assert.AreNotEqual(l, r);
        Assert.IsTrue(l.E > r.E, "looking right shifts the eye right");
    }

    [TestMethod]
    public void Pause_makes_the_next_tick_a_normal_frame()
    {
        var a = Make("alain", Expressions.Idle);
        var b = Make("alain", Expressions.Idle);
        a.Tick(100, GazeAim.None, 0, 0, 100, 3);
        a.Pause();
        a.Tick(60_000, GazeAim.Look, 400, 0, 100, 3);   // a long hidden gap
        b.Tick(60_000, GazeAim.Look, 400, 0, 100, 3);   // first-ever tick
        Assert.AreEqual(b.Frame(60_000, 3).Eyes[0], a.Frame(60_000, 3).Eyes[0]);
    }

    [TestMethod]
    public void Expression_kinds_line_up_with_the_roster()
    {
        foreach (var k in Enum.GetValues<ExpressionKind>())
            Assert.AreEqual(k.ToString().ToLowerInvariant(), Expressions.Of(k).Name);
    }
}
