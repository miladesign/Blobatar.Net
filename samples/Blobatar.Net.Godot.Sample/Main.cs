using Blobatar.Net.Core;
using Godot;

public partial class Main : Control
{
    BlobatarControl _preview = null!;
    readonly List<BlobatarControl> _gallery = new();

    public override void _Ready()
    {
        GetNode<Control>("EditorPreview").Hide();
        var theme = new Theme();
        theme.SetColor("font_color", "Label", new Color("243b53"));
        theme.SetColor("font_color", "CheckButton", new Color("243b53"));
        Theme = theme;
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        AddChild(scroll);
        scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var edge in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, 24);
        scroll.AddChild(margin);
        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", 12);
        margin.AddChild(content);
        var title = new Label { Text = "A face for every name" };
        title.AddThemeFontSizeOverride("font_size", 28);
        content.AddChild(title);
        content.AddChild(new Label { Text = "Move the mouse anywhere in the window. Explore expressions and change the seed." });
        var centre = new CenterContainer();
        content.AddChild(centre);
        _preview = new BlobatarControl { Seed = "MilaDesign", CustomMinimumSize = new Vector2(180, 180), Animate = true, Gaze = GazeMode.Pointer };
        centre.AddChild(_preview);
        content.AddChild(new Label { Text = "Seed (name, email, or ID)" });
        var seed = new LineEdit { Text = _preview.Seed };
        seed.TextChanged += value => _preview.Seed = value;
        content.AddChild(seed);
        AddToggle(content, "Lock hue", false, value => _preview.LockHue = value);
        AddSlider(content, 0, 360, 200, 1, value => _preview.HueValue = value);
        AddToggle(content, "Lock tone", false, value => _preview.LockTone = value);
        AddSlider(content, 0, 0.999, 0.5, 0.001, value => _preview.ToneValue = value);
        AddPicker<BackdropStyle>(content, "Backdrop", value => _preview.Backdrop = value);
        AddPicker<ExpressionKind>(content, "Expression", value => _preview.Expression = value);
        var gaze = AddPicker<GazeMode>(content, "Gaze", value => _preview.Gaze = value);
        gaze.Select((int)GazeMode.Pointer);
        AddToggle(content, "Look at the seed box", false, value => _preview.GazeTarget = value ? seed : null);
        content.AddChild(new Label { Text = "Gaze travel" });
        AddSlider(content, 0, 5, 3, 0.1, value => _preview.GazeTravel = value);
        AddToggle(content, "Animate", true, value => _preview.Animate = value);
        AddToggle(content, "Normalize seed", true, value => _preview.Normalize = value);
        AddToggle(content, "Enforce contrast", true, value => _preview.EnforceContrast = value);
        content.AddChild(new HSeparator());
        content.AddChild(new Label { Text = "A gallery of 40 users" });
        AddToggle(content, "Animate gallery", false, value => { foreach (var avatar in _gallery) avatar.Animate = value; });
        var grid = new GridContainer { Columns = 8, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        content.AddChild(grid);
        grid.Resized += () => grid.Columns = Math.Clamp((int)(grid.Size.X / 96), 1, 8);
        for (var i = 1; i <= 40; i++)
        {
            var item = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var avatar = new BlobatarControl { Seed = $"user{i}@example.com", CustomMinimumSize = new Vector2(64, 64), Gaze = GazeMode.Pointer };
            _gallery.Add(avatar);
            item.AddChild(avatar);
            item.AddChild(new Label { Text = $"user{i}", HorizontalAlignment = HorizontalAlignment.Center });
            grid.AddChild(item);
        }
        if (OS.GetCmdlineUserArgs().Contains("--smoke-test")) CallDeferred(nameof(RunSmokeTest));
    }

    static void AddToggle(VBoxContainer parent, string text, bool initial, Action<bool> changed)
    {
        var toggle = new CheckButton { Text = text, ButtonPressed = initial };
        toggle.Toggled += value => changed(value);
        parent.AddChild(toggle);
    }
    static void AddSlider(VBoxContainer parent, double min, double max, double initial, double step, Action<double> changed)
    {
        var slider = new HSlider { MinValue = min, MaxValue = max, Value = initial, Step = step };
        slider.ValueChanged += value => changed(value);
        parent.AddChild(slider);
    }
    static OptionButton AddPicker<T>(VBoxContainer parent, string title, Action<T> changed) where T : struct, Enum
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = title, CustomMinimumSize = new Vector2(110, 0) });
        var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var values = Enum.GetValues<T>();
        foreach (var value in values) picker.AddItem(value.ToString());
        picker.ItemSelected += index => changed(values[(int)index]);
        row.AddChild(picker);
        parent.AddChild(row);
        return picker;
    }
}
