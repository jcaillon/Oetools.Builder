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
echo.[%time:~0,8% INFO] BUILDING SERIALIZATION PROJECT

set "PROJECT_PATH=Oetools.Builder\Oetools.Builder.csproj"
set "CHANGE_DEFAULT_TARGETFRAMEWORK=true"
set TARGETED_FRAMEWORKS=(net461)
set "MSBUILD_DEFAULT_TARGET=Build"
set "CI_COMMIT_SHA=no_commit_just_for_no_pause"
set "CUSTOM_BUILD_PARAMS=/p:WithoutXsdAttribute=true /p:OutputPath=bin\XsdAnnotator"

call build.bat
if not "!ERRORLEVEL!"=="0" (
	GOTO ENDINERROR
)

echo.=========================
echo.[%time:~0,8% INFO] GENERATING XSD

"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\xsd.exe" -t:OeProject "Oetools.Builder\bin\XsdAnnotator\Oetools.Builder.dll"
if not "!ERRORLEVEL!"=="0" (
	GOTO ENDINERROR
)

move /y "schema0.xsd" "Oetools.Builder\Resources\Xsd\Project.xsd"
if not "!ERRORLEVEL!"=="0" (
	GOTO ENDINERROR
)

echo.=========================
echo.[%time:~0,8% INFO] BUILDING XSD ANNOTATION PROJECT

set "PROJECT_PATH=build\XsdAnnotation\XsdAnnotator.csproj"
set "CHANGE_DEFAULT_TARGETFRAMEWORK=false"
set "MSBUILD_DEFAULT_TARGET=Build"
set "CI_COMMIT_SHA=no_commit_just_for_no_pause"
set "CUSTOM_BUILD_PARAMS=/p:OutputPath=."

call build.bat
if not "!ERRORLEVEL!"=="0" (
	GOTO ENDINERROR
)

echo.=========================
echo.[%time:~0,8% INFO] ANNOTATING GENERATED XSD WITH DOCUMENTATION

"build\XsdAnnotation\XsdAnnotator.exe" "Oetools.Builder\Resources\Xsd\Project.xsd" "Oetools.Builder\bin\XsdAnnotator"

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
