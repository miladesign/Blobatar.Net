# Blobatar.Net.Maui

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

Part of [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net), an independent .NET port
maintained by MilaDesign and based on [Alain00/blobatar v2.7.0](https://github.com/Alain00/blobatar/tree/v2.7.0).

Install from NuGet using the command below. For development or offline use, reference the
project directly.

MAUI `BlobatarControl` backed by the shared Blobatar.Net.Core geometry and animation engine.
Draws with `GraphicsView` and `Microsoft.Maui.Graphics`; no SVG decoder or Skia dependency.

```shell
dotnet add package Blobatar.Net.Maui
```

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:bl="https://github.com/MilaDesign/Blobatar.Net/maui">
    <bl:BlobatarControl Seed="MilaDesign"
                     WidthRequest="96" HeightRequest="96"
                     Animate="True" Expression="Happy" Gaze="Pointer" />
</ContentPage>
```

The assembly maps the URL through `XmlnsDefinition` and suggests `bl` with `XmlnsPrefix`. MAUI protects its default
XAML namespace, so this control uses a separate namespace. An explicit CLR namespace also works:
`xmlns:bl="clr-namespace:Blobatar.Net.Maui;assembly=Blobatar.Net.Maui"`.

Properties match the existing controls: `Seed`, `Hue`, `Tone`, `Backdrop`, `Normalize`,
`EnforceContrast`, `Expression`, `Animate`, `Gaze`, `GazeTarget`, and `GazeTravel`.
Use MAUI's `WidthRequest` and `HeightRequest` for sizing. Avatars stay square and centred.

Static rendering is the default. With `Animate="True"`, idle animation, expression morphs,
and gaze use the shared Core scene. Pointer gaze follows the mouse across the page, including
over sibling controls. Leaving the page or deactivating the window returns the eyes to rest.
Touch gaze follows touches inside the avatar until release. Tracking is shared by avatars
in the same window and detached when no animated pointer avatars need it. `GazeTarget` accepts a `VisualElement`
in the same window, overriding pointer gaze. Ordinary layout, translation and scroll offsets
are supported for target positioning; rotated or scaled ancestors are not accounted for.
Animation timers stop when the control unloads or its window stops, and hidden ancestors
pause the scene.

On Windows, tracking observes native window pointer events even when a child handles them.
No mouse button needs to be pressed. Coordinates are transformed into each avatar's native
view, including scroll and layout changes.

The package targets .NET 8–10 with matching MAUI dependencies selected in `MauiVersions.props`.
It includes Windows-specific assemblies (`net8.0-windows10.0.19041.0` through
`net10.0-windows10.0.19041.0`) for native window tracking, alongside the portable assemblies.
MAUI Windows applications select the Windows assembly automatically.
.NET 6/7 MAUI used legacy workload-distributed assemblies that are unavailable in this setup;
these targets are not included.
The sample defaults to .NET 10 and the host's desktop target (Windows or Mac Catalyst),
or Android on Linux. Mobile and older .NET builds require the corresponding MAUI workloads
and platform tools.

```powershell
dotnet build samples/Blobatar.Net.Maui.Sample -t:Run -f net10.0-windows10.0.19041.0
dotnet build samples/Blobatar.Net.Maui.Sample -t:Run -p:BlobatarMauiSampleTargetFrameworks=net10.0-android
```

Build this multi-target repository with the .NET 10 SDK. Select an older sample target with
`-p:BlobatarMauiDotNetVersion=8` (8–10); compatible MAUI workloads and platform tooling are
required. The .NET 10 Windows sample is validated here; older sample configurations and mobile/Apple
platforms have not been built here. To restore/build only one library target, for example while preparing
an offline feed, use `-p:BlobatarMauiTargetFrameworks=net10.0`.
For a Windows application, select `net10.0-windows10.0.19041.0` instead so the native
implementation is included.

## Compatibility and sample

The Windows sample is verified on .NET 10. Older sample targets and mobile/Apple hosts require
matching workloads; they have not been validated in this workspace.

[Sample and run instructions](https://github.com/MilaDesign/Blobatar.Net/tree/main/samples/Blobatar.Net.Maui.Sample).

## License and attribution

MIT. Original Blobatar copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026
MilaDesign. This package includes `LICENSE` and `THIRD-PARTY-NOTICES.md`, preserving upstream
attribution. See the [repository license](https://github.com/MilaDesign/Blobatar.Net/blob/main/LICENSE).
