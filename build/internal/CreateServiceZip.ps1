# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# Creates a .zip file with the service and installer. Run in 2 phases.
# The CreateInstallScript phase creates .ps1/.sh scripts with the correct version & os info.
# The CreateZip phase then packages those scripts with the proper binaries.
[CmdletBinding(DefaultParameterSetName = 'CreateInstallScript')]
Param
(
    [Parameter(ParameterSetName = 'CreateZip', Mandatory = $true)][Parameter(ParameterSetName = 'CreateInstallScript', Mandatory = $true)][string]$BuildConfiguration,
    [Parameter(ParameterSetName = 'CreateZip', Mandatory = $true)][Parameter(ParameterSetName = 'CreateInstallScript', Mandatory = $true)][string]$BuildPlatform,
    [Parameter(ParameterSetName = 'CreateZip', Mandatory = $true)][Parameter(ParameterSetName = 'CreateInstallScript', Mandatory = $true)][string]$BuildOS,
    [Parameter(ParameterSetName = 'CreateZip', Mandatory = $true)][string]$BinDir,
    [Parameter(ParameterSetName = 'CreateZip', Mandatory = $true)][string]$DestinationDir,
    [Parameter(ParameterSetName = 'CreateZip', Mandatory = $false)][string]$TempDir = $env:FO_SERVICEZIP_TEMP_DIR
)

if ($null -ne $($env:VERSIONSUFFIXVPACK))
{
    $version = "$($env:VERSIONPREFIX)$($env:VERSIONSUFFIXVPACK)"
}
else
{
    $version = "$($env:VERSIONPREFIX)"
}

if ($PSCmdlet.ParameterSetName -eq "CreateInstallScript")
{
    [string]$randString = Get-Random
    $tmpdir = join-path "$([System.IO.Path]::GetTempPath())" "$randString"
    if ((Test-Path $tmpdir) -eq $false)
    {
        $null = New-Item -Path $tmpdir -ItemType Directory
    }

    Write-Host "Using $tmpdir as temporary directory."

    # copy install scripts to temp dir
    $installdir = Join-Path $PSScriptRoot "../../install"
    if ($BuildOS -eq "win")
    {
        $files = Get-ChildItem -Path $installdir -Filter "*.ps1"
    }
    else
    {
        $files =  Get-ChildItem -Path $installdir  -Filter "*"
    }

    # Set $variables$ and correct line ending in tmpdir files
    foreach ($file in $files)
    {
        $destfile = Join-Path $tmpdir $($file.Name)
        $content = Get-Content $file.FullName -raw |
        ForEach-Object{$_ -replace '\$Version\$', "$version"} |
        ForEach-Object{$_ -replace '\$BuildPlatform\$', "$BuildPlatform"} |
        ForEach-Object{$_ -replace '\$BuildOS\$', "$BuildOS"}

        if ($BuildOS -eq "win")
        {
            $content = $content | ForEach-Object{$_ -replace '\r\n', "`n"} | ForEach-Object{$_ -replace '\n', "`r`n"}
        }
        else
        {
            $content = $content | ForEach-Object{$_ -replace '\r\n', "`n"}
        }
        Set-Content -Path $destfile -Value $content -NoNewline
    }

    if ($null -ne $env:AGENT_ID)
    {
        $vstsCommandString = "vso[task.setvariable variable=FO_SERVICEZIP_TEMP_DIR]$tmpdir"
        Write-Host "sending " + $vstsCommandString
        Write-Host "##$vstsCommandString"
    }
    else
    {
        $env:FO_SERVICEZIP_TEMP_DIR=$tmpdir
    }
}
else
{
    # Create second temp dir for binary zip creation
    [string]$randString = Get-Random
    $tmppublishdir = join-path "$([System.IO.Path]::GetTempPath())" "$randString"
    if ((Test-Path $tmppublishdir) -eq $false)
    {
        $null = New-Item -Path $tmppublishdir -ItemType Directory
    }
    # create bin zip in temp dir
    $publishdir = Join-Path $BinDir "$BuildConfiguration/Publish/$BuildOS/Microsoft.FactoryOrchestrator.Service.$BuildOS-$BuildPlatform"
    Copy-Item -Path "$publishdir/*" -recurse -exclude "*.pdb" -destination $tmppublishdir
    Compress-Archive -Path "$tmppublishdir/*" -DestinationPath "$TempDir/Microsoft.FactoryOrchestrator.Service-$version-$BuildOS-$BuildPlatform-bin.zip" -Force

    # create zip with bin zip and install files
    if ((Test-Path $DestinationDir) -eq $false)
    {
        $null = New-Item -Path $DestinationDir -ItemType Directory
    }
    Compress-Archive -Path "$TempDir/*" -DestinationPath "$DestinationDir/Microsoft.FactoryOrchestrator.Service-$version-$BuildOS-$BuildPlatform.zip" -Force

    # create Symbols zip
    Get-ChildItem -Recurse -Filter "*.pdb" -Path "$publishdir" | Compress-Archive -DestinationPath "$DestinationDir/Microsoft.FactoryOrchestrator.Service-$version-$BuildOS-$BuildPlatform.Symbols.zip" -Force

    # clean temp dirs
    Remove-Item $TempDir -Recurse -Force
    Remove-Item $tmppublishdir -Recurse -Force
}