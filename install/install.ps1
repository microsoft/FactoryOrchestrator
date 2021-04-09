# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

Param
(
    [Parameter(Mandatory = $true)][switch]$AutoStart
)
$ErrorActionPreference = "stop"

if (-not $IsWindows)
{
    $command = $PSScriptRoot.Replace(".ps1", ".sh")
    if ($AutoStart)
    {
        . bash $command install
    }
    else
    {
        . bash $command
    }
}
else
{
    $dir = [System.IO.Path]::GetDirectoryName($PSScriptRoot)
    $installdir = Join-Path -path $env:ProgramFiles -childpath "FactoryOrchestrator"
    Write-Host "The FactoryOrchestrator service is installed as a systemd service!"
`   Write-Host "Start it manually with: Start-Service -Name `"Microsoft.FactoryOrchestrator`""


    if ($AutoStart)
    {
        New-Service -Name "Microsoft.FactoryOrchestrator" -BinaryPathName "Microsoft.FactoryOrchestrator.Service.exe" -StartupType Automatic
        Write-Host "The FactoryOrchestrator service is enabled! It will start on next boot or if started manually."
    }
    else
    {
        New-Service -Name "Microsoft.FactoryOrchestrator" -BinaryPathName "$dir\Microsoft.FactoryOrchestrator.Service.exe" -StartupType Manual
    }
    Write-Host ""
}