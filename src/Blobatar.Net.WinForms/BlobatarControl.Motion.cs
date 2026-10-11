using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Blobatar.Net.Core;

namespace Blobatar.Net.WinForms;

public partial class BlobatarControl
{
    bool _running;

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); UpdateClock(); }
    protected override void OnHandleDestroyed(EventArgs e) { Stop(); base.OnHandleDestroyed(e); }
    protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); UpdateClock(); }

    void UpdateClock()
    {
        var design = LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        var want = _animate && IsHandleCreated && !design;
        if (want == _running) return;
        if (want) { _running = true; BlobatarClock.Subscribe(OnTick); }
        else Stop();
    }

    void Stop()
    {
        if (!_running) return;
        _running = false;
        BlobatarClock.Unsubscribe(OnTick);
        _scene?.Pause();
    }

    void OnTick(double now)
    {
        if (_scene is null) return;
        var form = FindForm();
        if (!Visible || form is null || form.WindowState == FormWindowState.Minimized) { _scene.Pause(); return; }

        var aim = Aim(form, out var dx, out var dy);
        _scene.Tick(now, aim, dx, dy, Math.Min(ClientSize.Width, ClientSize.Height), _gazeTravel);
        Invalidate();
    }

    /// <summary>The aim for this frame, plus where it is relative to the face centre.</summary>
    GazeAim Aim(Form form, out double dx, out double dy)
    {
        dx = dy = 0;
        var centre = new PointF(ClientSize.Width / 2f, ClientSize.Height / 2f);
        if (GazeTarget is { } target && target.IsHandleCreated)
        {
            var c = PointToClient(target.PointToScreen(new Point(target.Width / 2, target.Height / 2)));
            (dx, dy) = (c.X - centre.X, c.Y - centre.Y);
            return GazeAim.Look;
        }
        switch (_gaze)
        {
            case GazeMode.Pointer:
                // Outside the form the eyes come home (the original documents this; its parked-pointer maths does not quite do it).
                if (!form.Bounds.Contains(MousePosition)) return GazeAim.Rest;
                var p = PointToClient(MousePosition);
                (dx, dy) = (p.X - centre.X, p.Y - centre.Y);
                return GazeAim.Look;
            case GazeMode.Rest: return GazeAim.Rest;
            default: return GazeAim.None;
        }
    }
}
