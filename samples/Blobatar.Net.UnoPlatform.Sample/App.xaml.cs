using Microsoft.UI.Xaml;

namespace Blobatar.Net.UnoPlatform.Sample;

public partial class App : Application
{
    Window? _window;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new Window { Title = "Blobatar.Net sample (Uno Platform)", Content = new MainPage() };
        _window.Activate();
    }
}
