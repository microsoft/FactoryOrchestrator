REM Copyright (c) Microsoft Corporation.
REM Licensed under the MIT license.

@echo off
start cmd.exe /C "dotnet %~dp0bin\Debug\AnyCPU\Microsoft.FactoryOrchestrator.ClientSample\netcoreapp3.1\Microsoft.FactoryOrchestrator.ClientSample.dll %1 %2 %3 %4 & PAUSE"
