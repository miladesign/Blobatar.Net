using Blobatar.Net.Core;
using System;
using FormsTimer = System.Windows.Forms.Timer;

namespace Blobatar.Net.WinForms;

/// <summary>
/// One shared UI-thread timer for every animated blobatar. The idle layer is a pure function of
/// <see cref="Clock"/>, so blobatars on the same clock stay in step with no state of their own.
/// </summary>
internal static class BlobatarClock
{
    static readonly FormsTimer Timer = new() { Interval = 15 };
    static Action<double>? _tick;
    static int _count;

    static BlobatarClock() => Timer.Tick += (_, _) => _tick?.Invoke(Clock.Now);

    public static void Subscribe(Action<double> handler)
    {
        _tick += handler;
        if (++_count == 1) Timer.Start();
    }

    public static void Unsubscribe(Action<double> handler)
    {
        _tick -= handler;
        if (--_count == 0) Timer.Stop();
    }
}
