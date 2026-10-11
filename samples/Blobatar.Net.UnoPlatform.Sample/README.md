# Uno Platform sample

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

The sample references the local adapter project. Build with the .NET 10 SDK and run with
the selected target's runtime. Commands below run from the repository root:

```shell
dotnet run --project samples/Blobatar.Net.UnoPlatform.Sample -f net8.0-desktop
dotnet run --project samples/Blobatar.Net.UnoPlatform.Sample -f net9.0-desktop
dotnet run --project samples/Blobatar.Net.UnoPlatform.Sample -f net10.0-desktop
```

The newer targets use Skia Desktop with the matching SDK selected by `UnoVersions.props`:

| Target | Uno.WinUI | Uno.Sdk / host |
| --- | --- | --- |
| .NET 6 | 4.10.39 | Microsoft.NET.Sdk with legacy GTK host |
| .NET 7 | 5.2.190 | Microsoft.NET.Sdk with legacy GTK host |
| .NET 8 | 5.6.99 | 5.6.54 |
| .NET 9 | 6.2.58 | 6.2.39 |
| .NET 10 | 6.6.184 | 6.6.42 |

For `-f net6.0` or `-f net7.0`, install GTK 3 and the matching .NET runtime.
The sample intentionally uses `xmlns:bl="using:Blobatar.Net.UnoPlatform"`, compatible with
WinUI XAML across these versions. It does not declare a GitHub URL namespace.

The UI demonstrates live settings, animation, expressions and pointer/target gaze.
See the [package README](../../src/Blobatar.Net.UnoPlatform/README.md).

## License and attribution

Part of MilaDesign's independent [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net)
port of [Alain00/blobatar](https://github.com/Alain00/blobatar). MIT: original Blobatar
copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026 MilaDesign.
See the repository [license](../../LICENSE) and [third-party notices](../../THIRD-PARTY-NOTICES.md).
