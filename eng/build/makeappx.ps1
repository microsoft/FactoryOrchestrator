param(
   [string]$projectDir,
   [bool]$useMonthVersion = $false,
   [bool]$noBundleVersion = $false,
   [bool]$signBundle = $false,
   [int]$signCert = 136020001,
   [string]$pfxFile,
   [bool]$multi = $true,   
   [bool]$debug = $true,   
   [string]$appxBundleFilename,
   [string]$appxBundleOutputSubDir = "appxbundle",
   $platforms = @("arm", "x64", "x86", "arm64"),
   $configurations = @("Release")
)
# Creates an appx or msix bundle 

#############
#  Globals  #  
#############
Set-Variable -Option ReadOnly -Force -Scope "Global" -Name SCRIPT_LOCATION -Value $(split-path $myinvocation.mycommand.path)
$appxextension = ".appx"
$appxbundleExtension = ".appxbundle"

$msixextension = ".msix"
$msixbundleExtension = ".msixbundle"

$appxLanguageTag = "*_language-*"
$appxScaleTag = "*_scale-*"

$multiPlatformName = "Multi"
$appxBundleContentsDirName ="Contents"
$mainAppxPath = $null

$sourcesDir = $env:TFS_SourcesDirectory
$appxBundleOutputRoot = $env:TFS_DropLocation
$tfsVersionNumber = $env:TFS_VersionNumber

$signToolPath = $env:TFS_ToolsDirectory + "\bin\SimpleSign.exe"
if (![System.IO.File]::Exists($signToolPath))
{
    $signToolPath = "SimpleSign.exe"
}
else
{
    Write-Warning ("Using simple sign from $signToolPath will be deprecated soon")
}

# deprecate this soon
if ($env:UseSimpleSignAsb -eq "true")
{
    $signToolPath = "SimpleSignAsb.exe"      
}

if ($env:UseSimpleSignLocal -eq "true")
{
    $signToolPath = "SimpleSignLocal.exe"      
}

function FindLocationOfExe
{
    param($exeName)

    # Use version on the path first. This will let any build package override the installed value.
    $pathOfExe = $exeName
    Get-Command $pathOfExe -ErrorAction SilentlyContinue -ErrorVariable exenotfound | Out-Null
    if ($exenotfound)
    {
        # New Windows 10 location
        $pathOfExe = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\App Certification Kit\$exeName"
        if (-Not (Test-Path $pathOfExe))
        {
            # Old Windows 10 location
            $pathOfExe = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin\x64\$exeName"
            if (-Not (Test-Path $pathOfExe))
            {
                Throw "ERROR: $exeName not found in Windows Kits directories or on path"
            }
        }
    }

    $pathOfExe
}

function CreateAppxBundle
{
    param($tool, $outputAppxBundle, $contentsDir, $version)	
    
    if ([string]::IsNullOrEmpty($version))
    {
        $bundleVersionParam = ""
    }
    else
    {
        $bundleVersionParam = "/bv " + $version
    }
    
    if (!$useMonthVersion -and !$noBundleVersion)
    {
       Write-Host "Main Appx for Versioning: $global:mainAppxPath"
       $appxScriptPath = "$SCRIPT_LOCATION\GetAppxVersion.ps1"
       . $appxScriptPath $global:mainAppxPath

       if ([RegEx]::IsMatch($manifestVersion, "^\d+\.\d+\.\d+\.\d+$"))
       {
          Write-Host "Using appx version $manifestVersion"
          $bundleVersionParam = "/bv " + $manifestVersion
       }
    }	
    
    $makeAppxCmd = ("& `"" + $tool + "`" bundle /o $bundleVersionParam /d `"" + $contentsDir + "`" /p `"" + $outputAppxBundle + "`"")
        
    Write-Host $makeAppxCmd
    Invoke-Expression $makeAppxCmd
    #& cmd /c ("`"" + $makeAppxCmd + "`"")	
        
    if (!$?) 
    {
       Exit 1
    }
}

