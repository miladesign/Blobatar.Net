using Blobatar.Net.Core;
using Blobatar.Net.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Blobatar.Net.WinForms.Sample;

/// <summary>Demo window: live seed, hue/tone locks, backdrop, expression, gaze and a 40-user gallery. Built in code, no designer.</summary>
public sealed class MainForm : Form
{
    readonly BlobatarControl _preview = new()
    {
        Size = new Size(180, 180), Seed = "MilaDesign", Animate = true, Gaze = GazeMode.Pointer, Margin = new Padding(0, 0, 0, 12),
    };
    readonly CheckBox _lockHue = new() { Text = "Lock hue", AutoSize = true };
    readonly CheckBox _lockTone = new() { Text = "Lock tone", AutoSize = true };
    readonly TrackBar _hue = new() { Minimum = 0, Maximum = 360, Value = 200, Width = 220, TickStyle = TickStyle.None };
    readonly TrackBar _tone = new() { Minimum = 0, Maximum = 999, Value = 500, Width = 220, TickStyle = TickStyle.None };
    readonly List<BlobatarControl> _gallery = new();

    public MainForm()
    {
        Text = "Blobatar.Net sample (WinForms)";
        ClientSize = new Size(780, 640);
        StartPosition = FormStartPosition.CenterScreen;

        var left = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 250, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12), AutoScroll = true };
        var seed = new TextBox { Text = _preview.Seed, Width = 220 };
        seed.TextChanged += (_, _) => _preview.Seed = seed.Text;

        _lockHue.CheckedChanged += (_, _) => ApplyLocks();
        _lockTone.CheckedChanged += (_, _) => ApplyLocks();
        _hue.ValueChanged += (_, _) => ApplyLocks();
        _tone.ValueChanged += (_, _) => ApplyLocks();

        var travel = new TrackBar { Minimum = 0, Maximum = 40, Value = 30, Width = 220, TickStyle = TickStyle.None };
        travel.ValueChanged += (_, _) => _preview.GazeTravel = travel.Value / 10.0;

        var animate = new CheckBox { Text = "Animate", Checked = true, AutoSize = true };
        animate.CheckedChanged += (_, _) => _preview.Animate = animate.Checked;
        var normalize = new CheckBox { Text = "Normalize seed", Checked = true, AutoSize = true };
        normalize.CheckedChanged += (_, _) => _preview.Normalize = normalize.Checked;

        left.Controls.AddRange(new Control[]
        {
            _preview, Caption("Seed (name, email, id)"), seed,
            _lockHue, _hue, _lockTone, _tone,
            Caption("Backdrop"), Combo(BackdropStyle.None, v => _preview.Backdrop = v),
            Caption("Expression"), Combo(ExpressionKind.Idle, v => _preview.Expression = v),
            Caption("Gaze (needs Animate)"), Combo(GazeMode.Pointer, v => _preview.Gaze = v),
            Caption("Gaze travel"), travel, animate, normalize,
        });

        var galleryOn = new CheckBox { Text = "Animate gallery", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(6) };
        galleryOn.CheckedChanged += (_, _) => _gallery.ForEach(b => b.Animate = galleryOn.Checked);
        var grid = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(6) };
        for (var i = 1; i <= 40; i++)
        {
            var cell = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Size = new Size(76, 90), WrapContents = false };
            var blob = new BlobatarControl { Seed = $"user{i}", Size = new Size(64, 64), Gaze = GazeMode.Pointer };
            _gallery.Add(blob);
            cell.Controls.Add(blob);
            cell.Controls.Add(new Label { Text = $"user{i}", AutoSize = true, Font = new Font(Font.FontFamily, 8f) });
            grid.Controls.Add(cell);
        }

        Controls.Add(grid);
        Controls.Add(galleryOn);
        Controls.Add(left);
    }

    static Label Caption(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(3, 10, 3, 2) };

    static ComboBox Combo<T>(T initial, Action<T> set) where T : struct, Enum
    {
        var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, DataSource = Enum.GetValues<T>() };
        box.SelectedItem = initial;
        box.SelectedIndexChanged += (_, _) => { if (box.SelectedItem is T v) set(v); };
        return box;
    }

    // Hue/Tone are nullable on the control: null means "derive from the seed".
    void ApplyLocks()
    {
        _preview.Hue = _lockHue.Checked ? _hue.Value : null;
        _preview.Tone = _lockTone.Checked ? _tone.Value / 1000.0 : null;
    }
}
