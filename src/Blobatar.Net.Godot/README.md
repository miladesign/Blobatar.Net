# Blobatar.Net.Godot

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

Part of [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net), an independent .NET port
maintained by MilaDesign and based on [Alain00/blobatar v2.7.0](https://github.com/Alain00/blobatar/tree/v2.7.0).

Install from NuGet using the command below. For development or offline use, reference the
project directly.

Godot .NET `BlobatarControl` using Blobatar.Net.Core for deterministic avatars, expressions,
idle animation and gaze. Requires Godot's **.NET edition** with the SVG module (included in
official builds).

```shell
dotnet add package Blobatar.Net.Godot
```

Use the control from C#:

```csharp
var avatar = new Blobatar.Net.Godot.BlobatarControl
{
    Seed = "MilaDesign",
    CustomMinimumSize = new Godot.Vector2(96, 96),
    Animate = true,
    Gaze = Blobatar.Net.Core.GazeMode.Pointer,
};
AddChild(avatar);
```

To add it through the editor, put this **project-local** script in `BlobatarControl.cs`, build
your Godot project, then choose `BlobatarControl` in Add Node:

```csharp
using Godot;
[Tool, GlobalClass]
public partial class BlobatarControl : Blobatar.Net.Godot.BlobatarControl { }
```

The sample includes this wrapper and a saved scene with an editor preview. Exported properties
appear in the Inspector: `Seed`, `Backdrop`, `Normalize`, `EnforceContrast`, `Expression`,
`Animate`, `Gaze`, `GazeTarget` and `GazeTravel`. C# also exposes nullable `Hue` and `Tone`;
the Inspector uses `LockHue`/`HueValue` and `LockTone`/`ToneValue` for these optional overrides.
Use Godot's `Size`, `CustomMinimumSize`, anchors and containers for sizing.

The NuGet package installs `addons/Blobatar.Net.Godot/BlobatarControl.cs` when the Godot
project builds. This resource belongs to the package assembly and is excluded from your
project's C# compilation. The package also registers its script before Godot loads your
scripts, giving the base control a stable path across editor assembly reloads. Keep the
generated resource for editor use and exports; you can ignore it in source control.
For a project reference instead of NuGet, import `buildTransitive/Blobatar.Net.Godot.targets`
and set `BlobatarGodotScriptSource` to the library's `BlobatarControl.cs`, as the sample does.

Static rendering is the default, including in the editor. With `Animate = true`, `_Process`
updates Core's scene and queues a redraw. Mouse gaze reads the whole viewport, including when
the mouse is over other controls, without requiring a click. `GazeTarget` accepts a `Control`
in the same viewport and takes priority. Coordinate conversion accounts for canvas transforms,
rotation, scaling and scrolling. Hidden controls pause gaze updates; Godot's normal scene-tree
pause rules apply. This control ignores mouse input so it doesn't block underlying UI.
Mouse entry/exit, resizing and visibility use native Godot notifications without signal
subscriptions, so assembly reloads don't leave callbacks pointing at unloaded C# delegates.

Rendering uses Godot's built-in SVG loader to rasterize each part into a cached white texture.
Native canvas transforms and tint apply animated expressions, colours and gaze to those textures.
Textures are rebuilt when geometry or display resolution changes, and released on tree exit.
Raster resolution follows the displayed size, up to 2048 pixels per part; each avatar stays
square and centred in its allocated rectangle.

| .NET target | Godot bindings |
| --- | --- |
| .NET 6 / 7 | Godot 4.3.0 |
| .NET 8 / 9 / 10 | Godot 4.7.0 |

Versions are selected per target by `GodotVersions.props`. Use an engine matching the bindings.
Godot 4.7's bindings require .NET 8 or later. Older targets compile against 4.3; they aren't
validated in a 4.3 engine here. The included sample defaults to .NET 8 / Godot 4.7.
The sample supports .NET 8–10, selected with `-p:BlobatarGodotDotNetVersion=9` or `10`.
For .NET 6/7, reference the package from a Godot 4.3 project using that engine's matching SDK.

Open `samples/Blobatar.Net.Godot.Sample/project.godot` in the matching .NET Godot editor,
build, and press F6/F5. Or build with `dotnet build samples/Blobatar.Net.Godot.Sample`
and run `godot --path samples/Blobatar.Net.Godot.Sample` using your engine's executable.

## Compatibility and sample

The .NET 8–10 sample is verified with Godot 4.7 .NET. The .NET 6/7 library targets compile
against Godot 4.3; runtime behavior in that engine has not been verified here.

[Sample and run instructions](https://github.com/MilaDesign/Blobatar.Net/tree/main/samples/Blobatar.Net.Godot.Sample).

## License and attribution

MIT. Original Blobatar copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026
MilaDesign. This package includes `LICENSE` and `THIRD-PARTY-NOTICES.md`, preserving upstream
attribution. See the [repository license](https://github.com/MilaDesign/Blobatar.Net/blob/main/LICENSE).