function CreateContentsDir
{
    param($platformBundleServerDir, $toPath, $platform)
   
    if (![System.IO.Directory]::Exists($toPath))
    {
        if ($debug)
        {
           Write-Host "Creating directory $toPath"
        }
        $nul = mkdir $toPath
    }
                
    if ($debug)	
    {
        Write-Host "Processing $platformBundleServerDir"
    }

    $appxFiles = Get-ChildItem -Path $platformBundleServerDir\* -Include @("*$msixextension", "*$appxextension")
    
    foreach($item in $appxFiles)
    {
        if ($debug)	
        {
            Write-Host "Processing file $item"
        }

        if ($item.BaseName -like $appxLanguageTag -or $item.BaseName -like $appxScaleTag)
        {
            # Copy the resource appx into the bundle staging folder. These are the "language" and "scale"
            # resource appx files, not the program appx.
            Write-Host ("Copying resource appx: " + $item.FullName)
            Copy-Item $item.FullName $toPath

            # Record appx name for versioning later
            if (!$global:mainAppxPath)
            {
                Set-Variable -Name "mainAppxPath" -Value $item.FullName -Scope Global                   
                Write-Warning "appxLanguageTag/appxScaleTag - Set global:mainAppxPath to : $global:mainAppxPath"
            }
        }
        else
        {
            # Copy and rename the program appx with platform specifier, i.e. "MyApp_x86.appx"
            if ($platform -and !($item.BaseName.ToLower().Contains($platform)))
            {
                if ($item.Extension -eq $appxextension)
                {
                    $appxFinalName = $item.BaseName + "_" + $platform + $appxextension
                }
                else
                {
                    $appxFinalName = $item.BaseName + "_" + $platform + $msixextension
                }

                $appxFinalPath = ([System.IO.Path]::Combine($toPath, $appxFinalName))
            }
            else 
            {
                $appxFinalName = $item.FullName
                $appxFinalPath = $toPath
            }
                
            # Record appx name for versioning later
            Set-Variable -Name "mainAppxPath" -Value $item.FullName -Scope Global  
            Write-Warning "Set global:mainAppxPath to : $global:mainAppxPath"

            Write-Host ("Copying " + $appxFinalName)
            Copy-Item $item.FullName $appxFinalPath
        }
    }
}

function Get-MajorMinorVersion
{
    param($useMonth = $false)
    $customPropsFile = [System.IO.Path]::Combine($sourcesDir, "Custom.props")

    if ([System.IO.File]::Exists($customPropsFile))
    {
       [xml]$customProps = Get-Content ($customPropsFile)
       
       if ($useMonth) 
       {
          $minorVersion = (Get-Date).month
       }
       else 
       {
          $minorVersion = $customProps.Project.PropertyGroup | Where-Object{$_.VersionMinor} | %{$_.VersionMinor}
       }   
       
       $majorVersion = $customProps.Project.PropertyGroup | Where-Object{$_.VersionMajor} | %{$_.VersionMajor}	   	   
       $majorMinorVer = [string]::Format("{0}.{1}", $majorVersion, $minorVersion)	   
    }
    else 
    {
       if ($useMonth) 
       {
          $minorVersion = (Get-Date).month
       }
       else 
       {
          $minorVersion = "0"
       }
       
       $majorMinorVer = ("1." + $minorVersion)
    }
    
    $majorMinorVer
}

function SignFile
{
   param($file)
   $signCommand = $signToolPath + " -i:`"" + $file + "`" -c:" + $signCert + " -a:gstolt;vigarg -s:`"CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US`""
   Write-Host $signCommand
   & cmd /c $signCommand   
   if (!$?) 
    {
       Exit 1
    }
}

