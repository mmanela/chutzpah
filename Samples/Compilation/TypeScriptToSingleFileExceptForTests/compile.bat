@echo off
call tsc src/StringLib.ts src/MathLib.ts  --sourcemap --declaration --outFile _out/merged.js
call tsc test/StringLibTests.ts test/MathLibTests.ts  --sourcemap --declaration