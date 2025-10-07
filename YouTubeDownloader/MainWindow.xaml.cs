using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using YouTubeDownloader.Models;
using YouTubeDownloader.Services;
using WinForms = System.Windows.Forms;

namespace YouTubeDownloader
{
    public partial class MainWindow : Window
    {
        private readonly DownloadService _downloadService;
        private readonly ObservableCollection<DownloadItem> _downloadQueue;
        private string _destinationPath;

        public MainWindow()
        {
            InitializeComponent();

            _downloadQueue = new ObservableCollection<DownloadItem>();
            _downloadService = new DownloadService();

            DownloadQueueGrid.ItemsSource = _downloadQueue;

            // Set default download path
            _destinationPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "YouTube Downloads"
            );
            Directory.CreateDirectory(_destinationPath);
            DestinationTextBox.Text = _destinationPath;

            // Subscribe to download service events
            _downloadService.DownloadProgressChanged += OnDownloadProgressChanged;
            _downloadService.DownloadCompleted += OnDownloadCompleted;
            _downloadService.DownloadFailed += OnDownloadFailed;
        }

        private void DownloadTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Null check to prevent errors during XAML initialization
            if (QualityComboBox == null || AudioFormatComboBox == null)
                return;

            if (DownloadTypeComboBox.SelectedIndex == 0) // Video
            {
                QualityComboBox.Visibility = Visibility.Visible;
                AudioFormatComboBox.Visibility = Visibility.Collapsed;
            }
            else // Audio Only
            {
                QualityComboBox.Visibility = Visibility.Collapsed;
                AudioFormatComboBox.Visibility = Visibility.Visible;
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog
            {
                Description = "Select download destination folder",
                SelectedPath = _destinationPath
            };

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                _destinationPath = dialog.SelectedPath;
                DestinationTextBox.Text = _destinationPath;
            }
        }

        private async void AddToQueueButton_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Please enter a YouTube URL.", "Invalid URL",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidYouTubeUrl(url))
            {
                MessageBox.Show("Please enter a valid YouTube URL.", "Invalid URL",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusTextBlock.Text = "Fetching video information...";
                AddToQueueButton.IsEnabled = false;

                var downloadType = DownloadTypeComboBox.SelectedIndex == 0 ? "Video" : "Audio";
                string quality;
                string format = "mp4";

                if (downloadType == "Video")
                {
                    quality = ((ComboBoxItem)QualityComboBox.SelectedItem).Content.ToString()!;
                }
                else
                {
                    quality = "Audio";
                    format = ((ComboBoxItem)AudioFormatComboBox.SelectedItem).Content.ToString()!.ToLower();
                }

                var downloadItem = await _downloadService.CreateDownloadItemAsync(
                    url, downloadType, quality, format, _destinationPath);

                _downloadQueue.Add(downloadItem);
                UrlTextBox.Clear();
                StatusTextBlock.Text = $"Added: {downloadItem.Title}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding download: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "Error occurred";
            }
            finally
            {
                AddToQueueButton.IsEnabled = true;
            }
        }

        private async void StartDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_downloadQueue.Count == 0)
            {
                MessageBox.Show("Download queue is empty.", "No Downloads",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Check disk space
            var drive = new DriveInfo(Path.GetPathRoot(_destinationPath)!);
            if (drive.AvailableFreeSpace < 100 * 1024 * 1024) // Less than 100MB
            {
                var result = MessageBox.Show(
                    "Low disk space warning. Continue anyway?",
                    "Disk Space Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return;
            }

            StartDownloadButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
            AddToQueueButton.IsEnabled = false;

            await _downloadService.StartDownloadsAsync(_downloadQueue);

            StartDownloadButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            AddToQueueButton.IsEnabled = true;
            StatusTextBlock.Text = "All downloads completed";
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _downloadService.PauseDownloads();
            PauseButton.IsEnabled = false;
            StartDownloadButton.IsEnabled = true;
            StatusTextBlock.Text = "Downloads paused";
        }

        private void ClearQueueButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear the download queue?",
                "Clear Queue",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _downloadQueue.Clear();
                StatusTextBlock.Text = "Queue cleared";
            }
        }

        private void OnDownloadProgressChanged(object? sender, DownloadProgressEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var item = _downloadQueue[e.ItemIndex];
                item.Progress = e.Progress;
                item.Status = "Downloading...";
                ActiveDownloadsTextBlock.Text = $"Active Downloads: {_downloadService.ActiveDownloads}";
            });
        }

        private void OnDownloadCompleted(object? sender, DownloadCompletedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var item = _downloadQueue[e.ItemIndex];
                item.Progress = 100;
                item.Status = "Completed";
                StatusTextBlock.Text = $"Completed: {item.Title}";

                MessageBox.Show($"Download completed: {item.Title}", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void OnDownloadFailed(object? sender, DownloadFailedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var item = _downloadQueue[e.ItemIndex];
                item.Status = $"Failed: {e.ErrorMessage}";
                StatusTextBlock.Text = $"Failed: {item.Title}";

                MessageBox.Show($"Download failed: {item.Title}\n\nError: {e.ErrorMessage}",
                    "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private bool IsValidYouTubeUrl(string url)
        {
            return url.Contains("youtube.com/watch") ||
                   url.Contains("youtu.be/") ||
                   url.Contains("youtube.com/playlist");
        }

        protected override void OnClosed(EventArgs e)
        {
            _downloadService.Dispose();
            base.OnClosed(e);
        }
    }
}
