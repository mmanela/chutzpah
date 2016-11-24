@echo off
%appdata%\npm\tsc.cmd %~dp0src/Controller.ts %~dp0tests/ControllerTests.ts --sourcemap --declaration