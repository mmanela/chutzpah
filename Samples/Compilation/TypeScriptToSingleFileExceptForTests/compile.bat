@echo off
call tsc.cmd src/StringLib.ts src/MathLib.ts  --sourcemap --declaration --outFile _out/merged.js
call tsc.cmd test/StringLibTests.ts test/MathLibTests.ts  --sourcemap --declaration