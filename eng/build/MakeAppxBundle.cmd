echo.
echo Creating multi-architecture(x86, x64, arm) Appx bundles ...
call %~dp0\makeappx.cmd -appxBundleFilename "Microsoft.FTFUWP_8wekyb3d8bbwe.msixbundle" -projectDir "FTFUWP" -multi $True -signBundle $False -platforms "x86","x64","arm" -configurations "Debug" -appxBundleOutputSubDir "msixbundle"
REM Comment out me for fast iteration
call %~dp0\makeappx.cmd -appxBundleFilename "Microsoft.FTFUWP_8wekyb3d8bbwe.msixbundle" -projectDir "FTFUWP" -multi $True -signBundle $True -signCert 136020001 -platforms "x86","x64","arm" -configurations "Release" -appxBundleOutputSubDir "msixbundle"
REM Uncomment me for fast iteration
REM call %TFS_ToolsDirectory%\bin\MakeAppxBundle.cmd -appxBundleFilename "Microsoft.FTFUWP_8wekyb3d8bbwe.appxbundle" -projectDir "FTFUWP" -multi $True -signBundle $False -platforms "x86","x64","arm" -configurations "Release"
