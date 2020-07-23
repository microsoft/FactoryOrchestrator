# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# This script sets the major and minor version numbers of all FactoryOrchestrator binaries based on src\custom.props
Param
(
[string]$SrcPath,
[switch]$MajorMinorOnly
)

$ErrorActionPreference = "stop"

Write-Host "Generating AssemblyInfo.cs for $SrcPath"


$buildNumber = $env:TFS_VersionNumber
$tfs = $false

[xml]$customprops = Get-Content "$PSScriptRoot\..\src\custom.props"
$msbldns = "http://schemas.microsoft.com/developer/msbuild/2003"
$ns = @{msbld = "$msbldns"}
$majorNode = Select-Xml -Xml $customprops -XPath "//msbld:VersionMajor" -Namespace $ns | Select-Object -First 1
$minorNode = Select-Xml -Xml $customprops -XPath "//msbld:VersionMinor" -Namespace $ns | Select-Object -First 1
$majorVersion = $majorNode.Node."#text"
$minorVersion = $minorNode.Node."#text"

if ($null -eq $buildNumber)
{
    $date = Get-Date
    $month = $date.Month.ToString()
    if ($month.Length -eq 1)
    {
        $month = "0" + $month
    }

    if ($MajorMinorOnly)
    {
        $buildNumber = "$majorVersion.$minorVersion.0.0"
    }
    else
    {
        $buildNumber = "$majorVersion.$minorVersion." + $date.Year.ToString().SubString(2) + "" + $month + "." + $date.Hour.ToString() + $date.Minute.ToString()
    }
}
else
{
    $tfs = $true
}

Write-Host "Build number is $buildNumber"

$file = Get-Item -Path "$SrcPath\Properties\AssemblyInfo.cs"
[string]$randString = Get-Random
$tempFile = $env:TEMP + "\" + $file.Name + $randString + ".tmp"

Write-Host "Using temp file $tempFile"

#now load all content of the original file and rewrite modified to the same file
 if ($tfs -eq $true)
{
    Get-Content $file.FullName |
    ForEach-Object{$_ -replace 'AssemblyDescription.+', "AssemblyDescription("""")]" }  > $tempFile
}
else
{
    Write-Host "Creating assembly info file for:" $file.FullName " build number:" $buildNumber

    Get-Content $file.FullName |
    ForEach-Object{$_ -replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyVersion(""$buildNumber"")" } |
    ForEach-Object{$_ -replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyFileVersion(""$buildNumber"")" } |
    ForEach-Object{$_ -replace 'AssemblyDescription.+', "AssemblyDescription(""PrivateBuild"")]" }  > $tempFile
}

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
    Write-Host "Success!"
}
else
{
    Write-Host "Version did not need updating, it is $buildNumber"
}