# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

Param
(
    [Parameter(Mandatory = $true)][string]$PublishFolder,
    [Parameter(Mandatory = $false)][string]$SourceRootFolder = "",
    [Parameter(Mandatory = $true)][string]$DestinationRootFolder,
	[Parameter(Mandatory = $true)][string]$OutputFile,
    [Parameter(Mandatory = $true)][string]$Owner,
    [Parameter(Mandatory = $true)][string]$Namespace,
    [Parameter(Mandatory = $true)][string]$Name
)

$ErrorActionPreference = "stop"

[xml] $templateXml = Get-Content "$PSScriptRoot\FactoryOrchestratorServiceTemplate.wm.xml"
$wmxmlNs = "urn:Microsoft.CompPlat/ManifestSchema.v1.00"
$ns = @{wmxml = "$wmxmlNs"}

Write-Host "Creating manifest $OutputFile from $PublishFolder"

# Set manifest info
$rootNode = Select-Xml -Xml $templateXml -XPath "//wmxml:identity" -Namespace $ns | Select-Object -First 1
$rootNode.Node.SetAttribute("owner", "$Owner")
$rootNode.Node.SetAttribute("namespace", "$Namespace")
$rootNode.Node.SetAttribute("name", "$Name")

$filesNode = Select-Xml -Xml $templateXml -XPath "//wmxml:files" -Namespace $ns | Select-Object -First 1

[System.IO.DirectoryInfo] $PublishDir = $PublishFolder
$filesToAdd = Get-ChildItem $PublishFolder -Recurse -File
$fileNamesAdded = @{}

foreach ($file in $filesToAdd)
{
    # Default destination
    $fileDestinationDir = $DestinationRootFolder
    # If file was in a subfolder, append it
    if (-not ($file.DirectoryName -like $PublishDir.FullName))
    {
        $subfolder = $file.DirectoryName.SubString($PublishDir.FullName.Length + 1)
        $fileDestinationDir += "\$subfolder"
    }
    else
    {
        $subfolder = ""
    }

    # Source is relative to SourceRootFolder if given
	if ($SourceRootFolder -ne "")
	{
		$source = $SourceRootFolder + "\" + $subfolder + "\" + $file.Name
	}
	else
	{
		$source = $file.FullName
	}

    # Clean extra slashes
    $source = $source.Replace("\\\", "\\")
    $source = $source.Replace("\\", "\")

    $newElement = $templateXml.CreateElement("file", $wmxmlNs)
    $newElement.SetAttribute("destinationDir", "$fileDestinationDir")
    $newElement.SetAttribute("source", "$source")
    if ($fileNamesAdded.ContainsKey($file.Name))
    {
        # Resource files always have the same name. All files need to use unique wm.xml names or a collision occurs
        # even though they are in different source & destination folders.
        $name = $file.Name;
        $newElement.SetAttribute("name", "$subfolder\$name")
    }
    $null = $filesNode.Node.AppendChild($newElement)
	Write-Host "Added $($file.FullName)."
	Write-Host "---source: $source"
	Write-Host "---destinationDir: $fileDestinationDir"

    if (!$fileNamesAdded.ContainsKey($file.Name))
    {
        $fileNamesAdded.Add($file.Name, 1)
    }
}

Write-Host "Saving $OutputFile"
$templateXml.Save($OutputFile)
