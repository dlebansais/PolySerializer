rem @echo off

if not exist ".\packages\OpenCover.4.7.922\tools\OpenCover.Console.exe" goto error_console1
if not exist ".\packages\Codecov.1.12.2\tools\codecov.exe" goto error_console2
if not exist ".\packages\NUnit.ConsoleRunner.3.11.1\tools\nunit3-console.exe" goto error_console3
if not exist ".\Test-PolySerializer\bin\x64\Debug\netcoreapp3.1\publish\Test-PolySerializer.dll" goto error_not_built
if not exist ".\Test-PolySerializer\bin\x64\Release\netcoreapp3.1\publish\Test-PolySerializer.dll" goto error_not_built
if exist .\Test-PolySerializer\*.log del .\Test-PolySerializer\*.log
if exist .\Test-PolySerializer\obj\x64\Debug\netcoreapp3.1\Coverage-PolySerializer-Debug_coverage.xml del .\Test-PolySerializer\obj\x64\Debug\netcoreapp3.1\Coverage-PolySerializer-Debug_coverage.xml
if exist .\Test-PolySerializer\obj\x64\Release\netcoreapp3.1\Coverage-PolySerializer-Release_coverage.xml del .\Test-PolySerializer\obj\x64\Release\netcoreapp3.1\Coverage-PolySerializer-Release_coverage.xml
".\packages\OpenCover.4.7.922\tools\OpenCover.Console.exe" -register:user -target:".\packages\NUnit.ConsoleRunner.3.11.1\tools\nunit3-console.exe" -targetargs:".\Test-PolySerializer\bin\x64\Debug\netcoreapp3.1\publish\Test-PolySerializer.dll --trace=Debug --labels=Before" -filter:"+[PolySerializer*]* -[Test-PolySerializer*]*" -output:".\Test-PolySerializer\obj\x64\Debug\netcoreapp3.1\Coverage-PolySerializer-Debug_coverage.xml" -showunvisited
".\packages\OpenCover.4.7.922\tools\OpenCover.Console.exe" -register:user -target:".\packages\NUnit.ConsoleRunner.3.11.1\tools\nunit3-console.exe" -targetargs:".\Test-PolySerializer\bin\x64\Release\netcoreapp3.1\publish\Test-PolySerializer.dll --trace=Debug --labels=Before" -filter:"+[PolySerializer*]* -[Test-PolySerializer*]*" -output:".\Test-PolySerializer\obj\x64\Release\netcoreapp3.1\Coverage-PolySerializer-Release_coverage.xml" -showunvisited
goto end
if exist .\Test-PolySerializer\obj\x64\Debug\netcoreapp3.1\Coverage-PolySerializer-Debug_coverage.xml .\packages\Codecov.1.12.2\tools\codecov -f ".\Test-PolySerializer\obj\x64\Debug\netcoreapp3.1\Coverage-PolySerializer-Debug_coverage.xml" -t "7174d319-55d3-45f6-bcdc-33f6fc5b163f"
if exist .\Test-PolySerializer\obj\x64\Release\netcoreapp3.1\Coverage-PolySerializer-Release_coverage.xml .\packages\Codecov.1.12.2\tools\codecov -f ".\Test-PolySerializer\obj\x64\Release\netcoreapp3.1\Coverage-PolySerializer-Release_coverage.xml" -t "7174d319-55d3-45f6-bcdc-33f6fc5b163f"
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
echo ERROR: Test-PolySerializer.dll not built (both Debug and Release are required).
goto end

:end
