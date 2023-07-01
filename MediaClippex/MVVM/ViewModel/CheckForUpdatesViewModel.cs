using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.MVVM.View;
using Octokit;
using Russkyc.DependencyInjection.Implementations;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class CheckForUpdatesViewModel : BaseViewModel
{
    [ObservableProperty] private string? _changeLog;
    [ObservableProperty] private string? _currentVersion;
    [ObservableProperty] private bool _enableCheckForUpdateButton = true;
    [ObservableProperty] private string _isProcessing = "Collapsed";
    [ObservableProperty] private bool _isProgressIndeterminate;
    [ObservableProperty] private string? _latestVersion;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _progressInfo;
    [ObservableProperty] private string _showChangeLog = "Collapsed";

    public CheckForUpdatesViewModel()
    {
        CurrentVersion = ReadCurrentVersion();
    }

    private static string Owner => "pitzzahh";
    private static string Repo => "MediaClippex";

    [RelayCommand]
    private async Task CheckForUpdate()
    {
        try
        {
            IsProcessing = "Visible";
            ProgressInfo = "Checking for updates...";
            IsProgressIndeterminate = true;
            
            if (LatestVersion != null && CurrentVersion != null && !ShouldUpdate(CurrentVersion, LatestVersion))
            {
                IsProgressIndeterminate = false;
                MessageBox.Show("You have the latest version of the application.", "No Updates Available");
                return;
            }
            
            var latestRelease = await new GitHubClient(new ProductHeaderValue(Repo))
                .Repository
                .Release
                .GetLatest(Owner, Repo);

            LatestVersion = latestRelease.TagName;

            var downloadUrl = latestRelease
                .Assets
                .Where(asset => asset.ContentType == "application/octet-stream")
                .FirstOrDefault(asset => asset.Name == "MediaClippex.exe");

            Debug.Print($"Download URL: {downloadUrl}");

            if (downloadUrl == null)
            {
                MessageBox.Show("Unable to find download URL.", "Update Error");
                return;
            }


            if (CurrentVersion == null)
            {
                MessageBox.Show("Unable to read current version.", "Update Error");
                return;
            }

            if (ShouldUpdate(CurrentVersion, LatestVersion))
            {
                ProgressInfo = "";
                ShowChangeLog = "Visible";
                IsProcessing = "Collapsed";
                IsProgressIndeterminate = false;
                var result = MessageBox.Show("An update is available. Do you want to install it?", "Update Available",
                    MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    IsProcessing = "Visible";
                    ProgressInfo = "Downloading update...";
                    EnableCheckForUpdateButton = false;
                    await DownloadAndInstallUpdate(downloadUrl);
                }
            }
            else
            {
                IsProgressIndeterminate = false;
                MessageBox.Show("You have the latest version of the application.", "No Updates Available");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error occurred while checking for updates: {ex.Message} Captured Latest Version: {LatestVersion}",
                "Update Error");
        }
        finally
        {
            ShowChangeLog = "Collapsed";
            IsProcessing = "Collapsed";
            ProgressInfo = "";
            IsProgressIndeterminate = false;
            EnableCheckForUpdateButton = true;
        }
    }

    private static bool ShouldUpdate(string currentVersion, string latestVersion)
    {
        return new Version(latestVersion) > new Version(currentVersion);
    }

    private static string? ReadCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    }

    private async Task DownloadAndInstallUpdate(ReleaseAsset releaseAsset)
    {
        using var httpClient = new HttpClient();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            using (var response = await httpClient.GetAsync(releaseAsset.BrowserDownloadUrl,
                       HttpCompletionOption.ResponseHeadersRead))
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

            // Install the update by replacing the current executable with the downloaded one
            var currentAssemblyLocation = AppContext.BaseDirectory;
            var newAssemblyLocation =
                Path.Combine(Path.GetDirectoryName(currentAssemblyLocation) ?? throw new InvalidOperationException(),
                    "MediaClippex_new.exe");

            File.Move(currentAssemblyLocation, newAssemblyLocation);
            File.Move(tempFilePath, currentAssemblyLocation);
            IsProcessing = "Collapsed";
            ShowChangeLog = "Collapsed";
            EnableCheckForUpdateButton = true;
            BuilderServices.Resolve<CheckForUpdatesView>().Close();

            MessageBox.Show("Update installed successfully. Please restart the application.", "Update Installed");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error occurred while downloading and installing the update: {ex.Message}",
                "Update Error");
        }
        finally
        {
            if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
        }
    }

    private void OnDownloadProgress(long bytesReceived, long totalBytes)
    {
        Progress = (double)bytesReceived / totalBytes * 100;
    }
}