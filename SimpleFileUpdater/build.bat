@echo off
REM Builds the 2 .exe

REM [works for gitlab and appveyor]
REM https://docs.gitlab.com/ee/ci/variables/
REM https://www.appveyor.com/docs/environment-variables/
rem if "%CI_BUILD_ID%"=="" set CI_BUILD_ID=%APPVEYOR_BUILD_ID%
if "%CI_COMMIT_SHA%"=="" set CI_COMMIT_SHA=%APPVEYOR_REPO_COMMIT%

REM @@@@@@@@@@@@@@
REM Are we on a CI build? 
set IS_CI_BUILD=false
if not "%CI_COMMIT_SHA%"=="" set IS_CI_BUILD=true

call :MS_BUILD SimpleFileUpdater\SimpleFileUpdater.csproj /p:Configuration=Release /p:Platform=AnyCPU /t:Rebuild /verbosity:minimal /p:AdminManifest=true /bl:SimpleFileUpdater.binlog %BUILD_PARAMS% %CUSTOM_BUILD_PARAMS%
if not "%ERRORLEVEL%"=="0" (
	exit /b 1
)

call :MS_BUILD SimpleFileUpdater\SimpleFileUpdater.csproj /p:Configuration=Release /p:Platform=AnyCPU /t:Rebuild /verbosity:minimal /p:AdminManifest=false /bl:SimpleFileUpdater.binlog %BUILD_PARAMS% %CUSTOM_BUILD_PARAMS%
if not "%ERRORLEVEL%"=="0" (
	exit /b 1
)

:DONE
echo.=========================
echo.[%time:~0,8% INFO] BUILD DONE

if "%IS_CI_BUILD%"=="false" (
	pause
)

REM @@@@@@@@@@@@@@
REM End of script
exit /b 0


REM =================================================================================
REM 								SUBROUTINES - LABELS
REM =================================================================================

REM - -------------------------------------
REM MS_BUILD
REM - -------------------------------------
:MS_BUILD

@REM Determine if MSBuild is already in the PATH
for /f "usebackq delims=" %%I in (`where msbuild.exe 2^>nul`) do (
    "%%I" %*
    exit /b !ERRORLEVEL!
)

@REM Find the latest MSBuild that supports our projects
pushd "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer"
for /f "usebackq delims=" %%I in (`vswhere.exe -version "[15.0,)" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -property InstallationPath`) do (
	set "MSBUILD_INSTALLPATH=%%I\MSBuild"
)
popd

for /f "usebackq delims=" %%J in (`where /r "%MSBUILD_INSTALLPATH%" msbuild.exe 2^>nul ^| sort /r`) do (
    "%%J" %*
    exit /b !ERRORLEVEL!
)

echo.=========================
echo.[%time:~0,8% ERRO] Could not find msbuild.exe 1>&2
exit /b 2