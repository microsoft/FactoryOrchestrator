%~dp0
@echo off
start cmd.exe /C "dotnet %~dp0\bin\Debug\AnyCPU\ClientSample\netcoreapp2.2\ClientSample.dll %1 %2 %3 %4 & PAUSE"