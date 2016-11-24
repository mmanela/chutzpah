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
goto Build

:Build

powershell -NonInteractive -NoProfile -ExecutionPolicy unrestricted -Command "& {Import-Module %~dp0\packages\psake.4.6.0\Tools\psake.psm1; Invoke-psake default.ps1 -properties @{configuration='%Configuration%'} -parameters @{arg0='%1'; arg1='%2'; arg2='%3'; arg3='%4'} -framework '4.5.1x86' %1"; exit !($psake.build_success);}"

goto end


:end
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%