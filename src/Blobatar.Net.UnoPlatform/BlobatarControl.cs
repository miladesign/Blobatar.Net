using Blobatar.Net.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using CoreExpression = Blobatar.Net.Core.Expression;
using WinPath = Microsoft.UI.Xaml.Shapes.Path;

namespace Blobatar.Net.UnoPlatform;

/// <summary>
/// Draws a deterministic blobatar for <see cref="Seed"/> from plain WinUI shapes, so it runs on every Uno target.
/// Static by default. With <see cref="Animate"/> it breathes, bobs, blinks, glances, morphs between
/// <see cref="Expression"/> values and follows <see cref="Gaze"/>. All logic lives in Blobatar.Net.Core.
/// </summary>
public sealed class BlobatarControl : UserControl
{
    static DependencyProperty Reg<T>(string name, T def, Action<BlobatarControl>? changed = null) =>
        DependencyProperty.Register(name, typeof(T), typeof(BlobatarControl),
            new PropertyMetadata(def, (d, _) => (changed ?? (c => c.Rebuild()))((BlobatarControl)d)));

    public static readonly DependencyProperty SeedProperty = Reg(nameof(Seed), "");
    public static readonly DependencyProperty HueProperty = Reg<double?>(nameof(Hue), null);
    public static readonly DependencyProperty ToneProperty = Reg<double?>(nameof(Tone), null);
    public static readonly DependencyProperty BackdropProperty = Reg(nameof(Backdrop), BackdropStyle.None);
    public static readonly DependencyProperty NormalizeProperty = Reg(nameof(Normalize), true);
    public static readonly DependencyProperty EnforceContrastProperty = Reg(nameof(EnforceContrast), true);
    public static readonly DependencyProperty ExpressionProperty = Reg(nameof(Expression), ExpressionKind.Idle, c => c.OnExpressionChanged());
    public static readonly DependencyProperty AnimateProperty = Reg(nameof(Animate), false, c => { c.UpdateClock(); c.Rebuild(); });
    public static readonly DependencyProperty GazeProperty = Reg(nameof(Gaze), GazeMode.None, _ => { });
    public static readonly DependencyProperty GazeTargetProperty = Reg<UIElement?>(nameof(GazeTarget), null, _ => { });
    public static readonly DependencyProperty GazeTravelProperty = Reg(nameof(GazeTravel), 3.0, _ => { });

