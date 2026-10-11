using System.Reflection;
using Blobatar.Net.Core;
using Godot;

public partial class Main
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    async void RunSmokeTest()
    {
        try
        {
            Check(GetNode<BlobatarControl>("EditorPreview").Seed == "editor@example.com", "Inherited exported Seed must deserialize from the saved scene.");
            var names = _preview.GetPropertyList().Select(p => p["name"].AsString()).ToArray();
            foreach (var name in new[] { "Seed", "Expression", "Animate", "GazeTarget", "LockHue", "HueValue" })
                Check(names.Contains(name), $"Missing inherited Inspector property: {name}.");
            var direct = new Blobatar.Net.Godot.BlobatarControl { Seed = "direct-package@example.com", Size = new Vector2(96, 96) };
            AddChild(direct);
            var window = direct.GetWindow();
            foreach (var signal in new[] { Window.SignalName.MouseEntered, Window.SignalName.MouseExited })
                Check(!window.GetSignalConnectionList(signal).Any(c => c["callable"].AsCallable().Target == direct),
                    "Controls must handle window notifications without mouse signal connections.");
            Check(direct.GetScript().As<Script>().ResourcePath == "res://addons/Blobatar.Net.Godot/BlobatarControl.cs",
                "Package controls must have a stable script resource path for editor reloads.");
            Check(_preview.GetScript().As<Script>().GetBaseScript().ResourcePath == "res://addons/Blobatar.Net.Godot/BlobatarControl.cs",
                "The local wrapper must reuse the registered package script.");
            // Reflecting game attributes again must not register the package twice.
            _ = typeof(Main).Assembly.GetCustomAttributes().ToArray();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(((List<ImageTexture>)typeof(Blobatar.Net.Godot.BlobatarControl).GetField("_textures", Private)!.GetValue(direct)!).Count > 0,
                "Programmatic package controls must draw without a project-local wrapper.");
            RemoveChild(direct);
            Check(((List<ImageTexture>)typeof(Blobatar.Net.Godot.BlobatarControl).GetField("_textures", Private)!.GetValue(direct)!).Count == 0,
                "Tree exit must release textures.");
            direct.Free();
            _preview.Animate = false;
            var cases = 0;
            foreach (var expression in Enum.GetValues<ExpressionKind>())
            foreach (var backdrop in Enum.GetValues<BackdropStyle>())
            {
                _preview.Expression = expression;
                _preview.Backdrop = backdrop;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var model = (BlobatarModel?)Field("_model");
                var textures = (List<ImageTexture>)Field("_textures")!;
                Check(model is not null && textures.Count == model.Marks.Count, $"Native textures not built for {expression}/{backdrop}.");
                Check(textures.All(t => t.GetWidth() >= 100 && t.GetHeight() >= 100), "Invalid raster dimensions.");
                Check((Field("_backdropTexture") is not null) == (backdrop != BackdropStyle.None), "Backdrop raster missing or unexpected.");
                Check(Field("_scene") is null, "Static avatars must not animate.");
                cases++;
            }
            _preview.Animate = true;
            _preview.Expression = ExpressionKind.Happy;
            _preview.Gaze = GazeMode.Pointer;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var oldTextures = ((List<ImageTexture>)Field("_textures")!).ToArray();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _preview.Expression = ExpressionKind.Sad;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(oldTextures.SequenceEqual((List<ImageTexture>)Field("_textures")!), "Animated expression changes must reuse neutral textures.");
            var target = new Control { Position = new Vector2(900, 400), Size = new Vector2(40, 40), MouseFilter = MouseFilterEnum.Ignore };
            AddChild(target);
            _preview.GazeTarget = target;
            object?[] arguments = { Vector2.Zero };
            var aim = (GazeAim)typeof(Blobatar.Net.Godot.BlobatarControl).GetMethod("Aim", Private)!.Invoke(_preview, arguments)!;
            Check(aim == GazeAim.Look && ((Vector2)arguments[0]!).Length() > 100, "Gaze must follow a sibling target outside the avatar.");
            _preview.GazeTarget = null;
            if (OS.GetCmdlineUserArgs().Contains("--native-pointer-test"))
            {
                Check(DisplayServer.GetName() != "headless", "Native pointer verification requires a desktop window.");
                GetWindow().GrabFocus();
                foreach (var point in new[] { new Vector2(30, 200), new Vector2(1000, 200) })
                {
                    Check(!Input.IsMouseButtonPressed(MouseButton.Left), "Pointer test requires no mouse button held.");
                    GetViewport().WarpMouse(point);
                    await ToSignal(GetTree().CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
                    var expected = _preview.GetGlobalTransformWithCanvas().AffineInverse() * GetViewport().GetMousePosition() - _preview.Size / 2;
                    aim = (GazeAim)typeof(Blobatar.Net.Godot.BlobatarControl).GetMethod("Aim", Private)!.Invoke(_preview, arguments)!;
                    Check(aim == GazeAim.Look && Math.Abs(expected.X) > _preview.Size.X / 2 && ((Vector2)arguments[0]!).DistanceTo(expected) < 2,
                        "Native mouse gaze must follow outside the avatar without a click.");
                }
                GD.Print("BLOBATAR_GODOT_NATIVE_POINTER_PASS: mouse moved to both sides outside the avatar, without clicking.");
                var originalMouse = DisplayServer.MouseGetPosition();
                try
                {
                    var client = new Rect2(DisplayServer.WindowGetPosition(window.GetWindowId()), DisplayServer.WindowGetSize(window.GetWindowId()));
                    var screen = DisplayServer.ScreenGetUsableRect(window.CurrentScreen);
                    var outside = new[] { client.Position + new Vector2(-20, 100), client.Position + new Vector2(client.Size.X + 20, 100),
                        client.Position + new Vector2(100, -20), client.Position + new Vector2(100, client.Size.Y + 20) }
                        .First(p => screen.HasPoint((Vector2I)p) && !client.HasPoint(p));
                    foreach (var pass in Enumerable.Range(0, 3))
                    {
                        DisplayServer.WarpMouse((Vector2I)(outside - client.Position));
                        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
                        Check(!client.HasPoint(DisplayServer.MouseGetPosition()), "The native cursor must leave the game window.");
                        Check((GazeAim)typeof(Blobatar.Net.Godot.BlobatarControl).GetMethod("Aim", Private)!.Invoke(_preview, arguments)! == GazeAim.Rest,
                            "Leaving the actual game window must reset gaze without signal callbacks.");
                        GetViewport().WarpMouse(new Vector2(30, 200));
                        await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
                        Check((GazeAim)typeof(Blobatar.Net.Godot.BlobatarControl).GetMethod("Aim", Private)!.Invoke(_preview, arguments)! == GazeAim.Look,
                            "Re-entering the actual game window must restore pointer gaze.");
                    }
                    GD.Print("BLOBATAR_GODOT_WINDOW_BOUNDARY_PASS: 3 actual window exits and entries.");
                }
                finally { DisplayServer.WarpMouse(originalMouse - DisplayServer.WindowGetPosition(window.GetWindowId())); }
            }
            window.PropagateNotification((int)Node.NotificationWMMouseExit);
            Check((GazeAim)typeof(Blobatar.Net.Godot.BlobatarControl).GetMethod("Aim", Private)!.Invoke(_preview, arguments)! == GazeAim.Rest, "Mouse exit must reset gaze.");
            window.PropagateNotification((int)Node.NotificationWMMouseEnter);
            Check((bool)Field("_mouseInside")!, "Mouse entry must restore gaze tracking through native notifications.");
            target.QueueFree();
            GD.Print($"BLOBATAR_GODOT_SMOKE_PASS: {cases} expression/backdrop combinations, inherited Inspector properties, cached animation, target gaze and mouse exit.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError("BLOBATAR_GODOT_SMOKE_FAIL: " + error);
            GetTree().Quit(1);
        }
    }

    object? Field(string name) => typeof(Blobatar.Net.Godot.BlobatarControl).GetField(name, Private)!.GetValue(_preview);
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
