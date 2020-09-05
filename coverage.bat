@echo off

setlocal

set PROJECTNAME=Test-PolySerializer
set RESULTFILENAME=Coverage-PolySerializer.xml
set OPENCOVER=OpenCover.4.7.922
set CODECOV=Codecov.1.12.2
set NUINT_CONSOLE=NUnit.ConsoleRunner.3.11.1
set FRAMEWORK=net48

if not exist ".\packages\%OPENCOVER%\tools\OpenCover.Console.exe" goto error_console1
if not exist ".\packages\%CODECOV%\tools\codecov.exe" goto error_console2
if not exist ".\packages\%NUINT_CONSOLE%\tools\nunit3-console.exe" goto error_console3

call ..\Certification\set_tokens.bat

dotnet publish %PROJECTNAME% -c Debug -f %FRAMEWORK% /p:Platform=x64 -o ./%PROJECTNAME%/publish/x64/Debug
dotnet publish %PROJECTNAME% -c Release -f %FRAMEWORK% /p:Platform=x64 -o ./%PROJECTNAME%/publish/x64/Release

if not exist ".\%PROJECTNAME%\publish\x64\Debug\%PROJECTNAME%.dll" goto error_not_built
if not exist ".\%PROJECTNAME%\publish\x64\Release\%PROJECTNAME%.dll" goto error_not_built
if exist .\%PROJECTNAME%\*.log del .\%PROJECTNAME%\*.log
if exist .\%PROJECTNAME%\obj\x64\Debug\%RESULTFILENAME% del .\%PROJECTNAME%\obj\x64\Debug\%RESULTFILENAME%
if exist .\%PROJECTNAME%\obj\x64\Release\%RESULTFILENAME% del .\%PROJECTNAME%\obj\x64\Release\%RESULTFILENAME%
".\packages\%OPENCOVER%\tools\OpenCover.Console.exe" -register:user -target:".\packages\%NUINT_CONSOLE%\tools\nunit3-console.exe" -targetargs:".\%PROJECTNAME%\publish\x64\Debug\%PROJECTNAME%.dll --trace=Debug --labels=Before" -filter:"+[PolySerializer*]* -[%PROJECTNAME%*]*" -output:".\%PROJECTNAME%\obj\x64\Debug\%RESULTFILENAME%"
".\packages\%OPENCOVER%\tools\OpenCover.Console.exe" -register:user -target:".\packages\%NUINT_CONSOLE%\tools\nunit3-console.exe" -targetargs:".\%PROJECTNAME%\publish\x64\Release\%PROJECTNAME%.dll --trace=Debug --labels=Before" -filter:"+[PolySerializer*]* -[%PROJECTNAME%*]*" -output:".\%PROJECTNAME%\obj\x64\Release\%RESULTFILENAME%"
if exist .\%PROJECTNAME%\obj\x64\Debug\%RESULTFILENAME% .\packages\%CODECOV%\tools\codecov -f ".\%PROJECTNAME%\obj\x64\Debug\%RESULTFILENAME%" -t %POLYSERIALIZER_CODECOV_TOKEN%
if exist .\%PROJECTNAME%\obj\x64\Release\%RESULTFILENAME% .\packages\%CODECOV%\tools\codecov -f ".\%PROJECTNAME%\obj\x64\Release\%RESULTFILENAME%" -t %POLYSERIALIZER_CODECOV_TOKEN%
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
echo ERROR: %PROJECTNAME%.dll not built (both Debug and Release are required).
goto end

:end
del *.log