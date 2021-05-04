# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# This script is manually run and makes a best effort attempt to add the copyright header to all source/build files in the repo.
function Add-CopyrightHeader
{	
    param
	(
        $file,
		$prefix,
		$suffix = ""
    )
	if ((($file -like "*/bin/*") -or ($file -like "*\bin\*")) -or (($file -like "*/obj/*") -or ($file -like "*\obj\*")) -or (($file -like "*/oss/*") -or ($file -like "*\oss\*")) -or (($file -like "*/docs/CoreLibrary/*") -or ($file -like "*\docs\CoreLibrary\*")) -or (($file -like "*/docs/ClientLibrary/*") -or ($file -like "*\docs\ClientLibrary\*")))
	{
		return
	}

	Write-Verbose "Checking $file"
	[System.Collections.ArrayList]$c = get-content $file
	if (($null -ne $c) -and (($c[0] -like "*<?xml*")) -and (-not ($c[1] -like "*Microsoft Corporation*")))
	{
		Write-Host "Adding copyright header to $file"
		$c.Insert(1, "$prefix Copyright (c) Microsoft Corporation.$suffix")
		$c.Insert(2, "$prefix Licensed under the MIT license.$suffix")
		$c.Insert(3, "")
		Set-Content -Path $file -Value $c
	}
	else	
	{
		if (($null -ne $c) -and (-not ($c[0] -like "*Microsoft Corporation*")))
		{
			Write-Host "Adding copyright header to $file"
			$c.Insert(0, "$prefix Copyright (c) Microsoft Corporation.$suffix")
			$c.Insert(1, "$prefix Licensed under the MIT license.$suffix")
			$c.Insert(2, "")
			Set-Content -Path $file -Value $c
		}
	}
}

$files = get-childitem "$PSScriptRoot/../../" -recurse -filter "*.yml"
$files += get-childitem "$PSScriptRoot/../../" -recurse -filter "*.ps1"
$files += get-childitem "$PSScriptRoot/../../" -recurse -filter "*.sh"
$files += get-childitem "$PSScriptRoot/../../" -recurse -filter "*.editorconfig"
foreach ($f in $files)
{
	Add-CopyrightHeader $f "#"
}

$files = get-childitem "$PSScriptRoot/../../" -recurse -filter "*.cs"
foreach ($f in $files)
{
	Add-CopyrightHeader $f "//"
}

$files = get-childitem "$PSScriptRoot/../../" -recurse -filter "*.cmd"
$files += get-childitem "$PSScriptRoot/../../" -recurse -filter "*.bat"
foreach ($f in $files)
{
	Add-CopyrightHeader $f "REM"
}

$files = get-childitem "$PSScriptRoot/../../" -recurse -filter "*.xml"
$files += get-childitem "$PSScriptRoot/../../" -recurse -filter "*.csproj"
$files += get-childitem "$PSScriptRoot/../../" -recurse -filter "*.props"
$files += get-childitem "$PSScriptRoot/../../" -recurse -filter "*.md"
foreach ($f in $files)
{
	Add-CopyrightHeader $f "<!--" " -->"
}
