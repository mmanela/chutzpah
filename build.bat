@echo off

if ERRORLEVEL 1 goto end
set Configuration=Debug

if /i "%1"=="Install" goto Install
if /i "%1"=="Package" goto Release
if /i "%1"=="run-perftester" goto Release
goto Build

:Release
set Configuration=Release
goto Build

:Install
.\Tools\nuget.exe restore Chutzpah.VS.sln

REM Download packages for further processing
set nugetOpts=install -OutputDirectory packages
.\Tools\nuget.exe %nugetOpts% psake -version 4.9.0
.\Tools\nuget.exe %nugetOpts% libuv -version 1.10.0
.\Tools\nuget.exe %nugetOpts% StructureMap -version 4.6.1
.\Tools\nuget.exe %nugetOpts% StructureMap.AutoMocking.Moq.Updated -version 1.0.2
rem .\Tools\nuget.exe %nugetOpts% structuremap.automocking.moq -version 4.0.0.315
.\Tools\nuget.exe %nugetOpts% Brutal.Dev.StrongNameSigner -version 2.9.1
.\Tools\nuget.exe %nugetOpts% xunit.runner.console -version 2.4.1


goto Build

:Build

powershell -NonInteractive -NoProfile -ExecutionPolicy unrestricted -Command "& {Import-Module %~dp0packages\psake.4.9.0\tools\psake\psake.psm1; Invoke-psake psakefile.ps1 -properties @{configuration='%Configuration%'} -parameters @{arg0='%1'; arg1='%2'; arg2='%3'; arg3='%4'} -framework '4.8x64' %1"; exit !($psake.build_success);}"

goto end


:end
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%