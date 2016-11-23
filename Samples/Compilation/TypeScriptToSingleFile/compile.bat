@echo off
tsc src/StringLib.ts src/MathLib.ts test/StringLibTests.ts test/MathLibTests.ts --sourcemap --declaration --outFile _out/merged.js