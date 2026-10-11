# Blobatar.Net.Wpf

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

Part of [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net), an independent .NET port
maintained by MilaDesign and based on [Alain00/blobatar v2.7.0](https://github.com/Alain00/blobatar/tree/v2.7.0).

Install from NuGet using the command below. For development or offline use, reference the
project directly.

WPF adapter for [blobatar](https://github.com/Alain00/blobatar) — deterministic geometric avatars generated from any string.

```shell
dotnet add package Blobatar.Net.Wpf
```

`Blobatar.Net.Core` is a dependency: the adapter carries the WPF control and rendering logic.

## Usage

Mapped to WPF's default presentation namespace (`http://schemas.microsoft.com/winfx/2006/xaml/presentation`), so no custom `xmlns` declaration is required:

```xml
<BlobatarControl Seed="MilaDesign" Width="64" Height="64" />
```

A custom namespace is also available. Declare this on the root element:

```xml
xmlns:bl="https://github.com/MilaDesign/Blobatar.Net/wpf"
```

Then use `<bl:BlobatarControl ... />`. Assembly `XmlnsDefinition` maps the URL to the CLR
namespace, and `XmlnsPrefix` suggests `bl`. The default namespace usage above remains valid.

## Animation & Expressions

Set `Animate="True"` to enable idle animations (breathing, bobbing, blinking, saccades):

```xml
<BlobatarControl Seed="MilaDesign"
                 Width="120"
                 Height="120"
                 Animate="True"
                 Expression="Happy" />
```

Supported expressions include `Idle`, `Happy`, `Sad`, `Mad`, `Surprised`, `Wink`, `Sleepy`, `Smug`, `Unsure`, `Scared`, `Love`, `Shy`, `Sick`, and `Thinking`.

## Following the Pointer (Gaze)

With `Animate="True"`, setting `Gaze="Pointer"` makes the eyes smoothly track the cursor across the window:

```xml
<BlobatarControl Seed="MilaDesign"
                 Width="120"
                 Height="120"
                 Animate="True"
                 Gaze="Pointer"
                 GazeTravel="3.0" />
```

- `Gaze="Pointer"`: Follow the cursor while it is over the window. Eyes ease back home when the pointer leaves the window.
- `Gaze="Rest"`: Look straight ahead and keep the idle glances off.
- `GazeTarget="{Binding ElementName=TargetElement}"`: Looks at a specific element's center instead of the pointer.
- `GazeTravel`: How far the eyes travel at full look in design units (default `3.0`, useful range `1.5` to `4.0`).

## Compatibility and sample

.NET 6–10 on Windows. The sample targets .NET 6, 7 and 8.

[Sample and run instructions](https://github.com/MilaDesign/Blobatar.Net/tree/main/samples/Blobatar.Net.Wpf.Sample).

## License and attribution

MIT. Original Blobatar copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026
MilaDesign. This package includes `LICENSE` and `THIRD-PARTY-NOTICES.md`, preserving upstream
attribution. See the [repository license](https://github.com/MilaDesign/Blobatar.Net/blob/main/LICENSE).
