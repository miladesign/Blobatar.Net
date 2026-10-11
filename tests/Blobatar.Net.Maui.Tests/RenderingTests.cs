using System.Numerics;
using System.Reflection;
using Blobatar.Net.Core;
using Blobatar.Net.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blobatar.Net.Maui.Tests;

[TestClass]
public class RenderingTests
{
    [TestMethod]
    public void StaticPosesDrawCoreMarksAndLeaveCanvasStateBalanced()
    {
        foreach (var kind in Enum.GetValues<ExpressionKind>())
        {
            var avatar = new BlobatarControl { Seed = "rendering@example.com", Expression = kind, Backdrop = BackdropStyle.Circle };
            var model = BlobatarFactory.Create(avatar.Seed, new BlobatarOptions { Expression = Expressions.Of(kind), Background = avatar.Backdrop });
            var canvas = DispatchProxy.Create<ICanvas, RecordingCanvas>();
            var recording = (RecordingCanvas)(object)canvas;
            ((IDrawable)avatar).Draw(canvas, new RectF(0, 0, 200, 100));
            var fills = recording.Calls.Where(c => c.Method is "FillPath" or "FillCircle").ToArray();
            Assert.HasCount(model.Marks.Count + 1, fills, kind.ToString());
            Assert.AreEqual("FillPath", fills[0].Method);
            Assert.AreEqual(Color.FromArgb(model.Backdrop!.Fill), recording.Colors[0]);
            CollectionAssert.AreEqual(model.Marks.Select(m => Color.FromArgb(m.Fill)).ToArray(), recording.Colors.Skip(1).ToArray());
            foreach (var path in fills.Where(c => c.Method == "FillPath"))
                Assert.AreEqual(WindingMode.NonZero, path.Args[1]);
            Assert.AreEqual(recording.Calls.Count(c => c.Method == "SaveState"), recording.Calls.Count(c => c.Method == "RestoreState"));
            Assert.AreEqual(50f, recording.Calls.Single(c => c.Method == "Translate").Args[0]);
            var wrap = (Matrix3x2)recording.Calls.First(c => c.Method == "ConcatenateTransform").Args[0]!;
            Assert.AreEqual((float)Expressions.Of(kind).P.Bdy, wrap.M32);
        }
    }

    [TestMethod]
    public void AnimatedExpressionAndPaletteChangesKeepGeometryValid()
    {
        var avatar = new BlobatarControl { Seed = "animated@example.com", Animate = true };
        foreach (var kind in Enum.GetValues<ExpressionKind>())
        {
            avatar.Expression = kind;
            avatar.Hue = 200;
            var canvas = DispatchProxy.Create<ICanvas, RecordingCanvas>();
            var recording = (RecordingCanvas)(object)canvas;
            ((IDrawable)avatar).Draw(canvas, new RectF(0, 0, 96, 96));
            Assert.IsTrue(recording.Colors.Count >= 3);
            foreach (var call in recording.Calls.Where(c => c.Method == "ConcatenateTransform"))
            {
                var m = (Matrix3x2)call.Args[0]!;
                Assert.IsTrue(float.IsFinite(m.M11) && float.IsFinite(m.M12) && float.IsFinite(m.M21)
                    && float.IsFinite(m.M22) && float.IsFinite(m.M31) && float.IsFinite(m.M32));
            }
        }
        avatar.Animate = false;
        avatar.GazeTravel = -1;
        Assert.AreEqual(3.0, avatar.GazeTravel, "Invalid gaze travel must leave the previous value intact.");
    }
}

public class RecordingCanvas : DispatchProxy
{
    public List<(string Method, object?[] Args)> Calls { get; } = new();
    public List<Color> Colors { get; } = new();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod!;
        Calls.Add((method.Name, args ?? Array.Empty<object?>()));
        if (method.Name == "set_FillColor") Colors.Add((Color)args![0]!);
        return method.ReturnType == typeof(void) ? null : method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
    }
}
