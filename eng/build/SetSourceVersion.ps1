Param
(
[string]$SrcPath
)

$ErrorActionPreference = "stop"

$buildNumber = $env:TFS_VersionNumber
$tfs = $false

[xml]$customprops = Get-Content "$PSScriptRoot\..\..\custom.props"
$msbldns = "http://schemas.microsoft.com/developer/msbuild/2003"
$ns = @{msbld = "$msbldns"}
$majorNode = Select-Xml -Xml $customprops -XPath "//msbld:VersionMajor" -Namespace $ns | Select -First 1
$minorNode = Select-Xml -Xml $customprops -XPath "//msbld:VersionMinor" -Namespace $ns | Select -First 1
$majorVersion = $majorNode.Node."#text"
$minorVersion = $minorNode.Node."#text"

if ($buildNumber -eq $null)
{
    $date = Get-Date
    $month = $date.Month.ToString()
    if ($month.Length -eq 1)
    {
        $month = "0" + $month
    }

    $buildNumber = "$majorVersion.$minorVersion." + $date.Year.ToString().SubString(2) + "" + $month + "." + $date.Hour.ToString() + $date.Minute.ToString()
}
else
{
    $tfs = $true
}

$file = Get-Item -Path "$SrcPath\Properties\AssemblyInfo.cs"
$tempFile = $env:TEMP + "\" + $file.Name + ".tmp"

#now load all content of the original file and rewrite modified to the same file
 if ($tfs -eq $true)
{
    Get-Content $file.FullName |
    %{$_ -replace 'AssemblyDescription.+', "AssemblyDescription("""")]" }  > $tempFile
}
else
{
    Write-Host "Creating assembly info file for:" $file.FullName " build number:" $buildNumber

    Get-Content $file.FullName |
    %{$_ -replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyVersion(""$buildNumber"")" } |
    %{$_ -replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyFileVersion(""$buildNumber"")" } |
    %{$_ -replace 'AssemblyDescription.+', "AssemblyDescription(""PrivateBuild"")]" }  > $tempFile
}

$destDir = $file.DirectoryName + "\..\obj\"
$dir = New-Item -Path $destDir -ItemType Directory -Force
$destFile = $destDir + $file.Name
Move-Item $tempFile $destFile -force