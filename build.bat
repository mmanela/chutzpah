@echo off

if ERRORLEVEL 1 goto end
set Configuration=Debug

if /i "%1"=="Package" goto Package
goto Build

:Package
set Configuration=Release
goto Build



:Build

powershell -NonInteractive -NoProfile -ExecutionPolicy unrestricted -Command "& {Import-Module %~dp0\packages\psake.4.0.1.0\Tools\psake.psm1; Invoke-psake -properties @{configuration='%Configuration%'} -parameters @{arg0='%1'; arg1='%2'; arg2='%3'} -framework '4.0' %*"}"

goto end


:end
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%