# Avalonia sample

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

Run from the repository root with the .NET 10 SDK and .NET 8 runtime:

```shell
dotnet run --project samples/Blobatar.Net.Avalonia.Sample -f net8.0
```

The sample also targets `net6.0` and `net7.0`; those runs need the matching runtime.
It uses Avalonia 11 and references the local library project, so no published Blobatar.Net
packages are needed. The preview demonstrates
`xmlns:bl="https://github.com/MilaDesign/Blobatar.Net/avalonia"`; the gallery uses Avalonia's
default namespace. The library's `XmlnsPrefix` suggests `bl`.

The UI provides live settings, pointer/target gaze, expressions, animation and a 40-avatar
gallery. See the [package README](../../src/Blobatar.Net.Avalonia/README.md).

## License and attribution

Part of MilaDesign's independent [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net)
port of [Alain00/blobatar](https://github.com/Alain00/blobatar). MIT: original Blobatar
copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026 MilaDesign.
See the repository [license](../../LICENSE) and [third-party notices](../../THIRD-PARTY-NOTICES.md).
