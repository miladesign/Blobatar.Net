using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Blobatar.Net.Core;

namespace Blobatar.Net.Wpf;

public partial class BlobatarControl
{
    bool _animated, _subscribed;
    (Geometry Geometry, Brush Brush)? _backdrop;
    readonly List<Geometry> _head = new();
    Geometry[] _eyeGeometry = Array.Empty<Geometry>();
    BlobatarScene? _scene;

    void BuildAnimated(BlobatarModel model)
    {
        // Geometry is the neutral face; the pose arrives as transforms every frame.
        _animated = true;
        _backdrop = model.Backdrop is { } bg ? (Path(bg.D), Fill(bg.Fill)) : null;

        _head.Clear();
        var eyeCount = model.Layout.Eyes.Length;
        var headCount = model.Marks.Count - eyeCount;
        _eyeGeometry = new Geometry[eyeCount];
        for (var i = 0; i < model.Marks.Count; i++)
        {
            if (i < headCount) _head.Add(GeometryOf(model.Marks[i]));
            else _eyeGeometry[i - headCount] = GeometryOf(model.Marks[i]);
        }
        _scene = new BlobatarScene(model, Seed ?? "", Normalize, Current);
    }

    /// <summary>The aim for this frame, plus where it is relative to the face centre.</summary>
    GazeAim Aim(out double dx, out double dy)
    {
        dx = dy = 0;
        var centre = new Point(ActualWidth / 2, ActualHeight / 2);
        if (GazeTarget is { } target)
        {
            try
            {
                var c = target.TranslatePoint(new Point(target.RenderSize.Width / 2, target.RenderSize.Height / 2), this);
                (dx, dy) = (c.X - centre.X, c.Y - centre.Y);
                return GazeAim.Look;
            }
            catch (InvalidOperationException) { return GazeAim.Rest; } // not in the same visual tree
        }
        switch (Gaze)
        {
            case GazeMode.Pointer:
                // Outside the window the eyes come home (the original documents this; its parked-pointer maths does not quite do it).
                if (Window.GetWindow(this) is not { } w) return GazeAim.Rest;
                var inWindow = Mouse.GetPosition(w);
                if (inWindow.X < 0 || inWindow.Y < 0 || inWindow.X > w.ActualWidth || inWindow.Y > w.ActualHeight) return GazeAim.Rest;
                var p = Mouse.GetPosition(this);
                (dx, dy) = (p.X - centre.X, p.Y - centre.Y);
                return GazeAim.Look;
            case GazeMode.Rest: return GazeAim.Rest;
            default: return GazeAim.None;
        }
    }

    void OnExpressionChanged()
    {
        if (_animated) _scene?.SetExpression(Current, BlobatarClock.Now);
        else Rebuild();
    }

    void UpdateClock()
    {
        var want = IsLoaded && Animating;
        if (want == _subscribed) return;
        if (want) BlobatarClock.Subscribe(OnTick); else BlobatarClock.Unsubscribe(OnTick);
        _subscribed = want;
    }

    void OnTick(double now)
    {
        if (_scene is null) return;
        if (!IsVisible) { _scene.Pause(); return; }
        var aim = Aim(out var dx, out var dy);
        _scene.Tick(now, aim, dx, dy, Math.Min(ActualWidth, ActualHeight), GazeTravel);
        InvalidateVisual();
    }

    static MatrixTransform Xform(Affine a)
    {
        var t = new MatrixTransform(ToMatrix(a));
        t.Freeze();
        return t;
    }

    void DrawAnimated(DrawingContext dc)
    {
        if (_backdrop is { } bg) dc.DrawGeometry(bg.Brush, null, bg.Geometry);
        if (_scene is null) return;

        var frame = _scene.Frame(BlobatarClock.Now, GazeTravel);
        var head = Fill(frame.Head);
        dc.PushTransform(Xform(frame.Body));
        foreach (var g in _head) dc.DrawGeometry(head, null, g);
        dc.Pop();

        var eye = Fill(frame.Eye);
        for (var i = 0; i < _eyeGeometry.Length; i++)
        {
            dc.PushTransform(Xform(frame.Eyes[i]));
            dc.DrawGeometry(eye, null, _eyeGeometry[i]);
            dc.Pop();
        }
    }
}
