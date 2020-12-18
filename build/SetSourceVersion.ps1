# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# This script manually updates the version of the UWP app & PowerShell module based on common.props's Version property. The rest are set directly by dotnet.
Param
(
[string]$SrcPath
)

$ErrorActionPreference = "stop"


[xml]$customprops = Get-Content "$PSScriptRoot\..\src\common.props"
$msbldns = "http://schemas.microsoft.com/developer/msbuild/2003"
$ns = @{msbld = "$msbldns"}
$fullVersion = Select-Xml -Xml $customprops -XPath "//msbld:Version" -Namespace $ns | Select-Object -First 1
Write-Host "Version is $fullVersion"

if (Test-Path -Path "$SrcPath\Properties\AssemblyInfo.cs")
{
    Write-Host "Generating AssemblyInfo.cs for $SrcPath"

    $file = Get-Item -Path "$SrcPath\Properties\AssemblyInfo.cs"

    [string]$randString = Get-Random
    $tempFile = $env:TEMP + "\" + $file.Name + $randString + ".tmp"

    Write-Host "Using temp file $tempFile"
    Write-Host "Creating assembly info file for:" $file.FullName " version:" $fullVersion

    #now load all content of the original file and rewrite modified to the same file
     if ($tfs -eq $true)
    {
        $assemblyContents = Get-Content $file.FullName |
                            ForEach-Object{$_ -replace 'AssemblyDescription.+', "AssemblyDescription("""")]" }
    }
    else
    {
        $assemblyContents = Get-Content $file.FullName |
                            ForEach-Object{$_ -replace 'AssemblyDescription.+', "AssemblyDescription(""PrivateBuild"")]" }
    }

    $assemblyContents = $assemblyContents | ForEach-Object{$_ -replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyVersion(""$fullVersion"")" } |
                        ForEach-Object{$_ -replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyFileVersion(""$fullVersion"")" } |
                        Set-Content $tempFile

    Write-Host "Sucessfully modified $tempFile"

    $destDir = $file.DirectoryName + "\..\obj\"
    $null = New-Item -Path $destDir -ItemType Directory -Force
    $destFile = $destDir + $file.Name
    $skip = $false

    if (Test-Path $destFile -PathType Leaf)
    {
        $currentFileContent = Get-Content $destFile
        $versionStr = $currentFileContent | Where-Object {$_ -like "*AssemblyVersion(*"}
        if ($null -ne $versionStr)
        {
            if ($versionStr -match $buildNumber)
            {
                $skip = $true
            }
        }
    }

    if ($skip -ne $true)
    {
        Write-Host "Moving $tempFile to $destFile"
        Move-Item $tempFile $destFile -force

        if ($null -ne $env:AGENT_MACHINENAME)
        {
            # Running in Azure Pipeline. Print entire file for logging.
            Write-Host "------------ $destFile contents ------------"
            Get-Content $destFile | Write-Host
            Write-Host "`n"
        }

        Write-Host "Success!"
    }
    else
    {
        Write-Host "Version did not need updating, it is $fullVersion"
    }
}

$psds = Get-ChildItem -Path $SrcPath -Filter "*.psd1"
ForEach ($psd in $psds)
{
    $currentFileContent = Get-Content $psd
    $versionStr = $currentFileContent | Where-Object {$_ -like "*ModuleVersion*"}
    if ($null -ne $versionStr)
    {
        if ($versionStr -match $fullVersion)
        {
            continue
        }
    }

    [string]$randString = Get-Random
    $tempFile = $env:TEMP + "\" + $file.Name + $randString + ".tmp"

    $psdContents = Get-Content $psd.FullName |
                        ForEach-Object{$_ -replace 'ModuleVersion.+', "ModuleVersion = '$fullVersion'"} |
                        Set-Content $tempFile

    Write-Host "Moving $tempFile to $psd"
    Move-Item $tempFile $psd -force
}


$appxs = Get-ChildItem -Path $SrcPath -Filter "*.appxmanifest"
ForEach ($appx in $appxs)
{
    $currentFileContent = Get-Content $appx
    $versionStr = $currentFileContent | Where-Object {$_ -like "*Identity Version=*"}
    if ($null -ne $versionStr)
    {
        if ($versionStr -match $fullVersion)
        {
            continue
        }
    }

    [string]$randString = Get-Random
    $tempFile = $env:TEMP + "\" + $file.Name + $randString + ".tmp"

    $appxContents = $currentFileContent |
                        ForEach-Object{$_ -replace 'Identity Version="[0-9,.]+?"', "Identity Version=`"$fullVersion.0`"" } |
                        Set-Content -Path $tempFile

    Write-Host "Moving $tempFile to $appx"
    Move-Item $tempFile $appx -force
}