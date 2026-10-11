using Uno.UI.Runtime.Skia;

#if NET9_0_OR_GREATER
using Uno.UI.Hosting;
#endif

namespace Blobatar.Net.UnoPlatform.Sample;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
#if NET9_0_OR_GREATER
        var host = UnoPlatformHostBuilder.Create()
#else
        var host = SkiaHostBuilder.Create()
#endif
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();

        host.Run();
    }
}
