TypeScript Compilation Settings Sample

This sample shows how to use the Chutzpah compile setting to run tests which compile TypeScript using Powershell.
It makes use of some variables Chutzpah sets for you:
   %powershellexe% - The path to the PowerShell exe
   %chutzpahsettingsdir% - The path to the chutzpah settings directory
   
This assumes you have the tsc.cmd command on your system path.