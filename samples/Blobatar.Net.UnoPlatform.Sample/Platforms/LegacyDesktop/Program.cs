#if NET7_0
using Uno.UI.Runtime.Skia.Gtk;
#else
using Uno.UI.Runtime.Skia;
#endif

namespace Blobatar.Net.UnoPlatform.Sample;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = new GtkHost(() => new App());
        host.Run();
    }
}
