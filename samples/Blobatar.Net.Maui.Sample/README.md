# MAUI sample

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

The sample references the local library project; no published Blobatar.Net packages are
needed. Use the .NET 10 SDK and the matching MAUI workload/platform tooling. Run these
commands from the repository root:

```powershell
dotnet build samples/Blobatar.Net.Maui.Sample -t:Run -f net10.0-windows10.0.19041.0
```

The host default is Windows on Windows, Mac Catalyst on macOS, and Android on Linux.
To select a platform explicitly, use `-p:BlobatarMauiSampleTargetFrameworks=net10.0-android`
(or `net10.0-ios` on a configured Mac). To select .NET 8/9, add
`-p:BlobatarMauiDotNetVersion=8` or `9` and install compatible workloads. The .NET 10 Windows
sample has been verified; the older sample configurations and mobile/Apple hosts have not
been validated in this workspace. The MAUI library supports .NET 8–10.

The page uses `xmlns:bl="https://github.com/MilaDesign/Blobatar.Net/maui"`, backed by
`XmlnsDefinition` and a suggested `bl` `XmlnsPrefix`. It has live settings, expressions,
page-wide pointer/target gaze, animation and a 40-avatar gallery. No click or held mouse
button is needed for Windows pointer gaze.

See the [package README](../../src/Blobatar.Net.Maui/README.md) and
[native pointer test instructions](../../tests/Blobatar.Net.Maui.Tests/README.md).
Bundled Open Sans fonts retain their [Apache 2.0 license](Resources/Fonts/LICENSE.txt),
which is also included as an application asset at `licenses/OpenSans-LICENSE.txt`.

## License and attribution

Part of MilaDesign's independent [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net)
port of [Alain00/blobatar](https://github.com/Alain00/blobatar). MIT: original Blobatar
copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026 MilaDesign.
See the repository [license](../../LICENSE) and [third-party notices](../../THIRD-PARTY-NOTICES.md).
