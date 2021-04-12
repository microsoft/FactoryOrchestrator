# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# Creates a .zip file with the service and installer.
Param
(
    [Parameter(Mandatory = $true)][string]$BuildConfiguration,
    [Parameter(Mandatory = $true)][string]$BuildPlatform,
    [Parameter(Mandatory = $true)][string]$BuildOS,
    [Parameter(Mandatory = $true)][string]$BinDir,
    [Parameter(Mandatory = $true)][string]$DestinationDir
)

[string]$randString = Get-Random
$tmpdir = join-path "$([System.IO.Path]::GetTempPath())" "$randString"
if ((Test-Path $tmpdir) -eq $false)
{
    $null = New-Item -Path $tmpdir -ItemType Directory
}
[string]$randString = Get-Random
$tmppublishdir = join-path "$([System.IO.Path]::GetTempPath())" "$randString"
if ((Test-Path $tmppublishdir) -eq $false)
{
    $null = New-Item -Path $tmppublishdir -ItemType Directory
}

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
    $files = Get-ChildItem -Path $installdir -Filter "*.ps1"
}
else
{
    $files =  Get-ChildItem -Path $installdir  -Filter "*"
}

# Set $variables$ in tempdir files
foreach ($file in $files)
{
    $destfile = Join-Path $tmpdir $($file.Name)
    $content = Get-Content $file.FullName -raw |
    ForEach-Object{$_ -replace '\$Version\$', "$version"} |
    ForEach-Object{$_ -replace '\$BuildPlatform\$', "$BuildPlatform"} |
    ForEach-Object{$_ -replace '\$BuildOS\$', "$BuildOS"}

    # Set correct line ending
    if ($BuildOS -eq "win")
    {
        $content = $content | ForEach-Object{$_ -replace '\n', "`r`n"}
    }
    else
    {
        $content = $content | ForEach-Object{$_ -replace '\r\n', "`n"}
    }
    Set-Content -Path $destfile -Value $content -NoNewline
}

# create bin zip in temp dir
$publishdir = Join-Path $BinDir "$BuildConfiguration/Publish/$BuildOS/Microsoft.FactoryOrchestrator.Service.$BuildOS-$BuildPlatform"
Copy-Item -Path $publishdir -recurse -exclude "*.pdb" -destination $tmppublishdir
Compress-Archive -Path "$tmppublishdir/*" -DestinationPath "$tmpdir/Microsoft.FactoryOrchestrator.Service-$version-$BuildOS-$BuildPlatform-bin.zip" -Force

# create zip with bin zip and install files
if ((Test-Path $DestinationDir) -eq $false)
{
    $null = New-Item -Path $DestinationDir -ItemType Directory
}
Compress-Archive -Path "$tmpdir/*" -DestinationPath "$DestinationDir/Microsoft.FactoryOrchestrator.Service-$version-$BuildOS-$BuildPlatform.zip" -Force

# clean temp dirs
Remove-Item $tmpdir -Recurse -Force
Remove-Item $tmppublishdir -Recurse -Force