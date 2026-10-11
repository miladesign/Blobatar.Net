using Blobatar.Net.Core;
using System.Windows.Media;

namespace Blobatar.Net.Wpf;

/// <summary>
/// One shared clock and one render hook for every animated blobatar. The idle layer is a pure
/// function of time, so blobatars that read the same clock stay in step with no state of their own.
/// </summary>
internal static class BlobatarClock
{
    static Action<double>? _tick;
    static int _count;

    public static double Now => Clock.Now;

    public static void Subscribe(Action<double> handler)
    {
        _tick += handler;
        if (++_count == 1) CompositionTarget.Rendering += OnRendering;
    }

    public static void Unsubscribe(Action<double> handler)
    {
        _tick -= handler;
        if (--_count == 0) CompositionTarget.Rendering -= OnRendering;
    }

    static void OnRendering(object? sender, EventArgs e) => _tick?.Invoke(Now);
}
