using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Octokit;
using Application = System.Windows.Application;
using FileMode = System.IO.FileMode;

namespace Elevator.MVVM.ViewModel;

public partial class MediaClippexUpdaterViewModel : ObservableObject
{
    [ObservableProperty] private string? _titleName = "MediaClippex Updater";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _progressInfo;
    [ObservableProperty] private string _progressBarVisibility = "Collapsed";
    [ObservableProperty] private bool _isProgressIndeterminate;
    [ObservableProperty] private string? _currentVersion;
    [ObservableProperty] private string? _latestVersion;
    [ObservableProperty] private string? _changeLog;
    [ObservableProperty] private string _showChangeLog = "Collapsed";
    private static string Owner => "pitzzahh";
    private static string Repo => "MediaClippex";

    public MediaClippexUpdaterViewModel()
    {
        CurrentVersion = ReadCurrentVersion();

        var gitHubClient = new GitHubClient(new ProductHeaderValue(Repo));
        
        Task.Run(async () =>
        {
            ProgressBarVisibility = "Visible";
            IsProgressIndeterminate = true;
            ProgressInfo = "Getting Latest Version...";
            
            var latestRelease = await gitHubClient
                .Repository
                .Release
                .GetLatest(Owner, Repo);

            LatestVersion = latestRelease.TagName;
            
            IsProgressIndeterminate = false;
            ProgressInfo = "Getting Changelog...";
            IsProgressIndeterminate = true;
            
            ProgressBarVisibility = "Collapsed";
            
            ChangeLog = latestRelease.Body.Replace("#", "");
            
            try
            {
                var downloadUrl = latestRelease
                    .Assets
                    .Where(asset => asset.ContentType == "application/zip")
                    .FirstOrDefault(asset => asset.Name == "Update.zip");
                
                if (downloadUrl == null)
                {
                    throw new Exception("Unable to find the download URL for the update.");
                }

                TitleName = "Downloading Update";
                
                ProgressBarVisibility = "Visible";
                IsProgressIndeterminate = false;
                ProgressInfo = "Downloading Update...";
                
                await DownloadAndInstallUpdate(downloadUrl);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error occurred while downloading the update: {e.Message}", "Update Error");
                Application.Current.Dispatcher.Invoke(() => { Application.Current.Shutdown(); });
            }
        });
    }

    private static string? ReadCurrentVersion()
    {
        return Assembly.LoadFile("MediaClippex.dll").GetName().Version?.ToString();
    }

    private async Task DownloadAndInstallUpdate(ReleaseAsset releaseAsset)
    {
        using var httpClient = new HttpClient();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            using (var response = await httpClient.GetAsync(releaseAsset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            await using (var stream = await response.Content.ReadAsStreamAsync())
            await using (var fileStream = File.Create(tempFilePath))
            {
                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var buffer = new byte[8192];
                long bytesRead;
                var totalBytesRead = 0L;

                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, (int)bytesRead));

                    totalBytesRead += bytesRead;

                    OnDownloadProgress(totalBytesRead, totalBytes);
                }
            }

            TitleName = "Installing Update";
            for (var i = Progress; i >= Progress; i--)
            {
                Progress = i;
            }
            
            ProgressInfo = "Installing Update...";
            await Installation(tempFilePath);
        }
        catch (Exception ex)
        {
            // Log or handle the error appropriately
            MessageBox.Show($"Error occurred while installing the update: {ex.Message}", "Update Error");
            Application.Current.Dispatcher.Invoke(() => { Application.Current.Shutdown(); });
        }
    }
    
    private async Task Installation(string zipFilePath)
    {
        try
        {
            var extractionPath = AppContext.BaseDirectory;

            await Task.Run(() =>
            {
                using (var archive = ZipFile.OpenRead(zipFilePath))
                {
                    var entryCount = archive.Entries.Count;
                    var processedCount = 0;

                    foreach (var entry in archive.Entries)
                    {
                        var targetFilePath = Path.Combine(extractionPath, entry.FullName);

                        // Check if the target file is in use
                        bool fileInUse;
                        try
                        {
                            using var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                            // The file is not in use
                            fileInUse = false;
                        }
                        catch (IOException)
                        {
                            // The file is in use
                            fileInUse = true;
                        }

                        if (!fileInUse)
                        {
                            // Replace the file
                            entry.ExtractToFile(targetFilePath, true);
                        }

                        processedCount++;

                        // Calculate and report progress based on the number of processed entries
                        OnDownloadProgress(processedCount, entryCount);
                    }
                }

                var result = MessageBox.Show("Update installed successfully. Do you want to open the application now?", "Update Installed", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("MediaClippex.exe");
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });
            });
        }
        catch (Exception ex)
        {
            // Log or handle the error appropriately
            MessageBox.Show($"Error occurred while extracting the update: {ex.Message}", "Update Error");
        }
        finally
        {
            if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
        }
    }

    private void OnDownloadProgress(long bytesReceived, long totalBytes)
    {
        Progress = (double)bytesReceived / totalBytes * 100;
    }
}