@echo off
start cmd.exe /C "dotnet %~dp0\bin\Debug\AnyCPU\ClientSample\netcoreapp3.1\Microsoft.FactoryOrchestrator.ClientSample.dll %1 %2 %3 %4 & PAUSE"