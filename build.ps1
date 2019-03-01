param ( 
	[string]
	$ProjectOrSolutionPath = "GithubUpdater\GithubUpdater.csproj",
	[bool]
	$TestOnly = $False,
	[bool]
	$BuildSimpleUpdaterOnly = $False,
	[bool]
	$ShouldExit = $False
)

function Main {
    # inspired by ci script from https://github.com/datalust/piggy.
	# inspired by ci script from https://github.com/Azure/azure-functions-core-tools.
    $path = $ProjectOrSolutionPath	
	[string] $ciTag = If ([string]::IsNullOrEmpty($env:CI_COMMIT_TAG)) {$env:CI_COMMIT_TAG} Else {$env:APPVEYOR_REPO_TAG_NAME}
    $isReleaseBuild = [string]::IsNullOrEmpty($ciTag)
    [string] $versionToBuild = $NULL
    if ($isReleaseBuild) {
        $versionToBuild = If ($ciTag.StartsWith('v')) {$ciTag.SubString(1)} Else {$env:ciTag}
    }
	Write-Host "Building $(If ([string]::IsNullOrEmpty($versionToBuild)) { "default version" } Else { " version $versionToBuild" } )"

	Push-Location $PSScriptRoot
	if ($TestOnly) {
		Start-Tests
	} elseif ($BuildSimpleUpdaterOnly) {
		Publish-SimpleUpdate -Version "$versionToBuild"
	} else {
		Publish-SimpleUpdate -Version "$versionToBuild"
		Start-Tests
		New-ArtifactDir
		Publish-NugetPackage -Path "$path" -Version "$versionToBuild"
	}
    Pop-Location
}

function New-ArtifactDir {
    if (Test-Path "./artifacts") { 
        Remove-Item -Path "./artifacts" -Force -Recurse 
    }
    New-Item -Path "." -Name "artifacts" -ItemType "directory" | Out-Null
}

function Start-Tests {
	if (-Not (Test-Exe("dotnet"))) {
        Throw "The executable dotnet was not found in your path."
	}
	
	foreach ($file in Get-ChildItem -Path . -Recurse | Where-Object {$_.Name -like "*.Test.csproj"}) {
		Write-Host "@@@@@@@@@@@@@@@@@@@@@@@"
		Write-Host "Testing assembly $($file.Name)"
		Write-Host "@@@@@@@@@@@@@@@@@@@@@@@"
		
		iu dotnet test "$($file.FullName)" --verbosity "minimal"
	}
}

function Publish-SimpleUpdate {
	param (
		$Version
	)
    if (-Not (Test-Exe("msbuild"))) {
        Throw "The executable msbuild was not found in your path."
	}

	$simpleUpdaterCsproj = "SimpleFileUpdater/SimpleFileUpdater/SimpleFileUpdater.csproj"

	Write-Host "@@@@@@@@@@@@@@@@@@@@@@@"
	Write-Host "Building SimpleFileUpdater"
	Write-Host "@@@@@@@@@@@@@@@@@@@@@@@"

	iu msbuild "$simpleUpdaterCsproj" "/verbosity:minimal" "/t:Restore,Publish" "/p:Configuration=Release" "/bl:SimpleFileUpdater/net20.binlog" "/p:AdminManifest=false" "/p:TargetFramework=net20" $(If ([string]::IsNullOrEmpty($Version)) { "" } Else { "/p:Version=$Version" })

	iu msbuild "$simpleUpdaterCsproj" "/verbosity:minimal" "/t:Restore,Publish" "/p:Configuration=Release" "/bl:SimpleFileUpdater/net20.admin.binlog" "/p:AdminManifest=true" "/p:TargetFramework=net20" $(If ([string]::IsNullOrEmpty($Version)) { "" } Else { "/p:Version=$Version" })

	iu msbuild "$simpleUpdaterCsproj" "/verbosity:minimal" "/t:Restore,Publish" "/p:Configuration=Release" "/bl:SimpleFileUpdater/netcoreapp2.0.binlog" "/p:AdminManifest=false" "/p:TargetFramework=netcoreapp2.0" $(If ([string]::IsNullOrEmpty($Version)) { "" } Else { "/p:Version=$Version" })
}

function Publish-NugetPackage {
	param (
		$Path, 
		$Version
	)
    if (-Not (Test-Exe("msbuild"))) {
        Throw "The executable msbuild was not found in your path."
	}

	$publishDir = Resolve-Path -Path "./artifacts"
	
	Write-Host "@@@@@@@@@@@@@@@@@@@@@@@"
	Write-Host "Publishing nuget package"
	Write-Host "@@@@@@@@@@@@@@@@@@@@@@@"

	iu msbuild "$path" "/verbosity:minimal" "/t:Restore,Pack" "/p:Configuration=Release" "/bl:pack.binlog" "/p:PackageOutputPath=$publishDir" $(If ([string]::IsNullOrEmpty($Version)) { "" } Else { "/p:Version=$Version" })
}

function Test-Exe($exeName) {
    return [bool](Get-Command $exeName -ErrorAction SilentlyContinue);
}

function Invoke-Utility {
    <#
.SYNOPSIS
Invokes an external utility, ensuring successful execution.

.DESCRIPTION
Invokes an external utility (program) and, if the utility indicates failure by 
way of a nonzero exit code, throws a script-terminating error.

* Pass the command the way you would execute the command directly.
* Do NOT use & as the first argument if the executable name is not a literal.

.EXAMPLE
Invoke-Utility git push

Executes `git push` and throws a script-terminating error if the exit code
is nonzero.
#>
    $exe, $argsForExe = $Args
    $ErrorActionPreference = 'Stop' # in case $exe isn't found
    & $exe $argsForExe
    if ($LASTEXITCODE) { 
        Throw "$exe indicated failure (exit code $LASTEXITCODE; full command: $Args)." 
    }
}

[int] $exitcode = 0

try {
	Set-Alias iu Invoke-Utility
    Main
} catch {
	if (-Not $ShouldExit) {
		throw $_.Exception
	}
    $exceptionCatched = $_.Exception.ToString()
	Write-Host "Exception : $exceptionCatched"
	$exitcode = 1	
}

if ($ShouldExit) {
	Write-Host "Exit code $exitcode"
	$host.SetShouldExit($exitcode)
	exit
}