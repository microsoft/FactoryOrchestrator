# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# This script is used to auto-generate the FactoryOrchestratorClient implementation of IFactoryOrchestratorService (IPCInterface.cs)
Param
(
    [string]$InterfaceName,
	[string]$InterfaceFile,
    [string]$TemplateFile,
	[string]$OutputFile
)

Write-Host "Autogenerating $OutputFile from $InterfaceName interface in $InterfaceFile"
$file = Get-Item -Path "$InterfaceFile"
$interfaceContent = Get-Content $file.FullName
$inInterface = $false
$indent = "    "
[string]$outputContent = ""
$currentSummary = @()
# find the interface, then iterate through the APIs in it
foreach ($line in $interfaceContent)
{
    if ($inInterface -eq $true)
    {
        if ($line -like "*/// <summary>*")
        {
            # add to methodSummaries
            $currentSummary += "$line`n"
        }
        elseif ($line -like "*///*")
        {
            # add to methodSummaries
            $currentSummary += "$line`n"
        }
        elseif ($line -like "*;")
        {
            # write out summary
            foreach ($summ in $currentSummary)
            {
                $outputContent += $summ
            }
            $currentSummary.Clear()

            # line is an API in the interface, parse it
            $api = $line
            [regex]$rx='\s*(.+)\s(.+)(\(.+);'
            $result = $rx.Match($api)
            $apiRet = $result.Groups[1].Value
            $apiName = $result.Groups[2].Value
            $apiArgs = $result.Groups[3].Value
            Write-Host "Found api: $apiName args: $apiArgs ret: $apiRet"

            # Create a new API per interface API which wraps the InvokeAsync call
            if ($apiRet -eq "void")
            {
                $outputContent += "$indent$indent" + "public async Task $apiName$apiArgs"
            }
            else
            {
                $outputContent += "$indent$indent" + "public async Task<$apiRet> $apiName$apiArgs"
            }

            $outputContent += "`n$indent$indent{"
            $outputContent += "`n$indent$indent$indent" + "if (!IsConnected)" + "`n$indent$indent$indent" + "{`n$indent$indent$indent$indent" + "throw new FactoryOrchestratorConnectionException(Resources.ClientNotConnected);`n$indent$indent$indent}"
            $outputContent += "`n`n$indent$indent$indent" + "try`n$indent$indent$indent{"
            $outputContent += "`n$indent$indent$indent$indent"
            
            if ($apiRet -eq "void")
            {
                $outputContent += " await _IpcClient.InvokeAsync(CreateIpcRequest(`"$apiName`""
            }
            else
            {
                $outputContent += "return await _IpcClient.InvokeAsync<$apiRet>(CreateIpcRequest(`"$apiName`""
            }

            # Split args, remove default values and variable typesd
            if ($apiArgs -like "()")
            {
                $outputContent += ")"
            }
            else
            {
                $outputContent += ", "
                $argsSplit = $apiArgs.Split(',')
                for ($i = 0 ; $i -lt $argsSplit.Count; $i++)
                {
                    $arg = $argsSplit[$i]
                    $equalSplit = $arg.Split('=')[0]
                    [regex]$rx='\s*.+\s+(.+)\s?'
                    $result = $rx.Match($equalSplit)
                    $outputContent += "$($result.Groups[1].Value)"

                    if ($i -ne $argsSplit.Count - 1)
                    {
                        $outputContent += ", "
                    }
                    elseif ("$($result.Groups[1].Value)" -notlike '*)*')
                    {
                        $outputContent += ")"
                    }
                }
            }
            $outputContent += ");"
            $outputContent += "`n$indent$indent$indent}`n$indent$indent$indent"
            $outputContent += "catch (Exception ex)`n$indent$indent$indent{`n$indent$indent$indent$indent"
            $outputContent += "throw CreateIpcException(ex);`n"
            $outputContent += "$indent$indent$indent}`n"
            $outputContent += "$indent$indent}`n`n"

        }
        elseif ($line -like "*}*$InterfaceName")
        {
            # no longer in interface
            break
        }
    }
    elseif ($line -like "*$InterfaceName")
    {
        $inInterface = $true
    }
}

$outputContent += "`n$indent}`n}"

# Save to file
Copy-Item -Path "$TemplateFile" -Destination "$OutputFile"
Add-Content -Path "$OutputFile" -Value "$outputContent"