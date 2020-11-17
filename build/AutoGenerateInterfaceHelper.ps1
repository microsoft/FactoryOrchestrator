# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# This script is used to auto-generate the FactoryOrchestratorClient implementation of IFactoryOrchestratorService (IPCInterface.cs)
Param
(
    [string]$InterfaceName,
	[string]$InterfaceFile,
    [string]$TemplateFile,
	[string]$OutputFile,
    [switch]$Async
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
            $currentSummary = ""
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
            if ($Async)
            {
                $summarySplit = $currentSummary.Split("`n")
                for ($i = 0 ; $i -lt $summarySplit.Count; $i++)
                {
                    $summ = $summarySplit[$i];
                    if ($i -eq 1)
                    {
                        $summ = $summ.Replace("///", "/// Asynchronously");
                    }

                    if ($i -ne $summarySplit.Count - 1)
                    {
                        $outputContent += "$summ`n"
                    }
                }
            }
            else
            {
                $outputContent += $currentSummary
            }

            # line is an API in the interface, parse it
            $api = $line
            [regex]$rx='\s*(.+)\s(.+)(\(.+);'
            $result = $rx.Match($api)
            $apiRet = $result.Groups[1].Value
            $apiName = $result.Groups[2].Value
            $apiArgs = $result.Groups[3].Value
            Write-Host "Found api: $apiName args: $apiArgs ret: $apiRet"

            # Create a new API per interface API which wraps the InvokeAsync call
            $outputContent += "$indent$indent" + "public "

            if ($Async)
            {
                if ($apiRet -eq "void")
                {
                    $outputContent += "async Task "
                }
                else
                {
                    $outputContent += "async Task<$apiRet> "
                }
            }
            else
            {
                $outputContent += "$apiRet "
            }

            $outputContent += "$apiName$apiArgs"
            $outputContent += "`n$indent$indent{`n$indent$indent$indent"

            if ($Async)
            {
                $outputContent += "if (!IsConnected)" + "`n$indent$indent$indent" + "{`n$indent$indent$indent$indent" + "throw new FactoryOrchestratorConnectionException(Resources.ClientNotConnected);`n$indent$indent$indent}"
                $outputContent += "`n`n$indent$indent$indent" + "try`n$indent$indent$indent{"
                $outputContent += "`n$indent$indent$indent$indent"
            
                if ($apiRet -eq "void")
                {
                    $outputContent += "await _IpcClient.InvokeAsync(CreateIpcRequest(`"$apiName`""
                }
                else
                {
                    $outputContent += "return await _IpcClient.InvokeAsync<$apiRet>(CreateIpcRequest(`"$apiName`""
                }
            }
            else
            {
                if ($apiRet -eq "void")
                {
                    $outputContent += "AsyncClient.$apiName("
                }
                else
                {
                    $outputContent += "return AsyncClient.$apiName("
                }
            }

            # Split args, remove default values and variable typesd
            if ($apiArgs -like "()")
            {
                $outputContent += ")"
            }
            else
            {
                if ($Async)
                {
                    $outputContent += ", "
                }
                $argsSplit = $apiArgs.Split(',')
                $invokeAsyncArgs = ""

                for ($i = 0 ; $i -lt $argsSplit.Count; $i++)
                {
                    $arg = $argsSplit[$i]
                    $equalSplit = $arg.Split('=')[0]
                    [regex]$rx='\s*.+\s+(.+)\s?'
                    $result = $rx.Match($equalSplit)
                    $invokeAsyncArgs += "$($result.Groups[1].Value)"

                    if ($i -ne $argsSplit.Count - 1)
                    {
                        $invokeAsyncArgs += ", "
                    }
                    elseif ("$($result.Groups[1].Value)" -notlike '*)*')
                    {
                        $invokeAsyncArgs += ")"
                    }
                }
                $outputContent += $invokeAsyncArgs
            }

            if ($Async)
            {
                $outputContent += ")"
            }
            else
            {
                if ($apiRet -eq "void")
                {
                    $outputContent += ".Wait()"
                }
                else
                {
                    $outputContent += ".Result"
                }
            }

            $outputContent += ";"

            if ($Async)
            {
                $outputContent += "`n$indent$indent$indent}`n$indent$indent$indent"
                $outputContent += "catch (Exception ex)`n$indent$indent$indent{`n$indent$indent$indent$indent"
                $outputContent += "throw CreateIpcException(ex);`n"
                $outputContent += "$indent$indent$indent}`n"
            }
            else
            {
                $outputContent += "`n"
            }

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

if ($null -ne $env:AGENT_MACHINENAME)
{
    # Running in Azure Pipeline. Print entire file for logging.
    Write-Host "------------ $OutputFile contents ------------"
    Get-Content $OutputFile | Write-Host
    Write-Host "`n"
}