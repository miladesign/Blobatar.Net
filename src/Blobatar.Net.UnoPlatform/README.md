# Blobatar.Net.UnoPlatform

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

Part of [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net), an independent .NET port
maintained by MilaDesign and based on [Alain00/blobatar v2.7.0](https://github.com/Alain00/blobatar/tree/v2.7.0).

Install from NuGet using the command below. For development or offline use, reference the
project directly.

Uno Platform adapter for [blobatar](https://github.com/Alain00/blobatar) — deterministic geometric avatars generated from any string.

Targets .NET 6–10 with framework-specific Uno.WinUI dependencies: 4.10.39 (.NET 6), 5.2.190 (.NET 7),
5.6.99 (.NET 8), 6.2.58 (.NET 9), and 6.6.184 (.NET 10).

```shell
dotnet add package Blobatar.Net.UnoPlatform
```

`Blobatar.Net.Core` is a dependency: the adapter carries the WinUI / Uno Platform control.

## Usage

```xml
<Page xmlns:bl="using:Blobatar.Net.UnoPlatform">
    <bl:BlobatarControl Seed="MilaDesign" Width="64" Height="64" />
</Page>
```

The control uses plain WinUI shapes. This package ships the targets listed above; mobile and
WebAssembly hosts may need their own compatible Uno target configuration. The included sample
validates desktop use. The `using:` declaration is intentional for compatibility across the
selected Uno versions; no GitHub URL mapping or `XmlnsPrefix` is required for WinUI XAML.

## Animation & Expressions

Set `Animate="True"` to enable idle animations (breathing, bobbing, blinking, saccades):

```xml
<bl:BlobatarControl Seed="MilaDesign"
                    Width="120"
                    Height="120"
                    Animate="True"
                    Expression="Happy" />
```

Supported expressions include `Idle`, `Happy`, `Sad`, `Mad`, `Surprised`, `Wink`, `Sleepy`, `Smug`, `Unsure`, `Scared`, `Love`, `Shy`, `Sick`, and `Thinking`.

## Following the Pointer (Gaze)

With `Animate="True"`, setting `Gaze="Pointer"` makes the eyes smoothly follow the cursor:

```xml
<bl:BlobatarControl Seed="MilaDesign"
                    Width="120"
                    Height="120"
                    Animate="True"
                    Gaze="Pointer"
                    GazeTravel="3.0" />
```

- `Gaze="Pointer"`: Follow the cursor while over the window. Eyes park home when the cursor leaves the window.
- `Gaze="Rest"`: Look straight ahead and keep the idle glances off.
- `GazeTarget="{Binding ElementName=TargetElement}"`: Looks at a specific element's center.
- `GazeTravel`: How far the eyes travel at full look (default `3.0`).

## Compatibility and sample

See the per-target Uno versions above and the sample README for GTK/Skia host requirements.

[Sample and run instructions](https://github.com/MilaDesign/Blobatar.Net/tree/main/samples/Blobatar.Net.UnoPlatform.Sample).

## License and attribution

MIT. Original Blobatar copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026
MilaDesign. This package includes `LICENSE` and `THIRD-PARTY-NOTICES.md`, preserving upstream
attribution. See the [repository license](https://github.com/MilaDesign/Blobatar.Net/blob/main/LICENSE).
