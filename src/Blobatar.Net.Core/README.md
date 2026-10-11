# Blobatar.Net.Core

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

Part of [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net), an independent .NET port
maintained by MilaDesign and based on [Alain00/blobatar v2.7.0](https://github.com/Alain00/blobatar/tree/v2.7.0).

Install from NuGet using the command below. For development or offline use, reference the
project directly.

Deterministic geometric blobatars from any string. No dependencies (.NET port of [blobatar](https://github.com/Alain00/blobatar)).

```csharp
using Blobatar.Net.Core;

// Render SVG markup directly
string svg = BlobatarSvg.Render("alain@example.com"); // => '<svg xmlns="..." viewBox="0 0 100 100">…'

// Or compute model layout and geometry
BlobatarModel model = BlobatarFactory.Create("alain@example.com");
```

```shell
dotnet add package Blobatar.Net.Core
```

For UI framework controls, see the framework adapters:
- `Blobatar.Net.Wpf` — WPF control
- `Blobatar.Net.Avalonia` — Avalonia control
- `Blobatar.Net.UnoPlatform` — Uno Platform control
- `Blobatar.Net.WinForms` — Windows Forms control
- `Blobatar.Net.Maui` — MAUI control (.NET 8–10)
- `Blobatar.Net.Godot` — Godot .NET control (.NET 6–10, matching Godot bindings per target)

## When to use it

Reach for blobatar when something needs a picture of somebody it has no picture of: a user who has not uploaded an avatar, a commit author, a bot, a team, a repository, a seat in a list. It turns any string into a stable geometric face, so the same handle is the same creature everywhere it appears — with nothing stored, no upload, and no network request if you render in-process.

## Shapes

A soft body and two capsule eyes, drawn from ten silhouettes: `round`, `organic`, `boxy`, `nub`, `cloud`, `sun`, `capsule`, `triangle`, `hexagon` and `droplet`. They are weighted so rounds and pebbles are everyday and the louder shapes remain a find. Transparent backdrop by default; the body is the blobatar.

## What it guarantees

- **Determinism**: The same name always renders the same blobatar within a major version.
- **Stability across versions**: Traits are addressed by string key rather than drawn from a sequential stream, so adding a trait in a later minor cannot disturb existing blobatars.
- **Contrast**: Eyes clear 4.5:1 against the body at every hue and every tone. Polarity flips automatically, so the near-black tone gets light eyes rather than an invisible face.
- **Name normalization**: Names are NFC-normalized, trimmed and lowercased before hashing by default (`Normalize = true`).
- **No element IDs**: No `<defs>`, gradients or filters, preventing ID collisions when rendering hundreds of blobatars.

## Options

Options can be passed via `BlobatarOptions`:

| Option | Type | Default | Notes |
| --- | --- | --- | --- |
| `Size` | `int?` | `null` | Emits `width`/`height` in SVG. |
| `Background` | `BackdropStyle` | `None` | `None`, `Circle`, `Square`, or `Squircle`. |
| `Hue` | `double?` | `null` | Locks hue in degrees (0–360). |
| `Tone` | `double?` | `null` | Locks swatch as 0–1 position (pale to ink). Use `0.999` for ink. |
| `Normalize` | `bool` | `true` | NFC + trim + lowercase. |
| `Contrast` | `bool` | `true` | Enforces contrast floors. |
| `Expression` | `Expression?` | `null` | Static expression pose baked into geometry (`Expressions.Happy`, etc.). |
| `Traits` | `IReadOnlyDictionary<string, double[]>?` | `null` | Pins individual traits as 0–1 positions. |

```csharp
var options = new BlobatarOptions
{
    Background = BackdropStyle.Circle,
    Hue = 200,
    Contrast = true
};
string svg = BlobatarSvg.Render("MilaDesign", options);
```

## Compatibility and sample

.NET 6–10. No framework dependencies. See the adapter packages for UI controls.

## License and attribution

MIT. Original Blobatar copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026
MilaDesign. This package includes `LICENSE` and `THIRD-PARTY-NOTICES.md`, preserving upstream
attribution. See the [repository license](https://github.com/MilaDesign/Blobatar.Net/blob/main/LICENSE).
