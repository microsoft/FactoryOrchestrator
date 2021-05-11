# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

if ((-not $PSEdition -eq "Desktop") -and (-not $IsWindows))
{
    $command = $PSCommandPath.Replace(".ps1", ".sh")
    . sudo bash $command
    exit
}

Write-Host "We're sorry to see you go! Please consider filing an issue on GitHub at https://github.com/microsoft/FactoryOrchestrator/issues with any questions, bugs, or feedback you have.`n`n"

$ErrorActionPreference = "SilentlyContinue"
$service = Get-Service -Name "Microsoft.FactoryOrchestrator"
$ErrorActionPreference = "stop"

# Stop Service
if ($null -ne $service)
{
    Set-Service -Name "Microsoft.FactoryOrchestrator" -Status Stopped
}

# Find the install directory
$folder = $PSScriptRoot
if (!(Test-Path (Join-Path $folder "Microsoft.FactoryOrchestrator.Service.exe")))
{
    $folder = Join-Path $env:ProgramFiles "FactoryOrchestrator"
    while (!(Test-Path (Join-Path $folder "Microsoft.FactoryOrchestrator.Service.exe")))
    {
        $folder = Read-Host -Prompt "Factory Orchestrator not found in $folder! Please enter the path to the folder where you installed Factory Orchestrator"
        $folder = $folder.TrimEnd('"').TrimStart('"')
        if ($folder.EndsWith("Microsoft.FactoryOrchestrator.Service.exe"))
        {
            $folder = [System.IO.Path]::GetDirectoryName($folder)
        }
        Write-Host $folder
    }
}

# Delete files
Remove-item $folder -Recurse -Force
Write-Host "Deleted all files in $folder."

# Remove or disable Windows service
if ($null -ne $service)
{
    if ($PSVersionTable.PSVersion.Major -ge 6)
    {
        # On PS 6+, we can fully uninstall the service
        Remove-Service -Name "Microsoft.FactoryOrchestrator"
        Write-Host "Deleted service configuration."
    }
    else
    {
        # Disable service
        Set-Service -Name "Microsoft.FactoryOrchestrator" -StartupType Disabled
        Write-Warning "Service configuration cannot be deleted, but all binaries are deleted and the service is set to 'Disabled'. If desired, you can manually delete the 'Microsoft.FactoryOrchestrator' service with services.msc."
    }
}
