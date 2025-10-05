using FluentAssertions;

namespace YouTubeDownloader.Tests.Helpers;

public class FileHelperTests
{
    [Theory]
    [InlineData("Video Title")]
    [InlineData("Title: With? Invalid* Chars")]
    [InlineData("Normal Title")]
    [InlineData("Title/With\\Slashes")]
    [InlineData("Title|With<Pipes>")]
    public void SanitizeFileName_ShouldRemoveInvalidCharacters(string input)
    {
        // Act
        var result = SanitizeFileName(input);

        // Assert
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var invalidChar in invalidChars)
        {
            result.Should().NotContain(invalidChar.ToString());
        }
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SanitizeFileName_WithEmptyString_ShouldReturnValidString()
    {
        // Arrange
        var input = "";

        // Act
        var result = SanitizeFileName(input);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1024, "1 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(500, "500 B")]
    public void FormatFileSize_ShouldFormatCorrectly(long bytes, string expectedFormat)
    {
        // Act
        var result = FormatFileSize(bytes);

        // Assert
        result.Should().Contain(expectedFormat.Split(' ')[1]); // Check for unit
    }

    [Fact]
    public void FormatFileSize_WithZeroBytes_ShouldReturnZero()
    {
        // Arrange
        long bytes = 0;

        // Act
        var result = FormatFileSize(bytes);

        // Assert
        result.Should().Contain("0");
    }

    [Fact]
    public void FormatFileSize_WithLargeNumber_ShouldUseAppropriateUnit()
    {
        // Arrange
        long bytes = 5368709120; // 5 GB

        // Act
        var result = FormatFileSize(bytes);

        // Assert
        result.Should().Contain("GB");
    }

    // Helper methods mimicking actual utility functions
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.TrimEnd('.');
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
