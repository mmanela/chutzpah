properties {
  $baseDir = Resolve-Path .
  $configuration = "debug"
  $xUnit = Resolve-Path .\3rdParty\XUnit\xunit.console.clr4.exe
    
  # Import environment variables for Visual Studio
  if (test-path ("vsvars2010.ps1")) { 
    . vsvars2010.ps1 
    }
  
}

# Aliases
task Default -depends Run-Build
task Build -depends Run-Build
task Clean -depends Clean-Solution

# Build Tasks
task Run-Build -depends  Clean-Solution, Build-Solution, Run-UnitTests, Run-IntegrationTests


task Run-Chutzpah -depends  Build-Solution {
  exec { & .\ConsoleRunner\bin\$configuration\chutzpah.console.exe ConsoleRunner\JS\test.html /file ConsoleRunner\JS\tests.js}
}

task Clean-Solution {
    exec { msbuild Chutzpah.sln /t:Clean /v:quiet }
}

task Build-Solution {
  echo $profile
    exec { msbuild Chutzpah.sln /maxcpucount /t:Build /v:Minimal /p:Configuration=$configuration }
}

task Run-UnitTests {
    exec { & $xUnit "Facts\bin\$configuration\Facts.Chutzpah.dll" }
}

task Run-IntegrationTests {
    exec { & $xUnit "Facts.Integration\bin\$configuration\Facts.Integration.Chutzpah.dll" }
}


# Help 
task ? -Description "Help information" {
	Write-Documentation
}

function roboexec([scriptblock]$cmd) {
    & $cmd | out-null
    if ($lastexitcode -eq 0) { throw "No files were copied for command: " + $cmd }
}
