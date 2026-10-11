# Godot sample

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

Open `project.godot` with **Godot 4.7 .NET**, build the C# project, and press F5.
The sample uses local project references; no published Blobatar.Net package is needed.
Commands below run from the repository root. Replace `godot` with the path to your Godot
.NET executable if it is not on PATH.
The main scene includes a static editor preview; the running sample provides a live seed,
palette locks, every expression/backdrop, pointer and target gaze, animation controls and
a responsive 40-user gallery.

```powershell
dotnet build samples/Blobatar.Net.Godot.Sample
godot --path samples/Blobatar.Net.Godot.Sample
```

The sample defaults to .NET 8. Build .NET 9 or 10 with
`-p:BlobatarGodotDotNetVersion=9` or `10`, using Godot 4.7's .NET edition.
These targets share the same output directory, so rebuild your selected target before running.
Godot exports use `ExportDebug`/`ExportRelease` configurations.

The smoke test runs **inside Godot**, testing inherited Inspector properties, scene deserialization,
direct package instantiation, 56 expression/backdrop combinations, texture reuse and release,
target gaze and mouse exit:

```powershell
godot --headless --path samples/Blobatar.Net.Godot.Sample -- --smoke-test
```

For real desktop mouse movement outside the avatar without clicking, run without `--headless`
and add `--native-pointer-test` after `--smoke-test`. It opens a temporary sample window,
moves the mouse to both sides of the avatar and out of/back into the game window three times,
verifies gaze, and closes the sample. A passing run prints `BLOBATAR_GODOT_NATIVE_POINTER_PASS`,
`BLOBATAR_GODOT_WINDOW_BOUNDARY_PASS` and `BLOBATAR_GODOT_SMOKE_PASS`.
Leave mouse buttons released while running it.

## License and attribution

Part of MilaDesign's independent [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net)
port of [Alain00/blobatar](https://github.com/Alain00/blobatar). MIT: original Blobatar
copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026 MilaDesign.
See the repository [license](../../LICENSE) and [third-party notices](../../THIRD-PARTY-NOTICES.md).
