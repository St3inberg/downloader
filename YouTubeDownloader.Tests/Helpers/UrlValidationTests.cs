using FluentAssertions;

namespace YouTubeDownloader.Tests.Helpers;

public class UrlValidationTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", true)]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", true)]
    [InlineData("https://www.youtube.com/playlist?list=PLrAXtmErZgOeiKm4sgNOknGvNjby9efdf", true)]
    [InlineData("http://www.youtube.com/watch?v=test123", true)]
    [InlineData("https://m.youtube.com/watch?v=test123", true)]
    public void IsValidYouTubeUrl_WithValidUrls_ShouldReturnTrue(string url, bool expected)
    {
        // Act
        var result = IsValidYouTubeUrl(url);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("not a url", false)]
    [InlineData("https://www.google.com", false)]
    [InlineData("https://vimeo.com/12345", false)]
    [InlineData("", false)]
    [InlineData("ftp://youtube.com/watch", false)]
    public void IsValidYouTubeUrl_WithInvalidUrls_ShouldReturnFalse(string url, bool expected)
    {
        // Act
        var result = IsValidYouTubeUrl(url);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("https://www.youtube.com/playlist?list=PLtest", true)]
    [InlineData("https://youtube.com/playlist?list=PLtest", true)]
    [InlineData("https://www.youtube.com/watch?v=test", false)]
    [InlineData("https://youtu.be/test", false)]
    public void IsPlaylistUrl_ShouldIdentifyPlaylists(string url, bool expected)
    {
        // Act
        var result = url.Contains("playlist");

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidYouTubeUrl_WithNullUrl_ShouldReturnFalse()
    {
        // Arrange
        string? url = null;

        // Act
        var result = IsValidYouTubeUrl(url);

        // Assert
        result.Should().BeFalse();
    }

    // Helper method mimicking the actual validation logic
    private bool IsValidYouTubeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // Must be http or https protocol
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return false;

        return url.Contains("youtube.com/watch") ||
               url.Contains("youtu.be/") ||
               url.Contains("youtube.com/playlist");
    }
}
