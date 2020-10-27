# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# Creates a .zip file containing all the test DLLs & files recursively found in a given root folder. It also creates a text file listing the test DLL relative paths.
Param
(
    [string]$TestBinRoot,
    [string]$OutFolder
)
$runtimeconfigs = Get-ChildItem $TestBinRoot -Recurse -Filter "*.runtimeconfig.json"
$testsXml = @('<?xml version="1.0"?>',
              '<Data>',
              '<Table Id="NetCoreTestsRelativePaths">'
              '<ParameterTypes>',
              '<ParameterType Name="NetCoreTestRelativePath">String</ParameterType>',
              '</ParameterTypes>')

foreach ($config in $runtimeconfigs)
{
    $testName = $config.Name.Replace('.runtimeconfig.json', '');
    $testRelPath = $config.FullName.Replace((Get-Item $TestBinRoot).FullName, '')
    $testRelPath = $testRelPath.Replace('.runtimeconfig.json', '.dll');
    $testsXml += "<Row Name='$testName' Owner=`"mfgcore`">"
    $testsXml += "<Parameter Name=`"NetCoreTestRelativePath`">$testRelPath</Parameter>"
    $testsXml += '</Row>'
}

$testsXml += '</Table>'
$testsXml += '</Data>'

if ((Test-Path $OutFolder) -eq $false)
{
    $Folder = New-Item -Path $OutFolder -ItemType Directory
}
$testsXml | Out-File "$OutFolder\NetCoreTests.xml"
Compress-Archive -Path "$TestBinRoot\*" -DestinationPath "$OutFolder\NetCoreTests.zip" -Force
