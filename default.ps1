
properties {
  $baseDir = Resolve-Path .
  $configuration = "debug"
  $filesDir = "$baseDir\_build"
  $nugetDir = "$baseDir\_nuget"
  $chocolateyDir = "$baseDir\_chocolatey"
  $packageDir = "$baseDir\_package"
  
  $autoSignedNugetPackages = "$baseDir/packages_autosigned"
  $nugetPackges = "$baseDir/packages"
}

# Aliases
task Default -depends Build

task Install -depends Sign-ForeignAssemblies

task Package -depends Clean-Solution-VS,Clean-PackageFiles, Set-Version, Update-VersionInFiles, Build-Solution-VS, Package-Files, Package-NuGet, Package-Chocolatey
task Clean -depends Clean-Solution-NoVS
task TeamCity -depends  Clean-TeamCitySolution, Build-TeamCitySolution, Run-UnitTests, Run-IntegrationTests


# Build Tasks
task Build -depends  Clean-Solution-NoVS, Build-Solution-NoVS, Run-UnitTests, Run-IntegrationTests
task Build-Full -depends  Clean-Solution-VS, Build-Solution-VS, Run-UnitTests, Run-IntegrationTests


function getLatestNugetPackagePath($name) {
  return @(Get-ChildItem "$nugetPackges\$name.*" | ? { $_.Name -match "$name.(\d)+" } | Sort-Object -Descending)[0]
}

task Set-Version {
  
  if($arg1) {
    $v = $arg1
    $global:version = $v + ".0"
    
    if($arg2) {
      $global:isBeta = $true
    }
  }
  else {
    $vtag = git describe --abbrev=0 --tags
    $v = $vtag.substring(1)
	  $global:version = $v + '.' + (git log $($vtag + '..') --pretty=oneline | measure-object).Count
  }
  

  $global:versionPart = $v
}

task Update-VersionInFiles {
  if($arg1) {
    $v = $arg1
    $commit = git log -1 --pretty=format:%H
  }
  else {
    $vtag = git describe --abbrev=0 --tags
    $v = $vtag.substring(1)
    $commit = git log -1 $($vtag + '..') --pretty=format:%H
  }
  
    
	Update-AssemblyInfoFiles $global:version $commit
	Update-OtherFiles $global:versionPart
}

task Run-Chutzpah -depends  Build-Solution {
  exec { & .\ConsoleRunner\bin\$configuration\chutzpah.console.exe ConsoleRunner\JS\test.html /file ConsoleRunner\JS\tests.js}
}

task Clean-PackageFiles {
    clean $nugetDir
    clean $filesDir
    clean $packageDir
}

# CodeBetter TeamCity does not have VS SDK installed so we use a custom solution that does not build the 
# VS components
task Clean-TeamCitySolution {
    exec { msbuild TeamCity.CodeBetter.sln /t:Clean /v:quiet }

}
task Clean-Solution-VS {
    exec { msbuild Chutzpah.VS.sln /t:Clean /v:quiet }
}

task Clean-Solution-NoVS {
    exec { msbuild Chutzpah.NoVS.sln /t:Clean /v:quiet }
}

# CodeBetter TeamCity does not have VS SDK installed so we use a custom solution that does not build the 
# VS components
task Build-TeamCitySolution {
    exec { msbuild TeamCity.CodeBetter.sln /maxcpucount /t:Build /v:Minimal /p:Configuration=$configuration }
}

task Build-Solution-VS {
    exec { msbuild Chutzpah.VS.sln /maxcpucount /t:Build /v:Minimal /p:Configuration=$configuration }
}

task Build-Solution-NoVS {
    exec { msbuild Chutzpah.NoVS.sln /maxcpucount /t:Build /v:Minimal /p:Configuration=$configuration }
}

task Run-PerfTester {
    $result = & "PerfTester\bin\$configuration\chutzpah.perftester.exe"
    Write-Output $result
    $result | Out-File "perf_results.txt" -Encoding ASCII
}

task Run-UnitTests {
    $xUnit = Join-Path (getLatestNugetPackagePath("xunit.runner.console")) "tools/xunit.console.exe"
    exec { & $xUnit "Facts\bin\$configuration\Facts.Chutzpah.dll" }
}

task Run-IntegrationTests {
    $xUnit = Join-Path (getLatestNugetPackagePath("xunit.runner.console")) "tools/xunit.console.exe"
    exec { & $xUnit "Facts.Integration\bin\$configuration\Facts.Integration.Chutzpah.dll" }
}

