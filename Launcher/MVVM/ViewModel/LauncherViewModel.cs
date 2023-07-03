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
using MediaClippex.Services;

namespace Launcher.MVVM.ViewModel;

public partial class LauncherViewModel : ObservableObject
{
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _progressBarVisibility = "Collapsed";
    [ObservableProperty] private string _downloadButtonContent = "Download FFmpeg";
    [ObservableProperty] private string? _progressInfo;
    [ObservableProperty] private bool _isProgressIndeterminate;

    public LauncherViewModel()
    {
        Task.Run(async () =>
        {
            using var response =
                await new HttpClient().GetAsync(await GetUrl(), HttpCompletionOption.ResponseHeadersRead);
            var headersContentLength = response.Content.Headers.ContentLength;
            DownloadButtonContent =
                string.Format(
                    $"{DownloadButtonContent} {StringService.ConvertBytesToFormattedString(headersContentLength ?? 0)}");
        });
    }

    [RelayCommand]
    private async Task Download()
    {
        try
        {
            ProgressBarVisibility = "Visible";
            ProgressInfo = "Getting ffmpeg info...";
            IsProgressIndeterminate = true;

            var downloadUrl = await GetUrl(); // Getting DownloadUrl

            if (string.IsNullOrEmpty(downloadUrl))
            {
                throw new Exception("Failed to retrieve the download URL.");
            }

            IsProgressIndeterminate = false;
            ProgressInfo = "Downloading ffmpeg...";

            var zipPath = await DownloadZip(downloadUrl); // Downloading the zip file

            if (string.IsNullOrEmpty(zipPath))
            {
                throw new Exception("Failed to download the zip file.");
            }

            for (var i = Progress; i >= Progress; i--) // Reset the progress bar
            {
                Progress = i;
            }

            ProgressInfo = "Installing ffmpeg...";

            var done = await Extraction(zipPath); // Extracting the zip file to the application directory

            if (!done)
            {
                throw new Exception("Failed to install ffmpeg.");
            }

            if (File.Exists(zipPath)) File.Delete(zipPath);
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

    private static async Task<string> GetUrl()
    {
        using var client = new HttpClient();
        var url = string.Empty;
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
            url = ffmpegElement.GetString();
        }

        return url ?? string.Empty;
    }

    private async Task<string> DownloadZip(string downloadUrl)
    {
        using var client = new HttpClient();
        using var headRequest = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
        using var headResponse = await client.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead);
        var contentLengthHeader = headResponse.Content.Headers.ContentLength;

        if (!contentLengthHeader.HasValue)
        {
            throw new Exception("Failed to retrieve the content length of the zip file.");
        }

        var totalBytes = contentLengthHeader.Value;

        await using var zipStream = await client.GetStreamAsync(downloadUrl);
        var tempFileName = Path.GetTempFileName();
        await using var fileStream = File.Create(tempFileName);

        var buffer = new byte[8192];
        long bytesRead;
        var totalBytesRead = 0L;

        while ((bytesRead = await zipStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, (int)bytesRead));

            totalBytesRead += bytesRead;

            OnUpdateProgress(totalBytesRead, totalBytes);
        }

        return tempFileName;
    }

    private async Task<bool> Extraction(string zipFilePath)
    {
        var extractionPath = AppContext.BaseDirectory;

        return await Task.Run(() =>
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

            return File.Exists(Path.Combine(extractionPath, "ffmpeg.exe"));
        });
    }

    private void OnUpdateProgress(long bytesReceived, long totalBytes)
    {
        Progress = (double)bytesReceived / totalBytes * 100;
    }
}