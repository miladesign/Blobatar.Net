using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Blobatar.Net.Core;
using CoreExpression = Blobatar.Net.Core.Expression;

[assembly: Avalonia.Metadata.XmlnsDefinition("https://github.com/avaloniaui", "Blobatar.Net.Avalonia")]
[assembly: Avalonia.Metadata.XmlnsDefinition("https://github.com/MilaDesign/Blobatar.Net/avalonia", "Blobatar.Net.Avalonia")]
[assembly: Avalonia.Metadata.XmlnsPrefix("https://github.com/MilaDesign/Blobatar.Net/avalonia", "bl")]

namespace Blobatar.Net.Avalonia;

/// <summary>
/// Draws a deterministic blobatar for <see cref="Seed"/>. Static by default: geometry is built once
/// per property change and drawn in Render. With <see cref="Animate"/> it breathes, bobs, blinks and
/// glances, morphs between <see cref="Expression"/> values and follows <see cref="Gaze"/>.
/// All logic lives in Blobatar.Net.Core; this class only supplies geometry, a clock and a pointer.
/// </summary>
public partial class BlobatarControl : Control
{
    const double DefaultSize = 48;

    public static readonly StyledProperty<string> SeedProperty = AvaloniaProperty.Register<BlobatarControl, string>(nameof(Seed), "");
    public static readonly StyledProperty<double?> HueProperty = AvaloniaProperty.Register<BlobatarControl, double?>(nameof(Hue));
    public static readonly StyledProperty<double?> ToneProperty = AvaloniaProperty.Register<BlobatarControl, double?>(nameof(Tone));
    public static readonly StyledProperty<BackdropStyle> BackdropProperty = AvaloniaProperty.Register<BlobatarControl, BackdropStyle>(nameof(Backdrop), BackdropStyle.None);
    public static readonly StyledProperty<bool> NormalizeProperty = AvaloniaProperty.Register<BlobatarControl, bool>(nameof(Normalize), true);
    public static readonly StyledProperty<bool> EnforceContrastProperty = AvaloniaProperty.Register<BlobatarControl, bool>(nameof(EnforceContrast), true);
    public static readonly StyledProperty<ExpressionKind> ExpressionProperty = AvaloniaProperty.Register<BlobatarControl, ExpressionKind>(nameof(Expression), ExpressionKind.Idle);
    public static readonly StyledProperty<bool> AnimateProperty = AvaloniaProperty.Register<BlobatarControl, bool>(nameof(Animate));
    public static readonly StyledProperty<GazeMode> GazeProperty = AvaloniaProperty.Register<BlobatarControl, GazeMode>(nameof(Gaze), GazeMode.None);
    public static readonly StyledProperty<Control?> GazeTargetProperty = AvaloniaProperty.Register<BlobatarControl, Control?>(nameof(GazeTarget));
    public static readonly StyledProperty<double> GazeTravelProperty = AvaloniaProperty.Register<BlobatarControl, double>(nameof(GazeTravel), 3.0);

    static BlobatarControl()
    {
        AffectsRender<BlobatarControl>(SeedProperty, HueProperty, ToneProperty, BackdropProperty, NormalizeProperty,
            EnforceContrastProperty, ExpressionProperty, AnimateProperty);
    }

    /// <summary>Name, email or id. The same seed always yields the same blobatar.</summary>
    public string Seed { get => GetValue(SeedProperty); set => SetValue(SeedProperty, value); }
    /// <summary>Locks the hue in degrees (0-360).</summary>
    public double? Hue { get => GetValue(HueProperty); set => SetValue(HueProperty, value); }
    /// <summary>Locks the tone, 0 (pale) to just under 1 (ink).</summary>
    public double? Tone { get => GetValue(ToneProperty); set => SetValue(ToneProperty, value); }
    public BackdropStyle Backdrop { get => GetValue(BackdropProperty); set => SetValue(BackdropProperty, value); }
    /// <summary>NFC, trim and lowercase the seed before hashing.</summary>
    public bool Normalize { get => GetValue(NormalizeProperty); set => SetValue(NormalizeProperty, value); }
    public bool EnforceContrast { get => GetValue(EnforceContrastProperty); set => SetValue(EnforceContrastProperty, value); }
    public ExpressionKind Expression { get => GetValue(ExpressionProperty); set => SetValue(ExpressionProperty, value); }
    /// <summary>Runs the idle loops.</summary>
    public bool Animate { get => GetValue(AnimateProperty); set => SetValue(AnimateProperty, value); }
    /// <summary>None: idle glances only. Pointer: follow the cursor while it is over the window. Rest: look straight ahead and stay still. Needs Animate.</summary>
    public GazeMode Gaze { get => GetValue(GazeProperty); set => SetValue(GazeProperty, value); }
    /// <summary>Looks at this control's centre instead of the pointer. Overrides <see cref="Gaze"/>.</summary>
    public Control? GazeTarget { get => GetValue(GazeTargetProperty); set => SetValue(GazeTargetProperty, value); }
    /// <summary>How far the eyes travel at full look, in design units (the figure is 100 wide). Useful range 1.5 to 4.</summary>
    public double GazeTravel { get => GetValue(GazeTravelProperty); set => SetValue(GazeTravelProperty, value); }

