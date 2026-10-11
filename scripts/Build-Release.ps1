[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Tag,
    [string]$NuGetConfig,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
if ($Tag -notmatch '^v(?<version>\d+\.\d+\.\d+(-[0-9A-Za-z]+([.-][0-9A-Za-z]+)*)?)$') {
    throw 'Use a version tag such as v2.7.0 or v2.7.1-beta.1.'
}
$version = $Matches.version
$root = Split-Path $PSScriptRoot -Parent
$output = Join-Path $root "artifacts/release/$Tag"
$packageIds = @('Blobatar.Net.Core', 'Blobatar.Net.Wpf', 'Blobatar.Net.WinForms',
    'Blobatar.Net.Avalonia', 'Blobatar.Net.UnoPlatform', 'Blobatar.Net.Maui', 'Blobatar.Net.Godot')
$changelog = Get-Content (Join-Path $root 'CHANGELOG.md') -Raw
$heading = '(?m)^## \[?' + [regex]::Escape($version) + '\]?(?=\s|$)[^\r\n]*\r?\n'
$entry = [regex]::Match($changelog, $heading)
if (!$entry.Success) { throw "Add a ## $version entry to CHANGELOG.md before releasing." }
$changes = ($changelog.Substring($entry.Index + $entry.Length) -split '(?m)^## ', 2)[0].Trim()
if (!$changes) { throw "The CHANGELOG.md entry for $version is empty." }
New-Item -ItemType Directory -Path $output -Force | Out-Null

function Invoke-DotNet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($args[0]) failed." }
}

$properties = @("-p:Version=$version", '-p:GeneratePackageOnBuild=false', '-p:ContinuousIntegrationBuild=true')
$restoreOptions = @()
if ($NuGetConfig) { $restoreOptions = @('--configfile', [IO.Path]::GetFullPath($NuGetConfig)) }

foreach ($id in $packageIds) {
    $project = Join-Path $root "src/$id/$id.csproj"
    Invoke-DotNet restore $project @restoreOptions @properties
    Invoke-DotNet build $project -c Release --no-restore @properties
}
if (!$SkipTests) {
    foreach ($id in @('Blobatar.Net.Tests', 'Blobatar.Net.Maui.Tests')) {
        $project = Join-Path $root "tests/$id/$id.csproj"
        Invoke-DotNet restore $project @restoreOptions @properties
        Invoke-DotNet test $project -c Release --no-restore @properties
    }
}
foreach ($id in $packageIds) {
    $project = Join-Path $root "src/$id/$id.csproj"
    Invoke-DotNet pack $project -c Release --no-build --no-restore '-p:BuildProjectReferences=false' -o $output @properties
}

$packages = @(Get-ChildItem $output -Filter '*.nupkg')
if ($packages.Count -ne $packageIds.Count) { throw 'Expected exactly seven library packages in the release directory.' }
foreach ($id in $packageIds) {
    $file = Join-Path $output "$id.$version.nupkg"
    $zip = [IO.Compression.ZipFile]::OpenRead($file)
    try {
        foreach ($name in @('LICENSE', 'THIRD-PARTY-NOTICES.md', 'README.md')) {
            if (!$zip.GetEntry($name)) { throw "$id is missing $name." }
        }
        $reader = [IO.StreamReader]::new($zip.GetEntry("$id.nuspec").Open())
        try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($nuspec.package.metadata.id -ne $id -or $nuspec.package.metadata.version -ne $version) {
            throw "Package metadata does not match $id $version."
        }
        foreach ($dependency in $nuspec.SelectNodes("//*[local-name()='dependency' and @id='Blobatar.Net.Core']")) {
            if ($dependency.version -ne $version) { throw "$id references the wrong Core version: $($dependency.version)" }
        }
    } finally { $zip.Dispose() }
}

$notes = @("# Blobatar.Net $version", '', '## Changes', '', $changes, '', '## NuGet packages', '',
    '| Package | Version |', '| --- | --- |')
foreach ($id in $packageIds) {
    $notes += "| [$id](https://www.nuget.org/packages/$id/$version) | $version |"
}
$notes += @('', 'All seven library packages are attached to this release. Samples are available in the repository.',
    '', 'NuGet.org may take a few minutes to validate and index newly uploaded packages.')
$notes | Set-Content (Join-Path $output 'release-notes.md') -Encoding utf8
Write-Host "Prepared seven packages and release notes in $output"
