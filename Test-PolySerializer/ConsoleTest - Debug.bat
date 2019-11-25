@echo off
del *.log > nul

if not exist "..\packages\NUnit.ConsoleRunner.3.10.0\tools\nunit3-console.exe" goto error_console
if not exist "..\Test-PolySerializer\bin\x64\Debug\Test-PolySerializer.dll" goto error_PolySerializer
"..\packages\NUnit.ConsoleRunner.3.10.0\tools\nunit3-console.exe" --trace=Debug --labels=All "..\Test-PolySerializer\bin\x64\Debug\Test-PolySerializer.dll"
goto end

:error_console
echo ERROR: nunit3-console not found.
goto end

:error_PolySerializer
echo ERROR: Test-PolySerializer.dll not built.
goto end

:end
