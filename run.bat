@echo off
echo ========================================
echo YouTube Downloader - Run Application
echo ========================================
echo.

cd /d "%~dp0YouTubeDownloader"

echo Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

if not exist "bin\Release\net8.0-windows\YouTubeDownloader.dll" (
    echo ERROR: Application not built yet!
    echo Please run build.bat first to build the application.
    pause
    exit /b 1
)

echo Starting YouTube Downloader...
echo.
dotnet bin\Release\net8.0-windows\YouTubeDownloader.dll

if errorlevel 1 (
    echo.
    echo ERROR: Application failed to start
    pause
)
