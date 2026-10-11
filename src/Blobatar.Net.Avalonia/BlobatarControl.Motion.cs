using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Blobatar.Net.Core;

namespace Blobatar.Net.Avalonia;

public partial class BlobatarControl
{
    bool _animated, _running;
    (Geometry Geometry, IBrush Brush)? _backdrop;
    readonly List<Geometry> _head = new();
    Geometry[] _eyeGeometry = Array.Empty<Geometry>();
    BlobatarScene? _scene;
    TopLevel? _top;
    Point _pointer;
    bool _pointerIn;

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

    void OnExpressionChanged()
    {
        if (_animated) _scene?.SetExpression(Current, Clock.Now);
        else Rebuild();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateClock();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UpdateClock(detaching: true);
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>Runs one frame loop per animated control while it is in a window.</summary>
    void UpdateClock(bool detaching = false)
    {
        var top = detaching ? null : TopLevel.GetTopLevel(this);
        var want = top is not null && Animate;
        if (want == _running) return;

        if (want)
        {
            _top = top;
            _running = true;
            top!.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
            top.AddHandler(InputElement.PointerExitedEvent, OnPointerExited, RoutingStrategies.Direct, true);
            top.RequestAnimationFrame(OnFrame);
        }
        else
        {
            _running = false;
            if (_top is { } t)
            {
                t.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
                t.RemoveHandler(InputElement.PointerExitedEvent, OnPointerExited);
            }
            _top = null;
            _pointerIn = false;
            _scene?.Pause();
        }
    }

    void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_top is null) return;
        _pointer = e.GetPosition(_top);
        _pointerIn = true;
    }

    void OnPointerExited(object? sender, PointerEventArgs e) => _pointerIn = false;

    void OnFrame(TimeSpan _)
    {
        if (!_running || _top is null) return;
        OnTick(Clock.Now);
        _top?.RequestAnimationFrame(OnFrame);
    }

    void OnTick(double now)
    {
        if (_scene is null) return;
        if (!IsEffectivelyVisible) { _scene.Pause(); return; }
        var aim = Aim(out var dx, out var dy);
        _scene.Tick(now, aim, dx, dy, Math.Min(Bounds.Width, Bounds.Height), GazeTravel);
        InvalidateVisual();
    }

    /// <summary>The aim for this frame, plus where it is relative to the face centre.</summary>
    GazeAim Aim(out double dx, out double dy)
    {
        dx = dy = 0;
        var centre = new Point(Bounds.Width / 2, Bounds.Height / 2);
        if (GazeTarget is { } target)
        {
            if (target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), this) is not { } c)
                return GazeAim.Rest; // not in the same visual tree
            (dx, dy) = (c.X - centre.X, c.Y - centre.Y);
            return GazeAim.Look;
        }
        switch (Gaze)
        {
            case GazeMode.Pointer:
                // Outside the window the eyes come home (the original documents this; its parked-pointer maths does not quite do it).
                if (!_pointerIn || _top is null || this.TranslatePoint(new Point(0, 0), _top) is not { } origin) return GazeAim.Rest;
                (dx, dy) = (_pointer.X - origin.X - centre.X, _pointer.Y - origin.Y - centre.Y);
                return GazeAim.Look;
            case GazeMode.Rest: return GazeAim.Rest;
            default: return GazeAim.None;
        }
    }

    void DrawAnimated(DrawingContext context)
    {
        if (_backdrop is { } bg) context.DrawGeometry(bg.Brush, null, bg.Geometry);
        if (_scene is null) return;

        var frame = _scene.Frame(Clock.Now, GazeTravel);
        var head = Fill(frame.Head);
        using (context.PushTransform(ToMatrix(frame.Body)))
        {
            foreach (var g in _head) context.DrawGeometry(head, null, g);
        }

        var eye = Fill(frame.Eye);
        for (var i = 0; i < _eyeGeometry.Length; i++)
        {
            using (context.PushTransform(ToMatrix(frame.Eyes[i]))) context.DrawGeometry(eye, null, _eyeGeometry[i]);
        }
    }
}
