@echo off
%appdata%\npm\tsc.cmd base/core.ts ui/screen.ts tests/base/base.qunit.test.ts tests/ui/ui.qunit.test.ts qunit.d.ts require.d.ts --module amd --sourcemap --outdir out