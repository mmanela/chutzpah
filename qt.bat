rem Quickly execute Chutzpah on an integration test file

@echo off
echo Runing chutzpah on a integration test file named .\Facts.Integration\js\Test\%1

IF ""=="%1" goto end

.\ConsoleRunner\bin\Debug\chutzpah.console.exe .\Facts.Integration\js\Test\%1 /debug
goto end


:end