using Blobatar.Net.Core;

namespace Blobatar.Net.Maui.Sample;

public partial class MainPage : ContentPage
{
    readonly List<BlobatarControl> _avatars = new();
    bool _ready;

    public MainPage()
    {
        InitializeComponent();
        BackdropPicker.ItemsSource = Enum.GetValues<BackdropStyle>();
        ExpressionPicker.ItemsSource = Enum.GetValues<ExpressionKind>();
        GazePicker.ItemsSource = Enum.GetValues<GazeMode>();
        BackdropPicker.SelectedItem = BackdropStyle.None;
        ExpressionPicker.SelectedItem = ExpressionKind.Idle;
        GazePicker.SelectedItem = GazeMode.Pointer;
        for (var i = 1; i <= 40; i++)
        {
            var seed = $"user{i}@example.com";
            var avatar = new BlobatarControl { Seed = seed, WidthRequest = 64, HeightRequest = 64, Gaze = GazeMode.Pointer };
            _avatars.Add(avatar);
            var item = new VerticalStackLayout { WidthRequest = 88, Spacing = 4, Margin = new Thickness(4) };
            item.Children.Add(avatar);
            item.Children.Add(new Label { Text = $"user{i}", FontSize = 12, HorizontalTextAlignment = TextAlignment.Center });
            Gallery.Children.Add(item);
        }
        _ready = true;
#if BLOBATAR_POINTER_SMOKE_TEST
        Loaded += OnPointerSmokeLoaded;
#endif
    }

    void OnPaletteChanged(object? sender, EventArgs e)
    {
        if (!_ready) return;
        Preview.Hue = LockHue.IsToggled ? HueSlider.Value : null;
        Preview.Tone = LockTone.IsToggled ? ToneSlider.Value : null;
    }

    void OnBackdropChanged(object? sender, EventArgs e)
    {
        if (BackdropPicker.SelectedItem is BackdropStyle value) Preview.Backdrop = value;
    }

    void OnExpressionChanged(object? sender, EventArgs e)
    {
        if (ExpressionPicker.SelectedItem is ExpressionKind value) Preview.Expression = value;
    }

    void OnGazeChanged(object? sender, EventArgs e)
    {
        if (GazePicker.SelectedItem is GazeMode value) Preview.Gaze = value;
    }

    void OnGalleryAnimationChanged(object? sender, ToggledEventArgs e)
    {
        foreach (var avatar in _avatars) avatar.Animate = e.Value;
    }
}
