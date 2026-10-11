# WPF sample

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

Run from the repository root on Windows with the .NET 10 SDK and .NET 8 runtime:

```powershell
dotnet run --project samples/Blobatar.Net.Wpf.Sample -f net8.0-windows
```

The sample also targets `net6.0-windows` and `net7.0-windows`; those runs need the matching
runtime. It references the local library project, so no published Blobatar.Net packages are
needed. The preview demonstrates `xmlns:bl="https://github.com/MilaDesign/Blobatar.Net/wpf"`,
while the gallery uses the default WPF namespace. The library's `XmlnsPrefix` suggests `bl`.

The UI has a live seed, palette locks, backdrop/expression/gaze controls, target gaze,
animation settings and a 40-avatar gallery. See the [package README](../../src/Blobatar.Net.Wpf/README.md).

## License and attribution

Part of MilaDesign's independent [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net)
port of [Alain00/blobatar](https://github.com/Alain00/blobatar). MIT: original Blobatar
copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026 MilaDesign.
See the repository [license](../../LICENSE) and [third-party notices](../../THIRD-PARTY-NOTICES.md).
