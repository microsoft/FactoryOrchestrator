# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

#requires -RunAsAdministrator
Param
(
    [switch]$AutoStart
)
$ErrorActionPreference = "stop"

if (-not $IsWindows)
{
    $command = $PSCommandPath.Replace(".ps1", ".sh")
    if ($AutoStart)
    {
        . sudo bash $command enable
    }
    else
    {
        . sudo bash $command
    }
}
else
{
    $installdir = Join-Path -path $env:ProgramFiles -childpath "FactoryOrchestrator"

    # Uninstall existing service, if installed
    $ErrorActionPreference = "SilentlyContinue"
    $service = Get-Service -Name "Microsoft.FactoryOrchestrator"
    $ErrorActionPreference = "stop"
    if ($null -ne $service)
    {
        Set-Service -InputObject $service -Status Stopped
        Remove-Service -InputObject $service
    }
    if ((Test-Path $installdir) -ne $false)
    {
        Remove-Item $installdir -Recurse -Force
    }

    # Install new service
    Join-Path $PSScriptRoot "Microsoft.FactoryOrchestrator.Service-$Version$-$BuildOS$-$BuildPlatform$-bin.zip" | Expand-Archive -DestinationPath $installdir
    Join-Path $PSScriptRoot "uninstall.ps1" | Copy-Item -DestinationPath $installdir

    $null = New-Service -Name "Microsoft.FactoryOrchestrator" -BinaryPathName "$installdir\Microsoft.FactoryOrchestrator.Service.exe" -Description "Factory Orchestrator service version $Version$" -StartupType Manual

    Write-Host "FactoryOrchestrator service version $Version$ is installed to $installdir and configured as a Windows service!"
    Write-Host "Start it manually with: Start-Service -Name `"Microsoft.FactoryOrchestrator`""

    if ($AutoStart)
    {
        Set-Service -Name "Microsoft.FactoryOrchestrator" -StartupType Automatic
        Write-Host "The FactoryOrchestrator service is set to `"Auto`" start! It will start on next boot or if started manually."
    }
}