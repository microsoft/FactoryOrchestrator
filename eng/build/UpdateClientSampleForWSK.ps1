# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

Param
(
    [string]$ProjectFolder
)

[string]$outputContent = ""
Write-Host "Modifying csproj in $ProjectFolder for WSK use..."
$projs = Get-ChildItem -Path "$ProjectFolder" -Filter "*.csproj"
foreach ($proj in $projs)
{
    $content = Get-Content $proj.FullName
    $added = $false
    foreach ($line in $content)
    {
        if (-not ($line -like "*ProjectReference*Include*"))
        {
            $outputContent += $line + "`n"
        }
        elseif ($added -eq $false)
        {
            # Add all the needed references, but for the WSK
            $outputContent += "    <Reference Include=`"FactoryOrchestratorClientLibrary`">`n"
            $outputContent += "      <HintPath>`..\lib\NetStandard\FactoryOrchestratorClientLibrary.dll</HintPath>`n"
            $outputContent += "    </Reference>`n"
            $outputContent += "    <Reference Include=`"FactoryOrchestratorCoreLibrary`">`n"
            $outputContent += "      <HintPath>`..\lib\NetStandard\FactoryOrchestratorCoreLibrary.dll</HintPath>`n"
            $outputContent += "    </Reference>`n"
            $outputContent += "    <Reference Include=`"IpcServiceFramework.Client`">`n"
            $outputContent += "      <HintPath>`..\lib\NetStandard\OpenSourceSoftware\IpcServiceFramework\IpcServiceFramework.Client.dll</HintPath>`n"
            $outputContent += "    </Reference>`n"
            $outputContent += "    <Reference Include=`"IpcServiceFramework.Core`">`n"
            $outputContent += "      <HintPath>`..\lib\NetStandard\OpenSourceSoftware\IpcServiceFramework\IpcServiceFramework.Core.dll</HintPath>`n"
            $outputContent += "    </Reference>`n"

            $added = $true
        }
    }

    Set-Content -Path "$($proj.FullName)" -Value $outputContent
}