    readonly List<(Geometry Geometry, IBrush Brush)> _layers = new();
    Matrix _wrap = Matrix.Identity;
    bool _hasWrap;

    public BlobatarControl() => Rebuild();

    CoreExpression Current => Expressions.Of(Expression);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == SeedProperty || p == HueProperty || p == ToneProperty || p == BackdropProperty
            || p == NormalizeProperty || p == EnforceContrastProperty) Rebuild();
        else if (p == ExpressionProperty) OnExpressionChanged();
        else if (p == AnimateProperty) { UpdateClock(); Rebuild(); }
    }

    BlobatarModel Build(bool bakeExpression) => BlobatarFactory.Create(Seed ?? "", new BlobatarOptions
    {
        Hue = Hue, Tone = Tone, Background = Backdrop, Normalize = Normalize, Contrast = EnforceContrast,
        Expression = bakeExpression ? Current : null,
    });

    void Rebuild()
    {
        _layers.Clear();
        if (Animate) { BuildAnimated(Build(false)); return; }
        _animated = false;

        var model = Build(true);
        if (model.Backdrop is { } bg) _layers.Add((Path(bg.D), Fill(bg.Fill)));
        foreach (var m in model.Marks) _layers.Add((GeometryOf(m), Fill(m.Fill)));
        _hasWrap = model.Wrap.Length > 0;
        _wrap = _hasWrap ? ToMatrix(new SvgTransform().Translate(0, Current.P.Bdy).ToAffine()) : Matrix.Identity;
    }

    static Geometry GeometryOf(Mark m) =>
        m.IsCircle ? new EllipseGeometry(new Rect(m.Cx - m.R, m.Cy - m.R, 2 * m.R, 2 * m.R)) : Path(m.D!);

    // "F1" selects nonzero fill, matching SVG. Path markup defaults to even-odd.
    static Geometry Path(string d) => Geometry.Parse("F1 " + d);
    static Matrix ToMatrix(Affine a) => new(a.A, a.B, a.C, a.D, a.E, a.F);

    static readonly Dictionary<string, IBrush> BrushCache = new();
    static IBrush Fill(string hex)
    {
        if (BrushCache.TryGetValue(hex, out var cached)) return cached;
        if (BrushCache.Count > 512) BrushCache.Clear();
        return BrushCache[hex] = new ImmutableSolidColorBrush(Color.Parse(hex));
    }

    protected override Size MeasureOverride(Size available)
    {
        var s = Math.Min(available.Width, available.Height);
        if (double.IsInfinity(s)) s = double.IsInfinity(available.Width) ? (double.IsInfinity(available.Height) ? DefaultSize : available.Height) : available.Width;
        return new Size(s, s);
    }

    public override void Render(DrawingContext context)
    {
        var side = Math.Min(Bounds.Width, Bounds.Height);
        if (side <= 0) return;
        var k = side / 100;
        using (context.PushTransform(Matrix.CreateTranslation((Bounds.Width - side) / 2, (Bounds.Height - side) / 2)))
        using (context.PushTransform(Matrix.CreateScale(k, k)))
        {
            if (_animated) DrawAnimated(context);
            else DrawStatic(context);
        }
    }

    void DrawStatic(DrawingContext context)
    {
        // The baked pose's body shift applies to everything but the backdrop (the first layer, when present).
        var hasBackdrop = Backdrop != BackdropStyle.None;
        for (var i = 0; i < _layers.Count; i++)
        {
            var (geometry, brush) = _layers[i];
            var moves = _hasWrap && !(hasBackdrop && i == 0);
            if (moves)
            {
                using (context.PushTransform(_wrap)) context.DrawGeometry(brush, null, geometry);
            }
            else context.DrawGeometry(brush, null, geometry);
        }
    }
}
