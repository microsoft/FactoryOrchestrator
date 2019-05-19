@echo off
if defined IsVSOBuild (
  echo vNext build
  if defined XES_SERIALPOSTBUILDREADY (    
    call Powershell -executionpolicy bypass %~dp0\makeappx.ps1 %*
  ) else (
    echo Not last build...
  )
) else (
  echo XAML build
  Call %TFS_ToolsDirectory%\bin\xenv.cmd %TFS_SourcesDirectory%
  Powershell -executionpolicy bypass %~dp0\makeappx.ps1 %*  
)

