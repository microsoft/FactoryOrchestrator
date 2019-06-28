Param
(
[string]$SrcPath
)

$buildNumber = $env:TFS_VersionNumber
$tfs = $false

if ($buildNumber -eq $null)
{
    $date = Get-Date
    $month = $date.Month.ToString()
    if ($month.Length -eq 1)
    {
        $month = "0" + $month
    }

    $buildNumber = "0.0." + $date.Year.ToString().SubString(2) + "" + $month + "." + $date.Hour.ToString() + $date.Minute.ToString()
}
else
{
    $tfs = $true
    $buildNumber = "1.0." + $buildNumber
}

$file = Get-Item -Path "$SrcPath\Properties\AssemblyInfo.cs"
Write-Host "Creating assembly info file for:" $file.FullName
$tempFile = $env:TEMP + "\" + $file.Name + ".tmp"
    
#now load all content of the original file and rewrite modified to the same file
if ($tfs -eq $true)
{
    Get-Content $file.FullName |
    %{$_ -replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyVersion(""$buildNumber"")" } |
    %{$_ -replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyFileVersion(""$buildNumber"")" } |
    %{$_ -replace 'AssemblyDescription.+', "AssemblyDescription("""")]" }  > $tempFile
}
else
{
    Get-Content $file.FullName |
    %{$_ -replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyVersion(""$buildNumber"")" } |
    %{$_ -replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', "AssemblyFileVersion(""$buildNumber"")" } |
    %{$_ -replace 'AssemblyDescription.+', "AssemblyDescription(""PrivateBuild"")]" }  > $tempFile
}

$destDir = $file.DirectoryName + "\..\obj\"
$dir = New-Item -Path $destDir -ItemType Directory -Force
$destFile = $destDir + $file.Name
Move-Item $tempFile $destFile -force