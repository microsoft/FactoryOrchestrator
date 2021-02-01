# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# This script is used create UWPClient NuGet packages from the signed binaries & the .nuspec files created during the initial build.
Param
(
    [string]$NuspecDirectory, # Path to UWPClient_Nuspecs artifact
    [string]$OutputDirectory
)

$specFolder = "$NuspecDirectory".Replace('/', '\')
$nugetexe = $env:NUGETEXETOOLPATH
$symspec = Get-ChildItem -Path $specFolder -Filter "*symbols.nuspec" | Select-Object -First 1
$nuspecs = Get-ChildItem -Path $specFolder -Filter "*.nuspec"

Write-Host "Found the following symbol NuSpec files:"
Write-Host $nuspecs

# Merge symbol nuspec into "normal" nuspec. Nuget.exe pack -Symbols will build both from one .nuspec
# Do this by finding the files referenced in the symbol nuspec
[xml]$symContentXml = get-content $symspec
$nuspecNs = "http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd"
$ns = @{nu = "$nuspecNs"}
$symFileNodes = (Select-Xml -Xml $symContentXml -XPath "//nu:file" -Namespace $ns).Node


foreach ($nuspec in $nuspecs)
{
  if ($($nuspec.Name) -like "*symbols.nuspec")
  {
    continue
  }

  # Merge in symbol files
  [xml]$contentXml = get-content $nuspec
  $filesNode = (Select-Xml -Xml $contentXml -XPath "//nu:files" -Namespace $ns).Node
  foreach ($symFile in $symFileNodes)
  {
    $importedNode = $contentXml.ImportNode($symFile, $true)
    $filesNode.AppendChild($importedNode)
  }
  $contentXml.Save($nuspec)

  # Fixup any references to src directory in .nuspec
  $content = get-content $nuspec
  $content = $content.Replace("$env:Build_SourcesDirectory", "$env:FORepoRoot")
  Set-Content -Path $nuspec -Value $content

  Write-Host "$nugetexe pack $($nuspec.FullName) -Symbols -SymbolPackageFormat snupkg -outputdirectory `"$OutputDirectory`""
  . $nugetexe pack $($nuspec.FullName) -Symbols -SymbolPackageFormat snupkg -outputdirectory "$OutputDirectory"
}
