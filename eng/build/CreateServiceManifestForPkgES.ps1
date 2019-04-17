Param
(
    [Parameter(Mandatory = $true)][string]$BuildConfiguration,
    [Parameter(Mandatory = $true)][string]$NtTreeRootFolder,
    [Parameter(Mandatory = $true)][string]$BinFolder
)

$ErrorActionPreference = "stop"
& "$PSScriptRoot\CreateServiceManifest.ps1" -PublishFolder "$BinFolder\$BuildConfiguration\x86\FTFService_SCD" -SourceRootFolder "`$(build.nttree)\$NtTreeRootFolder" -OutputFile "$BinFolder\$BuildConfiguration\x86\FTFService_SCD\Microsoft-OneCore-FactoryTestFramework-Service-x86.wm.xml" -Owner "Microsoft" -Namespace "OneCore" -Name "FactoryTestFramework-Service-x86"
& "$PSScriptRoot\CreateServiceManifest.ps1" -PublishFolder "$BinFolder\$BuildConfiguration\x64\FTFService_SCD" -SourceRootFolder "`$(build.nttree)\$NtTreeRootFolder" -OutputFile "$BinFolder\$BuildConfiguration\x64\FTFService_SCD\Microsoft-OneCore-FactoryTestFramework-Service-x64.wm.xml" -Owner "Microsoft" -Namespace "OneCore" -Name "FactoryTestFramework-Service-x64"
& "$PSScriptRoot\CreateServiceManifest.ps1" -PublishFolder "$BinFolder\$BuildConfiguration\ARM\FTFService_SCD" -SourceRootFolder "`$(build.nttree)\$NtTreeRootFolder" -OutputFile "$BinFolder\$BuildConfiguration\ARM\FTFService_SCD\Microsoft-OneCore-FactoryTestFramework-Service-ARM.wm.xml" -Owner "Microsoft" -Namespace "OneCore" -Name "FactoryTestFramework-Service-ARM"
& "$PSScriptRoot\CreateServiceManifest.ps1" -PublishFolder "$BinFolder\$BuildConfiguration\ARM64\FTFService_SCD" -SourceRootFolder "`$(build.nttree)\$NtTreeRootFolder" -OutputFile "$BinFolder\$BuildConfiguration\ARM64\FTFService_SCD\Microsoft-OneCore-FactoryTestFramework-Service-ARM64.wm.xml" -Owner "Microsoft" -Namespace "OneCore" -Name "FactoryTestFramework-Service-ARM64"