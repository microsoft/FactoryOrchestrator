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

$AllVersionFiles = Get-ChildItem $SrcPath AssemblyInfo.cs -recurse 

foreach ($file in $AllVersionFiles) 
{
    Write-Host "Modifying file " + $file.FullName
    #save the file for restore
    $backFile = $file.FullName + ".backup.tmp"
    $tempFile = $file.FullName + ".tmp"
    Copy-Item $file.FullName $backFile
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

    Move-Item $tempFile $file.FullName -force
}