@echo off
echo Starting Tests...
dotnet build -c Debug -v quiet
dotnet test --no-build -c Debug
dotnet build -c Release
dotnet test --no-build -c Release
