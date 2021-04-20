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
        if (($line -like "*<Target*BeforeBuildPS*") -or ($line -like "*<PropertyGroup*XES_OUTDIR*!=*"))
        {
            Write-host "skip $line"
            $inskip = $true
            continue
        }
        elseif ($line -like "*</Target>*")
        {
            Write-host "skip $line"
            $inskip = $false
            continue
        }
        elseif (($line -like "*</PropertyGroup>*") -and $inskip)
        {
            Write-host "skip  $line"
            $inskip = $false
            continue
        }

        if (-not $inskip)
        {
            if ($line -like "*<OutputPath>*")
            {
                # fix output path
                $outputContent += '    <OutputPath>.\bin\$(Configuration)\$(Platform)\$(TargetName)</OutputPath>' + "`n"
            }
            elseif (-not ($line -like "*ProjectReference*Include*"))
            {
                $outputContent += $line + "`n"
            }
            elseif ($added -eq $false)
            {
                # Add all the needed references, but for the WSK
                $outputContent += "    <Reference Include=`"Microsoft.FactoryOrchestrator.Client`">`n"
                $outputContent += "      <HintPath>`..\lib\NetStandard\Microsoft.FactoryOrchestrator.Client.dll</HintPath>`n"
                $outputContent += "    </Reference>`n"
                $outputContent += "    <Reference Include=`"Microsoft.FactoryOrchestrator.Core`">`n"
                $outputContent += "      <HintPath>`..\lib\NetStandard\Microsoft.FactoryOrchestrator.Core.dll</HintPath>`n"
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
    }

    Set-Content -Path "$($proj.FullName)" -Value $outputContent
}
