using Blobatar.Net.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blobatar.Net.Tests;

[TestClass]
public class PathTests
{
    static bool Near(double a, double b, double tol = 0.05) => Math.Abs(a - b) < tol;

    [TestMethod]
    public void Superellipse_parses_to_four_cubics_and_closes()
    {
        var segs = PathData.Parse(Geo.Superellipse(50, 50, 30, 20, 2));
        CollectionAssert.AreEqual(
            new[] { SegmentKind.Move, SegmentKind.Cubic, SegmentKind.Cubic, SegmentKind.Cubic, SegmentKind.Cubic, SegmentKind.Close },
            segs.Select(s => s.Kind).ToArray());
        Assert.IsTrue(Near(segs[4].X, segs[0].X) && Near(segs[4].Y, segs[0].Y), "last cubic returns to the start");
    }

    [TestMethod]
    public void Horizontal_and_vertical_commands_become_lines()
    {
        var segs = PathData.Parse(Geo.Box(50, 50, 20, 10));
        Assert.IsTrue(segs.Where(s => s.Kind == SegmentKind.Line).Count() == 3);
        var shape = new PathShape(Geo.Box(50, 50, 20, 10));
        Assert.IsTrue(Near(shape.Bounds.X, 30) && Near(shape.Bounds.Y, 40) && Near(shape.Bounds.W, 40) && Near(shape.Bounds.H, 20));
    }

    [TestMethod]
    public void Unsupported_commands_are_rejected()
    {
#if NET6_0 || NET7_0
        Assert.ThrowsException<FormatException>(() => PathData.Parse("M0 0A1 1 0 0 1 2 2Z"));
#else
        Assert.ThrowsExactly<FormatException>(() => PathData.Parse("M0 0A1 1 0 0 1 2 2Z"));
#endif
    }

    [TestMethod]
    public void Bounds_and_hit_test_follow_the_outline()
    {
        var s = new PathShape(Geo.Superellipse(50, 50, 30, 20, 2));
        Assert.IsTrue(Near(s.Bounds.X, 20) && Near(s.Bounds.Y, 30) && Near(s.Bounds.W, 60) && Near(s.Bounds.H, 40));
        Assert.IsTrue(s.Contains(50, 50) && s.Contains(78, 50) && s.Contains(50, 48));
        Assert.IsFalse(s.Contains(82, 50) || s.Contains(5, 5) || s.Contains(50, 75) || s.Contains(76, 66));
    }

    [TestMethod]
    public void Circles_and_polygons_hit_test()
    {
        Assert.IsTrue(PathShape.FromMark(new Mark(null, 10, 10, 5, "#000")).Contains(12, 12));
        Assert.IsFalse(PathShape.FromMark(new Mark(null, 10, 10, 5, "#000")).Contains(16, 10));
        var hex = new PathShape(Geo.Polygon(50, 50, 30, 30, 6, 0.3));
        Assert.IsTrue(hex.Contains(50, 50));
        Assert.IsFalse(hex.Contains(50, 90));
    }

    [TestMethod]
    public void Every_shape_parses_and_surveys_to_a_sane_face()
    {
        var shapes = new HashSet<string>();
        for (var i = 0; i < 400; i++)
        {
            var model = BlobatarFactory.Create("seed" + i);
            shapes.Add(model.Layout.Shape);
            foreach (var m in model.Marks) _ = PathShape.FromMark(m);

            var face = Gaze.Survey(model);
            Assert.IsNotNull(face, model.Layout.Shape);
            Assert.AreEqual(2, face!.Marks.Length);
            Assert.IsTrue(face.Rx >= 1 && face.Ry >= 1 && double.IsFinite(face.Rx) && double.IsFinite(face.Ry));
            foreach (var mk in face.Marks) Assert.IsTrue(Math.Abs(mk.X) < 1.5 && Math.Abs(mk.Y) < 1.5, $"{model.Layout.Shape}: {mk}");
        }
        Assert.AreEqual(10, shapes.Count);
    }

    [TestMethod]
    public void A_round_blob_surveys_close_to_the_hand_computed_disc()
    {
        // Same circle the hand-built survey test uses: radius 30 at (50, 50), eyes 6 x 12 at (42, 48) and (58, 48).
        var face = Gaze.Survey(
            (20, 20, 60, 60),
            new[] { (39.0, 42.0, 6.0, 12.0), (55.0, 42.0, 6.0, 12.0) },
            (x, y) => new PathShape(Geo.Superellipse(50, 50, 30, 30, 2)).Contains(x, y))!;
        Assert.IsTrue(Math.Abs(face.Rx - 27) < 0.5 && Math.Abs(face.Ry - 24) < 0.5, $"{face.Rx} {face.Ry}");
    }
}
