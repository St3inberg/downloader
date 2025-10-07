using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YouTubeDownloader.Models
{
    /// <summary>
    /// Represents a YouTube download item with progress tracking and metadata.
    /// Implements INotifyPropertyChanged for data binding in WPF.
    /// </summary>
    public class DownloadItem : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _url = string.Empty;
        private string _type = string.Empty;
        private string _quality = string.Empty;
        private string _format = string.Empty;
        private string _status = "Queued";
        private double _progress = 0;
        private string _size = "Unknown";
        private string _destinationPath = string.Empty;

        /// <summary>
        /// Gets or sets the title of the video or playlist.
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the YouTube URL for this download.
        /// </summary>
        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the download type ("Video" or "Audio").
        /// </summary>
        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the quality setting for the download.
        /// </summary>
        public string Quality
        {
            get => _quality;
            set
            {
                _quality = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the output format for audio downloads (e.g., "mp3", "aac").
        /// </summary>
        public string Format
        {
            get => _format;
            set
            {
                _format = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the current status of the download.
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the download progress percentage (0-100).
        /// </summary>
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the formatted file size string.
        /// </summary>
        public string Size
        {
            get => _size;
            set
            {
                _size = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the destination directory for the download.
        /// </summary>
        public string DestinationPath
        {
            get => _destinationPath;
            set
            {
                _destinationPath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed. This parameter is automatically provided by the compiler.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
