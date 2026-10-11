using Avalonia.Controls;
using Blobatar.Net.Core;

namespace Blobatar.Net.Avalonia.Sample;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        BackdropBox.ItemsSource = Enum.GetValues<BackdropStyle>();
        BackdropBox.SelectedItem = BackdropStyle.None;
        ExpressionBox.ItemsSource = Enum.GetValues<ExpressionKind>();
        ExpressionBox.SelectedItem = ExpressionKind.Idle;
        GazeBox.ItemsSource = Enum.GetValues<GazeMode>();
        GazeBox.SelectedItem = GazeMode.Pointer;
        Gallery.ItemsSource = Enumerable.Range(1, 40).Select(i => $"user{i}").ToList();

        LockHue.IsCheckedChanged += (_, _) => ApplyLocks();
        LockTone.IsCheckedChanged += (_, _) => ApplyLocks();
        HueSlider.ValueChanged += (_, _) => ApplyLocks();
        ToneSlider.ValueChanged += (_, _) => ApplyLocks();
    }

    // Hue/Tone are nullable on the control: null means "derive from the seed".
    void ApplyLocks()
    {
        Preview.Hue = LockHue.IsChecked == true ? HueSlider.Value : null;
        Preview.Tone = LockTone.IsChecked == true ? ToneSlider.Value : null;
    }
}
