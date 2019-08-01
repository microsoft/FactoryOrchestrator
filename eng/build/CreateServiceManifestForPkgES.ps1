Param
(
    [Parameter(Mandatory = $true)][string]$BuildConfiguration,
    [Parameter(Mandatory = $true)][string]$NtTreeRootFolder,
    [Parameter(Mandatory = $true)][string]$BinFolder
)

$ErrorActionPreference = "stop"
& "$PSScriptRoot\CreateServiceManifest.ps1" -PublishFolder "$BinFolder\$BuildConfiguration\x86\FactoryOrchestratorService_SCD" -SourceRootFolder "`$(build.nttree)\$NtTreeRootFolder" -OutputFile "$BinFolder\$BuildConfiguration\x86\FactoryOrchestratorService_SCD\Microsoft-OneCore-FactoryOrchestrator-Service-x86.wm.xml" -Owner "Microsoft" -Namespace "OneCore" -Name "FactoryOrchestrator-Service-x86" -DestinationRootFolder "`$(runtime.system32)\manufacturing\FactoryOrchestrator"
& "$PSScriptRoot\CreateServiceManifest.ps1" -PublishFolder "$BinFolder\$BuildConfiguration\x64\FactoryOrchestratorService_SCD" -SourceRootFolder "`$(build.nttree)\$NtTreeRootFolder" -OutputFile "$BinFolder\$BuildConfiguration\x64\FactoryOrchestratorService_SCD\Microsoft-OneCore-FactoryOrchestrator-Service-x64.wm.xml" -Owner "Microsoft" -Namespace "OneCore" -Name "FactoryOrchestrator-Service-x64" -DestinationRootFolder "`$(runtime.system32)\manufacturing\FactoryOrchestrator"
& "$PSScriptRoot\CreateServiceManifest.ps1" -PublishFolder "$BinFolder\$BuildConfiguration\ARM\FactoryOrchestratorService_SCD" -SourceRootFolder "`$(build.nttree)\$NtTreeRootFolder" -OutputFile "$BinFolder\$BuildConfiguration\ARM\FactoryOrchestratorService_SCD\Microsoft-OneCore-FactoryOrchestrator-Service-ARM.wm.xml" -Owner "Microsoft" -Namespace "OneCore" -Name "FactoryOrchestrator-Service-ARM" -DestinationRootFolder "`$(runtime.system32)\manufacturing\FactoryOrchestrator"
& "$PSScriptRoot\CreateServiceManifest.ps1" -PublishFolder "$BinFolder\$BuildConfiguration\ARM64\FactoryOrchestratorService_SCD" -SourceRootFolder "`$(build.nttree)\$NtTreeRootFolder" -OutputFile "$BinFolder\$BuildConfiguration\ARM64\FactoryOrchestratorService_SCD\Microsoft-OneCore-FactoryOrchestrator-Service-ARM64.wm.xml" -Owner "Microsoft" -Namespace "OneCore" -Name "FactoryOrchestrator-Service-ARM64" -DestinationRootFolder "`$(runtime.system32)\manufacturing\FactoryOrchestrator"