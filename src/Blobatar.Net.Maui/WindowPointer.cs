using System.Runtime.CompilerServices;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

[assembly: InternalsVisibleTo("Blobatar.Net.Maui.Tests")]

namespace Blobatar.Net.Maui;

// One observer per window, shared by every animated avatar.
internal sealed partial class WindowPointer
{
    static readonly ConditionalWeakTable<Window, WindowPointer> Trackers = new();
#if !WINDOWS
    readonly Dictionary<View, PointerGestureRecognizer> _views = new();
#endif
    int _users;

    internal Window Window { get; }
    internal Point? Position { get; private set; }

    WindowPointer(Window window)
    {
        Window = window;
#if WINDOWS
        window.HandlerChanged += OnHandlerChanged;
        AttachNativeWindow();
#else
        window.DescendantAdded += OnAdded;
        window.DescendantRemoved += OnRemoved;
        Attach(window);
#endif
        window.Deactivated += OnDeactivated;
    }

    internal static WindowPointer Acquire(Window window)
    {
        var tracker = Trackers.GetValue(window, w => new WindowPointer(w));
        tracker._users++;
        return tracker;
    }

    internal void Release()
    {
        if (--_users != 0) return;
#if WINDOWS
        Window.HandlerChanged -= OnHandlerChanged;
        DetachNativeWindow();
#else
        Window.DescendantAdded -= OnAdded;
        Window.DescendantRemoved -= OnRemoved;
        foreach (var view in _views.Keys.ToArray()) DetachView(view);
#endif
        Window.Deactivated -= OnDeactivated;
        Position = null;
        Trackers.Remove(Window);
    }

#if !WINDOWS
    internal bool TryGetPosition(VisualElement view, out Point position)
    {
        position = default;
        if (Position is not { } pointer || Window.Page is not { } page) return false;
        var centre = BlobatarControl.Centre(view);
        var pageCentre = BlobatarControl.Centre(page);
        position = new Point(pointer.X + pageCentre.X - page.Width / 2 - centre.X + view.Width / 2,
            pointer.Y + pageCentre.Y - page.Height / 2 - centre.Y + view.Height / 2);
        return true;
    }
    void Attach(IVisualTreeElement element)
    {
        if (element is View view && !_views.ContainsKey(view))
        {
            var pointer = new PointerGestureRecognizer();
            pointer.PointerEntered += OnPointer;
            pointer.PointerMoved += OnPointer;
            pointer.PointerExited += OnPointer;
            // Register before adding the gesture, which itself raises DescendantAdded.
            _views.Add(view, pointer);
            view.GestureRecognizers.Add(pointer);
        }
        foreach (var child in element.GetVisualChildren().ToArray()) Attach(child);
    }

    void Detach(IVisualTreeElement element)
    {
        foreach (var child in element.GetVisualChildren().ToArray()) Detach(child);
        if (element is View view) DetachView(view);
    }

    void DetachView(View view)
    {
        if (!_views.Remove(view, out var pointer)) return;
        pointer.PointerEntered -= OnPointer;
        pointer.PointerMoved -= OnPointer;
        pointer.PointerExited -= OnPointer;
        view.GestureRecognizers.Remove(pointer);
    }

    void OnAdded(object? sender, ElementEventArgs e) => Attach(e.Element);
    void OnRemoved(object? sender, ElementEventArgs e) => Detach(e.Element);

    void OnPointer(object? sender, PointerEventArgs e)
    {
        // A child exit is still a valid position elsewhere in the page. Clear only
        // when the pointer leaves the page, not when it leaves an individual avatar.
        var page = Window.Page;
        var position = page is null ? null : e.GetPosition(page);
        Position = position is { } p && p.X >= 0 && p.Y >= 0 && p.X < page!.Width && p.Y < page.Height
            ? p : null;
    }
#endif

    void OnDeactivated(object? sender, EventArgs e) => Position = null;
}
