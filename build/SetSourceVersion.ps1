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
$assemblyVersion = Select-Xml -Xml $customprops -XPath "//msbld:VersionPrefix" -Namespace $ns | Select-Object -First 1
Write-Host "VersionPrefix/AssemblyFileVersion is $assemblyVersion"

# Version suffix is set in ADO builds if build isn't a "tag" or main branch.
$versionSuffix = $env:VERSIONSUFFIX
if ([string]::IsNullOrEmpty($versionSuffix))
{
    $productVersion = $assemblyVersion
}
else
{
    $productVersion = "$assemblyVersion-$env:VersionSuffix"    
}
Write-Host "ProductVersion/AssemblyInformationalVersion is $productVersion"

if (Test-Path -Path "$SrcPath\Properties\AssemblyInfo.cs")
{
    Write-Host "Generating AssemblyInfo.cs for $SrcPath"

    $file = Get-Item -Path "$SrcPath\Properties\AssemblyInfo.cs"

    [string]$randString = Get-Random
    $tempFile = $env:TEMP + "\" + $file.Name + $randString + ".tmp"

    Write-Host "Using temp file $tempFile"
    Write-Host "Creating assembly info file for:" $file.FullName " version:" $assemblyVersion

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

    $assemblyContents = $assemblyContents | ForEach-Object{$_ -replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyVersion(""$assemblyVersion"")" } |
    ForEach-Object{$_ -replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyFileVersion(""$assemblyVersion"")" } |
    ForEach-Object{$_ -replace 'AssemblyInformationalVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyInformationalVersion(""$productVersion"")" } |
                        Set-Content $tempFile

    Write-Host "Sucessfully modified $tempFile"

    $destDir = $file.DirectoryName + "\..\obj\"
    $null = New-Item -Path $destDir -ItemType Directory -Force
    $destFile = $destDir + $file.Name
    $skip = $false

    if (Test-Path $destFile -PathType Leaf)
    {
        $currentFileContent = Get-Content $destFile
        $versionStr = $currentFileContent | Where-Object {$_ -like "*AssemblyInformationalVersion(*"}
        if ($null -ne $versionStr)
        {
            if ($versionStr -match $productVersion)
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
        Write-Host "Version did not need updating, it is $assemblyVersion"
    }
}

$psds = Get-ChildItem -Path $SrcPath -Filter "*.psd1"
ForEach ($psd in $psds)
{
    $destDir = $psd.DirectoryName + "\obj\"
    $null = New-Item -Path $destDir -ItemType Directory -Force
    $destFile = $destDir + $psd.Name

    if (Test-Path $destFile -PathType Leaf)
    {
        $currentFileContent = Get-Content $destFile
        $versionStr = $currentFileContent | Where-Object {$_ -like "*ModuleVersion*"}
        if ($null -ne $versionStr)
        {
            if ($versionStr -match $assemblyVersion)
            {
                Write-Host "$psd version is up-to-date"
                continue
            }
        }
    }

    [string]$randString = Get-Random
    $tempFile = $env:TEMP + "\" + $file.Name + $randString + ".tmp"

    $psdContents = Get-Content $psd.FullName |
                        ForEach-Object{$_ -replace 'ModuleVersion.+', "ModuleVersion = '$assemblyVersion'"} |
                        Set-Content $tempFile

    Write-Host "Moving $tempFile to $destFile"
    Move-Item $tempFile $destFile -force
}


$appxs = Get-ChildItem -Path $SrcPath -Filter "*.appxmanifest"
ForEach ($appx in $appxs)
{
    $currentFileContent = Get-Content $appx
    $versionStr = $currentFileContent | Where-Object {$_ -like "*Identity Version=*"}
    if ($null -ne $versionStr)
    {
        if ($versionStr -match $assemblyVersion)
        {
            continue
        }
    }

    [string]$randString = Get-Random
    $tempFile = $env:TEMP + "\" + $file.Name + $randString + ".tmp"

    $appxContents = $currentFileContent |
                        ForEach-Object{$_ -replace 'Identity Version="[0-9,.]+?"', "Identity Version=`"$assemblyVersion.0`"" } |
                        Set-Content -Path $tempFile

    Write-Host "Moving $tempFile to $appx"
    Move-Item $tempFile $appx -force
}