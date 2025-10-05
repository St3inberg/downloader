using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Playlists;
using YouTubeDownloader.Models;

namespace YouTubeDownloader.Services
{
    public class DownloadService : IDisposable
    {
        private readonly YoutubeClient _youtubeClient;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _activeDownloads = 0;

        public int ActiveDownloads => _activeDownloads;

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgressChanged;
        public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;
        public event EventHandler<DownloadFailedEventArgs>? DownloadFailed;

        public DownloadService()
        {
            // Create HttpClient with browser-like user agent to avoid rate limiting
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            
            _youtubeClient = new YoutubeClient(httpClient);
        }

        public async Task<DownloadItem> CreateDownloadItemAsync(
            string url, string type, string quality, string format, string destinationPath)
        {
            // Retry logic for rate limiting
            int maxRetries = 3;
            int retryDelayMs = 2000;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Add delay between retries
                    if (attempt > 0)
                    {
                        await Task.Delay(retryDelayMs * attempt);
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
                    // If this is the last attempt, throw the exception
                    if (attempt == maxRetries - 1)
                    {
                        if (ex.Message.Contains("watch page is broken") || ex.Message.Contains("rate limit"))
                        {
                            throw new Exception($"YouTube is temporarily blocking requests. Please wait a few minutes and try again. If this persists, YouTube may be rate-limiting your IP address.", ex);
                        }
                        throw new Exception($"Failed to fetch video information: {ex.Message}", ex);
                    }
                    // Otherwise, continue to next retry
                }
            }
            
            throw new Exception("Failed to fetch video information after multiple retries.");
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

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public int ItemIndex { get; }
        public double Progress { get; }

        public DownloadProgressEventArgs(int itemIndex, double progress)
        {
            ItemIndex = itemIndex;
            Progress = progress;
        }
    }

    public class DownloadCompletedEventArgs : EventArgs
    {
        public int ItemIndex { get; }
        public string OutputPath { get; }

        public DownloadCompletedEventArgs(int itemIndex, string outputPath)
        {
            ItemIndex = itemIndex;
            OutputPath = outputPath;
        }
    }

    public class DownloadFailedEventArgs : EventArgs
    {
        public int ItemIndex { get; }
        public string ErrorMessage { get; }

        public DownloadFailedEventArgs(int itemIndex, string errorMessage)
        {
            ItemIndex = itemIndex;
            ErrorMessage = errorMessage;
        }
    }
}
