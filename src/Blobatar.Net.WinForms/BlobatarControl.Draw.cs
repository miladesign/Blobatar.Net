using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Blobatar.Net.Core;

namespace Blobatar.Net.WinForms;

public partial class BlobatarControl
{
    sealed record Layer(GraphicsPath Path, string Fill);

    readonly List<Layer> _head = new();
    Layer? _backdropLayer;
    Layer[] _eyes = Array.Empty<Layer>();
    Matrix? _wrap;
    BlobatarScene? _scene;

    static readonly Dictionary<string, SolidBrush> BrushCache = new();
    static SolidBrush FillBrush(string hex)
    {
        if (BrushCache.TryGetValue(hex, out var b)) return b;
        if (BrushCache.Count > 512) { foreach (var old in BrushCache.Values) old.Dispose(); BrushCache.Clear(); }
        return BrushCache[hex] = new SolidBrush(ColorTranslator.FromHtml(hex));
    }

    void Rebuild()
    {
        DisposeLayers();
        var animated = _animate;
        var model = BlobatarFactory.Create(_seed, new BlobatarOptions
        {
            Hue = _hue, Tone = _tone, Background = _backdrop, Normalize = _normalize, Contrast = _contrast,
            Expression = animated ? null : Current,
        });

        if (model.Backdrop is { } bg) _backdropLayer = new Layer(ToPath(bg.D, null), bg.Fill);
        var eyeCount = model.Layout.Eyes.Length;
        var headCount = model.Marks.Count - eyeCount;
        _eyes = new Layer[eyeCount];
        for (var i = 0; i < model.Marks.Count; i++)
        {
            var m = model.Marks[i];
            var layer = new Layer(ToPath(m.D, m), m.Fill);
            if (i < headCount) _head.Add(layer); else _eyes[i - headCount] = layer;
        }

        // A baked pose shifts the figure (not the backdrop); an animated one moves through the scene instead.
        _wrap = !animated && model.Wrap.Length > 0 ? ToMatrix(new SvgTransform().Translate(0, Current.P.Bdy).ToAffine()) : null;
        _scene = animated ? new BlobatarScene(model, _seed, _normalize, Current) : null;
        Invalidate();
    }

    void DisposeLayers()
    {
        _backdropLayer?.Path.Dispose();
        foreach (var l in _head) l.Path.Dispose();
        foreach (var l in _eyes) l?.Path.Dispose();
        _wrap?.Dispose();
        _head.Clear();
        _backdropLayer = null;
        _eyes = Array.Empty<Layer>();
        _wrap = null;
    }

    static Matrix ToMatrix(Affine a) => new((float)a.A, (float)a.B, (float)a.C, (float)a.D, (float)a.E, (float)a.F);

    /// <summary>GDI+ has no quadratic curves, so they are raised to cubics. Winding fill matches SVG's nonzero rule.</summary>
    static GraphicsPath ToPath(string? d, Mark? mark)
    {
        var path = new GraphicsPath(FillMode.Winding);
        if (mark is { IsCircle: true } c)
        {
            path.AddEllipse((float)(c.Cx - c.R), (float)(c.Cy - c.R), (float)(2 * c.R), (float)(2 * c.R));
            return path;
        }
        float cx = 0, cy = 0;
        foreach (var s in Core.PathData.Parse(d!))
        {
            float x = (float)s.X, y = (float)s.Y;
            switch (s.Kind)
            {
                case SegmentKind.Move: path.StartFigure(); (cx, cy) = (x, y); break;
                case SegmentKind.Line: path.AddLine(cx, cy, x, y); (cx, cy) = (x, y); break;
                case SegmentKind.Cubic:
                    path.AddBezier(cx, cy, (float)s.X1, (float)s.Y1, (float)s.X2, (float)s.Y2, x, y);
                    (cx, cy) = (x, y);
                    break;
                case SegmentKind.Quad:
                    float qx = (float)s.X1, qy = (float)s.Y1;
                    path.AddBezier(cx, cy, cx + 2f / 3f * (qx - cx), cy + 2f / 3f * (qy - cy),
                                   x + 2f / 3f * (qx - x), y + 2f / 3f * (qy - y), x, y);
                    (cx, cy) = (x, y);
                    break;
                case SegmentKind.Close: path.CloseFigure(); break;
            }
        }
        return path;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var side = Math.Min(ClientSize.Width, ClientSize.Height);
        if (side <= 0) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform((ClientSize.Width - side) / 2f, (ClientSize.Height - side) / 2f);
        g.ScaleTransform(side / 100f, side / 100f);

        if (_backdropLayer is { } bg) g.FillPath(FillBrush(bg.Fill), bg.Path);
        if (_scene is not null) DrawAnimated(g, _scene);
        else DrawStatic(g);
    }

    void DrawStatic(Graphics g)
    {
        foreach (var l in _head.Concat(_eyes)) DrawLayer(g, l, _wrap, l.Fill);
    }

    void DrawAnimated(Graphics g, BlobatarScene scene)
    {
        var frame = scene.Frame(Clock.Now, _gazeTravel);
        using (var body = ToMatrix(frame.Body))
            foreach (var l in _head) DrawLayer(g, l, body, frame.Head);
        for (var i = 0; i < _eyes.Length; i++)
        {
            using var m = ToMatrix(frame.Eyes[i]);
            DrawLayer(g, _eyes[i], m, frame.Eye);
        }
    }

    // Prepend: the layer's own matrix applies first, then the view transform already on the graphics.
    static void DrawLayer(Graphics g, Layer layer, Matrix? m, string fill)
    {
        var state = g.Save();
        if (m is not null) g.MultiplyTransform(m, MatrixOrder.Prepend);
        g.FillPath(FillBrush(fill), layer.Path);
        g.Restore(state);
    }
}
