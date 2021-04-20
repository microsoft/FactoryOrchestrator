# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# Updates the gh-pages branch with the latest documentation changes
(
    [string]$RepoRoot,
    [switch]$Force
)

$zip = Join-Path $PSScriptRoot "gh-pages.zip"

if (!(Test-Path $zip))
{
    Write-Error "$zip not found!"
}

if (!(Test-Path (Join-Path $RepoRoot "src/FactoryOrchestrator.sln")))
{
    Write-Error "$RepoRoot is not a valid FactoryOrchestrator repo!"
}

Set-Location $RepoRoot
git fetch public
git reset HEAD --hard
git clean -d -f
git checkout public/gh-pages
get-childitem $RepoRoot -Directory | remove-item -recurse -force
    
# copy new website build to repro root
write-host "Extracting built website..."
Expand-Archive -Path $zip -DestinationPath $RepoRoot -Force
write-host "Extracting built website... DONE!"

write-host "Using git commands to check for changes..."
# restore sitemap.xml, as it has a timestamp that changes on build
git restore sitemap.xml*
# add all files and check for changes
git add -A
git diff --cached --exit-code

if ($LASTEXITCODE -ne 0)
{
    git commit -m "Update documentation"
    #git push public HEAD:gh-pages --force
}
