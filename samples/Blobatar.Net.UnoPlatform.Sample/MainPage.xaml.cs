using Blobatar.Net.Core;
using Blobatar.Net.UnoPlatform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Blobatar.Net.UnoPlatform.Sample;

public sealed partial class MainPage : Page
{
    readonly List<BlobatarControl> _gallery = new();

    public MainPage()
    {
        InitializeComponent();
        BackdropBox.ItemsSource = Enum.GetValues<BackdropStyle>();
        BackdropBox.SelectedItem = BackdropStyle.None;
        ExpressionBox.ItemsSource = Enum.GetValues<ExpressionKind>();
        ExpressionBox.SelectedItem = ExpressionKind.Idle;
        GazeBox.ItemsSource = Enum.GetValues<GazeMode>();
        GazeBox.SelectedItem = GazeMode.Pointer;
        
        for (var i = 1; i <= 40; i++)
        {
            var seed = $"user{i}";
            var blob = new BlobatarControl { Seed = seed, Width = 64, Height = 64, Gaze = GazeMode.Pointer };
            _gallery.Add(blob);
            var cell = new StackPanel { Width = 72, Margin = new Thickness(6), Spacing = 4 };
            cell.Children.Add(blob);
            cell.Children.Add(new TextBlock { Text = seed, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center });
            Gallery.Items.Add(cell);
        }
        
        LockHue.Checked += (_, _) => ApplyLocks();
        LockHue.Unchecked += (_, _) => ApplyLocks();
        LockTone.Checked += (_, _) => ApplyLocks();
        LockTone.Unchecked += (_, _) => ApplyLocks();
        HueSlider.ValueChanged += (_, _) => ApplyLocks();
        ToneSlider.ValueChanged += (_, _) => ApplyLocks();
        AnimateGallery.Checked += (_, _) => SetGalleryAnimation(true);
        AnimateGallery.Unchecked += (_, _) => SetGalleryAnimation(false);
    }

    // Hue/Tone are nullable on the control: null means "derive from the seed".
    void ApplyLocks()
    {
        Preview.Hue = LockHue.IsChecked == true ? HueSlider.Value : null;
        Preview.Tone = LockTone.IsChecked == true ? ToneSlider.Value : null;
    }

    void SetGalleryAnimation(bool on)
    {
        foreach (var blob in _gallery) blob.Animate = on;
    }
}
