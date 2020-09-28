# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# Creates a .zip file containing all the test DLLs & files recursively found in a given root folder. It also creates a text file listing the test DLL relative paths.
Param
(
    [string]$TestBinRoot,
    [string]$OutFolder
)
$runtimeconfigs = Get-ChildItem $TestBinRoot -Recurse -Filter "*.runtimeconfig.json"
$testsTxt = @() 
foreach ($config in $runtimeconfigs)
{
    $testName = $config.Name.Replace('.runtimeconfig.json', '.dll');
    $testRelPath = $config.FullName.Replace((Get-Item $TestBinRoot).FullName, '')
    $testRelPath = $testRelPath.Replace('.runtimeconfig.json', '.dll');
    $testsTxt += $testRelPath
}

$testsTxt | Out-File "$TestBinRoot\tests.txt"
if ((Test-Path $OutFolder) -eq $false)
{
    $Folder = New-Item -Path $OutFolder -ItemType Directory
}
Compress-Archive -Path "$TestBinRoot\*" -DestinationPath "$OutFolder\NetCoreTests.zip" -Force