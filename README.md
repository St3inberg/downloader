# YouTube Downloader

A modern Windows desktop application for downloading YouTube videos and extracting audio, built with C# and WPF.

## Features

**Video Downloads**
- Multiple resolution options (1080p, 720p, 480p, 360p, Best Quality)
- Automatic quality selection
- Progress tracking for each download

**Audio Extraction**
- Extract audio as MP3, AAC, or WAV
- Automatic metadata tagging (title, artist)
- High-quality audio extraction

**Playlist Support**
- Download entire playlists
- Automatic folder creation for playlists
- Individual video progress tracking

**Batch Downloads**
- Queue multiple URLs
- Download progress for each item
- Pause/Resume functionality

**User-Friendly Interface**
- Clean, intuitive WPF interface
- Real-time progress bars
- Status notifications
- Download queue management

**Error Handling**
- Invalid URL detection
- Network error recovery
- Disk space warnings
- Clear error messages

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime or SDK
- Internet connection

## Setup Instructions

### Option 1: Build from Source

1. **Install Prerequisites**
   - Download and install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Install [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) (Community Edition is free)
     - During installation, select the ".NET desktop development" workload

2. **Clone or Download the Project**
   ```cmd
   cd f:\downloader
   ```

3. **Restore NuGet Packages**
   ```cmd
   cd YouTubeDownloader
   dotnet restore
   ```

4. **Build the Project**
   
   **Using Command Line:**
   ```cmd
   dotnet build --configuration Release
   ```
   
   **Using Visual Studio:**
   - Open `YouTubeDownloader.sln` in Visual Studio
   - Press `Ctrl+Shift+B` or go to Build → Build Solution

5. **Run the Application**
   
   **From Command Line:**
   ```cmd
   dotnet run --configuration Release
   ```
   
   **From Visual Studio:**
   - Press `F5` or click the "Start" button

6. **Create Executable (Optional)**
   ```cmd
   dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
   ```
   The executable will be in: `YouTubeDownloader\bin\Release\net8.0-windows\win-x64\publish\`

### Option 2: Run Without Building

If you only have .NET Runtime installed:

```cmd
cd f:\downloader\YouTubeDownloader
dotnet run
```

## How to Use

1. **Launch the Application**
   - Run `YouTubeDownloader.exe` or use `dotnet run`

2. **Configure Download Settings**
   - Paste a YouTube URL in the "YouTube URL" field
   - Select download type: **Video** or **Audio Only**
   - Choose quality/format:
     - For videos: Best Quality, 1080p, 720p, 480p, or 360p
     - For audio: MP3, AAC, or WAV
   - Select destination folder (default: My Videos\YouTube Downloads)

3. **Add to Queue**
   - Click "Add to Queue" to add the download to the list
   - Repeat for multiple videos/playlists

4. **Start Downloads**
   - Click "Start Downloads" to begin downloading all queued items
   - Monitor progress in the download queue grid
   - Use "Pause" to stop downloads temporarily

5. **Manage Queue**
   - Select items in the grid to view details
   - Click "Clear Queue" to remove all items

## Project Structure

```
YouTubeDownloader/
├── YouTubeDownloader.csproj    # Project file with dependencies
├── App.xaml                     # Application resources and styles
├── App.xaml.cs                  # Application entry point
├── MainWindow.xaml              # Main UI layout
├── MainWindow.xaml.cs           # Main window logic
├── Models/
│   └── DownloadItem.cs         # Download item model
└── Services/
    └── DownloadService.cs      # Core download logic
```

## Key Dependencies

- **YoutubeExplode** (v6.3.16) - YouTube video information and downloading
- **TagLibSharp** (v2.3.0) - Audio metadata tagging
- **FFMpegCore** (v5.1.0) - Audio format conversion (optional enhancement)

## Best Practices Implemented

1. **MVVM Pattern** - Clean separation of UI and business logic
2. **Async/Await** - Non-blocking UI during downloads
3. **INotifyPropertyChanged** - Automatic UI updates
4. **Dependency Injection Ready** - Service-based architecture
5. **Error Handling** - Comprehensive try-catch blocks
6. **Resource Management** - Proper disposal of resources
7. **User Feedback** - Progress bars, status messages, notifications

## Troubleshooting

### Common YouTube Errors

#### "Failed to extract cipher manifest"
This error occurs when YouTube's anti-bot protection blocks access. **Solutions**:
1. **Wait 10-15 minutes** and try again
2. **Try a different video** to test connectivity
3. **Use a VPN** to change your IP address
4. **Check if the video is age-restricted** or region-locked

The app automatically:
- Retries 5 times with increasing delays
- Rotates User-Agent headers to avoid detection
- Recreates the connection with fresh headers

For detailed guidance, see [CIPHER_MANIFEST_TROUBLESHOOTING.md](CIPHER_MANIFEST_TROUBLESHOOTING.md)

#### "Rate limited" or "Watch page broken"
YouTube is temporarily blocking requests from your IP:
1. **Wait 10-15 minutes** before trying again
2. **Use a different internet connection**
3. **Try with a VPN** to change location

### Application Errors

#### "Unable to find package YoutubeExplode"
```cmd
dotnet restore
```

#### "This application requires .NET 8.0"
- Download and install [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Performance Issues

#### Downloads are slow
- Check your internet connection
- YouTube may be throttling requests
- Try downloading at a lower quality

#### "Access to path denied"
- Run the application as Administrator
- Change the destination folder to a location with write permissions

#### Playlist downloads fail
- Ensure the playlist is public
- Try downloading individual videos from the playlist

## Advanced Configuration

### Custom Download Path
- Click "Browse..." to select a custom download folder
- The path is saved for the current session

### Concurrent Downloads
- Currently downloads are sequential for stability
- To enable concurrent downloads, modify `DownloadService.cs` (advanced users)

## Future Enhancements

- [ ] Cloud storage integration (OneDrive, Google Drive)
- [ ] Download scheduling
- [ ] Subtitle download support
- [ ] Video preview before download
- [ ] Download history
- [ ] Settings persistence

## License

This project is for educational purposes. Please respect YouTube's Terms of Service and copyright laws.

## Support

For issues or questions:
1. Check the Troubleshooting section
2. Review error messages in the application
3. Ensure you have the latest .NET 8.0 SDK/Runtime

---


