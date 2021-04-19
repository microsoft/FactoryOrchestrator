# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

#requires -RunAsAdministrator
Param
(
    [switch]$AutoStart
)
$ErrorActionPreference = "stop"

if (($PSEdition -ne "Desktop") -and (-not $IsWindows))
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

    # Hide error this throws if the service isn't installed
    $ErrorActionPreference = "SilentlyContinue"
    $service = Get-Service -Name "Microsoft.FactoryOrchestrator"
    $ErrorActionPreference = "stop"

    if ($null -ne $service)
    {
        # Stop existing service
        Set-Service -InputObject $service -Status Stopped
        if ($PSVersionTable.PSVersion.Major -ge 6)
        {
            # On PS 6+, we can fully uninstall the service
            Remove-Service -InputObject $service
            $service = $null
        }
    }
    if ((Test-Path $installdir) -ne $false)
    {
        # Delete existing service binaries
        Remove-Item $installdir -Recurse -Force
    }

    # Install new service
    Join-Path $PSScriptRoot "Microsoft.FactoryOrchestrator.Service-$Version$-$BuildOS$-$BuildPlatform$-bin.zip" | Expand-Archive -DestinationPath $installdir
    Join-Path $PSScriptRoot "UninstallFactoryOrchestratorService.ps1" | Copy-Item -Destination $installdir

    if ($null -ne $service)
    {
        Set-Service -InputObject $service -Description "Factory Orchestrator service version $Version$" -StartupType Manual
    }
    else
    {
        $null = New-Service -Name "Microsoft.FactoryOrchestrator" -BinaryPathName "$installdir\Microsoft.FactoryOrchestrator.Service.exe" -Description "Factory Orchestrator service version $Version$" -StartupType Manual
    }
   
    Write-Host "Factory Orchestrator service version $Version$ is installed to `"$installdir`" and configured as a Windows service!`n"
    Write-Host "Start it manually with: Start-Service -Name `"Microsoft.FactoryOrchestrator`"`n"

    if ($AutoStart)
    {
        Set-Service -Name "Microsoft.FactoryOrchestrator" -StartupType Automatic
        Write-Host "The FactoryOrchestrator service is set to `"Auto`" start! It will start on next boot or if started manually."
    }
    else
    {
        Write-Host "The FactoryOrchestrator service is not set to `"Auto`" start. It will only start manually. Change it to `"Auto`" start on boot at any time with: Set-Service -Name `"Microsoft.FactoryOrchestrator`" -StartupType Automatic"
    }
}
