# Core verification

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

Run from the repository root with the .NET 10 SDK and the matching .NET runtime:

```shell
dotnet test tests/Blobatar.Net.Tests -c Release -f net8.0
```

The project also targets .NET 6 and 7. Omit `-f` to run all three when their runtimes are
installed. Tests reference the local Core project, without a published Blobatar.Net package.

The upstream-derived `golden.json`, `golden2.json` and `golden3.json` files cover SVG output,
expressions, idle motion and gaze math based on blobatar 2.7.0. There are also path parsing,
morph, layout and scene tests. Investigate parity failures instead of rewriting the reference
data. Native adapters have their own rendering and input checks; these Core tests do not
validate native window input or editor hot reload.

## License and attribution

Part of MilaDesign's independent [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net)
port of [Alain00/blobatar](https://github.com/Alain00/blobatar). MIT: original Blobatar
copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026 MilaDesign.
See the repository [license](../../LICENSE) and [third-party notices](../../THIRD-PARTY-NOTICES.md).
