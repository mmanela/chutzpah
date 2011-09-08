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
task Build-Package -depends Update-AssemblyInfoFiles, Run-Build
task Build -depends Run-Build
task Clean -depends Clean-Solution

# Build Tasks
task Run-Build -depends  Clean-Solution, Build-Solution, Run-UnitTests, Run-IntegrationTests

task Update-AssemblyInfoFiles {
	$commit = hg log --template '{rev}:{node}\n' -l 1
	Update-AssemblyInfoFiles $version $commit
}

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

# Borrowed from Luis Rocha's Blog (http://www.luisrocha.net/2009/11/setting-assembly-version-with-windows.html)
function Update-AssemblyInfoFiles ([string] $version, [string] $commit) {
    $assemblyVersionPattern = 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
    $fileVersionPattern = 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
    $fileCommitPattern = 'AssemblyTrademarkAttribute\("[a-f0-9]{40}"\)'
    $assemblyVersion = 'AssemblyVersion("' + $version + '")';
    $fileVersion = 'AssemblyFileVersion("' + $version + '")';
    $commitVersion = 'AssemblyTrademarkAttribute("' + $commit + '")';

    Get-ChildItem -path $baseDir -r -filter AssemblyInfo.cs | ForEach-Object {
        $filename = $_.Directory.ToString() + '\' + $_.Name
        $filename + ' -> ' + $version
        
        # If you are using a source control that requires to check-out files before 
        # modifying them, make sure to check-out the file here.
        # For example, TFS will require the following command:
        # tf checkout $filename
    
        (Get-Content $filename) | ForEach-Object {
            % {$_ -replace $assemblyVersionPattern, $assemblyVersion } |
            % {$_ -replace $fileVersionPattern, $fileVersion } |
			% {$_ -replace $fileCommitPattern, $commitVersion }
        } | Set-Content $filename
    }
}