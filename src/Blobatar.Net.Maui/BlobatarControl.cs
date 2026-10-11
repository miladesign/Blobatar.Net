using System.Numerics;
using Blobatar.Net.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;

[assembly: XmlnsDefinition("https://github.com/MilaDesign/Blobatar.Net/maui", "Blobatar.Net.Maui")]
[assembly: Microsoft.Maui.Controls.XmlnsPrefix("https://github.com/MilaDesign/Blobatar.Net/maui", "bl")]

namespace Blobatar.Net.Maui;

/// <summary>A vector avatar with expressions, idle animation and pointer/touch gaze.</summary>
public sealed class BlobatarControl : GraphicsView, IDrawable
{
    public static readonly BindableProperty SeedProperty = Property(nameof(Seed), "");
    public static readonly BindableProperty HueProperty = Property<double?>(nameof(Hue), null);
    public static readonly BindableProperty ToneProperty = Property<double?>(nameof(Tone), null);
    public static readonly BindableProperty BackdropProperty = Property(nameof(Backdrop), BackdropStyle.None);
    public static readonly BindableProperty NormalizeProperty = Property(nameof(Normalize), true);
    public static readonly BindableProperty EnforceContrastProperty = Property(nameof(EnforceContrast), true);
    public static readonly BindableProperty ExpressionProperty = BindableProperty.Create(nameof(Expression), typeof(ExpressionKind), typeof(BlobatarControl), ExpressionKind.Idle,
        propertyChanged: (b, _, _) => ((BlobatarControl)b).ChangeExpression());
    public static readonly BindableProperty AnimateProperty = BindableProperty.Create(nameof(Animate), typeof(bool), typeof(BlobatarControl), false,
        propertyChanged: (b, _, _) => { var control = (BlobatarControl)b; control.Rebuild(); control.UpdateTimer(); });
    public static readonly BindableProperty GazeProperty = BindableProperty.Create(nameof(Gaze), typeof(GazeMode), typeof(BlobatarControl), GazeMode.None,
        propertyChanged: (b, _, _) => ((BlobatarControl)b).UpdateTimer());
    public static readonly BindableProperty GazeTargetProperty = BindableProperty.Create(nameof(GazeTarget), typeof(VisualElement), typeof(BlobatarControl), null,
        propertyChanged: (b, _, _) => ((BlobatarControl)b).UpdateTimer());
    public static readonly BindableProperty GazeTravelProperty = BindableProperty.Create(nameof(GazeTravel), typeof(double), typeof(BlobatarControl), 3.0,
        validateValue: (_, value) => value is double travel && double.IsFinite(travel) && travel >= 0,
        propertyChanged: (b, _, _) => ((BlobatarControl)b).Invalidate());

    static BindableProperty Property<T>(string name, T value) => BindableProperty.Create(name, typeof(T), typeof(BlobatarControl), value,
        propertyChanged: (b, _, _) => ((BlobatarControl)b).Rebuild());

