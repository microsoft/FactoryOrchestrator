echo.
echo Creating multi-architecture(x86, x64, arm) Appx bundles ...
call %TFS_ToolsDirectory%\bin\MakeAppxBundle.cmd -appxBundleFilename "Microsoft.FactoryOrchestratorApp_8wekyb3d8bbwe.msixbundle" -projectDir "FactoryOrchestratorApp" -multi $True -signBundle $False -platforms "x86","x64","arm" -configurations "Debug" -appxBundleOutputSubDir "msixbundle"
REM Comment out me for fast iteration
call %TFS_ToolsDirectory%\bin\MakeAppxBundle.cmd -appxBundleFilename "Microsoft.FactoryOrchestratorApp_8wekyb3d8bbwe.msixbundle" -projectDir "FactoryOrchestratorApp" -multi $True -signBundle $True -signCert 136020001 -platforms "x86","x64","arm" -configurations "Release" -appxBundleOutputSubDir "msixbundle"
REM Uncomment me for fast iteration
REM call %TFS_ToolsDirectory%\bin\MakeAppxBundle.cmd -appxBundleFilename "Microsoft.FactoryOrchestratorApp_8wekyb3d8bbwe.appxbundle" -projectDir "FactoryOrchestratorApp" -multi $True -signBundle $False -platforms "x86","x64","arm" -configurations "Release"
