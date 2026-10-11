using Blobatar.Net.Core;
using Godot;

namespace Blobatar.Net.Godot;

/// <summary>A Godot UI avatar using the shared Core geometry and animation engine.</summary>
[Tool, ScriptPath("res://addons/Blobatar.Net.Godot/BlobatarControl.cs")]
public partial class BlobatarControl : Control
{
    string _seed = "";
    double? _hue, _tone;
    double _hueValue = 200, _toneValue = 0.5, _gazeTravel = 3;
    BackdropStyle _backdrop;
    bool _normalize = true, _contrast = true, _animate;
    ExpressionKind _expression;
    GazeMode _gaze;
    Control? _gazeTarget;

    [Export] public string Seed { get => _seed; set { if (_seed == value) return; _seed = value ?? ""; Changed(); } }
    public double? Hue { get => _hue; set { if (_hue == value) return; _hue = value; if (value.HasValue) _hueValue = value.Value; Changed(); } }
    public double? Tone { get => _tone; set { if (_tone == value) return; _tone = value; if (value.HasValue) _toneValue = value.Value; Changed(); } }
    // Nullable numbers are not Godot Variant properties. These pairs expose the
    // same optional palette overrides to the Inspector and scene serialization.
    [Export] public bool LockHue { get => Hue.HasValue; set => Hue = value ? _hueValue : null; }
    [Export(PropertyHint.Range, "0,360,1")] public double HueValue { get => _hueValue; set { _hueValue = value; if (LockHue) Hue = value; } }
    [Export] public bool LockTone { get => Tone.HasValue; set => Tone = value ? _toneValue : null; }
    [Export(PropertyHint.Range, "0,0.999,0.001")] public double ToneValue { get => _toneValue; set { _toneValue = value; if (LockTone) Tone = value; } }
    [Export] public BackdropStyle Backdrop { get => _backdrop; set { if (_backdrop == value) return; _backdrop = value; Changed(); } }
    [Export] public bool Normalize { get => _normalize; set { if (_normalize == value) return; _normalize = value; Changed(); } }
    [Export] public bool EnforceContrast { get => _contrast; set { if (_contrast == value) return; _contrast = value; Changed(); } }
    [Export] public ExpressionKind Expression
    {
        get => _expression;
        set
        {
            if (_expression == value) return;
            _expression = value;
            if (!_dirty && _scene is { } scene) { scene.SetExpression(Expressions.Of(value), Clock.Now); QueueRedraw(); }
            else Changed();
        }
    }
    [Export] public bool Animate { get => _animate; set { if (_animate == value) return; _animate = value; Changed(); UpdateProcessing(); } }
    [Export] public GazeMode Gaze { get => _gaze; set => _gaze = value; }
    [Export] public Control? GazeTarget { get => _gazeTarget; set => _gazeTarget = value; }
    [Export(PropertyHint.Range, "0,5,0.1")] public double GazeTravel
    {
        get => _gazeTravel;
        set { if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _gazeTravel = value; QueueRedraw(); }
    }

    BlobatarModel? _model;
    BlobatarScene? _scene;
    readonly List<ImageTexture> _textures = new();
    ImageTexture? _backdropTexture;
    Affine _wrap = Affine.Identity;
    int _headCount, _textureSide;
    bool _dirty = true, _mouseInside;

    public BlobatarControl()
    {
        CustomMinimumSize = new Vector2(48, 48);
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Linear;
        SetProcess(false);
    }

    void Changed() { _dirty = true; QueueRedraw(); }
    void UpdateProcessing() => SetProcess(Animate && !Engine.IsEditorHint());

    public override void _EnterTree()
    {
        _mouseInside = !Engine.IsEditorHint();
        UpdateProcessing();
        Changed();
    }

    public override void _ExitTree()
    {
        _mouseInside = false;
        SetProcess(false);
        _scene?.Pause();
        ReleaseTextures();
    }

    public override void _Notification(int what)
    {
        // Window notifications reach child nodes without signal connections or
        // delegate handles, including in embedded game windows.
        if (what == NotificationWMMouseEnter) _mouseInside = !Engine.IsEditorHint();
        else if (what == NotificationWMMouseExit) _mouseInside = false;
        else if (what == NotificationResized) QueueRedraw();
        else if (what == NotificationVisibilityChanged && !IsVisibleInTree()) _scene?.Pause();
    }

    void EnsureModel()
    {
        if (!_dirty) return;
        var expression = Expressions.Of(Expression);
        var animated = Animate && !Engine.IsEditorHint();
        _model = BlobatarFactory.Create(Seed, new BlobatarOptions
        {
            Hue = Hue, Tone = Tone, Background = Backdrop, Normalize = Normalize,
            Contrast = EnforceContrast, Expression = animated ? null : expression,
        });
        _scene = animated ? new BlobatarScene(_model, Seed, Normalize, expression) : null;
        _headCount = _model.Marks.Count - _model.Layout.Eyes.Length;
        _wrap = new SvgTransform().Translate(0, expression.P.Bdy).ToAffine();
        ReleaseTextures();
        _dirty = false;
    }

