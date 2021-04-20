# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

Param
(
    [string]$RepositoryRoot
)

$ErrorActionPreference = "stop"
Set-Location $RepositoryRoot

if (!(Test-Path ".\src\FactoryOrchestrator.sln"))
{
    Write-Error "$RepositoryRoot is not the root of the Factory Orchestrator repository!"
}

