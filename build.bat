@echo off
echo ========================================
echo YouTube Downloader - Build Script
echo ========================================
echo.

cd /d "%~dp0YouTubeDownloader"

echo [1/4] Checking .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)
echo .NET SDK found!
echo.

echo [2/4] Restoring NuGet packages...
dotnet restore
if errorlevel 1 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)
echo.

echo [3/4] Building the application...
dotnet build --configuration Release
if errorlevel 1 (
    echo ERROR: Build failed
    pause
    exit /b 1
)
echo.

echo [4/4] Build successful!
echo.
echo ========================================
echo Executable location:
echo bin\Release\net8.0-windows\YouTubeDownloader.dll
echo ========================================
echo.
echo Build complete! Use run.bat to start the application.
echo.
pause
