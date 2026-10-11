# Blobatar.Net.WinForms

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

Part of [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net), an independent .NET port
maintained by MilaDesign and based on [Alain00/blobatar v2.7.0](https://github.com/Alain00/blobatar/tree/v2.7.0).

Install from NuGet using the command below. For development or offline use, reference the
project directly.

Windows Forms adapter for [blobatar](https://github.com/Alain00/blobatar) — deterministic geometric avatars generated from any string.

```shell
dotnet add package Blobatar.Net.WinForms
```

`Blobatar.Net.Core` is a dependency: the adapter carries the Windows Forms control with GDI+ rendering.

## Usage

```csharp
using Blobatar.Net.Core;
using Blobatar.Net.WinForms;

var blob = new BlobatarControl
{
    Seed = "MilaDesign",
    Width = 64,
    Height = 64,
    Animate = true,
    Gaze = GazeMode.Pointer
};
this.Controls.Add(blob);
```

## Features

- High performance GDI+ rendering with anti-aliasing and double buffering.
- Transparent background support.
- Idle animations (breathing, bobbing, blinking, saccades) driven by synchronized clock.
- Pointer gaze tracking and expression morphing.

## Compatibility and sample

.NET 6–10 on Windows. The sample targets .NET 6, 7 and 8.

[Sample and run instructions](https://github.com/MilaDesign/Blobatar.Net/tree/main/samples/Blobatar.Net.WinForms.Sample).

## License and attribution

MIT. Original Blobatar copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026
MilaDesign. This package includes `LICENSE` and `THIRD-PARTY-NOTICES.md`, preserving upstream
attribution. See the [repository license](https://github.com/MilaDesign/Blobatar.Net/blob/main/LICENSE).
