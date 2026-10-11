using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using Blobatar.Net.Core;
using CoreExpression = Blobatar.Net.Core.Expression;

[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "Blobatar.Net.Wpf")]
[assembly: XmlnsDefinition("https://github.com/MilaDesign/Blobatar.Net/wpf", "Blobatar.Net.Wpf")]
[assembly: XmlnsPrefix("https://github.com/MilaDesign/Blobatar.Net/wpf", "bl")]
namespace Blobatar.Net.Wpf;

/// <summary>
/// Draws a deterministic blobatar for <see cref="Seed"/>. Static by default: geometry is built once
/// per property change and drawn in OnRender. With <see cref="Animate"/> it breathes, bobs, blinks and
/// glances, and morphs between <see cref="Expression"/> values.
/// </summary>
public partial class BlobatarControl : FrameworkElement
{
    const double DefaultSize = 48;

    static DependencyProperty Reg<T>(string name, T def, Action<BlobatarControl>? changed = null) =>
        DependencyProperty.Register(name, typeof(T), typeof(BlobatarControl),
            new FrameworkPropertyMetadata(def, FrameworkPropertyMetadataOptions.AffectsRender,
                (d, _) => (changed ?? (c => c.Rebuild()))((BlobatarControl)d)));

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
    /// <summary>Runs the idle loops. Ignored when Windows animations are turned off.</summary>
    public bool Animate { get => (bool)GetValue(AnimateProperty); set => SetValue(AnimateProperty, value); }

    /// <summary>None: idle glances only. Pointer: follow the cursor while it is over the window. Rest: look straight ahead and stay still. Needs Animate.</summary>
    public GazeMode Gaze { get => (GazeMode)GetValue(GazeProperty); set => SetValue(GazeProperty, value); }
    /// <summary>Looks at this element's centre instead of the pointer. Overrides <see cref="Gaze"/>.</summary>
    public UIElement? GazeTarget { get => (UIElement?)GetValue(GazeTargetProperty); set => SetValue(GazeTargetProperty, value); }
    /// <summary>How far the eyes travel at full look, in design units (the figure is 100 wide). Useful range 1.5 to 4.</summary>
    public double GazeTravel { get => (double)GetValue(GazeTravelProperty); set => SetValue(GazeTravelProperty, value); }

    readonly List<(Geometry Geometry, Brush Brush)> _layers = new();
    Matrix _wrap = Matrix.Identity;

    public BlobatarControl()
    {
        Loaded += (_, _) => UpdateClock();
        Unloaded += (_, _) => UpdateClock();
        Rebuild();
    }

    bool Animating => Animate && SystemParameters.ClientAreaAnimation;
    CoreExpression Current => Expressions.Of(Expression);

    BlobatarModel Build(bool bakeExpression) => BlobatarFactory.Create(Seed ?? "", new BlobatarOptions
    {
        Hue = Hue, Tone = Tone, Background = Backdrop, Normalize = Normalize, Contrast = EnforceContrast,
        Expression = bakeExpression ? Current : null,
    });

    void Rebuild()
    {
        _layers.Clear();
        if (Animating) { BuildAnimated(Build(false)); return; }
        _animated = false;

        var model = Build(true);
        if (model.Backdrop is { } bg) _layers.Add((Path(bg.D), Fill(bg.Fill)));
        foreach (var m in model.Marks) _layers.Add((GeometryOf(m), Fill(m.Fill)));
        _wrap = model.Wrap.Length == 0 ? Matrix.Identity : ToMatrix(new SvgTransform().Translate(0, Current.P.Bdy).ToAffine());
    }

    static Geometry GeometryOf(Mark m) =>
        m.IsCircle ? Frozen(new EllipseGeometry(new Point(m.Cx, m.Cy), m.R, m.R)) : Path(m.D!);

    // "F1" selects nonzero fill, matching SVG. Geometry.Parse defaults to even-odd.
    static Geometry Path(string d) => Frozen(Geometry.Parse("F1 " + d));
    static Geometry Frozen(Geometry g) { g.Freeze(); return g; }
    static Matrix ToMatrix(Affine a) => new(a.A, a.B, a.C, a.D, a.E, a.F);

    static readonly Dictionary<string, Brush> BrushCache = new();
    static Brush Fill(string hex)
    {
        if (BrushCache.TryGetValue(hex, out var cached)) return cached;
        if (BrushCache.Count > 512) BrushCache.Clear();
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return BrushCache[hex] = b;
    }

    protected override Size MeasureOverride(Size available)
    {
        var s = Math.Min(available.Width, available.Height);
        if (double.IsInfinity(s)) s = double.IsInfinity(available.Width) ? (double.IsInfinity(available.Height) ? DefaultSize : available.Height) : available.Width;
        return new Size(s, s);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var side = Math.Min(ActualWidth, ActualHeight);
        if (side <= 0) return;
        var k = side / 100;
        dc.PushTransform(new TranslateTransform((ActualWidth - side) / 2, (ActualHeight - side) / 2));
        dc.PushTransform(new ScaleTransform(k, k));
        if (_animated) DrawAnimated(dc);
        else DrawStatic(dc);
        dc.Pop();
        dc.Pop();
    }

    void DrawStatic(DrawingContext dc)
    {
        // The baked pose's body shift applies to everything but the backdrop (the first layer, when present).
        var hasBackdrop = Backdrop != BackdropStyle.None;
        for (var i = 0; i < _layers.Count; i++)
        {
            var (geometry, brush) = _layers[i];
            var moves = !(hasBackdrop && i == 0) && !_wrap.IsIdentity;
            if (moves) dc.PushTransform(new MatrixTransform(_wrap));
            dc.DrawGeometry(brush, null, geometry);
            if (moves) dc.Pop();
        }
    }
}