function TestSignFile
{
    param($file)
    $signCommandExePath = FindLocationOfExe "signtool.exe"

    $testSignCommand = ("& `"" + $signCommandExePath + " sign /v /fd SHA256 /f `"" + $pfxFile + "`" `"" + $file + "`"") 
    Write-Host $testSignCommand
    Invoke-Expression $testSignCommand
}

# Validate params
if (!$projectDir)
{
    Write-Host -ForegroundColor RED "-projectDir is a required param"
    exit 1
}

if (!$multi -and ($platforms.Count -gt 1))
{
    Write-Host -ForegroundColor RED "Only 1 platform can be specified for non-multi bundles"
    exit 1
}

if ($noBundleVersion)
{
    $appxBundleVersion = $nul
}
else
{
    # Get version
    $majorMinorVersion = Get-MajorMinorVersion $useMonthVersion

    if ($useMonthVersion)
    {   
       $parts = $tfsVersionNumber.Split("{.}")    
       $appxBundleVersion = ($majorMinorVersion + "." + $parts[1] + ".0")
    }
    else
    {	   
       $appxBundleVersion = ($majorMinorVersion + "." + $tfsVersionNumber)
    }

    Write-Host ("Using appxbundle version" + $appxBundleVersion)
}

foreach ($configuration in $configurations)
{
    #\\<drop>\<config>
    $configServerBuildPath = [System.IO.Path]::Combine($appxBundleOutputRoot, $configuration)	
    
    #\\<drop>\<config>\<optional_subdir>\
    if ($appxBundleOutputSubDir)
    {	   
       $configServerBuildPathWithSub = [System.IO.Path]::Combine($configServerBuildPath, $appxBundleOutputSubDir)
    }
    else 
    {
       $configServerBuildPathWithSub = $configServerBuildPath 
    }
    
    #\\<drop>\<config>\<optional_subdir>\<platform>
    if ($multi)
    {
       $configOutputDir = [System.IO.Path]::Combine($configServerBuildPathWithSub, $multiPlatformName)   
    }
    else 
    {
       $configOutputDir = [System.IO.Path]::Combine($configServerBuildPathWithSub, $platforms)   
    }	
    
    #\\<drop>\<config>\<optional_subdir>\<platform>\<proj>
    $projectOutputDir = [System.IO.Path]::Combine($configOutputDir, $projectDir)
    
    #\\<drop>\<config>\<optional_subdir>\<platform>\<proj>\contents
    $contentsOutputDir = [System.IO.Path]::Combine($projectOutputDir, $appxBundleContentsDirName)

    "Contents directory (contentsOutputDir): $contentsOutputDir"

    # Create directory with desired contents of appxbundle
    foreach ($platform in $platforms)
    {    
        $platformBundleServerDir = [System.IO.Path]::Combine($configServerBuildPath, $platform, $projectDir)	
        if ($debug)
        {
           Write-Host "Looking for $platformBundleServerDir"
        }
        if (Test-Path $platformBundleServerDir)
        {		    
            CreateContentsDir $platformBundleServerDir $contentsOutputDir $platform       
        }
    }

    # Create appxbundle
    
    if (!$appxBundleFilename)
    {
        if ((([System.IO.FileInfo]$global:mainAppxPath).Extension).Contains("msix"))
        {
            $appxBundleFilename = ($projectDir + $msixbundleExtension)
        }
        else
        {
            $appxBundleFilename = ($projectDir + $appxbundleExtension)
        }
    }

    Write-Host -ForegroundColor GREEN "##### Running makeappx to create the bundle #####"
    $appxBundleOutput = [System.IO.Path]::Combine($projectOutputDir, $appxBundleFilename)

    $makeAppxInstalledExe = FindLocationOfExe "makeappx.exe" 
    CreateAppxBundle $makeAppxInstalledExe $appxBundleOutput $contentsOutputDir $appxBundleVersion
    if (Test-Path $appxBundleOutput)
    {
        Write-Host -ForegroundColor GREEN "##### Path for configuration:" $configuration "#####"
        Write-Host -ForegroundColor GREEN $appxBundleOutput
    }
    else
    {
        Write-Host -ForegroundColor RED "##### makeappx bundle failed #####"
        exit 1
    }


    # Sign appxbundle
    if ($signBundle)
    {
        if($pfxFile)
        {
            TestSignFile $appxBundleOutput
        }
        elseif ($signCert)
        {
            SignFile $appxBundleOutput
        }		
    }
}
