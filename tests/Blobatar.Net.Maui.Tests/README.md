# MAUI verification

## Vibe coding with AI

Blobatar.Net was **vibe coded using GPT, Claude and Gemini**, with MilaDesign
directing development and maintaining the project. This disclosure applies to
the .NET port; the original Blobatar project is credited separately.

`dotnet test tests/Blobatar.Net.Maui.Tests -c Release` (from the repository root) runs rendering and portable pointer lifecycle tests on .NET 8–10.

The Windows sample also has an optional native input smoke test. It moves the actual cursor
outside the avatar to its left and right, then over the native Entry. It checks the resulting
gaze coordinates without pressing a mouse button. The sample closes after testing and restores
the cursor and foreground window. Run it from the repository root on an interactive Windows desktop with no mouse button down:

```powershell
dotnet build samples/Blobatar.Net.Maui.Sample -c Debug -p:BlobatarPointerSmokeTest=true
$env:BLOBATAR_POINTER_TEST_RESULT = "$env:TEMP/blobatar-pointer-debug.json"
$app = Start-Process samples/Blobatar.Net.Maui.Sample/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Blobatar.Net.Maui.Sample.exe -WindowStyle Hidden -PassThru
$app.WaitForExit(15000)
Get-Content $env:BLOBATAR_POINTER_TEST_RESULT
Remove-Item Env:BLOBATAR_POINTER_TEST_RESULT
```

Repeat with `Release` to verify that configuration. A passing JSON result has `Passed: true`
and three results with `Aim: Look` and `Touch: false`. Build again without
`-p:BlobatarPointerSmokeTest=true` to return to the ordinary sample. The test harness is
excluded from normal sample builds and the library package.

## License and attribution

Part of MilaDesign's independent [Blobatar.Net](https://github.com/MilaDesign/Blobatar.Net)
port of [Alain00/blobatar](https://github.com/Alain00/blobatar). MIT: original Blobatar
copyright (c) 2026 Alain; .NET port and adapters copyright (c) 2026 MilaDesign.
See the repository [license](../../LICENSE) and [third-party notices](../../THIRD-PARTY-NOTICES.md).
