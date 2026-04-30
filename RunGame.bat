@echo off
echo Building X0...
cd /d "%~dp0X0"
dotnet build -c Debug --nologo -v q
if errorlevel 1 (
    echo Build FAILED. Press any key to exit.
    pause
    exit /b 1
)
echo.
echo Starting X0 at https://localhost:7118
echo Press Ctrl+C to stop.
echo.
start "" "https://localhost:7118"
dotnet "%TEMP%\X0Build\Debug\net8.0\X0.dll" --urls "https://localhost:7118;http://localhost:5176"
