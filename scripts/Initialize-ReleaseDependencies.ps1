[CmdletBinding()]
param([Parameter(Mandatory)][string]$Destination)

$ErrorActionPreference = 'Stop'
$Destination = [IO.Path]::GetFullPath($Destination)
$feed = Join-Path $Destination 'godot-feed'
New-Item -ItemType Directory -Path $feed -Force | Out-Null

# Godot ships its managed binding packages inside the official .NET editor ZIP.
# Keep downloads outside the repository so project source globs cannot include them.
foreach ($version in @('4.3', '4.7')) {
    $name = "Godot_v$version-stable_mono_win64.zip"
    $archive = Join-Path $Destination $name
    $url = "https://github.com/godotengine/godot/releases/download/$version-stable/$name"
    & curl.exe --fail --location --retry 3 --max-time 600 --silent --show-error --output $archive $url
    if ($LASTEXITCODE -ne 0) { throw "Could not download $name." }
    $zip = [IO.Compression.ZipFile]::OpenRead($archive)
    try {
        $entries = @($zip.Entries | Where-Object { $_.FullName -match '/GodotSharp/Tools/nupkgs/[^/]+\.nupkg$' })
        if ($entries.Count -eq 0) { throw "No Godot NuGet packages found in $name." }
        foreach ($entry in $entries) {
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $feed $entry.Name), $true)
        }
    } finally { $zip.Dispose() }
    foreach ($id in @('GodotSharp', 'Godot.SourceGenerators')) {
        if (!(Test-Path (Join-Path $feed "$id.$version.0.nupkg"))) {
            throw "Missing required $id $version.0 binding package."
        }
    }
}

$escapedFeed = [Security.SecurityElement]::Escape($feed)
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="Godot bindings" value="$escapedFeed" />
  </packageSources>
</configuration>
"@ | Set-Content (Join-Path $Destination 'NuGet.Config') -Encoding utf8
