using System.Collections.ObjectModel;
using FluentAssertions;
using YouTubeDownloader.Models;
using YouTubeDownloader.Services;

namespace YouTubeDownloader.Tests.Integration;

public class DownloadWorkflowTests
{
    [Fact]
    public void DownloadItem_CreationToCompletion_ShouldFollowExpectedWorkflow()
    {
        // Arrange
        var item = new DownloadItem
        {
            Title = "Test Video",
            Url = "https://youtube.com/test",
            Type = "Video",
            Quality = "720p",
            Format = "mp4",
            Status = "Queued",
            Progress = 0,
            DestinationPath = Path.GetTempPath()
        };

        var statusHistory = new List<string>();
        item.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(DownloadItem.Status))
                statusHistory.Add(item.Status);
        };

        // Act - Simulate workflow
        item.Status = "Downloading";
        item.Progress = 50;
        item.Progress = 100;
        item.Status = "Completed";

        // Assert
        statusHistory.Should().Contain("Downloading");
        statusHistory.Should().Contain("Completed");
        item.Progress.Should().Be(100);
    }

    [Fact]
    public void MultipleDownloadItems_InQueue_ShouldMaintainIndependentState()
    {
        // Arrange
        var queue = new ObservableCollection<DownloadItem>
        {
            new DownloadItem { Title = "Video 1", Status = "Queued", Progress = 0 },
            new DownloadItem { Title = "Video 2", Status = "Queued", Progress = 0 },
            new DownloadItem { Title = "Video 3", Status = "Queued", Progress = 0 }
        };

        // Act
        queue[0].Status = "Downloading";
        queue[0].Progress = 50;
        queue[1].Status = "Downloading";
        queue[1].Progress = 25;

        // Assert
        queue[0].Status.Should().Be("Downloading");
        queue[0].Progress.Should().Be(50);
        queue[1].Status.Should().Be("Downloading");
        queue[1].Progress.Should().Be(25);
        queue[2].Status.Should().Be("Queued");
        queue[2].Progress.Should().Be(0);
    }

    [Fact]
    public void DownloadItem_ErrorScenario_ShouldUpdateStatusCorrectly()
    {
        // Arrange
        var item = new DownloadItem
        {
            Title = "Test Video",
            Status = "Queued"
        };

        // Act - Simulate error
        item.Status = "Downloading";
        item.Progress = 30;
        item.Status = "Failed: Network error";

        // Assert
        item.Status.Should().Contain("Failed");
        item.Progress.Should().Be(30);
    }

    [Fact]
    public void ObservableCollection_AddRemove_ShouldTriggerEvents()
    {
        // Arrange
        var queue = new ObservableCollection<DownloadItem>();
        var addedCount = 0;
        var removedCount = 0;

        queue.CollectionChanged += (sender, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                addedCount++;
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                removedCount++;
        };

        // Act
        queue.Add(new DownloadItem { Title = "Video 1" });
        queue.Add(new DownloadItem { Title = "Video 2" });
        queue.RemoveAt(0);

        // Assert
        addedCount.Should().Be(2);
        removedCount.Should().Be(1);
        queue.Should().HaveCount(1);
    }
}
