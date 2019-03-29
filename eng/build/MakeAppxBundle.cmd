echo.
echo Creating multi-architecture(x86, x64, arm) Appx bundles ...
call %TFS_ToolsDirectory%\bin\MakeAppxBundle.cmd -appxBundleFilename "Microsoft.FTFUWP_8wekyb3d8bbwe.appxbundle" -projectDir "FTFUWP" -multi $True -signBundle $False -platforms "x86","x64","arm" -configurations "Debug"
REM Comment out me for fast iteration
call %TFS_ToolsDirectory%\bin\MakeAppxBundle.cmd -appxBundleFilename "Microsoft.FTFUWP_8wekyb3d8bbwe.appxbundle" -projectDir "FTFUWP" -multi $True -signBundle $True -signCert 136020001 -platforms "x86","x64","arm" -configurations "Release"
REM Uncomment me for fast iteration
REM call %TFS_ToolsDirectory%\bin\MakeAppxBundle.cmd -appxBundleFilename "Microsoft.FTFUWP_8wekyb3d8bbwe.appxbundle" -projectDir "FTFUWP" -multi $True -signBundle $False -platforms "x86","x64","arm" -configurations "Release"
