using System;
using System.IO;
using System.Windows;

namespace YouTubeDownloader
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Log any unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                var errorLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "YouTubeDownloader_Error.txt");
                File.WriteAllText(errorLog, $"Error: {exception?.Message}\n\nStack Trace:\n{exception?.StackTrace}");
                MessageBox.Show($"An error occurred. Details saved to:\n{errorLog}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                var errorLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "YouTubeDownloader_Error.txt");
                File.WriteAllText(errorLog, $"Error: {args.Exception.Message}\n\nStack Trace:\n{args.Exception.StackTrace}");
                MessageBox.Show($"An error occurred. Details saved to:\n{errorLog}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}
