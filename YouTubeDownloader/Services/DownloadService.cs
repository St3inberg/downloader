using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using YouTubeDownloader.Models;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YouTubeDownloader.Services
{
    /// <summary>
    /// Service for downloading YouTube videos and audio with retry logic and progress tracking.
    /// </summary>
    public class DownloadService : IDisposable
    {
        private YoutubeClient _youtubeClient;
        private HttpClient _httpClient;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _activeDownloads = 0;
        private bool _disposed = false;

        /// <summary>
        /// Gets the number of currently active downloads.
        /// </summary>
        public int ActiveDownloads => _activeDownloads;

        /// <summary>
        /// Raised when download progress changes for any item.
        /// </summary>
        public event EventHandler<DownloadProgressEventArgs>? DownloadProgressChanged;

        /// <summary>
        /// Raised when a download completes successfully.
        /// </summary>
        public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;

        /// <summary>
        /// Raised when a download fails after all retry attempts.
        /// </summary>
        public event EventHandler<DownloadFailedEventArgs>? DownloadFailed;

        /// <summary>
        /// Initializes a new instance of the DownloadService with browser-like headers.
        /// </summary>
        public DownloadService()
        {
            // Create HttpClient with rotating browser-like user agent to avoid detection
            _httpClient = new HttpClient();
            
            // Use different User-Agent strings to avoid pattern detection
            var userAgents = new[]
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/121.0",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            };
            
            var selectedUserAgent = userAgents[Random.Shared.Next(userAgents.Length)];
            
            _httpClient.DefaultRequestHeaders.Add("User-Agent", selectedUserAgent);
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/avif,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _httpClient.DefaultRequestHeaders.Add("DNT", "1");
            _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            
            // Set timeout to handle slow responses
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            _youtubeClient = new YoutubeClient(_httpClient);
        }

        /// <summary>
        /// Creates a download item from a YouTube URL with retry logic for rate limiting.
        /// </summary>
        /// <param name="url">The YouTube video or playlist URL.</param>
        /// <param name="type">Download type: "Video" or "Audio".</param>
        /// <param name="quality">Video quality preference or audio format.</param>
        /// <param name="format">Output format for audio downloads.</param>
        /// <param name="destinationPath">Directory to save the downloaded files.</param>
        /// <returns>A configured DownloadItem ready for download.</returns>
        /// <exception cref="ArgumentException">Thrown when URL is invalid.</exception>
        /// <exception cref="HttpRequestException">Thrown when YouTube blocks the request after retries.</exception>
        public async Task<DownloadItem> CreateDownloadItemAsync(
            string url, string type, string quality, string format, string destinationPath)
        {
            // Enhanced retry logic for cipher manifest and rate limiting
            int maxRetries = 5; // Increased retries for cipher issues
            int baseRetryDelayMs = 1000;
            
            Exception? lastException = null;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Progressive delay with jitter to avoid detection patterns
                    if (attempt > 0)
                    {
                        var delay = baseRetryDelayMs * (int)Math.Pow(2, attempt) + Random.Shared.Next(500, 1500);
                        await Task.Delay(delay);
                        
                        // Log retry attempt for debugging
                        System.Diagnostics.Debug.WriteLine($"Retry attempt {attempt + 1}/{maxRetries} for URL: {url}");
                    }

                    // Clean URL by removing tracking parameters
                    url = CleanYouTubeUrl(url);

                    // Check if it's a playlist
                    if (url.Contains("playlist"))
                    {
                        var playlist = await _youtubeClient.Playlists.GetAsync(url);
                        var videos = new List<PlaylistVideo>();
                        await foreach (var video in _youtubeClient.Playlists.GetVideosAsync(playlist.Id))
                        {
                            videos.Add(video);
                        }

                        return new DownloadItem
                        {
                            Title = $"{playlist.Title} (Playlist - {videos.Count} videos)",
                            Url = url,
                            Type = type,
                            Quality = quality,
                            Format = format,
                            DestinationPath = destinationPath,
                            Status = "Queued (Playlist)"
                        };
                    }
                    else
                    {
                        var video = await _youtubeClient.Videos.GetAsync(url);
                        var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);

                        long size = 0;
                        if (type == "Video")
                        {
                            var videoStream = GetVideoStream(streamManifest, quality);
                            size = videoStream?.Size.Bytes ?? 0;
                        }
                        else
                        {
                            var audioStream = streamManifest.GetAudioStreams().GetWithHighestBitrate();
                            size = audioStream?.Size.Bytes ?? 0;
                        }

                        return new DownloadItem
                        {
                            Title = video.Title,
                            Url = url,
                            Type = type,
                            Quality = quality,
                            Format = format,
                            DestinationPath = destinationPath,
                            Size = FormatFileSize(size),
                            Status = "Queued"
                        };
                    }
                }
                catch (YoutubeExplode.Exceptions.VideoUnavailableException ex)
                {
                    throw new Exception($"Video is unavailable. It may be private, deleted, or region-restricted. Error: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    // Handle specific cipher manifest errors
                    if (ex.Message.Contains("cipher manifest") || ex.Message.Contains("cipher") || ex.Message.Contains("signature"))
                    {
                        System.Diagnostics.Debug.WriteLine($"Cipher error on attempt {attempt + 1}: {ex.Message}");
                        
                        // Recreate YouTube client with fresh headers after cipher errors
                        if (attempt < maxRetries - 1)
                        {
                            RecreateYouTubeClient();
                        }
                        
                        // If this is the last attempt for cipher errors, provide specific guidance
                        if (attempt == maxRetries - 1)
                        {
                            throw new Exception($"Failed to extract cipher manifest. YouTube has enhanced their anti-bot protection. " +
                                $"Try these solutions:\n" +
                                $"1. Wait 10-15 minutes before trying again\n" +
                                $"2. Try a different video URL\n" +
                                $"3. Use a VPN to change your IP address\n" +
                                $"4. Check if the video is age-restricted or region-locked\n" +
                                $"\nTechnical error: {ex.Message}", ex);
                        }
                        continue; // Continue to next retry for cipher errors
                    }
                    
                    // Handle rate limiting errors
                    if (ex.Message.Contains("watch page is broken") || ex.Message.Contains("rate limit") || ex.Message.Contains("429"))
                    {
                        System.Diagnostics.Debug.WriteLine($"Rate limit error on attempt {attempt + 1}: {ex.Message}");
                        
                        if (attempt == maxRetries - 1)
                        {
                            throw new Exception($"YouTube is rate-limiting requests. Please wait 10-15 minutes and try again. " +
                                $"If this persists, your IP address may be temporarily blocked.\n" +
                                $"\nTechnical error: {ex.Message}", ex);
                        }
                        continue; // Continue to next retry for rate limit errors
                    }
                    
                    // For other errors, fail immediately if it's not a network issue
                    if (!IsRetryableError(ex))
                    {
                        throw new Exception($"Failed to fetch video information: {ex.Message}", ex);
                    }
                    
                    // If this is the last attempt for retryable errors
                    if (attempt == maxRetries - 1)
                    {
                        throw new Exception($"Failed to fetch video information after {maxRetries} attempts. " +
                            $"Last error: {ex.Message}", ex);
                    }
                }
            }

            // This should never be reached, but provide fallback with last exception info
            var errorMessage = lastException != null 
                ? $"Failed to fetch video information after {maxRetries} attempts. Last error: {lastException.Message}"
                : "Failed to fetch video information after multiple retries.";
            throw new Exception(errorMessage, lastException);
        }

        /// <summary>
        /// Determines if an error is worth retrying based on its characteristics.
        /// </summary>
        /// <param name="ex">The exception to evaluate.</param>
        /// <returns>True if the error might succeed on retry, false otherwise.</returns>
        private static bool IsRetryableError(Exception ex)
        {
            var message = ex.Message.ToLower();
            
            // Network-related errors that might succeed on retry
            return message.Contains("timeout") ||
                   message.Contains("connection") ||
                   message.Contains("network") ||
                   message.Contains("socket") ||
                   message.Contains("dns") ||
                   message.Contains("ssl") ||
                   message.Contains("tls") ||
                   ex is HttpRequestException ||
                   ex is TaskCanceledException;
        }

        /// <summary>
        /// Recreates the YouTube client with fresh headers to bypass cipher detection.
        /// </summary>
        private void RecreateYouTubeClient()
        {
            try
            {
                // Dispose old client
                _httpClient?.Dispose();
                
                // Create new HttpClient with different headers
                _httpClient = new HttpClient();
                
                // Rotate to a different User-Agent
                var userAgents = new[]
                {
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:122.0) Gecko/20100101 Firefox/122.0",
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.1 Safari/605.1.15",
                    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36"
                };
                
                var selectedUserAgent = userAgents[Random.Shared.Next(userAgents.Length)];
                
                _httpClient.DefaultRequestHeaders.Add("User-Agent", selectedUserAgent);
                _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
                _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/avif,*/*;q=0.8");
                _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
                
                // Add random viewport size
                var viewports = new[] { "1920,1080", "1366,768", "1536,864", "1440,900", "1280,720" };
                var selectedViewport = viewports[Random.Shared.Next(viewports.Length)];
                _httpClient.DefaultRequestHeaders.Add("Viewport-Width", selectedViewport.Split(',')[0]);
                
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                // Create new YoutubeClient
                _youtubeClient = new YoutubeClient(_httpClient);
                
                System.Diagnostics.Debug.WriteLine($"Recreated YouTube client with User-Agent: {selectedUserAgent}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error recreating YouTube client: {ex.Message}");
                // Fallback to basic client if recreation fails
                _httpClient = new HttpClient();
                _youtubeClient = new YoutubeClient(_httpClient);
            }
        }

        private string CleanYouTubeUrl(string url)
        {
            try
            {
                // Remove tracking parameters like 'si', 'feature', etc.
                var uri = new Uri(url);

                // For youtu.be short URLs
                if (uri.Host.Contains("youtu.be"))
                {
                    // Extract just the video ID from the path
                    var videoId = uri.AbsolutePath.TrimStart('/').Split('?')[0];
                    return $"https://youtu.be/{videoId}";
                }

                // For youtube.com URLs
                if (uri.Host.Contains("youtube.com"))
                {
                    var queryParams = uri.Query.TrimStart('?').Split('&');
                    var essentialParams = new List<string>();

                    foreach (var param in queryParams)
                    {
                        var parts = param.Split('=');
                        if (parts.Length == 2)
                        {
                            var key = parts[0];
                            var value = parts[1];

                            // Keep only essential parameters
                            if (key == "v" || key == "list" || key == "t")
                            {
                                essentialParams.Add($"{key}={value}");
                            }
                        }
                    }

                    if (essentialParams.Count > 0)
                    {
                        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}?{string.Join("&", essentialParams)}";
                    }

                    return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
                }

                // Return original URL if not a recognized YouTube format
                return url;
            }
            catch
            {
                // If URL parsing fails, return original
                return url;
            }
        }

        public async Task StartDownloadsAsync(ObservableCollection<DownloadItem> downloadQueue)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            for (int i = 0; i < downloadQueue.Count; i++)
            {
                if (token.IsCancellationRequested)
                    break;

                var item = downloadQueue[i];
                if (item.Status.Contains("Completed") || item.Status.Contains("Failed"))
                    continue;

                try
                {
                    Interlocked.Increment(ref _activeDownloads);

                    if (item.Url.Contains("playlist"))
                    {
                        await DownloadPlaylistAsync(item, i, token);
                    }
                    else
                    {
                        await DownloadSingleVideoAsync(item, i, token);
                    }
                }
                catch (Exception ex)
                {
                    OnDownloadFailed(new DownloadFailedEventArgs(i, ex.Message));
                }
                finally
                {
                    Interlocked.Decrement(ref _activeDownloads);
                }
            }
        }

        private async Task DownloadSingleVideoAsync(DownloadItem item, int index, CancellationToken token)
        {
            try
            {
                var video = await _youtubeClient.Videos.GetAsync(item.Url);
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);

                var sanitizedTitle = SanitizeFileName(video.Title);
                string outputPath;

                if (item.Type == "Video")
                {
                    var videoStream = GetVideoStream(streamManifest, item.Quality);
                    if (videoStream == null)
                        throw new Exception("Requested quality not available");

                    outputPath = Path.Combine(item.DestinationPath, $"{sanitizedTitle}.{videoStream.Container}");

                    var progress = new Progress<double>(p =>
                    {
                        OnDownloadProgressChanged(new DownloadProgressEventArgs(index, p * 100));
                    });

                    await _youtubeClient.Videos.Streams.DownloadAsync(videoStream, outputPath, progress, token);
                }
                else // Audio
                {
                    var audioStream = streamManifest.GetAudioStreams().GetWithHighestBitrate();
                    if (audioStream == null)
                        throw new Exception("No audio stream available");

                    var tempPath = Path.Combine(item.DestinationPath, $"{sanitizedTitle}_temp.{audioStream.Container}");
                    outputPath = Path.Combine(item.DestinationPath, $"{sanitizedTitle}.{item.Format}");

                    var progress = new Progress<double>(p =>
                    {
                        OnDownloadProgressChanged(new DownloadProgressEventArgs(index, p * 100));
                    });

                    await _youtubeClient.Videos.Streams.DownloadAsync(audioStream, tempPath, progress, token);

                    // Convert to desired format if needed
                    if (item.Format != audioStream.Container.Name)
                    {
                        await ConvertAudioAsync(tempPath, outputPath, item.Format);
                        File.Delete(tempPath);
                    }
                    else
                    {
                        File.Move(tempPath, outputPath, true);
                    }

                    // Add metadata
                    await AddMetadataAsync(outputPath, video);
                }

                OnDownloadCompleted(new DownloadCompletedEventArgs(index, outputPath));
            }
            catch (Exception ex)
            {
                throw new Exception($"Download failed: {ex.Message}", ex);
            }
        }

        private async Task DownloadPlaylistAsync(DownloadItem item, int index, CancellationToken token)
        {
            var playlist = await _youtubeClient.Playlists.GetAsync(item.Url);
            var playlistFolder = Path.Combine(item.DestinationPath, SanitizeFileName(playlist.Title ?? "Playlist"));
            Directory.CreateDirectory(playlistFolder);

            var videos = new List<PlaylistVideo>();
            await foreach (var video in _youtubeClient.Playlists.GetVideosAsync(playlist.Id))
            {
                videos.Add(video);
            }
            int videoCount = 0;

            foreach (var video in videos)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    var videoItem = new DownloadItem
                    {
                        Title = video.Title,
                        Url = video.Url,
                        Type = item.Type,
                        Quality = item.Quality,
                        Format = item.Format,
                        DestinationPath = playlistFolder
                    };

                    await DownloadSingleVideoAsync(videoItem, index, token);
                    videoCount++;

                    var progress = (double)videoCount / videos.Count * 100;
                    OnDownloadProgressChanged(new DownloadProgressEventArgs(index, progress));
                }
                catch
                {
                    // Continue with next video if one fails
                    continue;
                }
            }

            OnDownloadCompleted(new DownloadCompletedEventArgs(index, playlistFolder));
        }

        private IVideoStreamInfo? GetVideoStream(StreamManifest manifest, string quality)
        {
            var videoStreams = manifest.GetVideoStreams().OrderByDescending(s => s.VideoQuality.MaxHeight).ToList();

            return quality switch
            {
                "Best Quality" => videoStreams.FirstOrDefault(),
                "1080p" => videoStreams.FirstOrDefault(s => s.VideoQuality.MaxHeight == 1080) ?? videoStreams.FirstOrDefault(),
                "720p" => videoStreams.FirstOrDefault(s => s.VideoQuality.MaxHeight == 720) ?? videoStreams.FirstOrDefault(),
                "480p" => videoStreams.FirstOrDefault(s => s.VideoQuality.MaxHeight == 480) ?? videoStreams.FirstOrDefault(),
                "360p" => videoStreams.FirstOrDefault(s => s.VideoQuality.MaxHeight == 360) ?? videoStreams.FirstOrDefault(),
                _ => videoStreams.FirstOrDefault()
            };
        }

        private async Task ConvertAudioAsync(string inputPath, string outputPath, string format)
        {
            // Simple file copy for now - in production, use FFMpegCore for proper conversion
            await Task.Run(() => File.Copy(inputPath, outputPath, true));
        }

        private async Task AddMetadataAsync(string filePath, Video video)
        {
            await Task.Run(() =>
            {
                try
                {
                    var file = TagLib.File.Create(filePath);
                    file.Tag.Title = video.Title;
                    file.Tag.Performers = new[] { video.Author.ChannelTitle };
                    file.Tag.Comment = video.Description;
                    file.Save();
                }
                catch
                {
                    // Metadata tagging is optional, don't fail if it doesn't work
                }
            });
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
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

        public void PauseDownloads()
        {
            _cancellationTokenSource?.Cancel();
        }

        protected virtual void OnDownloadProgressChanged(DownloadProgressEventArgs e)
        {
            DownloadProgressChanged?.Invoke(this, e);
        }

        protected virtual void OnDownloadCompleted(DownloadCompletedEventArgs e)
        {
            DownloadCompleted?.Invoke(this, e);
        }

        protected virtual void OnDownloadFailed(DownloadFailedEventArgs e)
        {
            DownloadFailed?.Invoke(this, e);
        }

        /// <summary>
        /// Releases all resources used by the DownloadService.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cancellationTokenSource?.Dispose();
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Event arguments for download progress updates.
    /// </summary>
    public class DownloadProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the index of the download item in the queue.
        /// </summary>
        public int ItemIndex { get; }

        /// <summary>
        /// Gets the download progress percentage (0-100).
        /// </summary>
        public double Progress { get; }

        /// <summary>
        /// Initializes a new instance of the DownloadProgressEventArgs class.
        /// </summary>
        /// <param name="itemIndex">The index of the download item.</param>
        /// <param name="progress">The progress percentage (0-100).</param>
        public DownloadProgressEventArgs(int itemIndex, double progress)
        {
            ItemIndex = itemIndex;
            Progress = progress;
        }
    }

    /// <summary>
    /// Event arguments for successful download completion.
    /// </summary>
    public class DownloadCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the index of the completed download item.
        /// </summary>
        public int ItemIndex { get; }

        /// <summary>
        /// Gets the full path to the downloaded file.
        /// </summary>
        public string OutputPath { get; }

        /// <summary>
        /// Initializes a new instance of the DownloadCompletedEventArgs class.
        /// </summary>
        /// <param name="itemIndex">The index of the download item.</param>
        /// <param name="outputPath">The path to the downloaded file.</param>
        public DownloadCompletedEventArgs(int itemIndex, string outputPath)
        {
            ItemIndex = itemIndex;
            OutputPath = outputPath;
        }
    }

    /// <summary>
    /// Event arguments for failed downloads.
    /// </summary>
    public class DownloadFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the index of the failed download item.
        /// </summary>
        public int ItemIndex { get; }

        /// <summary>
        /// Gets the error message describing the failure.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Initializes a new instance of the DownloadFailedEventArgs class.
        /// </summary>
        /// <param name="itemIndex">The index of the failed download item.</param>
        /// <param name="errorMessage">The error message describing the failure.</param>
        public DownloadFailedEventArgs(int itemIndex, string errorMessage)
        {
            ItemIndex = itemIndex;
            ErrorMessage = errorMessage;
        }
    }
}
