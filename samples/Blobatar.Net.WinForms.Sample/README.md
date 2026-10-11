# Windows Forms sample

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

Run from the repository root on Windows with the .NET 10 SDK and .NET 8 runtime:

```powershell
dotnet run --project samples/Blobatar.Net.WinForms.Sample -f net8.0-windows
```

The sample also targets `net6.0-windows` and `net7.0-windows`; those runs need the matching
runtime. It references the local library project, so no published Blobatar.Net packages are
needed. Controls are created in C#; Windows Forms does not use XAML namespaces.

The UI provides a live seed, palette locks, backdrop/expression/gaze controls, animation
settings and a 40-avatar gallery. See the [package README](../../src/Blobatar.Net.WinForms/README.md).

## License and attribution

Part of MilaDesign's independent [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net)
port of [Alain00/blobatar](https://github.com/Alain00/blobatar). MIT: original Blobatar
copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026 MilaDesign.
See the repository [license](../../LICENSE) and [third-party notices](../../THIRD-PARTY-NOTICES.md).
