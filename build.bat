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

echo "Here"
:Install
.\Tools\nuget.exe restore Chutzpah.VS.sln
.\Tools\nuget.exe install psake -version 4.9.0 -OutputDirectory Packages
.\Tools\nuget.exe install libuv -version 1.10.0 -OutputDirectory Packages
.\Tools\nuget.exe install StructureMap -version 4.7.1 -OutputDirectory Packages
.\Tools\nuget.exe install structuremap.automocking.moq -version 4.0.0.315 -OutputDirectory Packages
.\Tools\nuget.exe install Brutal.Dev.StrongNameSigner -version 2.9.1 -OutputDirectory Packages

goto Build

:Build

powershell -NonInteractive -NoProfile -ExecutionPolicy unrestricted -Command "& {Import-Module %~dp0packages\psake.4.9.0\tools\psake\psake.psm1; Invoke-psake default.ps1 -properties @{configuration='%Configuration%'} -parameters @{arg0='%1'; arg1='%2'; arg2='%3'; arg3='%4'} -framework '4.8x64' %1"; exit !($psake.build_success);}"

goto end


:end
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%