    public override void _Process(double delta)
    {
        if (!Animate || Engine.IsEditorHint()) return;
        EnsureModel();
        if (!IsVisibleInTree()) { _scene?.Pause(); return; }
        var aim = Aim(out var direction);
        _scene?.Tick(Clock.Now, aim, direction.X, direction.Y, Math.Min(Size.X, Size.Y), GazeTravel);
        QueueRedraw();
    }

    GazeAim Aim(out Vector2 direction)
    {
        direction = Vector2.Zero;
        var viewport = GetViewport();
        var transform = GetGlobalTransformWithCanvas();
        if (Mathf.IsZeroApprox(transform.X.X * transform.Y.Y - transform.X.Y * transform.Y.X)) return GazeAim.Rest;
        var inverse = transform.AffineInverse();
        if (IsInstanceValid(GazeTarget) && GazeTarget!.IsInsideTree() && GazeTarget.GetViewport() == viewport)
        {
            var target = GazeTarget.GetGlobalTransformWithCanvas() * (GazeTarget.Size / 2);
            direction = inverse * target - Size / 2;
            return GazeAim.Look;
        }
        if (GazeTarget is not null) return GazeAim.Rest;
        var mouse = viewport.GetMousePosition();
        if (Gaze == GazeMode.Pointer && _mouseInside && viewport.GetVisibleRect().HasPoint(mouse))
        {
            direction = inverse * mouse - Size / 2;
            return GazeAim.Look;
        }
        return Gaze == GazeMode.None ? GazeAim.None : GazeAim.Rest;
    }

    public override void _Draw()
    {
        EnsureModel();
        var side = Math.Min(Size.X, Size.Y);
        if (side <= 0 || _model is null) return;
        // Godot rasterizes each neutral SVG part once, then reuses GPU textures.
        // Pose/gaze transforms and colour changes do not rasterize on every frame.
        var screen = GetViewport().GetFinalTransform() * GetGlobalTransformWithCanvas();
        var resolution = Math.Clamp((int)Math.Ceiling(side * Math.Max(screen.X.Length(), screen.Y.Length())), 100, 2048);
        EnsureTextures(resolution);
        var scale = side / 100;
        var fit = new Transform2D(new Vector2(scale, 0), new Vector2(0, scale), (Size - new Vector2(side, side)) / 2);
        var rect = new Rect2(0, 0, 100, 100);
        if (_backdropTexture is not null)
        {
            DrawSetTransformMatrix(fit);
            DrawTextureRect(_backdropTexture, rect, false, Color.FromHtml(_model.Backdrop!.Fill));
        }
        SceneFrame? frame = _scene?.Frame(Clock.Now, GazeTravel);
        for (var i = 0; i < _textures.Count; i++)
        {
            var transform = frame is { } pose ? (i < _headCount ? pose.Body : pose.Eyes[i - _headCount]) : _wrap;
            var fill = frame is { } palette ? (i < _headCount ? palette.Head : palette.Eye) : _model.Marks[i].Fill;
            DrawSetTransformMatrix(fit * ToTransform(transform));
            DrawTextureRect(_textures[i], rect, false, Color.FromHtml(fill));
        }
        DrawSetTransformMatrix(Transform2D.Identity);
    }

    static Transform2D ToTransform(Affine a) => new(new Vector2((float)a.A, (float)a.B), new Vector2((float)a.C, (float)a.D), new Vector2((float)a.E, (float)a.F));

    void EnsureTextures(int side)
    {
        if (_textureSide == side) return;
        ReleaseTextures();
        if (_model!.Backdrop is { } bg) _backdropTexture = Rasterize($"<path d=\"{bg.D}\"/>", side);
        foreach (var mark in _model.Marks)
            _textures.Add(Rasterize(mark.IsCircle
                ? $"<circle cx=\"{Geo.F(mark.Cx)}\" cy=\"{Geo.F(mark.Cy)}\" r=\"{Geo.F(mark.R)}\"/>"
                : $"<path d=\"{mark.D}\"/>", side));
        _textureSide = side;
    }

    static ImageTexture Rasterize(string primitive, int side)
    {
        using var image = new Image();
        var error = image.LoadSvgFromString($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\" viewBox=\"0 0 100 100\"><g fill=\"white\">{primitive}</g></svg>", side / 100f);
        if (error != Error.Ok) throw new InvalidOperationException($"Godot could not rasterize Blobatar SVG: {error}.");
        return ImageTexture.CreateFromImage(image);
    }

    void ReleaseTextures()
    {
        foreach (var texture in _textures) texture.Dispose();
        _textures.Clear();
        _backdropTexture?.Dispose();
        _backdropTexture = null;
        _textureSide = 0;
    }
}