    /// <summary>Name, email or id. The same seed always yields the same blobatar.</summary>
    public string Seed { get => (string)GetValue(SeedProperty); set => SetValue(SeedProperty, value); }
    /// <summary>Locks the hue in degrees (0-360).</summary>
    public double? Hue { get => (double?)GetValue(HueProperty); set => SetValue(HueProperty, value); }
    /// <summary>Locks the tone, 0 (pale) to just under 1 (ink).</summary>
    public double? Tone { get => (double?)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public BackdropStyle Backdrop { get => (BackdropStyle)GetValue(BackdropProperty); set => SetValue(BackdropProperty, value); }
    /// <summary>NFC, trim and lowercase the seed before hashing.</summary>
    public bool Normalize { get => (bool)GetValue(NormalizeProperty); set => SetValue(NormalizeProperty, value); }
    public bool EnforceContrast { get => (bool)GetValue(EnforceContrastProperty); set => SetValue(EnforceContrastProperty, value); }
    public ExpressionKind Expression { get => (ExpressionKind)GetValue(ExpressionProperty); set => SetValue(ExpressionProperty, value); }
    /// <summary>Runs the idle loops.</summary>
    public bool Animate { get => (bool)GetValue(AnimateProperty); set => SetValue(AnimateProperty, value); }
    /// <summary>None: idle glances only. Pointer: follow the cursor while it is over the window. Rest: look straight ahead and stay still. Needs Animate.</summary>
    public GazeMode Gaze { get => (GazeMode)GetValue(GazeProperty); set => SetValue(GazeProperty, value); }
    /// <summary>Looks at this element's centre instead of the pointer. Overrides <see cref="Gaze"/>.</summary>
    public UIElement? GazeTarget { get => (UIElement?)GetValue(GazeTargetProperty); set => SetValue(GazeTargetProperty, value); }
    /// <summary>How far the eyes travel at full look, in design units (the figure is 100 wide). Useful range 1.5 to 4.</summary>
    public double GazeTravel { get => (double)GetValue(GazeTravelProperty); set => SetValue(GazeTravelProperty, value); }

    // canvas: [backdrop] figure[ head[parts...] eyes... ]. The head layer and each eye carry a matrix.
    readonly Canvas _canvas = new() { Width = 100, Height = 100 };
    readonly Canvas _figure = new() { Width = 100, Height = 100 };
    readonly Canvas _head = new() { Width = 100, Height = 100 };
    readonly MatrixTransform _figureTf = new(), _headTf = new();
    readonly List<WinPath> _headParts = new();
    WinPath[] _eyes = Array.Empty<WinPath>();
    MatrixTransform[] _eyeTf = Array.Empty<MatrixTransform>();
    BlobatarScene? _scene;
    string? _lastHead, _lastEye;

    public BlobatarControl()
    {
        _figure.RenderTransform = _figureTf;
        _head.RenderTransform = _headTf;
        Content = new Viewbox { Stretch = Stretch.Uniform, Child = _canvas };
        Loaded += (_, _) => UpdateClock();
        Unloaded += (_, _) => UpdateClock(unloading: true);
        Rebuild();
    }

    CoreExpression Current => Expressions.Of(Expression);

    BlobatarModel Build(bool bakeExpression) => BlobatarFactory.Create(Seed ?? "", new BlobatarOptions
    {
        Hue = Hue, Tone = Tone, Background = Backdrop, Normalize = Normalize, Contrast = EnforceContrast,
        Expression = bakeExpression ? Current : null,
    });

    void Rebuild()
    {
        var animated = Animate;
        var model = Build(bakeExpression: !animated);

        _canvas.Children.Clear();
        _figure.Children.Clear();
        _head.Children.Clear();
        _headParts.Clear();
        if (model.Backdrop is { } bg) _canvas.Children.Add(MakePath(bg.D, null, bg.Fill));
        _canvas.Children.Add(_figure);
        _figure.Children.Add(_head);

        var eyeCount = model.Layout.Eyes.Length;
        var headCount = model.Marks.Count - eyeCount;
        _eyes = new WinPath[eyeCount];
        _eyeTf = new MatrixTransform[eyeCount];
        for (var i = 0; i < model.Marks.Count; i++)
        {
            var m = model.Marks[i];
            var path = MakePath(m.D, m, m.Fill);
            if (i < headCount) { _head.Children.Add(path); _headParts.Add(path); }
            else
            {
                var e = i - headCount;
                _eyeTf[e] = new MatrixTransform();
                path.RenderTransform = _eyeTf[e];
                _figure.Children.Add(path);
                _eyes[e] = path;
            }
        }

        // A baked pose moves the whole figure (not the backdrop); an animated one moves it through the scene.
        _figureTf.Matrix = !animated && model.Wrap.Length > 0
            ? ToMatrix(new SvgTransform().Translate(0, Current.P.Bdy).ToAffine())
            : Matrix.Identity;
        _headTf.Matrix = Matrix.Identity;

        _scene = animated ? new BlobatarScene(model, Seed ?? "", Normalize, Current) : null;
        _lastHead = _lastEye = null;
    }

    static WinPath MakePath(string? d, Mark? circle, string hex) => new()
    {
        Data = circle is { IsCircle: true } c
            ? new EllipseGeometry { Center = new Point(c.Cx, c.Cy), RadiusX = c.R, RadiusY = c.R }
            : ToGeometry(d!),
        Fill = Fill(hex),
        IsHitTestVisible = false,
    };

    static PathGeometry ToGeometry(string d)
    {
        var geometry = new PathGeometry { FillRule = FillRule.Nonzero };
        PathFigure? figure = null;
        foreach (var s in PathData.Parse(d))
        {
            switch (s.Kind)
            {
                case SegmentKind.Move:
                    figure = new PathFigure { StartPoint = new Point(s.X, s.Y), IsClosed = false, IsFilled = true };
                    geometry.Figures.Add(figure);
                    break;
                case SegmentKind.Line:
                    figure?.Segments.Add(new LineSegment { Point = new Point(s.X, s.Y) });
                    break;
                case SegmentKind.Cubic:
                    figure?.Segments.Add(new BezierSegment
                    {
                        Point1 = new Point(s.X1, s.Y1), Point2 = new Point(s.X2, s.Y2), Point3 = new Point(s.X, s.Y),
                    });
                    break;
                case SegmentKind.Quad:
                    figure?.Segments.Add(new QuadraticBezierSegment { Point1 = new Point(s.X1, s.Y1), Point2 = new Point(s.X, s.Y) });
                    break;
                case SegmentKind.Close:
                    if (figure is not null) figure.IsClosed = true;
                    break;
            }
        }
        return geometry;
    }

    static Matrix ToMatrix(Affine a) => new(a.A, a.B, a.C, a.D, a.E, a.F);

    static readonly Dictionary<string, SolidColorBrush> BrushCache = new();
    static SolidColorBrush Fill(string hex)
    {
        if (BrushCache.TryGetValue(hex, out var cached)) return cached;
        if (BrushCache.Count > 512) BrushCache.Clear();
        var n = Convert.ToInt32(hex.AsSpan(1).ToString(), 16);
        var color = Windows.UI.Color.FromArgb(255, (byte)(n >> 16), (byte)(n >> 8), (byte)n);
        return BrushCache[hex] = new SolidColorBrush(color);
    }

    // ---- animation -------------------------------------------------------------------------------------------

    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    UIElement? _root;
    Point _pointer;
    bool _pointerIn, _running;

    void OnExpressionChanged()
    {
        if (_scene is not null) _scene.SetExpression(Current, Clock.Now);
        else Rebuild();
    }

    /// <summary>One frame hook per animated control while it is on screen.</summary>
    void UpdateClock(bool unloading = false)
    {
        var want = !unloading && IsLoaded && Animate;
        if (want == _running) return;
        _running = want;
        if (want)
        {
            _root = XamlRoot?.Content;
            _root?.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnPointerMoved), true);
            _root?.AddHandler(UIElement.PointerExitedEvent, new PointerEventHandler(OnPointerExited), true);
            _timer.Tick += OnTick;
            _timer.Start();
        }
        else
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _root?.RemoveHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnPointerMoved));
            _root?.RemoveHandler(UIElement.PointerExitedEvent, new PointerEventHandler(OnPointerExited));
            _root = null;
            _pointerIn = false;
            _scene?.Pause();
        }
    }

    void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_root is null) return;
        _pointer = e.GetCurrentPoint(_root).Position;
        _pointerIn = true;
    }

    void OnPointerExited(object sender, PointerRoutedEventArgs e) => _pointerIn = false;

    void OnTick(object? sender, object e)
    {
        if (_scene is null) return;
        if (!IsLoaded || ActualWidth <= 0 || ActualHeight <= 0) { _scene.Pause(); return; }

        var now = Clock.Now;
        var aim = Aim(out var dx, out var dy);
        _scene.Tick(now, aim, dx, dy, Math.Min(ActualWidth, ActualHeight), GazeTravel);
        var frame = _scene.Frame(now, GazeTravel);

        _headTf.Matrix = ToMatrix(frame.Body);
        for (var i = 0; i < _eyes.Length; i++) _eyeTf[i].Matrix = ToMatrix(frame.Eyes[i]);
        if (frame.Head != _lastHead) { _lastHead = frame.Head; foreach (var p in _headParts) p.Fill = Fill(frame.Head); }
        if (frame.Eye != _lastEye) { _lastEye = frame.Eye; foreach (var p in _eyes) p.Fill = Fill(frame.Eye); }
    }

    /// <summary>The aim for this frame, plus where it is relative to the face centre.</summary>
    GazeAim Aim(out double dx, out double dy)
    {
        dx = dy = 0;
        var centre = new Point(ActualWidth / 2, ActualHeight / 2);
        try
        {
            if (GazeTarget is { } target)
            {
                var c = target.TransformToVisual(this).TransformPoint(new Point(target.ActualSize.X / 2, target.ActualSize.Y / 2));
                (dx, dy) = (c.X - centre.X, c.Y - centre.Y);
                return GazeAim.Look;
            }
            switch (Gaze)
            {
                case GazeMode.Pointer:
                    // Outside the window the eyes come home (the original documents this; its parked-pointer maths does not quite do it).
                    if (!_pointerIn || _root is null) return GazeAim.Rest;
                    var origin = TransformToVisual(_root).TransformPoint(new Point(0, 0));
                    (dx, dy) = (_pointer.X - origin.X - centre.X, _pointer.Y - origin.Y - centre.Y);
                    return GazeAim.Look;
                case GazeMode.Rest: return GazeAim.Rest;
                default: return GazeAim.None;
            }
        }
        catch (ArgumentException) { return GazeAim.Rest; } // not in the same visual tree
    }
}
