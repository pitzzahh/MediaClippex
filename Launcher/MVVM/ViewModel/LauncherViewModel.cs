using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Launcher.MVVM.ViewModel;

public partial class LauncherViewModel : ObservableObject
{
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _progressBarVisibility = "Collapsed";
    [ObservableProperty] private string? _progressInfo;
    [ObservableProperty] private bool _isProgressIndeterminate;

    private string? _downloadUrl = string.Empty;
    private string? _tempFilePath = string.Empty;

    [RelayCommand]
    private async Task Download()
    {
        try
        {
            ProgressBarVisibility = "Visible";
            ProgressInfo = "Getting ffmpeg info...";
            IsProgressIndeterminate = true;
            await GetZipUrl();

            IsProgressIndeterminate = false;
            ProgressInfo = "Downloading ffmpeg...";
            await DownloadZip(_downloadUrl, _tempFilePath);

            ProgressInfo = "Installing ffmpeg...";
            await Installation(_tempFilePath);
            ProgressInfo = "Done!";

            var result = MessageBox.Show("Ffmpeg installed successfully. Do you want to open the application now?",
                "Update Installed", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                Process.Start("MediaClippex.exe");
            }

            Application.Current.Dispatcher.Invoke(() => { Application.Current.Shutdown(); });
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ProgressBarVisibility = "Collapsed";
            IsProgressIndeterminate = false;
            ProgressInfo = string.Empty;
        }
    }

    private async Task GetZipUrl()
    {
        using var client = new HttpClient();
        const string endpoint = "https://ffbinaries.com/api/v1/version/latest";

        var json = await client.GetStringAsync(endpoint);

        if (string.IsNullOrEmpty(json))
        {
            throw new TimeoutException("Failed to retrieve the API response.");
        }

        using var document = JsonDocument.Parse(json);
        var data = document.RootElement;
        var windows64Element = data.GetProperty("bin").GetProperty("windows-64");

        if (windows64Element.TryGetProperty("ffmpeg", out var ffmpegElement))
        {
            _downloadUrl = ffmpegElement.GetString();
        }

        if (string.IsNullOrEmpty(_downloadUrl))
        {
            throw new Exception("Failed to retrieve the download URL.");
        }
    }

    private async Task DownloadZip(string downloadUrl, string tempFilePath)
    {
        await using var zipStream = await new HttpClient().GetStreamAsync(_downloadUrl);
        tempFilePath = Path.GetTempFileName();
        await using var fileStream = File.Create(_tempFilePath);

        var buffer = new byte[8192];
        long bytesRead;
        var totalBytesRead = 0L;

        while ((bytesRead = await zipStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, (int)bytesRead));

            totalBytesRead += bytesRead;

            OnUpdateProgress(totalBytesRead, fileStream.Length);
        }

        for (var i = Progress; i >= Progress; i--)
        {
            Progress = i;
        }
    }


    private async Task Installation(string zipFilePath)
    {
        try
        {
            var extractionPath = AppContext.BaseDirectory;

            await Task.Run(() =>
            {
                using var archive = ZipFile.OpenRead(zipFilePath);
                var entryCount = archive.Entries.Count;
                var processedCount = 0;

                foreach (var entry in archive.Entries)
                {
                    if (!entry.FullName.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase)) continue;
                    entry.ExtractToFile(Path.Combine(extractionPath, entry.FullName), true);
                    processedCount++;

                    OnUpdateProgress(processedCount, entryCount);
                }
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Error occurred while extracting the update: {ex.Message}");
        }
        finally
        {
            if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
        }
    }

    private void OnUpdateProgress(long bytesReceived, long totalBytes)
    {
        Progress = (double)bytesReceived / totalBytes * 100;
    }
}