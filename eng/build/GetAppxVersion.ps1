param([string]$appxPath, $manifestFileName ="AppxManifest.xml")

if (!$appxPath -or !([system.io.file]::Exists($appxPath)))
{
   Write-Warning "GetAppxVersion: Warning: Path not set or does not exist."
   exit 1
}
Add-Type -assembly "system.io.compression"
Add-Type -assembly "system.io.compression.filesystem"

$archive = [io.compression.zipfile]::Open($appxPath, [System.IO.Compression.ZipArchiveMode]::Read)
$entry = $archive.GetEntry($manifestFileName)

Write-Host ("GetAppxVersion: Found manifest file: " + $entry.Name)

$reader = new-object System.IO.BinaryReader $entry.Open()
$byteArr = $reader.ReadBytes($entry.Length)
$reader.Dispose()

#$manifestContents = [System.Text.Encoding]::UTF8.GetString($byteArr)
$manifestContents = [System.Text.Encoding]::Default.GetString($byteArr)
$manifestContents = $manifestContents -replace "\xEF\xBB\xBF", ""

$xdoc = new-object System.Xml.XmlDocument 
$xmlManifestContents = $xdoc.LoadXml($manifestContents)

$manifestVersion = $xdoc.Package.Identity.Version
Write-Host $manifestVersion
