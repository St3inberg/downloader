@echo off
echo ========================================
echo YouTube Downloader - Create Standalone Executable
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
echo.

echo [1/3] Restoring packages...
dotnet restore
if errorlevel 1 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)
echo.

echo [2/3] Publishing standalone executable...
echo This may take a few minutes...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
if errorlevel 1 (
    echo ERROR: Publish failed
    pause
    exit /b 1
)
echo.

echo [3/3] Success!
echo.
echo ========================================
echo Standalone executable created at:
echo bin\Release\net8.0-windows\win-x64\publish\YouTubeDownloader.exe
echo.
echo This file can be copied anywhere and run without .NET SDK!
echo File size: ~70-80MB (includes .NET runtime)
echo ========================================
echo.

echo Opening publish folder...
start "" "bin\Release\net8.0-windows\win-x64\publish"

pause
