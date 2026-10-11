using System.Reflection;
using Blobatar.Net.Core;
using Blobatar.Net.Maui;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blobatar.Net.Maui.Tests;

[TestClass]
public class PointerTests
{
    [TestMethod]
    public void MovingOverSiblingAndLeavingAvatarKeepsGazeUntilPageExit()
    {
        var avatar = new BlobatarControl { Gaze = GazeMode.Pointer };
        var sibling = new Label { Text = "Move here" };
        var layout = new Grid();
        layout.Children.Add(avatar);
        layout.Children.Add(sibling);
        var page = new ContentPage { Content = layout };
        var window = new Window(page);
        ((IView)page).Arrange(new Rect(0, 0, 600, 400));
        ((IView)layout).Arrange(new Rect(0, 0, 600, 400));
        ((IView)avatar).Arrange(new Rect(50, 70, 100, 100));
        var tracker = WindowPointer.Acquire(window);
        typeof(BlobatarControl).GetField("_windowPointer", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(avatar, tracker);
        try
        {
            Send(sibling, "Moved", new Point(500, 300));
            var (aim, dx, dy) = Aim(avatar);
            Assert.AreEqual(GazeAim.Look, aim);
            Assert.AreEqual(400.0, dx);
            Assert.AreEqual(180.0, dy);

            Send(avatar, "Exited", new Point(500, 300));
            Assert.AreEqual(GazeAim.Look, Aim(avatar).Aim, "Leaving the avatar must not reset gaze.");

            avatar.TranslationX = 20;
            Assert.AreEqual(380.0, Aim(avatar).Dx, "Layout movement must update gaze without another mouse event.");

            Send(layout, "Exited", new Point(601, 300));
            Assert.AreEqual(GazeAim.Rest, Aim(avatar).Aim);
        }
        finally { tracker.Release(); }
    }

    [TestMethod]
    public void SharedTrackerObservesNewViewsAndRemovesOnlyItsOwnGestures()
    {
        var layout = new Grid();
        var userGesture = new TapGestureRecognizer();
        layout.GestureRecognizers.Add(userGesture);
        var window = new Window(new ContentPage { Content = layout });
        var first = WindowPointer.Acquire(window);
        var second = WindowPointer.Acquire(window);
        Assert.AreSame(first, second);
        Assert.HasCount(1, layout.GestureRecognizers.OfType<PointerGestureRecognizer>().ToArray());
        var sibling = new Label();
        layout.Children.Add(sibling);
        Assert.HasCount(1, sibling.GestureRecognizers.OfType<PointerGestureRecognizer>().ToArray());
        layout.Children.Remove(sibling);
        Assert.IsEmpty(sibling.GestureRecognizers);
        first.Release();
        Assert.HasCount(1, layout.GestureRecognizers.OfType<PointerGestureRecognizer>().ToArray());
        second.Release();
        CollectionAssert.AreEqual(new IGestureRecognizer[] { userGesture }, layout.GestureRecognizers.ToArray());
        var afterRelease = new Label();
        layout.Children.Add(afterRelease);
        Assert.IsEmpty(afterRelease.GestureRecognizers);
    }

    static void Send(View view, string kind, Point position)
    {
        var pointer = view.GestureRecognizers.OfType<PointerGestureRecognizer>().Single();
        // Simulate the MAUI platform dispatch, including its coordinate conversion.
        Func<IElement?, Point?> getPosition = _ => position;
        var method = typeof(PointerGestureRecognizer).GetMethod("SendPointer" + kind, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var arguments = method.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray();
        arguments[0] = view;
        arguments[1] = getPosition;
        method.Invoke(pointer, arguments);
    }

    static (GazeAim Aim, double Dx, double Dy) Aim(BlobatarControl avatar)
    {
        object?[] arguments = { 0.0, 0.0 };
        var aim = (GazeAim)typeof(BlobatarControl).GetMethod("Aim", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(avatar, arguments)!;
        return (aim, (double)arguments[0]!, (double)arguments[1]!);
    }
}
