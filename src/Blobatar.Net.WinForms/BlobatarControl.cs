using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Blobatar.Net.Core;
using CoreExpression = Blobatar.Net.Core.Expression;

namespace Blobatar.Net.WinForms;

/// <summary>
/// Draws a deterministic blobatar for <see cref="Seed"/> with GDI+. Static by default. With
/// <see cref="Animate"/> it breathes, bobs, blinks, glances, morphs between <see cref="Expression"/>
/// values and follows <see cref="Gaze"/>. All logic lives in Blobatar.Net.Core.
/// </summary>
[DefaultProperty(nameof(Seed))]
public partial class BlobatarControl : Control
{
    string _seed = "";
    double? _hue, _tone;
    BackdropStyle _backdrop = BackdropStyle.None;
    bool _normalize = true, _contrast = true, _animate;
    ExpressionKind _expression = ExpressionKind.Idle;
    GazeMode _gaze = GazeMode.None;
    double _gazeTravel = 3;

    public BlobatarControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Size = new Size(48, 48);
        Rebuild();
    }

    [Category("Blobatar"), DefaultValue(""), Description("Name, email or id. The same seed always yields the same blobatar.")]
    public string Seed { get => _seed; set => Set(ref _seed, value ?? ""); }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double? Hue { get => _hue; set => Set(ref _hue, value); }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double? Tone { get => _tone; set => Set(ref _tone, value); }

    [Category("Blobatar"), DefaultValue(BackdropStyle.None)]
    public BackdropStyle Backdrop { get => _backdrop; set => Set(ref _backdrop, value); }

    [Category("Blobatar"), DefaultValue(true), Description("NFC, trim and lowercase the seed before hashing.")]
    public bool Normalize { get => _normalize; set => Set(ref _normalize, value); }

    [Category("Blobatar"), DefaultValue(true)]
    public bool EnforceContrast { get => _contrast; set => Set(ref _contrast, value); }

    [Category("Blobatar"), DefaultValue(ExpressionKind.Idle)]
    public ExpressionKind Expression
    {
        get => _expression;
        set
        {
            if (_expression == value) return;
            _expression = value;
            if (_scene is not null) _scene.SetExpression(Current, Clock.Now);
            else Rebuild();
        }
    }

    [Category("Blobatar"), DefaultValue(false), Description("Runs the idle loops.")]
    public bool Animate { get => _animate; set { if (Set(ref _animate, value)) UpdateClock(); } }

    [Category("Blobatar"), DefaultValue(GazeMode.None),
     Description("None: idle glances only. Pointer: follow the cursor while it is over the form. Rest: look straight ahead. Needs Animate.")]
    public GazeMode Gaze { get => _gaze; set => _gaze = value; }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
     Description("Looks at this control's centre instead of the pointer. Overrides Gaze.")]
    public Control? GazeTarget { get; set; }

    [Category("Blobatar"), DefaultValue(3.0), Description("How far the eyes travel at full look, in design units. Useful range 1.5 to 4.")]
    public double GazeTravel { get => _gazeTravel; set => _gazeTravel = value; }

    CoreExpression Current => Expressions.Of(_expression);

    /// <summary>Sets a field; rebuilds and repaints when it changed. Returns whether it changed.</summary>
    bool Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Rebuild();
        return true;
    }

    public override Size GetPreferredSize(Size proposedSize) => new(48, 48);

    protected override void Dispose(bool disposing)
    {
        if (disposing) { Stop(); DisposeLayers(); }
        base.Dispose(disposing);
    }
}