task Run-Phantom {
  $testFilePath = Resolve-Path $arg1;
  $type = $arg2;
  $mode = $arg3;
  if(-not $type){
    $type = "qunit";
  }
  if($type -eq "jasmine") {
    $suffix = "V2"
  }
  
  $phantom = "3rdParty\Phantom\phantomjs.exe";
  $testFilePath = $testFilePath.Path.Replace("\","/");
  
  exec {  & $phantom "Chutzpah\JSRunners\$($type)Runner$suffix.js" "file:///$testFilePath" $mode }
}

task Sign-ForeignAssemblies {

  $packagesToSign = @{"ServiceStack.Text" = "lib/net45"}
  
  clean $autoSignedNugetPackages
 
  if( -not (Test-Path $autoSignedNugetPackages)) {
    create $autoSignedNugetPackages
  }
  
  $signerToolFolder = (getLatestNugetPackagePath "Brutal.Dev.StrongNameSigner").FullName
  $signerExe = Join-Path $signerToolFolder Tools/StrongNameSigner.Console.exe
  
  Write-Host "Signing assemblies"
  $folderPaths = ""
  
  foreach($name in $packagesToSign.Keys) {
    $targetFolder = $packagesToSign[$name]
    $folderToSign = getLatestNugetPackagePath $name
    $fullPath = Join-Path $folderToSign.FullName $targetFolder
    $folderName = $folderToSign.Name
    
    $folderPaths += $fullpath + "|"
  } 
  
  $folderPaths = $folderPaths.TrimEnd("|");
  
  Write-Host "Signing dll's in $autoSignedNugetPackages"
  exec { & $signerExe -in $folderPaths -out $autoSignedNugetPackages -k "$baseDir/chutzpah.snk" }
}

task Package-Files -depends Clean-PackageFiles {
    
    create $filesDir, $packageDir
    copy-item "$baseDir\License.txt" -destination $filesDir
    copy-item "$baseDir\3rdParty\ServiceStack\LICENSE.BSD" -destination $filesDir\ServiceStack.LICENSE.BSD
    roboexec {robocopy "$baseDir\ConsoleRunner\bin\$configuration\" $filesDir /S /xd JS /xf *.xml}
    
    
    # Copy Adapter files to package zip  
    copy-item "$baseDir\VS2012\bin\$configuration\Chutzpah.VS.Common.*" -destination $filesDir
    copy-item "$baseDir\VS2012\bin\$configuration\Chutzpah.VS2012.TestAdapter.*" -destination $filesDir
 
    
    cd $filesDir
    exec { &"$baseDir\3rdParty\Zip\zip.exe" -r -9 "$packageDir\Chutzpah.$($global:versionPart).zip" *.* }
    cd $baseDir
    
    # Copy over Vsix Files
    copy-item "$baseDir\VisualStudioContextMenu\bin\$configuration\Chutzpah.VisualStudioContextMenu.vsix" -destination $packageDir
    copy-item "$baseDir\VS2012\bin\$configuration\Chutzpah.VS2012.vsix" -destination $packageDir
}

task Package-NuGet -depends Clean-PackageFiles, Set-Version {
    $nugetTools = "$nugetDir\tools"
    $nuspec = "$baseDir\Chutzpah.nuspec"
    
    create $nugetDir, $nugetTools, $packageDir
    
    copy-item "$baseDir\License.txt", $nuspec -destination $nugetDir
    copy-item "$baseDir\3rdParty\ServiceStack\LICENSE.BSD" -destination $nugetDir\ServiceStack.LICENSE.BSD
    roboexec {robocopy "$baseDir\ConsoleRunner\bin\$configuration\" $nugetTools /S /xd JS /xf *.xml}
    
    
    # Copy Adapter files to nuget zip  
    copy-item "$baseDir\VS2012\bin\$configuration\Chutzpah.VS.Common.*" -destination $nugetTools
    copy-item "$baseDir\VS2012\bin\$configuration\Chutzpah.VS2012.TestAdapter.*" -destination $nugetTools
    
    
    $v = new-object -TypeName System.Version -ArgumentList $global:version
    
    
    $vStr = $v.ToString(3)
    if($global:isBeta) {
      $vStr += "-beta"
    }
    
    regex-replace "$nugetDir\Chutzpah.nuspec" '(?m)@Version@' $vStr
    exec { .\Tools\nuget.exe pack "$nugetDir\Chutzpah.nuspec" -o $packageDir }
}

task Push-Public -depends Push-Nuget, Push-Chocolatey

task Package-Chocolatey -depends Clean-PackageFiles, Set-Version {
    $nuspec = "$baseDir\Chutzpah.nuspec"
    $chocolateyInstall = "$baseDir\chocolateyInstall.ps1"
    
    create $chocolateyDir, $packageDir
    copy-item $chocolateyInstall, $nuspec -destination $chocolateyDir
 
    $v = new-object -TypeName System.Version -ArgumentList $global:version
    regex-replace "$chocolateyDir\chocolateyInstall.ps1" '(?m)@Version@' $v.ToString(3)
    
    push-location $chocolateyDir
    $v = new-object -TypeName System.Version -ArgumentList $global:version
    regex-replace "$chocolateyDir\Chutzpah.nuspec" '(?m)@Version@' $v.ToString(3)
    exec { cpack "$chocolateyDir\Chutzpah.nuspec" }
    pop-location
}

task Push-Nuget -depends Set-Version {
  $v = new-object -TypeName System.Version -ArgumentList $global:version
	exec { .\Tools\nuget.exe push $packageDir\Chutzpah.$($v.ToString(3)).nupkg }
}


task Push-Chocolatey -depends Set-Version {
  $v = new-object -TypeName System.Version -ArgumentList $global:version
	exec { chocolatey push $chocolateyDir\Chutzpah.$($v.ToString(3)).nupkg }
}

# Help 
task ? -Description "Help information" {
	Write-Documentation
}

function create([string[]]$paths) {
  foreach ($path in $paths) {
    if(-not (Test-Path $path)) {
      new-item -path $path -type directory | out-null
    }
  }
}

function regex-replace($filePath, $find, $replacement) {
    $regex = [regex] $find
    $content = [System.IO.File]::ReadAllText($filePath)
    
    Assert $regex.IsMatch($content) "Unable to find the regex '$find' to update the file '$filePath'"
    
    [System.IO.File]::WriteAllText($filePath, $regex.Replace($content, $replacement))
}

function clean([string[]]$paths) {
	foreach ($path in $paths) {
		remove-item -force -recurse $path -ErrorAction SilentlyContinue
	}
}

function roboexec([scriptblock]$cmd) {
    & $cmd | out-null
    if ($lastexitcode -eq 0) { throw "No files were copied for command: " + $cmd }
}

# Borrowed from Luis Rocha's Blog (http://www.luisrocha.net/2009/11/setting-assembly-version-with-windows.html)
function Update-AssemblyInfoFiles ([string] $version, [string] $commit) {
    $assemblyVersionPattern = 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
    $fileVersionPattern = 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)'
    $fileCommitPattern = 'AssemblyTrademark\("[a-f0-9]*"\)'
    $assemblyVersion = 'AssemblyVersion("' + $version + '")';
    $fileVersion = 'AssemblyFileVersion("' + $version + '")';
    $commitVersion = 'AssemblyTrademark("' + $commit + '")';

    echo "Setting version: $version and commit:$commit"
    Get-ChildItem -path $baseDir -r -filter AssemblyInfo.cs | ForEach-Object {
        $filename = $_.Directory.ToString() + '\' + $_.Name
       
        (Get-Content $filename) | ForEach-Object {
            % {$_ -replace $assemblyVersionPattern, $assemblyVersion } |
            % {$_ -replace $fileVersionPattern, $fileVersion } |
            % {$_ -replace $fileCommitPattern, $commitVersion }
        } | Set-Content $filename
    }
}


function Update-OtherFiles ([string] $version) {

    $chutzpahVersionPattern = 'public const string ChutzpahVersion = "(([0-9]\.?)*?)"'
    $chutzpahVersion = 'public const string ChutzpahVersion = "'+$version+'"'
    
    $contantsFile = Join-Path $baseDir "Chutzpah\Constants.cs"
    (Get-Content $contantsFile) | ForEach-Object { $_ -replace $chutzpahVersionPattern, $chutzpahVersion } | Set-Content $contantsFile
    
     $manifestFile = Join-Path $baseDir "VS2012\source.extension.vsixmanifest"
     $manifest = [xml](Get-Content $manifestFile)
     $manifest.PackageManifest.Metadata.Identity.Version = $version
     $manifest.Save($manifestFile)
     
     $manifestFile = Join-Path $baseDir "VisualStudioContextMenu\source.extension.vsixmanifest"
     $manifest = [xml](Get-Content $manifestFile)
     $manifest.PackageManifest.Metadata.Identity.Version = $version
     $manifest.Save($manifestFile)
}