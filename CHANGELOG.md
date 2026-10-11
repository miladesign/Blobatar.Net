# Changelog

Versions marked `(planned)` have not been published yet.

## Unreleased

- No changes yet.

## 2.7.0 (planned)

Initial independent .NET port of Alain00/blobatar v2.7.0.

- Shared Core for deterministic generation, geometry, palettes, SVG rendering, expressions,
  idle animation and gaze, checked against upstream-derived reference data.
- WPF, Windows Forms, Avalonia, Uno Platform, MAUI and Godot .NET adapters with samples.
- .NET 6–10 library targets, except MAUI, which starts at .NET 8; dependencies vary by framework.
- Whole-window mouse gaze for supported desktop adapters, including MAUI's native Windows tracking.
- Godot script registration for editor reloads and native window notifications for mouse entry/exit.
- MIT license and upstream attribution in each NuGet package; MilaDesign repository metadata
  and custom XAML namespace URLs with suggested `bl` prefixes where supported.
- GitHub Actions workflow to build and test all seven libraries, publish their NuGet packages,
  and create one combined GitHub release with packages, changes and NuGet links.
