# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# Creates a .zip file with the service and installer.
Param
(
    [string]$BuildConfiguration,
    [string]$BuildPlatform,
    [string]$BuildOS,
    [string]$BinDir,
    [string]$DestinationDir
)

if ($null -ne $($env:VERSIONSUFFIXVPACK))
{
    $version = "$($env:VERSIONPREFIX)$($env:VERSIONSUFFIXVPACK)"
}
else
{
    $version = "$($env:VERSIONPREFIX)"
}

# copy install scripts to temp dir
$installdir = Join-Path $env:FORepoRoot "install"
if ($BuildOS -eq "win")
{
    Copy-Item -Path $installdir -Destination $tmpdir -Filter "*.ps1"
}
else
{
    Copy-Item -Path $installdir -Destination $tmpdir -Filter "*"
}

# Set $variables$ in tempdir files
foreach ($file in Get-ChildItem $tmpdir)
{
    $null = Get-Content $file.FullName |
    ForEach-Object{$_ -replace '$Version$', "$version"} |
    ForEach-Object{$_ -replace '$BuildPlatform$', "$BuildPlatform"} |
    ForEach-Object{$_ -replace '$BuildOS$', "$BuildOS"} |
    Set-Content $tempFile
}

# create bin zip in temp dir
$publishdir = Join-Path $BinDir "$BuildConfiguration/Publish/$BuildOS/Microsoft.FactoryOrchestrator.Service.$BuildOS-$BuildPlatform"
Copy-Item -Path $publishdir -recurse -exclude "*.pdb" -destination $tmppublishdir
Compress-Archive -Path "$tmppublishdir" -DestinationPath "$tmpdir/Microsoft.FactoryOrchestrator.Service-$version-$BuildOS-$BuildPlatform.zip" -Force

# create zip with bin zip and install files

if ((Test-Path $DestinationDir) -eq $false)
{
    $null = New-Item -Path $DestinationDir -ItemType Directory
}
Compress-Archive -Path "$tmpdir" -DestinationPath "$DestinationDir/Microsoft.FactoryOrchestrator.Service-$version-$BuildOS-$BuildPlatform.zip" -Force

# clean temp dirs
Remove-Item $tmpdir -Recurse -Force
Remove-Item $tmppublishdir -Recurse -Force