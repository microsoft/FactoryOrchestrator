# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# This script performs minor cleanup of the .md files created by DefaultDocumentation, so that links are not broken
Param
(
[string]$DefaultDocumentationFolder
)

$ErrorActionPreference = "stop"

Write-Host "Fixing markdown in $DefaultDocumentationFolder."
Write-Host "Replacing '``1'' strings..."

$mds = Get-ChildItem -Path $DefaultDocumentationFolder -Include "*.md" -Recurse

foreach ($md in $mds)
{
    $content = Get-Content $md
    $content = $content.Replace("``1'","'")
    Set-Content -Path $md -Value $content 
}