    public string Seed { get => (string)GetValue(SeedProperty); set => SetValue(SeedProperty, value); }
    public double? Hue { get => (double?)GetValue(HueProperty); set => SetValue(HueProperty, value); }
    public double? Tone { get => (double?)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public BackdropStyle Backdrop { get => (BackdropStyle)GetValue(BackdropProperty); set => SetValue(BackdropProperty, value); }
    public bool Normalize { get => (bool)GetValue(NormalizeProperty); set => SetValue(NormalizeProperty, value); }
    public bool EnforceContrast { get => (bool)GetValue(EnforceContrastProperty); set => SetValue(EnforceContrastProperty, value); }
    public ExpressionKind Expression { get => (ExpressionKind)GetValue(ExpressionProperty); set => SetValue(ExpressionProperty, value); }
    public bool Animate { get => (bool)GetValue(AnimateProperty); set => SetValue(AnimateProperty, value); }
    /// <summary>Pointer follows the mouse across the page, or touch within this control. Requires Animate.</summary>
    public GazeMode Gaze { get => (GazeMode)GetValue(GazeProperty); set => SetValue(GazeProperty, value); }
    /// <summary>A visual element in the same window whose centre the eyes follow.</summary>
    public VisualElement? GazeTarget { get => (VisualElement?)GetValue(GazeTargetProperty); set => SetValue(GazeTargetProperty, value); }
    public double GazeTravel { get => (double)GetValue(GazeTravelProperty); set => SetValue(GazeTravelProperty, value); }

    readonly List<(Mark Mark, PathF? Path, Color Fill)> _marks = new();
    (PathF Path, Color Fill)? _backdrop;
    BlobatarScene? _scene;
    Affine _wrap = Affine.Identity;
    int _headCount;
    IDispatcherTimer? _timer;
    Window? _window;
    WindowPointer? _windowPointer;
    bool _loaded, _paused, _hasPointer;
    PointF _pointer;

    public BlobatarControl()
    {
        WidthRequest = HeightRequest = 48;
        Drawable = this;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        StartInteraction += OnPointer;
        DragInteraction += OnPointer;
        EndInteraction += (_, _) => _hasPointer = false;
        CancelInteraction += (_, _) => _hasPointer = false;
        Rebuild();
    }

    void Rebuild()
    {
        var expression = Expressions.Of(Expression);
        var model = BlobatarFactory.Create(Seed ?? "", new BlobatarOptions
        {
            Hue = Hue, Tone = Tone, Background = Backdrop, Normalize = Normalize,
            Contrast = EnforceContrast, Expression = Animate ? null : expression,
        });
        _marks.Clear();
        foreach (var mark in model.Marks)
            _marks.Add((mark, mark.IsCircle ? null : MakePath(mark.D!), Color.FromArgb(mark.Fill)));
        _backdrop = model.Backdrop is { } bg ? (MakePath(bg.D), Color.FromArgb(bg.Fill)) : null;
        _headCount = model.Marks.Count - model.Layout.Eyes.Length;
        _wrap = new SvgTransform().Translate(0, expression.P.Bdy).ToAffine();
        _scene = Animate ? new BlobatarScene(model, Seed ?? "", Normalize, expression) : null;
        Invalidate();
    }

    void ChangeExpression()
    {
        if (_scene is { } scene) { scene.SetExpression(Expressions.Of(Expression), Clock.Now); Invalidate(); }
        else Rebuild();
    }

    static PathF MakePath(string data)
    {
        var path = new PathF();
        foreach (var s in PathData.Parse(data))
        {
            switch (s.Kind)
            {
                case SegmentKind.Move: path.MoveTo((float)s.X, (float)s.Y); break;
                case SegmentKind.Line: path.LineTo((float)s.X, (float)s.Y); break;
                case SegmentKind.Cubic: path.CurveTo((float)s.X1, (float)s.Y1, (float)s.X2, (float)s.Y2, (float)s.X, (float)s.Y); break;
                case SegmentKind.Quad: path.QuadTo((float)s.X1, (float)s.Y1, (float)s.X, (float)s.Y); break;
                case SegmentKind.Close: path.Close(); break;
            }
        }
        return path;
    }

    static void Transform(ICanvas canvas, Affine m) => canvas.ConcatenateTransform(new Matrix3x2(
        (float)m.A, (float)m.B, (float)m.C, (float)m.D, (float)m.E, (float)m.F));

    void IDrawable.Draw(ICanvas canvas, RectF dirtyRect)
    {
        var width = Width > 0 ? Width : dirtyRect.Width;
        var height = Height > 0 ? Height : dirtyRect.Height;
        var side = Math.Min(width, height);
        if (side <= 0) return;
        canvas.SaveState();
        try
        {
            canvas.Translate((float)((width - side) / 2), (float)((height - side) / 2));
            canvas.Scale((float)(side / 100), (float)(side / 100));
            if (_backdrop is { } bg) { canvas.FillColor = bg.Fill; canvas.FillPath(bg.Path, WindingMode.NonZero); }
            SceneFrame? frame = _scene?.Frame(Clock.Now, GazeTravel);
            var head = frame is { } f ? Color.FromArgb(f.Head) : null;
            var eye = frame is { } e ? Color.FromArgb(e.Eye) : null;
            for (var i = 0; i < _marks.Count; i++)
            {
                var (mark, path, fill) = _marks[i];
                canvas.SaveState();
                try
                {
                    Transform(canvas, frame is { } pose ? (i < _headCount ? pose.Body : pose.Eyes[i - _headCount]) : _wrap);
                    canvas.FillColor = frame.HasValue ? (i < _headCount ? head! : eye!) : fill;
                    if (path is not null) canvas.FillPath(path, WindingMode.NonZero);
                    else canvas.FillCircle((float)mark.Cx, (float)mark.Cy, (float)mark.R);
                }
                finally { canvas.RestoreState(); }
            }
        }
        finally { canvas.RestoreState(); }
    }

    void OnLoaded(object? sender, EventArgs e)
    {
        if (_loaded) return;
        _loaded = true;
        _paused = false;
        _window = Window;
        if (_window is not null) { _window.Stopped += OnStopped; _window.Resumed += OnResumed; _window.Deactivated += OnDeactivated; }
        UpdateTimer();
    }

    void OnUnloaded(object? sender, EventArgs e)
    {
        _loaded = false;
        _hasPointer = false;
        if (_window is not null) { _window.Stopped -= OnStopped; _window.Resumed -= OnResumed; _window.Deactivated -= OnDeactivated; }
        _window = null;
        UpdateTimer();
    }

    void OnDeactivated(object? sender, EventArgs e) => _hasPointer = false;
    void OnStopped(object? sender, EventArgs e) { _paused = true; _hasPointer = false; UpdateTimer(); }
    void OnResumed(object? sender, EventArgs e) { _paused = false; UpdateTimer(); }

    void UpdateTimer()
    {
        var trackPointer = Animate && _loaded && !_paused && Gaze == GazeMode.Pointer && GazeTarget is null && _window is not null;
        if (!trackPointer && _windowPointer is not null)
        {
            _windowPointer.Release();
            _windowPointer = null;
        }
        else if (trackPointer && _windowPointer is null)
            _windowPointer = WindowPointer.Acquire(_window!);
        if (!Animate || !_loaded || _paused)
        {
            _timer?.Stop();
            _scene?.Pause();
            return;
        }
        if (_timer is null)
        {
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(16);
            _timer.Tick += OnTick;
        }
        if (!_timer.IsRunning) _timer.Start();
    }

    void OnPointer(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        _pointer = e.Touches[0];
        _hasPointer = true;
    }

    void OnTick(object? sender, EventArgs e)
    {
        if (_scene is not { } scene) return;
        for (Element? element = this; element is not null; element = element.Parent)
            if (element is VisualElement { IsVisible: false }) { scene.Pause(); return; }
        var aim = Aim(out var dx, out var dy);
        scene.Tick(Clock.Now, aim, dx, dy, Math.Min(Width, Height), GazeTravel);
        Invalidate();
    }

    GazeAim Aim(out double dx, out double dy)
    {
        dx = dy = 0;
        if (GazeTarget is { } target)
        {
            if (Window is null || target.Window != Window) return GazeAim.Rest;
            var from = Centre(this);
            var to = Centre(target);
            (dx, dy) = (to.X - from.X, to.Y - from.Y);
            return GazeAim.Look;
        }
        if (Gaze == GazeMode.Pointer && _windowPointer is { } tracker && tracker.TryGetPosition(this, out var pointer))
        {
            (dx, dy) = (pointer.X - Width / 2, pointer.Y - Height / 2);
            return GazeAim.Look;
        }
        if (Gaze == GazeMode.Pointer && _hasPointer)
        {
            (dx, dy) = (_pointer.X - Width / 2, _pointer.Y - Height / 2);
            return GazeAim.Look;
        }
        return Gaze == GazeMode.None ? GazeAim.None : GazeAim.Rest;
    }

    internal static Point Centre(VisualElement view)
    {
        double x = view.Width / 2, y = view.Height / 2;
        for (Element? element = view; element is not null; element = element.Parent)
        {
            if (element is VisualElement v) { x += v.X + v.TranslationX; y += v.Y + v.TranslationY; }
            if (element is ScrollView scroll) { x -= scroll.ScrollX; y -= scroll.ScrollY; }
        }
        return new Point(x, y);
    }
}
