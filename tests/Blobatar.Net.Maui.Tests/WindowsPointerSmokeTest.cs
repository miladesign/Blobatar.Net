// Compiled into the Windows sample only with -p:BlobatarPointerSmokeTest=true.
// Exercises real native mouse movement without synthesizing MAUI gesture callbacks.
#if WINDOWS && BLOBATAR_POINTER_SMOKE_TEST
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using NativePoint = Windows.Foundation.Point;

namespace Blobatar.Net.Maui.Sample;

public partial class MainPage
{
    async void OnPointerSmokeLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnPointerSmokeLoaded;
        var path = Environment.GetEnvironmentVariable("BLOBATAR_POINTER_TEST_RESULT");
        if (string.IsNullOrEmpty(path)) return;
        var results = new List<object>();
        GetCursorPos(out var previous);
        var previousWindow = GetForegroundWindow();
        try
        {
            await Task.Delay(1000);
            var nativeWindow = (Microsoft.UI.Xaml.Window)Window.Handler!.PlatformView!;
            var root = (FrameworkElement)nativeWindow.Content;
            var avatarView = (UIElement)Preview.Handler!.PlatformView!;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            SetForegroundWindow(hwnd);
            await Task.Delay(250);
            GetClientRect(hwnd, out var rect);
            var scale = (double)(rect.Right - rect.Left) / root.ActualWidth;
            var centre = avatarView.TransformToVisual(root).TransformPoint(new NativePoint(Preview.Width / 2, Preview.Height / 2));
            var entry = (FrameworkElement)SeedBox.Handler!.PlatformView!;
            var entryCentre = entry.TransformToVisual(root).TransformPoint(new NativePoint(entry.ActualWidth / 2, entry.ActualHeight / 2));
            var targets = new[]
            {
                (Name: "outside avatar, left", Point: new NativePoint(10, centre.Y)),
                (Name: "outside avatar, right", Point: new NativePoint(root.ActualWidth - 10, centre.Y)),
                (Name: "over Entry native child", Point: entryCentre),
            };
            foreach (var target in targets)
            {
                if ((GetAsyncKeyState(1) & 0x8000) != 0) throw new InvalidOperationException("Left mouse button is down; hover test requires no button.");
                var screen = new WinPoint { X = (int)(target.Point.X * scale), Y = (int)(target.Point.Y * scale) };
                ClientToScreen(hwnd, ref screen);
                var inputs = new[] { new MouseInputEvent { Mouse = new MouseInput
                {
                    X = (screen.X - GetSystemMetrics(76)) * 65536 / GetSystemMetrics(78) + 1,
                    Y = (screen.Y - GetSystemMetrics(77)) * 65536 / GetSystemMetrics(79) + 1,
                    Flags = 0xC001,
                } } };
                if (SendInput(1, inputs, Marshal.SizeOf<MouseInputEvent>()) != 1) throw new InvalidOperationException("Mouse movement injection failed.");
                await Task.Delay(100);
                GetCursorPos(out var actualCursor);
                object?[] args = { 0.0, 0.0 };
                var aim = typeof(BlobatarControl).GetMethod("Aim", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Preview, args)!.ToString();
                var local = root.TransformToVisual(avatarView).TransformPoint(target.Point);
                var dx = (double)args[0]!;
                var dy = (double)args[1]!;
                var touch = (bool)typeof(BlobatarControl).GetField("_hasPointer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Preview)!;
                var tracker = typeof(BlobatarControl).GetField("_windowPointer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Preview);
                var observedRoot = tracker?.GetType().GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(tracker);
                var position = tracker?.GetType().GetProperty("Position", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(tracker);
                if (aim != "Look" || touch || Math.Abs(dx - (local.X - Preview.Width / 2)) > 20 || Math.Abs(dy - (local.Y - Preview.Height / 2)) > 20)
                    throw new InvalidOperationException($"{target.Name}: aim={aim}, dx={dx}, dy={dy}, touch={touch}; expected dx={local.X - Preview.Width / 2}, dy={local.Y - Preview.Height / 2}. tracker={tracker}, root={observedRoot}, position={position}, visible={IsWindowVisible(hwnd)}, window={hwnd}, hit root={GetAncestor(WindowFromPoint(screen), 2)}, screen={screen.X},{screen.Y}, actual cursor={actualCursor.X},{actualCursor.Y}, root size={root.ActualWidth},{root.ActualHeight}, scale={scale}.");
                results.Add(new { target.Name, Aim = aim, Dx = dx, Dy = dy, Touch = touch });
            }
            File.WriteAllText(path, JsonSerializer.Serialize(new { Passed = true, Results = results }));
        }
        catch (Exception error)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(new { Passed = false, Error = error.ToString(), Results = results }));
        }
        finally
        {
            SetCursorPos(previous.X, previous.Y);
            Microsoft.Maui.Controls.Application.Current!.CloseWindow(Window);
            SetForegroundWindow(previousWindow);
        }
    }

    [StructLayout(LayoutKind.Sequential)] struct WinPoint { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] struct WinRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] struct MouseInput { public int X, Y; public uint Data, Flags, Time; public nint Extra; }
    [StructLayout(LayoutKind.Explicit, Size = 40)] struct MouseInputEvent { [FieldOffset(0)] public uint Type; [FieldOffset(8)] public MouseInput Mouse; }
    [DllImport("user32.dll")] static extern uint SendInput(uint count, MouseInputEvent[] inputs, int size);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out WinPoint point);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern bool ClientToScreen(nint hwnd, ref WinPoint point);
    [DllImport("user32.dll")] static extern bool GetClientRect(nint hwnd, out WinRect rect);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] static extern nint WindowFromPoint(WinPoint point);
    [DllImport("user32.dll")] static extern nint GetAncestor(nint hwnd, uint flags);
    [DllImport("user32.dll")] static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(nint hwnd);
}
#endif
