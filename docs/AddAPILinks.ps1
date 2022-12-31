# Copyright (c) Microsoft Corporation.
# Licensed under the MIT license.

# This script is used to add links to API documentation for manually authored Markdown files.
# It attempts to be as cautious as possible to avoid mis-linking an unrelated work to a C# class/API/etc.
# It should be run whenever you edit a manually authored Markdown file.
$regex = '\(.*\)'

# Files to search
$scriptDir = Split-Path $script:MyInvocation.MyCommand.Path
$docPath = [System.IO.Path]::Join($scriptDir, "docs")
$userDocs = Get-ChildItem -Path $docPath -filter "*.md"
$apiDocs = Get-ChildItem -Path $docPath -filter "TaskList*.md" -Recurse
$apiDocs += Get-ChildItem -Path $docPath -filter "TaskBase*.md" -Recurse
$apiDocs += Get-ChildItem -Path $docPath -filter "UWPTask.md" -Recurse
$apiDocs += Get-ChildItem -Path $docPath -filter "ExecutableTask.md" -Recurse
$apiDocs += Get-ChildItem -Path $docPath -filter "PowerShellTask.md" -Recurse
$apiDocs += Get-ChildItem -Path $docPath -filter "ExternalTask.md" -Recurse
$apiDocs += Get-ChildItem -Path $docPath -filter "TaskRun*.md" -Recurse
$apiDocs += Get-ChildItem -Path $docPath -filter "FactoryOrchestratorClient_*.md" -Recurse
$apiDocs += Get-ChildItem -Path $docPath -filter "ServerPoller_*.md" -Recurse
$apiDocs = $apiDocs | Sort-Object -Descending
$apiNames = @{}

# Ignore these words in .md even though they map to APIs, as they are used in multiple classes or are vague words.
$dupNames = @("Equals", "Name", "Guid", "ToString", "IsRunningOrPending", "Path", "Tasks", "Connect", "Status", "IpAddress", "EnumerateFiles", "EnumerateDirectories")
foreach ($doc in $apiDocs)
{
    $name = $($doc.BaseName)

    $regex = '\(.*\)'
    $name = $name -replace $regex, ''

    $name = $name.Split('-') | Select-Object -Last 1
    Write-Verbose "$name from $($doc.BaseName)"

    if (-not $apiNames.ContainsKey($name))
    {
        if (-not $dupNames.Contains($name))
        {
            # Change file path to html path
            $path = $($doc.FullName).Replace($docPath, "..").Replace('\','/').Replace('(', '%28').Replace(')', '%29').Replace('.md', '/')
            $apiNames.add($name, $path)
            Write-Verbose "Added $name to $path"
        }
    }
    else
    {
        # if a name is in two different classes, dont link it as we might link the wrong one
        Write-Verbose "Duplicate $name"

        if (-not $dupNames.Contains($name))
        {
            $dupNames += $name
        }

        if (-not ($apiNames[$name] -like "*()"))
        {
            $apiNames.remove($name)
            Write-Warning "Possible confusion with $name"
        }
    }
}

# Add links until no more changes are found
$changeMade = $true
while ($changeMade -eq $true)
{
    $changeMade = $false

    foreach ($doc in $userDocs)
    {
        Write-Verbose "Checking $doc"
        $content = Get-Content $doc
        $edited = @()
        $incode = $false
        foreach ($line in $content)
        {
            $newline = $null
            if ($line.Contains('```'))
            {
                # sadly I didn't find a way to add links to code snippets :(
                $incode = -not $incode
            }
            
            if (-not $incode)
            {
                # Split line into words and words into sections
                $words = $line.Split(' ')
                $sections = $words.Split('.')
                $sections = $sections.Split('(')
                $sections = $sections.Split(',')
                foreach ($section in $sections)
                {
                    $section = $section.Replace(')', '')
                    $section = $section.Replace(';', '')
                    if ([System.Char]::IsUpper($section[0]))
                    {
                        if ($apiNames.ContainsKey($section))
                        {
                            $index = $line.IndexOf($section)
                            
                            # Don't add a link if it's already a link :)
                            while ($line[$index-1] -eq '[')
                            {
                                $index = $line.IndexOf($section, $index+1)                            
                                if ($index -eq -1)
                                {
                                    break
                                }
                            }

                            if ($index -eq -1)
                            {
                                continue
                            }

                            # Ensure we are at the start of a new word, not in the middle of one.
                            while (($line[$index-1] -ne ' ') -and ($line[$index-1] -ne '(') -and  ($line[$index-1] -ne '.'))
                            {
                                $index = $line.IndexOf($section, $index+1)
                                if ($index -eq -1)
                                {
                                    break
                                }

                            }

                            if ($index -eq -1)
                            {
                                continue
                            }

                            # All checks are passed! Add the link!
                            Write-Host "found $section at $index ($($line.Substring($index, $section.Length)))"
                            Write-Host "                 $line"
                            Write-Host ""
                            Write-Host "                $section maps to $($apiNames[$section])"
                            $newline = $line.Substring(0, $index) + "[$section]($($apiNames[$section]))" + $line.Substring($index + $section.Length)
                            $changeMade = $true
                        }
                    }
                }
            }

            if ($null -ne $newline)
            {
                $edited += $newline
            }
            else
            {    
                $edited += $line
            }
        }

        Out-File -FilePath $doc -InputObject $edited
    }
}