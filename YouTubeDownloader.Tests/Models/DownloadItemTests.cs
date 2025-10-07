using System.ComponentModel;
using FluentAssertions;
using YouTubeDownloader.Models;

namespace YouTubeDownloader.Tests.Models;

public class DownloadItemTests
{
    [Fact]
    public void DownloadItem_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var item = new DownloadItem();

        // Assert
        item.Title.Should().BeEmpty();
        item.Url.Should().BeEmpty();
        item.Type.Should().BeEmpty();
        item.Quality.Should().BeEmpty();
        item.Format.Should().BeEmpty();
        item.Status.Should().Be("Queued");
        item.Progress.Should().Be(0);
        item.Size.Should().Be("Unknown");
        item.DestinationPath.Should().BeEmpty();
    }

    [Fact]
    public void Title_WhenSet_ShouldUpdateValue()
    {
        // Arrange
        var item = new DownloadItem();
        var newTitle = "Test Video Title";

        // Act
        item.Title = newTitle;

        // Assert
        item.Title.Should().Be(newTitle);
    }

    [Fact]
    public void Status_WhenChanged_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var item = new DownloadItem();
        var eventRaised = false;
        item.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(DownloadItem.Status))
                eventRaised = true;
        };

        // Act
        item.Status = "Downloading";

        // Assert
        eventRaised.Should().BeTrue();
        item.Status.Should().Be("Downloading");
    }

    [Fact]
    public void Progress_WhenChanged_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var item = new DownloadItem();
        var eventRaised = false;
        item.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(DownloadItem.Progress))
                eventRaised = true;
        };

        // Act
        item.Progress = 50.5;

        // Assert
        eventRaised.Should().BeTrue();
        item.Progress.Should().Be(50.5);
    }

    [Theory]
    [InlineData("Video")]
    [InlineData("Audio")]
    public void Type_WhenSet_ShouldAcceptValidValues(string type)
    {
        // Arrange
        var item = new DownloadItem();

        // Act
        item.Type = type;

        // Assert
        item.Type.Should().Be(type);
    }

    [Theory]
    [InlineData("720p")]
    [InlineData("1080p")]
    [InlineData("Best Quality")]
    public void Quality_WhenSet_ShouldAcceptValidValues(string quality)
    {
        // Arrange
        var item = new DownloadItem();

        // Act
        item.Quality = quality;

        // Assert
        item.Quality.Should().Be(quality);
    }

    [Theory]
    [InlineData("mp3")]
    [InlineData("aac")]
    [InlineData("wav")]
    [InlineData("mp4")]
    public void Format_WhenSet_ShouldAcceptValidValues(string format)
    {
        // Arrange
        var item = new DownloadItem();

        // Act
        item.Format = format;

        // Assert
        item.Format.Should().Be(format);
    }

    [Fact]
    public void AllProperties_WhenChanged_ShouldRaisePropertyChangedEvent()
    {
        // Arrange
        var item = new DownloadItem();
        var propertiesChanged = new List<string>();
        item.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName != null)
                propertiesChanged.Add(e.PropertyName);
        };

        // Act
        item.Title = "Test";
        item.Url = "https://youtube.com/test";
        item.Type = "Video";
        item.Quality = "720p";
        item.Format = "mp4";
        item.Status = "Downloading";
        item.Progress = 50;
        item.Size = "10 MB";
        item.DestinationPath = "C:\\Downloads";

        // Assert
        propertiesChanged.Should().Contain(nameof(DownloadItem.Title));
        propertiesChanged.Should().Contain(nameof(DownloadItem.Url));
        propertiesChanged.Should().Contain(nameof(DownloadItem.Type));
        propertiesChanged.Should().Contain(nameof(DownloadItem.Quality));
        propertiesChanged.Should().Contain(nameof(DownloadItem.Format));
        propertiesChanged.Should().Contain(nameof(DownloadItem.Status));
        propertiesChanged.Should().Contain(nameof(DownloadItem.Progress));
        propertiesChanged.Should().Contain(nameof(DownloadItem.Size));
        propertiesChanged.Should().Contain(nameof(DownloadItem.DestinationPath));
    }

    [Fact]
    public void DownloadItem_ShouldImplementINotifyPropertyChanged()
    {
        // Arrange & Act
        var item = new DownloadItem();

        // Assert
        item.Should().BeAssignableTo<INotifyPropertyChanged>();
    }
}
