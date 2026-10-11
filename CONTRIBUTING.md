# Contributing to Blobatar.Net

Blobatar.Net is an independent .NET port maintained by MilaDesign. Core behavior is based
on [Alain00/blobatar v2.7.0](https://github.com/Alain00/blobatar/tree/v2.7.0).
Contributions to this repository are distributed under its [MIT license](LICENSE).

## Build and test

Use the .NET 10 SDK to build all target frameworks. The complete solution is intended to
build on Windows; WPF and Windows Forms require Windows, and the MAUI sample requires the
matching workload. Individual Core, Avalonia and Godot projects can be built separately.
Install the matching .NET runtime to run tests or samples for an older target.

```powershell
dotnet restore Blobatar.Net.sln
dotnet build Blobatar.Net.sln -c Release --no-restore
dotnet test tests/Blobatar.Net.Tests -c Release --no-restore
dotnet test tests/Blobatar.Net.Maui.Tests -c Release --no-restore
```

Godot dependencies are selected in `GodotVersions.props`; the sample uses Godot 4.7 .NET.
If the matching SDK/bindings are unavailable on your NuGet sources, add the `GodotSharp/Tools/nupkgs`
directory from your Godot installation as a local source. The .NET 6/7 library targets also
need the 4.3.0 bindings. Uno and MAUI versions are selected in their respective root `.props` files.

## Changes

- Keep deterministic generation and default options in Core. Adapters should render its results.
- Preserve the upstream-derived golden fixtures. Investigate differences instead of regenerating
  expected output to make a failing test pass; intentional changes need explicit documentation.
- Test UI behavior in the native framework when it changes. Rendering, window boundaries,
  lifecycle events and hot reload can fail even when Core tests pass.
- Update the affected package/sample README and add an entry to `CHANGELOG.md`.
- Preserve upstream attribution and include notices for any new bundled assets.

Describe the user-visible change, how it was verified, which platforms were checked, and
whether the seed-to-avatar mapping changed.
