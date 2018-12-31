@if not defined _echo echo off
setlocal enabledelayedexpansion

REM [works for gitlab and appveyor]
REM https://docs.gitlab.com/ee/ci/variables/
REM https://www.appveyor.com/docs/environment-variables/
if "%CI_COMMIT_SHA%"=="" set CI_COMMIT_SHA=%APPVEYOR_REPO_COMMIT%

REM @@@@@@@@@@@@@@
REM Are we on a CI build? 
set IS_CI_BUILD=false
if not "%CI_COMMIT_SHA%"=="" set IS_CI_BUILD=true

echo.=========================
echo.[%time:~0,8% INFO] BUILD USER VERSION

REM on CI, the version built will be replaced by the tag name instead of taking the version from csproj
REM if PROJECT_PATH is empty, we use the solution
set "PROJECT_PATH=SimpleFileUpdater/SimpleFileUpdater/SimpleFileUpdater.csproj"
set "CUSTOM_BUILD_PARAMS=/p:AdminManifest=false"
REM set below to false if you don't want to change the target framework on build
set "CHANGE_DEFAULT_TARGETFRAMEWORK=true"
set TARGETED_FRAMEWORKS=(net20)
REM if you are packing a lib, CHANGE_DEFAULT_TARGETFRAMEWORK should be false and MSBUILD_DEFAULT_TARGET = Pack
REM otherwise, CHANGE_DEFAULT_TARGETFRAMEWORK should be true with correct TARGETED_FRAMEWORKS and MSBUILD_DEFAULT_TARGET = Publish
set "MSBUILD_DEFAULT_TARGET=Clean,Publish"
if "%CI_COMMIT_SHA%"=="" set "CI_COMMIT_SHA=no_commit_just_for_no_pause"

call build.bat
if not "!ERRORLEVEL!"=="0" (
	GOTO ENDINERROR
)

set TARGETED_FRAMEWORKS=(netcoreapp2.0)

call build.bat
if not "!ERRORLEVEL!"=="0" (
	GOTO ENDINERROR
)

echo.=========================
echo.[%time:~0,8% INFO] BUILD ADMIN VERSION

set "CUSTOM_BUILD_PARAMS=/p:AdminManifest=true"
set TARGETED_FRAMEWORKS=(net20)

call build.bat
if not "!ERRORLEVEL!"=="0" (
	GOTO ENDINERROR
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
REM Ending in error
REM - -------------------------------------
:ENDINERROR

echo.=========================
echo.[%time:~0,8% ERRO] ENDED IN ERROR, ERRORLEVEL = %errorlevel%

if "%IS_CI_BUILD%"=="false" (
	pause
)

exit /b 1
