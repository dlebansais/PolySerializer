@echo off

setlocal

set PROJECTNAME=PolySerializer
set TESTPROJECTNAME=Test-%PROJECTNAME%
set RESULTFILENAME=Coverage-%PROJECTNAME%.xml
set OPENCOVER_VERSION=4.7.1189
set OPENCOVER=OpenCover.%OPENCOVER_VERSION%
set CODECOV_VERSION=1.13.0
set CODECOV=Codecov.%CODECOV_VERSION%
set NUINT_CONSOLE_VERSION=3.12.0
set NUINT_CONSOLE=NUnit.ConsoleRunner.%NUINT_CONSOLE_VERSION%
set FRAMEWORK=net48

nuget install OpenCover -Version %OPENCOVER_VERSION% -OutputDirectory packages
nuget install CodeCov -Version %CODECOV_VERSION% -OutputDirectory packages
nuget install NUnit.ConsoleRunner -Version %NUINT_CONSOLE_VERSION% -OutputDirectory packages

if not exist ".\packages\%OPENCOVER%\tools\OpenCover.Console.exe" goto error_console1
if not exist ".\packages\%CODECOV%\tools\codecov.exe" goto error_console2
if not exist ".\packages\%NUINT_CONSOLE%\tools\nunit3-console.exe" goto error_console3

call ..\Certification\set_tokens.bat

dotnet publish ./Test/%TESTPROJECTNAME% -c Debug -f %FRAMEWORK% /p:Platform=x64 -o ./Test/%TESTPROJECTNAME%/publish/x64/Debug
dotnet publish ./Test/%TESTPROJECTNAME% -c Release -f %FRAMEWORK% /p:Platform=x64 -o ./Test/%TESTPROJECTNAME%/publish/x64/Release

if not exist ".\Test\%TESTPROJECTNAME%\publish\x64\Debug\%TESTPROJECTNAME%.dll" goto error_not_built
if not exist ".\Test\%TESTPROJECTNAME%\publish\x64\Release\%TESTPROJECTNAME%.dll" goto error_not_built
if exist .\Test\%TESTPROJECTNAME%\*.log del .\Test\%TESTPROJECTNAME%\*.log
if exist .\Test\%TESTPROJECTNAME%\obj\x64\Debug\%RESULTFILENAME% del .\Test\%TESTPROJECTNAME%\obj\x64\Debug\%RESULTFILENAME%
if exist .\Test\%TESTPROJECTNAME%\obj\x64\Release\%RESULTFILENAME% del .\Test\%TESTPROJECTNAME%\obj\x64\Release\%RESULTFILENAME%
".\packages\%OPENCOVER%\tools\OpenCover.Console.exe" -register:user -target:".\packages\%NUINT_CONSOLE%\tools\nunit3-console.exe" -targetargs:".\Test\%TESTPROJECTNAME%\publish\x64\Debug\%TESTPROJECTNAME%.dll --trace=Debug --labels=Before" -filter:"+[%PROJECTNAME%*]* -[%TESTPROJECTNAME%*]*" -output:".\Test\%TESTPROJECTNAME%\obj\x64\Debug\%RESULTFILENAME%"
".\packages\%OPENCOVER%\tools\OpenCover.Console.exe" -register:user -target:".\packages\%NUINT_CONSOLE%\tools\nunit3-console.exe" -targetargs:".\Test\%TESTPROJECTNAME%\publish\x64\Release\%TESTPROJECTNAME%.dll --trace=Debug --labels=Before" -filter:"+[%PROJECTNAME%*]* -[%TESTPROJECTNAME%*]*" -output:".\Test\%TESTPROJECTNAME%\obj\x64\Release\%RESULTFILENAME%"
if exist .\Test\%TESTPROJECTNAME%\obj\x64\Debug\%RESULTFILENAME% .\packages\%CODECOV%\tools\win7-x86\codecov -f ".\Test\%TESTPROJECTNAME%\obj\x64\Debug\%RESULTFILENAME%" -t %POLYSERIALIZER_CODECOV_TOKEN%
if exist .\Test\%TESTPROJECTNAME%\obj\x64\Release\%RESULTFILENAME% .\packages\%CODECOV%\tools\win7-x86\codecov -f ".\Test\%TESTPROJECTNAME%\obj\x64\Release\%RESULTFILENAME%" -t %POLYSERIALIZER_CODECOV_TOKEN%
goto end

:error_console1
echo ERROR: OpenCover.Console not found.
goto end

:error_console2
echo ERROR: Codecov not found.
goto end

:error_console3
echo ERROR: nunit3-console not found.
goto end

:error_not_built
echo ERROR: %TESTPROJECTNAME%.dll not built (both Debug and Release are required).
goto end

:end
del *.log