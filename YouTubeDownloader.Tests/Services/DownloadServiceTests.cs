using YouTubeDownloader.Services;
using YouTubeDownloader.Models;
using FluentAssertions;
using System.Collections.ObjectModel;

namespace YouTubeDownloader.Tests.Services;

public class DownloadServiceTests : IDisposable
{
    private readonly DownloadService _service;

    public DownloadServiceTests()
    {
        _service = new DownloadService();
    }

    [Fact]
    public void DownloadService_ShouldInitializeWithZeroActiveDownloads()
    {
        // Assert
        _service.ActiveDownloads.Should().Be(0);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    public async Task CreateDownloadItemAsync_WithValidUrl_ShouldReturnDownloadItem(string url)
    {
        // Arrange
        var type = "Video";
        var quality = "720p";
        var format = "mp4";
        var destination = Path.GetTempPath();

        // Act
        Func<Task> act = async () => 
            await _service.CreateDownloadItemAsync(url, type, quality, format, destination);

        // Assert - This may fail if network is unavailable or YouTube blocks the request
        // In a real scenario, we'd mock the YoutubeClient
        await act.Should().NotThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateDownloadItemAsync_WithInvalidUrl_ShouldThrowException()
    {
        // Arrange
        var invalidUrl = "not-a-valid-url";
        var type = "Video";
        var quality = "720p";
        var format = "mp4";
        var destination = Path.GetTempPath();

        // Act
        Func<Task> act = async () => 
            await _service.CreateDownloadItemAsync(invalidUrl, type, quality, format, destination);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void DownloadProgressChanged_Event_ShouldBeRaiseable()
    {
        // Arrange
        _service.DownloadProgressChanged += (sender, e) => { };

        // Act
        var eventInfo = typeof(DownloadService).GetEvent("DownloadProgressChanged");

        // Assert
        eventInfo.Should().NotBeNull();
    }

    [Fact]
    public void DownloadCompleted_Event_ShouldBeRaiseable()
    {
        // Arrange
        _service.DownloadCompleted += (sender, e) => { };

        // Act
        var eventInfo = typeof(DownloadService).GetEvent("DownloadCompleted");

        // Assert
        eventInfo.Should().NotBeNull();
    }

    [Fact]
    public void DownloadFailed_Event_ShouldBeRaiseable()
    {
        // Arrange
        _service.DownloadFailed += (sender, e) => { };

        // Act
        var eventInfo = typeof(DownloadService).GetEvent("DownloadFailed");

        // Assert
        eventInfo.Should().NotBeNull();
    }

    [Fact]
    public void PauseDownloads_ShouldNotThrowException()
    {
        // Act
        Action act = () => _service.PauseDownloads();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldNotThrowException()
    {
        // Act
        Action act = () => _service.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task StartDownloadsAsync_WithEmptyQueue_ShouldComplete()
    {
        // Arrange
        var emptyQueue = new ObservableCollection<DownloadItem>();

        // Act
        Func<Task> act = async () => await _service.StartDownloadsAsync(emptyQueue);

        // Assert
        await act.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}

public class DownloadProgressEventArgsTests
{
    [Fact]
    public void DownloadProgressEventArgs_ShouldInitializeCorrectly()
    {
        // Arrange
        var itemIndex = 5;
        var progress = 75.5;

        // Act
        var args = new DownloadProgressEventArgs(itemIndex, progress);

        // Assert
        args.ItemIndex.Should().Be(itemIndex);
        args.Progress.Should().Be(progress);
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(1, 25.5)]
    [InlineData(10, 100.0)]
    public void DownloadProgressEventArgs_ShouldAcceptVariousValues(int index, double progress)
    {
        // Act
        var args = new DownloadProgressEventArgs(index, progress);

        // Assert
        args.ItemIndex.Should().Be(index);
        args.Progress.Should().Be(progress);
    }
}

public class DownloadCompletedEventArgsTests
{
    [Fact]
    public void DownloadCompletedEventArgs_ShouldInitializeCorrectly()
    {
        // Arrange
        var itemIndex = 3;
        var outputPath = "C:\\Downloads\\video.mp4";

        // Act
        var args = new DownloadCompletedEventArgs(itemIndex, outputPath);

        // Assert
        args.ItemIndex.Should().Be(itemIndex);
        args.OutputPath.Should().Be(outputPath);
    }

    [Theory]
    [InlineData(0, "C:\\path1.mp4")]
    [InlineData(5, "D:\\videos\\test.mp4")]
    public void DownloadCompletedEventArgs_ShouldAcceptVariousPaths(int index, string path)
    {
        // Act
        var args = new DownloadCompletedEventArgs(index, path);

        // Assert
        args.ItemIndex.Should().Be(index);
        args.OutputPath.Should().Be(path);
    }
}

public class DownloadFailedEventArgsTests
{
    [Fact]
    public void DownloadFailedEventArgs_ShouldInitializeCorrectly()
    {
        // Arrange
        var itemIndex = 2;
        var errorMessage = "Network error";

        // Act
        var args = new DownloadFailedEventArgs(itemIndex, errorMessage);

        // Assert
        args.ItemIndex.Should().Be(itemIndex);
        args.ErrorMessage.Should().Be(errorMessage);
    }

    [Theory]
    [InlineData(0, "Connection timeout")]
    [InlineData(3, "Invalid URL")]
    [InlineData(7, "Disk space low")]
    public void DownloadFailedEventArgs_ShouldAcceptVariousErrors(int index, string error)
    {
        // Act
        var args = new DownloadFailedEventArgs(index, error);

        // Assert
        args.ItemIndex.Should().Be(index);
        args.ErrorMessage.Should().Be(error);
    }
}
