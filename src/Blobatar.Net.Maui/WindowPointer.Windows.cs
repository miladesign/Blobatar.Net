#if WINDOWS
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;

namespace Blobatar.Net.Maui;

internal sealed partial class WindowPointer
{
    FrameworkElement? _root;
    PointerEventHandler? _pointerHandler;

    void OnHandlerChanged(object? sender, EventArgs e) => AttachNativeWindow();

    void AttachNativeWindow()
    {
        DetachNativeWindow();
        if (Window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window { Content: FrameworkElement root }) return;
        _root = root;
        _pointerHandler = OnNativePointer;
        // GraphicsView and other native children handle these events themselves.
        // Observe the routed event even when handled; never capture or consume it.
        root.AddHandler(UIElement.PointerMovedEvent, _pointerHandler, true);
        root.AddHandler(UIElement.PointerEnteredEvent, _pointerHandler, true);
        root.AddHandler(UIElement.PointerExitedEvent, _pointerHandler, true);
        root.AddHandler(UIElement.PointerPressedEvent, _pointerHandler, true);
    }

    void DetachNativeWindow()
    {
        if (_root is not null && _pointerHandler is not null)
        {
            _root.RemoveHandler(UIElement.PointerMovedEvent, _pointerHandler);
            _root.RemoveHandler(UIElement.PointerEnteredEvent, _pointerHandler);
            _root.RemoveHandler(UIElement.PointerExitedEvent, _pointerHandler);
            _root.RemoveHandler(UIElement.PointerPressedEvent, _pointerHandler);
        }
        _root = null;
        _pointerHandler = null;
        Position = null;
    }

    void OnNativePointer(object sender, PointerRoutedEventArgs e)
    {
        if (_root is null) return;
        if (e.Pointer.PointerDeviceType != PointerDeviceType.Mouse)
        {
            // A touch on the avatar takes over from the last mouse position.
            Position = null;
            return;
        }
        var point = e.GetCurrentPoint(_root).Position;
        Position = point.X >= 0 && point.Y >= 0 && point.X < _root.ActualWidth && point.Y < _root.ActualHeight
            ? new Point(point.X, point.Y) : null;
    }

    internal bool TryGetPosition(VisualElement view, out Point position)
    {
        position = default;
        if (Position is not { } pointer || _root is null || view.Handler?.PlatformView is not UIElement nativeView) return false;
        var inverse = nativeView.TransformToVisual(_root).Inverse;
        if (inverse is null) return false;
        var local = inverse.TransformPoint(new Windows.Foundation.Point(pointer.X, pointer.Y));
        position = new Point(local.X, local.Y);
        return true;
    }
}
#endif
