using System.Windows;
using Blobatar.Net.Core;
using Blobatar.Net.Wpf;

namespace Blobatar.Net.Wpf.Sample;

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
    }

    // Hue/Tone are nullable on the control: null means "derive from the seed".
    void OnLockChanged(object sender, RoutedEventArgs e)
    {
        if (Preview is null || LockHue is null || LockTone is null) return;
        Preview.Hue = LockHue.IsChecked == true ? HueSlider.Value : null;
        Preview.Tone = LockTone.IsChecked == true ? ToneSlider.Value : null;
    }
}
