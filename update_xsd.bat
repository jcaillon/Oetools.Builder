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
set "CUSTOM_BUILD_PARAMS=/p:WithoutXsdAttribute=true"

call build.bat

echo.=========================
echo.[%time:~0,8% INFO] GENERATING XSD

"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\xsd.exe" -t:OeProject "Oetools.Builder\bin\Any Cpu\Release\net461\Oetools.Builder.dll"
if not "!ERRORLEVEL!"=="0" (
	GOTO ENDINERROR
)

move /y "schema0.xsd" "Oetools.Builder\Resources\Xsd\Project.xsd"
if not "!ERRORLEVEL!"=="0" (
	GOTO ENDINERROR
)

"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\xsd.exe" -t:OeBuildConfiguration "Oetools.Builder\bin\Any Cpu\Release\net461\Oetools.Builder.dll"
if not "!ERRORLEVEL!"=="0" (
	GOTO ENDINERROR
)

move /y "schema0.xsd" "Oetools.Builder\Resources\Xsd\BuildConfiguration.xsd"
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

pause

exit /b 1
